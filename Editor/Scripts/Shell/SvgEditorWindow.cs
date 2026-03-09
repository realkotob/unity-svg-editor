using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using InspectorSection = Core.UI.Foundation.Tooling.InspectorSection;
using InspectorSectionClasses = Core.UI.Foundation.Tooling.InspectorSectionClasses;
using FoundationToggle = Core.UI.Foundation.Components.Toggle.Toggle;
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

            public const string FLOATING_AUX_BUTTON_ACTIVE = Prefix + "floating-aux-btn--active";
            public const string SUBSECTION_TITLE = Prefix + "subsection-title";
            public const string SOURCE_HEADER = Prefix + "source-header";
            public const string SECTION_TITLE = Prefix + "section-title";
            public const string BUTTON_ROW = Prefix + "button-row";
            public const string INSPECTOR_CARD = Prefix + "inspector-card";
            public const string INSPECTOR_CARD_ACCENT = INSPECTOR_CARD + "--accent";
        }

        private const string WINDOW_MENU_PATH = "Window/Unity SVG Editor/SVG Editor";
        private const string THEME_RESOURCE_PATH = "UI/Theme/SvgEditorTheme";
        private const string WINDOW_RESOURCE_PATH = "UI/UXML/SvgEditorWindow";
        private static readonly string[] _windowStyleSheetResourcePaths =
        {
            "UI/USS/Variables/Dimensions",
            "UI/USS/Variables/Palette",
            "UI/USS/Variables/Semantic",
            "UI/USS/Variables/Typography",
            "UI/USS/Layout",
            "UI/USS/Toolbar",
            "UI/USS/Canvas",
            "UI/USS/Inspector",
            "UI/USS/ProjectTab.Common"
        };

        #region Variables

        private readonly AttributePatcher _attributePatcher = new();
        private readonly DocumentRepository _documentRepository = new();
        private readonly PreviewSnapshotBuilder _previewSnapshotBuilder = new();
        private readonly AssetLibraryBrowser _assetLibraryBrowser;
        private readonly InspectorPanelController _inspectorPanelController;
        private readonly DocumentLifecycleController _documentLifecycleController;

        private VisualElement _designInspectorPanel;
        private VisualElement _codeInspectorPanel;
        private readonly Dictionary<ToolbarMode, Toggle> _toolbarModeToggles = new();
        private ToolbarMode _activeToolbarMode = ToolbarMode.Vector;
        private EditorWorkspaceCoordinator _workspaceCoordinator;

        VisualElement IEditorWorkspaceHost.RootVisualElement => rootVisualElement;
        DocumentSession IEditorWorkspaceHost.CurrentDocument => _documentLifecycleController.CurrentDocument;
        PreviewSnapshot IEditorWorkspaceHost.PreviewSnapshot => _documentLifecycleController.PreviewSnapshot;
        Image IEditorWorkspaceHost.PreviewImage => _documentLifecycleController.PreviewImage;
        AttributePatcher IEditorWorkspaceHost.AttributePatcher => _attributePatcher;

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
            _inspectorPanelController = new InspectorPanelController(_attributePatcher, new InspectorPanelState());
            _documentLifecycleController = new DocumentLifecycleController(
                _documentRepository,
                _previewSnapshotBuilder,
                _inspectorPanelController,
                () => WorkspaceCoordinator,
                UpdateEditorInteractivity);
        }

        private void CreateGUI()
        {
            UnbindWindowControls();
            _assetLibraryBrowser.Unbind();
            _inspectorPanelController.Unbind();
            _documentLifecycleController.Unbind();
            rootVisualElement.Clear();
            AddWindowStyleSheets();
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
            UnbindWindowControls();
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
            _toolbarModeToggles.Clear();

            BindToolbarModeToggle("toolbar-mode-vector", ToolbarMode.Vector);
            BindToolbarModeToggle("toolbar-mode-code", ToolbarMode.Code);
            ApplyToolbarIcons();

            _assetLibraryBrowser.Bind(
                rootVisualElement,
                _documentLifecycleController.LoadAsset,
                () => _documentLifecycleController.CurrentDocument?.AssetPath,
                _documentLifecycleController.CanSwitchDocument);

            _documentLifecycleController.Bind(rootVisualElement);
            BuildSharedInspectorSections();

            _designInspectorPanel = rootVisualElement.Q<VisualElement>("design-inspector");
            _codeInspectorPanel = rootVisualElement.Q<VisualElement>("code-inspector");
            ApplyInspectorMode();

            CanvasStageView canvasStageView = rootVisualElement.Q<CanvasStageView>("canvas-stage-view");
            if (canvasStageView != null)
            {
                canvasStageView.PrepareRuntime();
                WorkspaceCoordinator.Bind(canvasStageView, rootVisualElement.Q<Toggle>("tool-move"));
            }
            _inspectorPanelController.Bind(rootVisualElement, this);
        }

        private void UnbindWindowControls()
        {
            foreach (var toggle in _toolbarModeToggles.Values)
            {
                toggle?.UnregisterValueChangedCallback(OnToolbarModeToggleChanged);
            }

            _toolbarModeToggles.Clear();
        }

        private void BindToolbarModeToggle(string name, ToolbarMode mode)
        {
            var toggle = rootVisualElement.Q<Toggle>(name);
            if (toggle == null)
            {
                return;
            }

            toggle.text = string.Empty;
            toggle.userData = mode;
            toggle.UnregisterValueChangedCallback(OnToolbarModeToggleChanged);
            toggle.RegisterValueChangedCallback(OnToolbarModeToggleChanged);

            _toolbarModeToggles[mode] = toggle;
            UpdateToolbarModeVisualState();
        }

        private void OnToolbarModeToggleChanged(ChangeEvent<bool> evt)
        {
            if (evt.target is not Toggle toggle || toggle.userData is not ToolbarMode mode)
            {
                return;
            }

            if (evt.newValue)
            {
                SetToolbarMode(mode);
                return;
            }

            if (_activeToolbarMode == mode)
            {
                toggle.SetValueWithoutNotify(true);
            }
        }

        private void SetToolbarMode(ToolbarMode mode)
        {
            if (_activeToolbarMode == mode)
            {
                return;
            }

            _activeToolbarMode = mode;
            UpdateToolbarModeVisualState();
        }

        private void UpdateToolbarModeVisualState()
        {
            foreach (var pair in _toolbarModeToggles)
            {
                var isActive = pair.Key == _activeToolbarMode;
                pair.Value.SetValueWithoutNotify(isActive);
                pair.Value.EnableClass(UssClassName.FLOATING_AUX_BUTTON_ACTIVE, isActive);
            }

            ApplyInspectorMode();
        }

        private void BuildSharedInspectorSections()
        {
            ReplaceInspectorSection("structure-panel", "Selection", accent: true);
            ReplaceInspectorSection("patch-panel", "Appearance", accent: false);
            ReplaceSourceInspectorSection();
        }

        private void ReplaceInspectorSection(string panelName, string fallbackTitle, bool accent)
        {
            var original = rootVisualElement.Q<VisualElement>(panelName);
            if (original?.parent == null)
                return;

            var title = original.Q<Label>(className: UssClassName.SUBSECTION_TITLE);
            var section = CreateInspectorSection(original, title?.text ?? fallbackTitle, sourceMode: false);
            section.SetAccent(accent);

            title?.RemoveFromHierarchy();
            MoveChildren(original, section.Body);
            ReplaceElement(original, section);
        }

        private void ReplaceSourceInspectorSection()
        {
            var original = rootVisualElement.Q<VisualElement>("source-panel");
            if (original?.parent == null)
                return;

            var sourceHeader = original.Q<VisualElement>(className: UssClassName.SOURCE_HEADER);
            var title = sourceHeader?.Q<Label>(className: UssClassName.SECTION_TITLE);
            var actions = sourceHeader?.Q<VisualElement>(className: UssClassName.BUTTON_ROW);

            title?.RemoveFromHierarchy();
            actions?.RemoveFromHierarchy();
            sourceHeader?.RemoveFromHierarchy();

            var section = CreateInspectorSection(original, title?.text ?? "Source Editor", sourceMode: true);
            section.SetActions(actions == null ? null : new[] { actions });

            MoveChildren(original, section.Body);
            ReplaceElement(original, section);
        }

        private static InspectorSection CreateInspectorSection(VisualElement original, string title, bool sourceMode)
        {
            var section = new InspectorSection(title, new InspectorSectionClasses
            {
                rootClass = UssClassName.INSPECTOR_CARD,
                accentClass = UssClassName.INSPECTOR_CARD_ACCENT,
                headerClass = sourceMode ? UssClassName.SOURCE_HEADER : string.Empty,
                titleClass = sourceMode ? UssClassName.SECTION_TITLE : UssClassName.SUBSECTION_TITLE,
                actionsClass = sourceMode ? UssClassName.BUTTON_ROW : string.Empty
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

        private enum ToolbarMode
        {
            Vector,
            Code
        }

        private void ApplyInspectorMode()
        {
            if (_designInspectorPanel != null)
            {
                _designInspectorPanel.style.display = _activeToolbarMode == ToolbarMode.Vector
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_codeInspectorPanel != null)
            {
                _codeInspectorPanel.style.display = _activeToolbarMode == ToolbarMode.Code
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
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

        private void RefreshPatchTargets()
        {
            _inspectorPanelController.RefreshTargets(_documentLifecycleController.CurrentDocument?.WorkingSourceText);
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        string IEditorWorkspaceHost.FormatNumber(float value) => FormatNumber(value);

        void IEditorWorkspaceHost.RefreshLivePreview(bool keepExistingPreviewOnFailure) =>
            _documentLifecycleController.RefreshLivePreview(keepExistingPreviewOnFailure);

        bool IEditorWorkspaceHost.TryRefreshTransientPreview(string sourceText) =>
            _documentLifecycleController.TryRefreshTransientPreview(sourceText);

        private bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus) =>
            WorkspaceCoordinator.TryApplyPatchRequest(request, successStatus);

        void IEditorWorkspaceHost.ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus);
        }

        private void UpdateEditorInteractivity()
        {
            var hasDocument = _documentLifecycleController.CurrentDocument != null;

            _documentLifecycleController.UpdateInteractivity();
            _inspectorPanelController.UpdateInteractivity(hasDocument);
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
        void IInspectorPanelHost.UpdateSourceStatus(string status) => UpdateSourceStatus(status);

        private void AddWindowStyleSheets()
        {
            foreach (var resourcePath in _windowStyleSheetResourcePaths)
            {
                var styleSheet = Resources.Load<StyleSheet>(resourcePath);
                if (styleSheet != null && !rootVisualElement.styleSheets.Contains(styleSheet))
                {
                    rootVisualElement.styleSheets.Add(styleSheet);
                }
            }
        }

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
            ApplyToggleIcon("toolbar-mode-vector", "Icons/pen");
            ApplyToggleIcon("toolbar-mode-code", "Icons/terminal");
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

        #endregion Help Methods
    }
}
