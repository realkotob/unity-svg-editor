using System;
using InspectorSectionClasses = SvgEditor.Core.Shared.InspectorSectionClasses;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.UI.AssetLibrary.Browser;
using SvgEditor.UI.Canvas;
using SvgEditor.UI.Workspace.Coordination;
using SvgEditor.UI.Workspace.Document;
using SvgEditor.UI.Inspector;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.UI.Shell
{
    internal static class WindowLayoutResourceLoader
    {
        private const string THEME_RESOURCE_PATH = "SvgEditorTheme";
        private const string WINDOW_RESOURCE_PATH = "SvgEditorWindow";

        public static Result<Unit> BuildRootVisualTree(VisualElement root)
        {
            if (root == null)
            {
                return Result.Failure<Unit>("Root visual element is unavailable.");
            }

            Result<VisualTreeAsset> visualTreeResult = LoadWindowVisualTree();
            if (visualTreeResult.IsFailure)
            {
                return Result.Failure<Unit>(visualTreeResult.Error);
            }

            visualTreeResult.Value.CloneTree(root);
            ApplyTheme(root);
            return Result.Success(Unit.Default);
        }

        private static Result<VisualTreeAsset> LoadWindowVisualTree()
        {
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(WINDOW_RESOURCE_PATH);
            return visualTree != null
                ? Result.Success(visualTree)
                : Result.Failure<VisualTreeAsset>("SvgEditorWindow.uxml not found.");
        }

        private static void ApplyTheme(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            ThemeStyleSheet theme = Resources.Load<ThemeStyleSheet>(THEME_RESOURCE_PATH);
            if (theme == null)
            {
                Debug.LogError($"Theme stylesheet '{THEME_RESOURCE_PATH}' could not be loaded from Resources.");
                return;
            }

            if (!root.styleSheets.Contains(theme))
            {
                root.styleSheets.Add(theme);
            }
        }
    }

    internal sealed class WindowLayoutBinder
    {
        private static class ElementName
        {
            public const string ASSET_LIBRARY_REFRESH_BUTTON = "asset-library-refresh-button";
            public const string CANVAS_STAGE_VIEW = "canvas-stage-view";
            public const string MOVE_TOOL = "tool-move";
        }

        private static class UssClassName
        {
            private const string Prefix = "svg-editor__";

            public const string SUBSECTION_TITLE = Prefix + "subsection-title";
            public const string INSPECTOR_CARD = Prefix + "inspector-card";
            public const string INSPECTOR_CARD_ACCENT = INSPECTOR_CARD + "--accent";
        }

        private static readonly InspectorSectionClasses InspectorSectionChrome = new()
        {
            rootClass = UssClassName.INSPECTOR_CARD,
            accentClass = UssClassName.INSPECTOR_CARD_ACCENT,
            headerClass = string.Empty,
            titleClass = UssClassName.SUBSECTION_TITLE,
            actionsClass = string.Empty
        };

        private readonly VisualElement _root;
        private readonly AssetBrowser _assetLibraryBrowser;
        private readonly LifecycleController _documentLifecycleController;
        private readonly PanelController _inspectorPanelController;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Func<IPanelHost> _panelHostAccessor;
        private readonly EventCallback<KeyDownEvent> _rootKeyDownHandler;

        public WindowLayoutBinder(
            VisualElement root,
            AssetBrowser assetLibraryBrowser,
            LifecycleController documentLifecycleController,
            PanelController inspectorPanelController,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Func<IPanelHost> panelHostAccessor,
            EventCallback<KeyDownEvent> rootKeyDownHandler)
        {
            _root = root;
            _assetLibraryBrowser = assetLibraryBrowser;
            _documentLifecycleController = documentLifecycleController;
            _inspectorPanelController = inspectorPanelController;
            _workspaceCoordinatorAccessor = workspaceCoordinatorAccessor;
            _panelHostAccessor = panelHostAccessor;
            _rootKeyDownHandler = rootKeyDownHandler;
        }

        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinatorAccessor?.Invoke();
        private IPanelHost PanelHost => _panelHostAccessor?.Invoke();

        public Result<Unit> RebuildLayout()
        {
            DetachLayout();
            _root.Clear();

            Result<Unit> result = WindowLayoutResourceLoader.BuildRootVisualTree(_root);
            if (result.IsFailure)
            {
                return result;
            }

            RegisterWindowCallbacks();
            BindWindowLayout();
            RefreshAssetLibrary();
            return Result.Success(Unit.Default);
        }

        public void DetachLayout()
        {
            _assetLibraryBrowser.Unbind();
            _inspectorPanelController.Unbind();
            _documentLifecycleController.Unbind();
            _root.UnregisterCallback<KeyDownEvent>(_rootKeyDownHandler, TrickleDown.TrickleDown);
        }

        private void RegisterWindowCallbacks()
        {
            _root.RegisterCallback<KeyDownEvent>(_rootKeyDownHandler, TrickleDown.TrickleDown);
        }

        private void RefreshAssetLibrary()
        {
            _assetLibraryBrowser.RefreshAssetList(selectFirst: false);
        }

        private void BindWindowLayout()
        {
            ApplyToolbarIcons();
            BindDocumentPanels();
            BindCanvasStage();
            BindInspectorPanel();
        }

        private void BindDocumentPanels()
        {
            BindAssetLibrary();
            BindDocumentLifecycle();
            UpgradeInspectorSections();
        }

        private void BindCanvasStage()
        {
            CanvasStageView canvasStageView = _root.Q<CanvasStageView>(ElementName.CANVAS_STAGE_VIEW);
            if (canvasStageView != null)
            {
                canvasStageView.PrepareRuntime();
                canvasStageView.DocumentResetRequested += _documentLifecycleController.ReloadCurrentDocument;
                WorkspaceCoordinator?.Bind(canvasStageView, _root.Q<Toggle>(ElementName.MOVE_TOOL));
            }
        }

        private void BindInspectorPanel()
        {
            _inspectorPanelController.Bind(_root, PanelHost);
        }

        private void BindAssetLibrary()
        {
            _assetLibraryBrowser.Bind(
                _root,
                _documentLifecycleController.LoadAsset,
                () => _documentLifecycleController.CurrentDocument?.AssetPath,
                _documentLifecycleController.CanSwitchDocument);
        }

        private void BindDocumentLifecycle()
        {
            _documentLifecycleController.Bind(_root);
        }

        private void UpgradeInspectorSections()
        {
            ReplaceInspectorSection("structure-panel", "Selection", accent: true);
            ReplaceInspectorSection("patch-panel", "Appearance", accent: false);
        }

        private void ReplaceInspectorSection(string panelName, string fallbackTitle, bool accent)
        {
            EditorInspectorSectionUtility.TryUpgradeToInspectorSection(
                _root.Q<VisualElement>(panelName),
                UssClassName.SUBSECTION_TITLE,
                fallbackTitle,
                InspectorSectionChrome,
                accent);
        }

        private void ApplyToolbarIcons()
        {
            EditorFoundationIconUtility.ApplyToggleVectorImage(_root, ElementName.MOVE_TOOL, IconPath.Lucide.Move);
        }
    }
}
