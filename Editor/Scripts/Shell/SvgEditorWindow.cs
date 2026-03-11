using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using InspectorSection = Core.UI.Foundation.Tooling.InspectorSection;
using InspectorSectionClasses = Core.UI.Foundation.Tooling.InspectorSectionClasses;
using FoundationButton = Core.UI.Foundation.Components.Button.Button;
using FoundationToggle = Core.UI.Foundation.Components.Toggle.Toggle;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Foundation;

namespace UnitySvgEditor.Editor
{
    public sealed class SvgEditorWindow : EditorWindow, IEditorWorkspaceHost, IInspectorPanelHost
    {
        private static class UssClassName
        {
            private const string Prefix = "svg-editor__";

            public const string SUBSECTION_TITLE = Prefix + "subsection-title";
            public const string INSPECTOR_CARD = Prefix + "inspector-card";
            public const string INSPECTOR_CARD_ACCENT = INSPECTOR_CARD + "--accent";
        }

        private const string WINDOW_MENU_PATH = "Window/Unity SVG Editor/SVG Editor";
        private const string THEME_RESOURCE_PATH = "Theme/SvgEditorTheme";
        private const string WINDOW_RESOURCE_PATH = "UXML/SvgEditorWindow";

        #region Variables

        private readonly DocumentRepository _documentRepository = new();
        private readonly PreviewSnapshotBuilder _previewSnapshotBuilder = new();
        private readonly AssetLibraryBrowser _assetLibraryBrowser;
        private readonly InspectorPanelController _inspectorPanelController;
        private readonly DocumentLifecycleController _documentLifecycleController;

        private EditorWorkspaceCoordinator _workspaceCoordinator;

        VisualElement IEditorWorkspaceHost.RootVisualElement => rootVisualElement;
        DocumentSession IEditorWorkspaceHost.CurrentDocument => _documentLifecycleController.CurrentDocument;
        PreviewSnapshot IEditorWorkspaceHost.PreviewSnapshot => _documentLifecycleController.PreviewSnapshot;
        Image IEditorWorkspaceHost.PreviewImage => _documentLifecycleController.PreviewImage;

        #endregion Variables

        #region Unity Methods

        [MenuItem(WINDOW_MENU_PATH)]
        public static void Open()
        {
            SvgEditorWindow window = GetWindow<SvgEditorWindow>();
            window.titleContent = new GUIContent("SVG Editor");
            window.minSize = new Vector2(980f, 640f);
            window.Show();
        }

        public SvgEditorWindow()
        {
            _assetLibraryBrowser = new AssetLibraryBrowser(_documentRepository);
            _inspectorPanelController = new InspectorPanelController(new InspectorPanelState());
            _documentLifecycleController = new DocumentLifecycleController(
                _documentRepository,
                _previewSnapshotBuilder,
                _inspectorPanelController,
                () => WorkspaceCoordinator,
                UpdateEditorInteractivity);
        }

        private void CreateGUI()
        {
            _assetLibraryBrowser.Unbind();
            _inspectorPanelController.Unbind();
            _documentLifecycleController.Unbind();
            rootVisualElement.Clear();
            var visualTree = FindVisualTreeAsset();
            if (visualTree == null)
            {
                rootVisualElement.Add(new HelpBox("SvgEditorWindow.uxml not found.", HelpBoxMessageType.Error));
                return;
            }

            visualTree.CloneTree(rootVisualElement);
            BindUxmlLayout();

            ApplyThemeStyleSheet();
            _assetLibraryBrowser.RefreshAssetList(selectFirst: true);
        }

        private void OnDisable()
        {
            _assetLibraryBrowser.Unbind();
            _inspectorPanelController.Unbind();
            _documentLifecycleController.Dispose();
            _workspaceCoordinator?.Dispose();
        }

        #endregion Unity Methods

        #region Help Methods
        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinator ??= new EditorWorkspaceCoordinator(this);

        private void BindUxmlLayout()
        {
            ApplyToolbarIcons();
            ApplyPositionIcons();

            _assetLibraryBrowser.Bind(
                rootVisualElement,
                _documentLifecycleController.LoadAsset,
                () => _documentLifecycleController.CurrentDocument?.AssetPath,
                _documentLifecycleController.CanSwitchDocument);

            _documentLifecycleController.Bind(rootVisualElement);
            BuildSharedInspectorSections();

            CanvasStageView canvasStageView = rootVisualElement.Q<CanvasStageView>("canvas-stage-view");
            if (canvasStageView != null)
            {
                canvasStageView.PrepareRuntime();
                WorkspaceCoordinator.Bind(canvasStageView, rootVisualElement.Q<Toggle>("tool-move"));
            }
            _inspectorPanelController.Bind(rootVisualElement, this);
        }

