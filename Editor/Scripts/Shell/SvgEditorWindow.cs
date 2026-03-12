using System;
using System.Globalization;
using Core.UI.Foundation.Editor;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Preview;
using SvgEditor.Workspace;
using SvgEditor.Workspace.AssetLibrary;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.InspectorPanel;
using SvgEditor.DocumentModel;
using SvgEditor.Document;

namespace SvgEditor.Shell
{
    public sealed class SvgEditorWindow : EditorWindow, IEditorWorkspaceHost, IPanelHost
    {
        private const string WINDOW_MENU_PATH = "Window/Tools/SVG Editor";

        #region Variables

        private DocumentRepository _documentRepository;
        private PreviewSnapshotBuilder _previewSnapshotBuilder;
        private AssetDatabaseVectorImageSourceProvider _vectorImageSourceProvider;
        private AssetLibraryBrowser _assetLibraryBrowser;
        private PanelController _inspectorPanelController;
        private DocumentLifecycleController _documentLifecycleController;
        private SvgEditorWindowLayoutBinder _layoutBinder;
        private SvgEditorWindowShortcutRouter _shortcutRouter;
        private PreviewGeometryLookupService _previewGeometryLookupService;
        private EditorWorkspaceCoordinator _workspaceCoordinator;

        VisualElement IEditorWorkspaceHost.RootVisualElement => rootVisualElement;

        DocumentSession IEditorWorkspaceHost.CurrentDocument
        {
            get
            {
                EnsureInitialized();
                return _documentLifecycleController.CurrentDocument;
            }
        }

        PreviewSnapshot IEditorWorkspaceHost.PreviewSnapshot
        {
            get
            {
                EnsureInitialized();
                return _documentLifecycleController.PreviewSnapshot;
            }
        }

        Image IEditorWorkspaceHost.PreviewImage
        {
            get
            {
                EnsureInitialized();
                return _documentLifecycleController.PreviewImage;
            }
        }

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
            EnsureInitialized();
        }

        private void CreateGUI()
        {
            EnsureInitialized();
            LayoutBinder.RebuildLayout();
        }

        private void OnDisable()
        {
            EnsureInitialized();
            _layoutBinder?.DetachLayout();
            _documentLifecycleController.Dispose();
            _workspaceCoordinator?.Dispose();
        }

        private void OnGUI()
        {
            EnsureInitialized();
            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.KeyDown)
                return;

