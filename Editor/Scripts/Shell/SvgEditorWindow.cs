using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Core.UI.Foundation.Editor;
using InspectorSectionClasses = Core.UI.Foundation.Tooling.InspectorSectionClasses;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Foundation;
using SvgEditor.Preview;
using SvgEditor.Workspace;
using SvgEditor.Workspace.AssetLibrary;
using SvgEditor.Workspace.Canvas;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.InspectorPanel;
using SvgEditor.DocumentModel;
using SvgEditor.Shared;
using SvgEditor.Document;

namespace SvgEditor.Shell
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

        private const string WINDOW_MENU_PATH = "Window/Tools/SVG Editor";
        private const string THEME_RESOURCE_PATH = "Theme/SvgEditorTheme";
        private const string WINDOW_RESOURCE_PATH = "UXML/SvgEditorWindow";
        private const double ShortcutDedupSeconds = 0.05d;
        private static readonly InspectorSectionClasses InspectorSectionChrome = new()
        {
            rootClass = UssClassName.INSPECTOR_CARD,
            accentClass = UssClassName.INSPECTOR_CARD_ACCENT,
            headerClass = string.Empty,
            titleClass = UssClassName.SUBSECTION_TITLE,
            actionsClass = string.Empty
        };

        #region Variables

        private readonly DocumentRepository _documentRepository = new();
        private readonly PreviewSnapshotBuilder _previewSnapshotBuilder = new();
        private readonly AssetDatabaseVectorImageSourceProvider _vectorImageSourceProvider = new();
        private readonly AssetLibraryBrowser _assetLibraryBrowser;
        private readonly InspectorPanelController _inspectorPanelController;
        private readonly DocumentLifecycleController _documentLifecycleController;

        private EditorWorkspaceCoordinator _workspaceCoordinator;
        private double _lastShortcutSelectionHandledAt;
        private KeyCode _lastShortcutKey = KeyCode.None;
        private EventModifiers _lastShortcutModifiers;

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
            _assetLibraryBrowser = new AssetLibraryBrowser(_documentRepository, _vectorImageSourceProvider);
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
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);
            rootVisualElement.Clear();
            var visualTree = FindVisualTreeAsset();
            if (visualTree == null)
            {
                rootVisualElement.Add(new HelpBox("SvgEditorWindow.uxml not found.", HelpBoxMessageType.Error));
                return;
            }

            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);
            BindUxmlLayout();

            ApplyThemeStyleSheet();
            _assetLibraryBrowser.RefreshAssetList(selectFirst: false);
        }

        private void OnDisable()
        {
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);
            _assetLibraryBrowser.Unbind();
            _inspectorPanelController.Unbind();
            _documentLifecycleController.Dispose();
            _workspaceCoordinator?.Dispose();
        }

        private void OnGUI()
        {
            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.KeyDown)
                return;

            if (TrySelectionHandleCancelActiveDrag(currentEvent.keyCode))
            {
                currentEvent.Use();
                return;
            }

            if (!TrySelectionHandleShortcut(currentEvent.keyCode, currentEvent.modifiers))
                return;

            currentEvent.Use();
        }

        #endregion Unity Methods

        #region Help Methods
        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinator ??= new EditorWorkspaceCoordinator(this);

        private void BindUxmlLayout()
        {
            ApplyToolbarIcons();
            ApplyPositionIcons();
            ApplyInspectorAttributeIcons();

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
                canvasStageView.DocumentResetRequested += _documentLifecycleController.ReloadCurrentDocument;
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
            EditorInspectorSectionUtility.TryUpgradeToInspectorSection(
                rootVisualElement.Q<VisualElement>(panelName),
                UssClassName.SUBSECTION_TITLE,
                fallbackTitle,
                InspectorSectionChrome,
                accent);
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

        bool IEditorWorkspaceHost.TryRefreshTransientPreview(SvgDocumentModel documentModel) =>
            _documentLifecycleController.TryRefreshTransientPreview(documentModel);

        void IEditorWorkspaceHost.RefreshInspector() =>
            _inspectorPanelController.QueueRefreshTargets();

        void IEditorWorkspaceHost.RefreshInspector(SvgDocumentModel documentModel) =>
            _inspectorPanelController.RefreshTargets(documentModel);

        private bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate) =>
            WorkspaceCoordinator.TryApplyPatchRequest(request, successStatus, recordingMode);

        void IEditorWorkspaceHost.ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus);
        }

        void IEditorWorkspaceHost.ApplyUpdatedSource(
            string updatedSource,
            string successStatus,
            HistoryRecordingMode recordingMode)
        {
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus, recordingMode);
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
        bool IInspectorPanelHost.TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode) => TryApplyPatchRequest(request, successStatus, recordingMode);
        bool IInspectorPanelHost.TryApplyTargetFrameRect(
            string targetKey,
            Rect targetSceneRect,
            string successStatus,
            HistoryRecordingMode recordingMode) =>
            WorkspaceCoordinator.TryApplyTargetFrameRect(targetKey, targetSceneRect, successStatus, recordingMode);
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
        bool IInspectorPanelHost.TryGetTargetRotationPivotParentSpace(string targetKey, out Vector2 parentPivot)
        {
            parentPivot = default;
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

                parentPivot = element.RotationPivotParentSpace;
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
            EditorThemeUtility.ApplyThemeStyleSheet(rootVisualElement, THEME_RESOURCE_PATH);
        }

        private void ApplyToolbarIcons()
        {
            EditorFoundationIconUtility.ApplyToggleVectorImage(rootVisualElement, "tool-move", SvgEditorIconClass.RESOURCE_MOVE);
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (TrySelectionHandleCancelActiveDrag(evt.keyCode))
            {
                evt.StopPropagation();
                return;
            }

            if (!TrySelectionHandleShortcut(evt.keyCode, evt.modifiers))
                return;

            evt.StopPropagation();
        }

        private bool TrySelectionHandleCancelActiveDrag(KeyCode keyCode)
        {
            return keyCode == KeyCode.Escape &&
                   WorkspaceCoordinator.TryCancelActiveDrag();
        }

        private bool TrySelectionHandleShortcut(KeyCode keyCode, EventModifiers modifiers)
        {
            if (_documentLifecycleController.CurrentDocument == null)
                return false;

            EventModifiers normalizedModifiers = NormalizeShortcutModifiers(modifiers);
            if ((normalizedModifiers & (EventModifiers.Command | EventModifiers.Control)) == 0)
                return false;

            if (IsDuplicateShortcut(keyCode, normalizedModifiers))
                return true;

            bool handled = false;
            if (keyCode == KeyCode.Z)
            {
                handled = (normalizedModifiers & EventModifiers.Shift) != 0
                    ? _documentLifecycleController.TryRedo()
                    : _documentLifecycleController.TryUndo();
            }
            else if (keyCode == KeyCode.S)
            {
                _documentLifecycleController.SaveCurrentDocument();
                handled = true;
            }

            if (handled)
                RememberSelectionHandledShortcut(keyCode, normalizedModifiers);

            return handled;
        }

        private bool IsDuplicateShortcut(KeyCode keyCode, EventModifiers modifiers)
        {
            return keyCode == _lastShortcutKey &&
                   modifiers == _lastShortcutModifiers &&
                   EditorApplication.timeSinceStartup - _lastShortcutSelectionHandledAt <= ShortcutDedupSeconds;
        }

        private void RememberSelectionHandledShortcut(KeyCode keyCode, EventModifiers modifiers)
        {
            _lastShortcutSelectionHandledAt = EditorApplication.timeSinceStartup;
            _lastShortcutKey = keyCode;
            _lastShortcutModifiers = modifiers;
        }

        private static EventModifiers NormalizeShortcutModifiers(EventModifiers modifiers)
        {
            return modifiers & (EventModifiers.Command | EventModifiers.Control | EventModifiers.Shift);
        }

        private void ApplyPositionIcons()
        {
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-align-left", IconClass.ALIGN_HORIZONTAL_LEFT);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-align-center", IconClass.ALIGN_HORIZONTAL_CENTER);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-align-right", IconClass.ALIGN_HORIZONTAL_RIGHT);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-align-top", IconClass.ALIGN_VERTICAL_TOP);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-align-middle", IconClass.ALIGN_VERTICAL_CENTER);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-align-bottom", IconClass.ALIGN_VERTICAL_BOTTOM);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-rotate-clockwise-90", IconClass.ROTATE_90);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-flip-horizontal", IconClass.FLIP_HORIZONTAL);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "position-flip-vertical", IconClass.FLIP_VERTICAL);
        }

        private void ApplyInspectorAttributeIcons()
        {
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "fill-add-button", IconClass.PLUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "fill-remove-button", IconClass.MINUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "stroke-add-button", IconClass.PLUS);
            EditorFoundationIconUtility.ApplyButtonIconClass(rootVisualElement, "stroke-remove-button", IconClass.MINUS);
        }

        #endregion Help Methods
    }
}
