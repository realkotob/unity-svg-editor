using System;
using Core.UI.Foundation;
using Core.UI.Foundation.Editor;
using InspectorSectionClasses = Core.UI.Foundation.Tooling.InspectorSectionClasses;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Workspace.AssetLibrary.Browser;
using SvgEditor.Workspace.Canvas;
using SvgEditor.Workspace.Coordination;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.InspectorPanel;

namespace SvgEditor.Shell
{
    internal static class WindowLayoutResources
    {
        private const string THEME_RESOURCE_PATH = "Theme/SvgEditorTheme";
        private const string WINDOW_RESOURCE_PATH = "UXML/SvgEditorWindow";

        public static bool TryBuildRootVisualTree(VisualElement root, out string error)
        {
            error = string.Empty;
            if (root == null)
            {
                error = "Root visual element is unavailable.";
                return false;
            }

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(WINDOW_RESOURCE_PATH);
            if (visualTree == null)
            {
                error = "SvgEditorWindow.uxml not found.";
                return false;
            }

            visualTree.CloneTree(root);
            EditorThemeUtility.ApplyThemeStyleSheet(root, THEME_RESOURCE_PATH);
            return true;
        }
    }

    internal sealed class WindowLayoutBinder
    {
        private static class ElementName
        {
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

        public bool RebuildLayout()
        {
            DetachLayout();
            _root.Clear();

            if (!WindowLayoutResources.TryBuildRootVisualTree(_root, out string error))
            {
                _root.Add(new HelpBox(error, HelpBoxMessageType.Error));
                return false;
            }

            _root.RegisterCallback<KeyDownEvent>(_rootKeyDownHandler, TrickleDown.TrickleDown);
            BindUxmlLayout();
            _assetLibraryBrowser.RefreshAssetList(selectFirst: false);
            return true;
        }

        public void DetachLayout()
        {
            _assetLibraryBrowser.Unbind();
            _inspectorPanelController.Unbind();
            _documentLifecycleController.Unbind();
            _root.UnregisterCallback<KeyDownEvent>(_rootKeyDownHandler, TrickleDown.TrickleDown);
        }

        private void BindUxmlLayout()
        {
            ApplyToolbarIcons();
            ApplyPositionIcons();
            ApplyAttributeIcons();
            BindPanels();
            BindCanvasStage();
            _inspectorPanelController.Bind(_root, PanelHost);
        }

        private void BindPanels()
        {
            _assetLibraryBrowser.Bind(
                _root,
                _documentLifecycleController.LoadAsset,
                () => _documentLifecycleController.CurrentDocument?.AssetPath,
                _documentLifecycleController.CanSwitchDocument);

            _documentLifecycleController.Bind(_root);
            BuildInspectorSections();
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

        private void BuildInspectorSections()
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

        private void ApplyPositionIcons()
        {
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_ALIGN_LEFT, IconClass.ALIGN_HORIZONTAL_LEFT);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_ALIGN_CENTER, IconClass.ALIGN_HORIZONTAL_CENTER);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_ALIGN_RIGHT, IconClass.ALIGN_HORIZONTAL_RIGHT);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_ALIGN_TOP, IconClass.ALIGN_VERTICAL_TOP);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_ALIGN_MIDDLE, IconClass.ALIGN_VERTICAL_CENTER);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_ALIGN_BOTTOM, IconClass.ALIGN_VERTICAL_BOTTOM);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_ROTATE_CLOCKWISE_90, IconClass.ROTATE_90);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_FLIP_HORIZONTAL, IconClass.FLIP_HORIZONTAL);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.POSITION_FLIP_VERTICAL, IconClass.FLIP_VERTICAL);
        }

        private void ApplyAttributeIcons()
        {
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.FILL_ADD_BUTTON, IconClass.PLUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.FILL_REMOVE_BUTTON, IconClass.MINUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.STROKE_ADD_BUTTON, IconClass.PLUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, FormControls.ElementName.STROKE_REMOVE_BUTTON, IconClass.MINUS);
        }
    }
}