            if (_shortcutRouter.TryHandle(currentEvent.keyCode, currentEvent.modifiers))
                currentEvent.Use();
        }

        #endregion Unity Methods

        #region Help Methods

        private SvgEditorWindowLayoutBinder LayoutBinder => _layoutBinder ??= new SvgEditorWindowLayoutBinder(
            rootVisualElement,
            _assetLibraryBrowser,
            _documentLifecycleController,
            _inspectorPanelController,
            () => WorkspaceCoordinator,
            () => this,
            OnRootKeyDown);

        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinator ??= new EditorWorkspaceCoordinator(this);

        private void EnsureInitialized()
        {
            _documentRepository ??= new DocumentRepository();
            _previewSnapshotBuilder ??= new PreviewSnapshotBuilder();
            _vectorImageSourceProvider ??= new AssetDatabaseVectorImageSourceProvider();
            _assetLibraryBrowser ??= new AssetLibraryBrowser(_documentRepository, _vectorImageSourceProvider);
            _inspectorPanelController ??= new PanelController(new PanelState());
            _documentLifecycleController ??= new DocumentLifecycleController(
                _documentRepository,
                _previewSnapshotBuilder,
                _inspectorPanelController,
                () => WorkspaceCoordinator,
                UpdateEditorInteractivity);
            _shortcutRouter ??= new SvgEditorWindowShortcutRouter(
                () => _documentLifecycleController.CurrentDocument,
                () => WorkspaceCoordinator.TryCancelActiveDrag(),
                () => _documentLifecycleController.TryUndo(),
                () => _documentLifecycleController.TryRedo(),
                _documentLifecycleController.SaveCurrentDocument);
            _previewGeometryLookupService ??= new PreviewGeometryLookupService(() => _documentLifecycleController.PreviewSnapshot);
        }

        private string ResolveSelectedPatchTargetKey()
        {
            EnsureInitialized();
            return _inspectorPanelController.ResolveSelectedTargetKey();
        }

        bool IEditorWorkspaceHost.TrySelectPatchTargetByKey(string targetKey)
        {
            EnsureInitialized();
            return _inspectorPanelController.TrySelectTargetByKey(targetKey, out _);
        }

        string IEditorWorkspaceHost.ResolveSelectedPatchTargetKey() => ResolveSelectedPatchTargetKey();

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        string IEditorWorkspaceHost.FormatNumber(float value) => FormatNumber(value);

        void IEditorWorkspaceHost.RefreshLivePreview(bool keepExistingPreviewOnFailure)
        {
            EnsureInitialized();
            _documentLifecycleController.RefreshLivePreview(keepExistingPreviewOnFailure);
        }

        bool IEditorWorkspaceHost.TryRefreshTransientPreview(SvgDocumentModel documentModel)
        {
            EnsureInitialized();
            return _documentLifecycleController.TryRefreshTransientPreview(documentModel);
        }

        void IEditorWorkspaceHost.RefreshInspector()
        {
            EnsureInitialized();
            _inspectorPanelController.QueueRefreshTargets();
        }

        void IEditorWorkspaceHost.RefreshInspector(SvgDocumentModel documentModel)
        {
            EnsureInitialized();
            _inspectorPanelController.RefreshTargets(documentModel);
        }

        private bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate)
        {
            EnsureInitialized();
            return WorkspaceCoordinator.TryApplyPatchRequest(request, successStatus, recordingMode);
        }

        void IEditorWorkspaceHost.ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            EnsureInitialized();
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus);
        }

        void IEditorWorkspaceHost.ApplyUpdatedSource(
            string updatedSource,
            string successStatus,
            HistoryRecordingMode recordingMode)
        {
            EnsureInitialized();
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus, recordingMode);
        }

        private void UpdateEditorInteractivity()
        {
            EnsureInitialized();
            DocumentSession currentDocument = _documentLifecycleController.CurrentDocument;
            bool hasDocument = currentDocument != null;
            bool hasInspectableDocument = currentDocument?.DocumentModel != null &&
                                          string.IsNullOrWhiteSpace(currentDocument.DocumentModelLoadError) &&
                                          string.Equals(currentDocument.DocumentModel.SourceText, currentDocument.WorkingSourceText, StringComparison.Ordinal);

            _documentLifecycleController.UpdateInteractivity();
            _inspectorPanelController.UpdateInteractivity(hasInspectableDocument);
            WorkspaceCoordinator.UpdateStructureInteractivity(hasDocument);
        }

        void IEditorWorkspaceHost.UpdateEditorInteractivity() => UpdateEditorInteractivity();

        private void UpdateSourceStatus(string status)
        {
            EnsureInitialized();
            _documentLifecycleController.UpdateSourceStatus(status);
        }

        void IEditorWorkspaceHost.UpdateSourceStatus(string status) => UpdateSourceStatus(status);

        DocumentSession IPanelHost.CurrentDocument
        {
            get
            {
                EnsureInitialized();
                return _documentLifecycleController.CurrentDocument;
            }
        }

        bool IPanelHost.TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode) => TryApplyPatchRequest(request, successStatus, recordingMode);

        bool IPanelHost.TryApplyTargetFrameRect(
            string targetKey,
            Rect targetSceneRect,
            string successStatus,
            HistoryRecordingMode recordingMode)
        {
            EnsureInitialized();
            return WorkspaceCoordinator.TryApplyTargetFrameRect(targetKey, targetSceneRect, successStatus, recordingMode);
        }

        bool IPanelHost.TryGetTargetSceneRect(string targetKey, out Rect sceneRect)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetTargetSceneRect(targetKey, out sceneRect);
        }

        bool IPanelHost.TryGetTargetRotationPivotParentSpace(string targetKey, out Vector2 parentPivot)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetTargetRotationPivotParentSpace(targetKey, out parentPivot);
        }

        bool IPanelHost.TryGetTargetParentWorldTransform(string targetKey, out Matrix2D parentWorldTransform)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetTargetParentWorldTransform(targetKey, out parentWorldTransform);
        }

        bool IPanelHost.TryGetCanvasViewportSceneRect(out Rect sceneRect)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetCanvasViewportSceneRect(out sceneRect);
        }

        void IPanelHost.SyncSelectionFromInspectorTarget(string targetKey)
        {
            EnsureInitialized();
            WorkspaceCoordinator.SyncSelectionFromInspectorTarget(targetKey);
        }

        void IPanelHost.UpdateSourceStatus(string status) => UpdateSourceStatus(status);

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            EnsureInitialized();
            if (_shortcutRouter.TryHandle(evt.keyCode, evt.modifiers))
                evt.StopPropagation();
        }

        #endregion Help Methods
    }
}
