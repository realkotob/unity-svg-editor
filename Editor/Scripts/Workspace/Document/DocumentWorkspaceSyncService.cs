using System;
using SvgEditor.Workspace.InspectorPanel;
using SvgEditor.Document;

namespace SvgEditor.Workspace.Document
{
    internal sealed class DocumentWorkspaceSyncService
    {
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly PanelController _inspectorPanelController;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Action _updateEditorInteractivity;

        public DocumentWorkspaceSyncService(
            DocumentLifecycleView view,
            DocumentPreviewService previewService,
            PanelController inspectorPanelController,
            Func<DocumentSession> currentDocumentAccessor,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Action updateEditorInteractivity)
        {
            _view = view;
            _previewService = previewService;
            _inspectorPanelController = inspectorPanelController;
            _currentDocumentAccessor = currentDocumentAccessor;
            _workspaceCoordinatorAccessor = workspaceCoordinatorAccessor;
            _updateEditorInteractivity = updateEditorInteractivity;
        }

        private DocumentSession CurrentDocument => _currentDocumentAccessor?.Invoke();
        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinatorAccessor?.Invoke();

        public void ApplyBoundState()
        {
            _updateEditorInteractivity?.Invoke();
        }

        public void ResetSelection()
        {
            WorkspaceCoordinator?.ResetSelection();
        }

        public void SelectionHandleLoadFailure(string error)
        {
            _view.ShowLoadFailure(error);
            _updateEditorInteractivity?.Invoke();
        }

        public void SelectionHandleDocumentLoaded()
        {
            if (CurrentDocument == null)
            {
                return;
            }

            RefreshDocumentState("Loaded SVG source.", keepExistingPreviewOnFailure: false);
            WorkspaceCoordinator?.FitCanvasView(clearSelection: true);
        }

        public void RefreshDocumentState(string status, bool keepExistingPreviewOnFailure)
        {
            if (CurrentDocument == null)
            {
                return;
            }

            _previewService.RefreshLivePreview(keepExistingPreviewOnFailure);
            _inspectorPanelController.RefreshTargets();
            WorkspaceCoordinator?.RefreshStructureViews();
            _updateEditorInteractivity?.Invoke();
            _view.SetStatus(status);
        }

        public void SelectionHandleSaveSucceeded()
        {
            _previewService.ResetPreviewState();
            RefreshDocumentState("Saved SVG and reimported asset.", keepExistingPreviewOnFailure: false);
            _view.ShowToast("Saved SVG", DocumentLifecycleView.ToastVariant.Success);
        }
    }
}
