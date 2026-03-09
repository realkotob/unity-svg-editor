using System;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentSourceSyncService
    {
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly InspectorPanelController _inspectorPanelController;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Action _updateEditorInteractivity;

        public DocumentSourceSyncService(
            DocumentLifecycleView view,
            DocumentPreviewService previewService,
            InspectorPanelController inspectorPanelController,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Func<DocumentSession> currentDocumentAccessor,
            Action updateEditorInteractivity)
        {
            _view = view;
            _previewService = previewService;
            _inspectorPanelController = inspectorPanelController;
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

        public void SyncCurrentSource(
            string status,
            bool keepExistingPreviewOnFailure,
            bool updateSourceField,
            bool skipPreviewRefresh = false)
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

            _inspectorPanelController.RefreshTargets(currentDocument.WorkingSourceText);
            if (!skipPreviewRefresh)
            {
                _previewService.RefreshLivePreview(keepExistingPreviewOnFailure);
            }
            WorkspaceCoordinator?.RefreshStructureViews();
            _updateEditorInteractivity?.Invoke();
            _view.SetStatus(status);
        }
    }
}
