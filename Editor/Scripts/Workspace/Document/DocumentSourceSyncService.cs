using System;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentSourceSyncService
    {
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly PatchInspectorController _patchInspectorController;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Action _updateEditorInteractivity;

        public DocumentSourceSyncService(
            DocumentLifecycleView view,
            DocumentPreviewService previewService,
            PatchInspectorController patchInspectorController,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Func<DocumentSession> currentDocumentAccessor,
            Action updateEditorInteractivity)
        {
            _view = view;
            _previewService = previewService;
            _patchInspectorController = patchInspectorController;
            _workspaceCoordinatorAccessor = workspaceCoordinatorAccessor;
            _currentDocumentAccessor = currentDocumentAccessor;
            _updateEditorInteractivity = updateEditorInteractivity;
        }

        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinatorAccessor?.Invoke();
        private DocumentSession CurrentDocument => _currentDocumentAccessor?.Invoke();

        public void ApplyCurrentDocumentToView()
        {
            var currentDocument = CurrentDocument;
            _view.SetSourceText(currentDocument?.WorkingSourceText ?? string.Empty);
        }

        public void HandleLoadFailure(string error)
        {
            _view.ShowLoadFailure(error);
            _updateEditorInteractivity?.Invoke();
        }

        public void HandleDocumentLoaded()
        {
            var currentDocument = CurrentDocument;
            if (currentDocument == null)
            {
                return;
            }

            SyncCurrentSource("Loaded SVG source.", keepExistingPreviewOnFailure: false, updateSourceField: true);
            WorkspaceCoordinator?.ResetCanvasView(clearSelection: true);
        }

        public void SyncCurrentSource(string status, bool keepExistingPreviewOnFailure, bool updateSourceField)
        {
            var currentDocument = CurrentDocument;
            if (currentDocument == null)
            {
                return;
            }

            if (updateSourceField)
            {
                _view.SetSourceText(currentDocument.WorkingSourceText);
            }

            _patchInspectorController.RefreshTargets(currentDocument.WorkingSourceText);
            WorkspaceCoordinator?.RefreshStructureViews();
            _previewService.RefreshLivePreview(keepExistingPreviewOnFailure);
            _updateEditorInteractivity?.Invoke();
            _view.SetStatus(status);
        }
    }
}