        private void BuildSharedInspectorSections()
        {
            ReplaceInspectorSection("structure-panel", "Selection", accent: true);
            ReplaceInspectorSection("patch-panel", "Appearance", accent: false);
        }

        private void ReplaceInspectorSection(string panelName, string fallbackTitle, bool accent)
        {
            var original = rootVisualElement.Q<VisualElement>(panelName);
            if (original?.parent == null)
                return;

            var title = original.Q<Label>(className: UssClassName.SUBSECTION_TITLE);
            var section = CreateInspectorSection(original, title?.text ?? fallbackTitle);
            section.SetAccent(accent);

            title?.RemoveFromHierarchy();
            MoveChildren(original, section.Body);
            ReplaceElement(original, section);
        }

        private static InspectorSection CreateInspectorSection(VisualElement original, string title)
        {
            var section = new InspectorSection(title, new InspectorSectionClasses
            {
                rootClass = UssClassName.INSPECTOR_CARD,
                accentClass = UssClassName.INSPECTOR_CARD_ACCENT,
                headerClass = string.Empty,
                titleClass = UssClassName.SUBSECTION_TITLE,
                actionsClass = string.Empty
            });

            section.name = original.name;
            foreach (var className in original.GetClasses())
            {
                section.AddClass(className);
            }

            return section;
        }

        private static void MoveChildren(VisualElement source, VisualElement target)
        {
            while (source.childCount > 0)
            {
                var child = source[0];
                child.RemoveFromHierarchy();
                target.Add(child);
            }
        }

        private static void ReplaceElement(VisualElement original, VisualElement replacement)
        {
            var parent = original.parent;
            if (parent == null)
                return;

            var index = parent.IndexOf(original);
            original.RemoveFromHierarchy();
            parent.Insert(index, replacement);
        }

        private string ResolveSelectedPatchTargetKey()
        {
            return _inspectorPanelController.ResolveSelectedTargetKey();
        }

        bool IEditorWorkspaceHost.TrySelectPatchTargetByKey(string targetKey)
        {
            return _inspectorPanelController.TrySelectTargetByKey(targetKey, out _);
        }

        string IEditorWorkspaceHost.ResolveSelectedPatchTargetKey() => ResolveSelectedPatchTargetKey();

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        string IEditorWorkspaceHost.FormatNumber(float value) => FormatNumber(value);

        void IEditorWorkspaceHost.RefreshLivePreview(bool keepExistingPreviewOnFailure) =>
            _documentLifecycleController.RefreshLivePreview(keepExistingPreviewOnFailure);

        bool IEditorWorkspaceHost.TryRefreshTransientPreview(string sourceText) =>
            _documentLifecycleController.TryRefreshTransientPreview(sourceText);

        void IEditorWorkspaceHost.RefreshInspectorFromSource(string sourceText) =>
            _inspectorPanelController.QueueRefreshTargets(sourceText);

        private bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus) =>
            WorkspaceCoordinator.TryApplyPatchRequest(request, successStatus);

