using System;
using SvgEditor.Workspace.InspectorPanel;
using SvgEditor.Workspace.Coordination;
using SvgEditor.Document;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.Document
{
    internal sealed class DocumentSyncService
    {
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly PanelController _inspectorPanelController;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Action _updateEditorInteractivity;

        public DocumentSyncService(
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

        public void HandleLoadFailure(string error)
        {
            _view.ShowLoadFailure(error);
            _updateEditorInteractivity?.Invoke();
        }

        public void HandleDocumentLoaded()
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
            _view.SetStatus(BuildStatusMessage(status));
        }

        public void HandleSaveSucceeded()
        {
            _previewService.ResetPreviewState();
            RefreshDocumentState("Saved SVG and reimported asset.", keepExistingPreviewOnFailure: false);
            _view.ShowToast("Saved SVG", DocumentLifecycleView.ToastVariant.Success);
        }

        private string BuildStatusMessage(string status)
        {
            if (CurrentDocument == null || string.IsNullOrWhiteSpace(CurrentDocument.ModelEditingBlockReason))
            {
                return status;
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return CurrentDocument.ModelEditingBlockReason;
            }

            return $"{status} {CurrentDocument.ModelEditingBlockReason}";
        }
    }
}
