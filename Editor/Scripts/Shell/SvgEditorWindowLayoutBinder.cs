using System;
using Core.UI.Foundation;
using Core.UI.Foundation.Editor;
using InspectorSectionClasses = Core.UI.Foundation.Tooling.InspectorSectionClasses;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Shared;
using SvgEditor.Workspace;
using SvgEditor.Workspace.AssetLibrary;
using SvgEditor.Workspace.Canvas;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.InspectorPanel;

namespace SvgEditor.Shell
{
    internal sealed class SvgEditorWindowLayoutBinder
    {
        private static class UssClassName
        {
            private const string Prefix = "svg-editor__";

            public const string SUBSECTION_TITLE = Prefix + "subsection-title";
            public const string INSPECTOR_CARD = Prefix + "inspector-card";
            public const string INSPECTOR_CARD_ACCENT = INSPECTOR_CARD + "--accent";
        }

        private const string THEME_RESOURCE_PATH = "Theme/SvgEditorTheme";
        private const string WINDOW_RESOURCE_PATH = "UXML/SvgEditorWindow";
        private static readonly InspectorSectionClasses InspectorSectionChrome = new()
        {
            rootClass = UssClassName.INSPECTOR_CARD,
            accentClass = UssClassName.INSPECTOR_CARD_ACCENT,
            headerClass = string.Empty,
            titleClass = UssClassName.SUBSECTION_TITLE,
            actionsClass = string.Empty
        };

        private readonly VisualElement _root;
        private readonly AssetLibraryBrowser _assetLibraryBrowser;
        private readonly DocumentLifecycleController _documentLifecycleController;
        private readonly PanelController _inspectorPanelController;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Func<IPanelHost> _panelHostAccessor;
        private readonly EventCallback<KeyDownEvent> _rootKeyDownHandler;

        public SvgEditorWindowLayoutBinder(
            VisualElement root,
            AssetLibraryBrowser assetLibraryBrowser,
            DocumentLifecycleController documentLifecycleController,
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

            var visualTree = FindVisualTreeAsset();
            if (visualTree == null)
            {
                _root.Add(new HelpBox("SvgEditorWindow.uxml not found.", HelpBoxMessageType.Error));
                return false;
            }

            visualTree.CloneTree(_root);
            _root.RegisterCallback<KeyDownEvent>(_rootKeyDownHandler, TrickleDown.TrickleDown);
            BindUxmlLayout();
            ApplyThemeStyleSheet();
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
            ApplyInspectorAttributeIcons();

            _assetLibraryBrowser.Bind(
                _root,
                _documentLifecycleController.LoadAsset,
                () => _documentLifecycleController.CurrentDocument?.AssetPath,
                _documentLifecycleController.CanSwitchDocument);

            _documentLifecycleController.Bind(_root);
            BuildSharedInspectorSections();

            CanvasStageView canvasStageView = _root.Q<CanvasStageView>("canvas-stage-view");
            if (canvasStageView != null)
            {
                canvasStageView.PrepareRuntime();
                canvasStageView.DocumentResetRequested += _documentLifecycleController.ReloadCurrentDocument;
                WorkspaceCoordinator?.Bind(canvasStageView, _root.Q<Toggle>("tool-move"));
            }

            _inspectorPanelController.Bind(_root, PanelHost);
        }

        private void BuildSharedInspectorSections()
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

        private static VisualTreeAsset FindVisualTreeAsset()
        {
            return Resources.Load<VisualTreeAsset>(WINDOW_RESOURCE_PATH);
        }

        private void ApplyThemeStyleSheet()
        {
            EditorThemeUtility.ApplyThemeStyleSheet(_root, THEME_RESOURCE_PATH);
        }

        private void ApplyToolbarIcons()
        {
            EditorFoundationIconUtility.ApplyToggleVectorImage(_root, "tool-move", SvgEditorIconClass.RESOURCE_MOVE);
        }

        private void ApplyPositionIcons()
        {
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-align-left", IconClass.ALIGN_HORIZONTAL_LEFT);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-align-center", IconClass.ALIGN_HORIZONTAL_CENTER);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-align-right", IconClass.ALIGN_HORIZONTAL_RIGHT);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-align-top", IconClass.ALIGN_VERTICAL_TOP);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-align-middle", IconClass.ALIGN_VERTICAL_CENTER);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-align-bottom", IconClass.ALIGN_VERTICAL_BOTTOM);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-rotate-clockwise-90", IconClass.ROTATE_90);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-flip-horizontal", IconClass.FLIP_HORIZONTAL);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "position-flip-vertical", IconClass.FLIP_VERTICAL);
        }

        private void ApplyInspectorAttributeIcons()
        {
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "fill-add-button", IconClass.PLUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "fill-remove-button", IconClass.MINUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "stroke-add-button", IconClass.PLUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(_root, "stroke-remove-button", IconClass.MINUS);
        }
    }
}