        void IEditorWorkspaceHost.ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus);
        }

        private void UpdateEditorInteractivity()
        {
            var currentDocument = _documentLifecycleController.CurrentDocument;
            var hasDocument = currentDocument != null;
            var hasInspectableDocument = currentDocument?.DocumentModel != null &&
                                         string.IsNullOrWhiteSpace(currentDocument.DocumentModelLoadError) &&
                                         string.Equals(currentDocument.DocumentModel.SourceText, currentDocument.WorkingSourceText, StringComparison.Ordinal);

            _documentLifecycleController.UpdateInteractivity();
            _inspectorPanelController.UpdateInteractivity(hasInspectableDocument);
            WorkspaceCoordinator.UpdateStructureInteractivity(hasDocument);
        }

        void IEditorWorkspaceHost.UpdateEditorInteractivity() => UpdateEditorInteractivity();

        private void UpdateSourceStatus(string status)
        {
            _documentLifecycleController.UpdateSourceStatus(status);
        }

        void IEditorWorkspaceHost.UpdateSourceStatus(string status) => UpdateSourceStatus(status);

        DocumentSession IInspectorPanelHost.CurrentDocument => _documentLifecycleController.CurrentDocument;
        bool IInspectorPanelHost.TryApplyPatchRequest(AttributePatchRequest request, string successStatus) => TryApplyPatchRequest(request, successStatus);
        bool IInspectorPanelHost.TryApplyTargetFrameRect(string targetKey, Rect targetSceneRect, string successStatus) =>
            WorkspaceCoordinator.TryApplyTargetFrameRect(targetKey, targetSceneRect, successStatus);
        bool IInspectorPanelHost.TryGetTargetSceneRect(string targetKey, out Rect sceneRect)
        {
            sceneRect = default;
            var snapshot = _documentLifecycleController.PreviewSnapshot;
            if (snapshot?.Elements == null || string.IsNullOrWhiteSpace(targetKey))
                return false;

            for (var i = 0; i < snapshot.Elements.Count; i++)
            {
                var element = snapshot.Elements[i];
                if (element == null)
                    continue;

                if (!string.Equals(element.TargetKey, targetKey, StringComparison.Ordinal))
                    continue;

                sceneRect = element.VisualBounds;
                return true;
            }

            return false;
        }
        bool IInspectorPanelHost.TryGetTargetParentWorldTransform(string targetKey, out Matrix2D parentWorldTransform)
        {
            parentWorldTransform = Matrix2D.identity;
            var snapshot = _documentLifecycleController.PreviewSnapshot;
            if (snapshot?.Elements == null || string.IsNullOrWhiteSpace(targetKey))
                return false;

            for (var i = 0; i < snapshot.Elements.Count; i++)
            {
                var element = snapshot.Elements[i];
                if (element == null)
                    continue;

                if (!string.Equals(element.TargetKey, targetKey, StringComparison.Ordinal))
                    continue;

                parentWorldTransform = element.ParentWorldTransform;
                return true;
            }

            return false;
        }
        bool IInspectorPanelHost.TryGetCanvasViewportSceneRect(out Rect sceneRect)
        {
            sceneRect = default;
            var snapshot = _documentLifecycleController.PreviewSnapshot;
            if (snapshot == null)
                return false;

            sceneRect = snapshot.CanvasViewportRect;
            return sceneRect.width > 0f || sceneRect.height > 0f;
        }
        void IInspectorPanelHost.SyncSelectionFromInspectorTarget(string targetKey) => WorkspaceCoordinator.SyncSelectionFromInspectorTarget(targetKey);
        void IInspectorPanelHost.UpdateSourceStatus(string status) => UpdateSourceStatus(status);

        private static VisualTreeAsset FindVisualTreeAsset()
        {
            return Resources.Load<VisualTreeAsset>(WINDOW_RESOURCE_PATH);
        }

        private void ApplyThemeStyleSheet()
        {
            var theme = Resources.Load<ThemeStyleSheet>(THEME_RESOURCE_PATH);
            if (theme == null)
            {
                return;
            }

            if (theme is UnityEngine.Object unityObject &&
                unityObject is StyleSheet styleSheet &&
                !rootVisualElement.styleSheets.Contains(styleSheet))
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            PropertyInfo property = typeof(VisualElement).GetProperty(
                "themeStyleSheet",
                BindingFlags.Instance | BindingFlags.Public);

            if (property == null || !property.CanWrite)
            {
                return;
            }

            property.SetValue(rootVisualElement, theme);
        }

        private void ApplyToolbarIcons()
        {
            ApplyToggleIcon("tool-move", "Icons/move");
        }

        private void ApplyPositionIcons()
        {
            ApplyButtonIcon("position-align-left", SvgEditorIconClass.POSITION_ALIGN_LEFT);
            ApplyButtonIcon("position-align-center", SvgEditorIconClass.POSITION_ALIGN_CENTER);
            ApplyButtonIcon("position-align-right", SvgEditorIconClass.POSITION_ALIGN_RIGHT);
            ApplyButtonIcon("position-align-top", SvgEditorIconClass.POSITION_ALIGN_TOP);
            ApplyButtonIcon("position-align-middle", SvgEditorIconClass.POSITION_ALIGN_MIDDLE);
            ApplyButtonIcon("position-align-bottom", SvgEditorIconClass.POSITION_ALIGN_BOTTOM);
            ApplyButtonIcon("position-rotate-reset", SvgEditorIconClass.POSITION_ROTATE_RESET);
            ApplyButtonIcon("position-flip-horizontal", SvgEditorIconClass.POSITION_FLIP_HORIZONTAL);
            ApplyButtonIcon("position-flip-vertical", SvgEditorIconClass.POSITION_FLIP_VERTICAL);
        }

        private void ApplyToggleIcon(string toggleName, string resourcePath)
        {
            var toggle = rootVisualElement.Q<FoundationToggle>(toggleName);
            if (toggle == null)
            {
                return;
            }

            var icon = Resources.Load<VectorImage>(resourcePath);
            toggle.CheckIcon = icon == null
                ? default
                : new Background { vectorImage = icon };
        }

        private void ApplyButtonIcon(string buttonName, string iconClass)
        {
            var button = rootVisualElement.Q<FoundationButton>(buttonName);
            if (button == null || string.IsNullOrWhiteSpace(iconClass))
            {
                return;
            }

            var icon = button.Q(className: Core.UI.Foundation.Components.Button.Button.ClassName.ICON);
            if (icon == null)
            {
                return;
            }

            icon.AddToClassList(iconClass);
            icon.Show();
        }

        #endregion Help Methods
    }
}
