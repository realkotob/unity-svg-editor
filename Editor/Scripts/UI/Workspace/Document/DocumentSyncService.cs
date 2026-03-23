using System;
using SvgEditor.UI.Inspector;
using SvgEditor.UI.Workspace.Coordination;
using SvgEditor.Core.Svg.Source;
using Core.UI.Extensions;

namespace SvgEditor.UI.Workspace.Document
{
    internal sealed class DocumentSyncService
    {
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly PanelController _inspectorPanelController;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Action _updateEditorInteractivity;
        private readonly Func<bool, string> _resyncPathEditSession;

        public DocumentSyncService(
            DocumentLifecycleView view,
            DocumentPreviewService previewService,
            PanelController inspectorPanelController,
            Func<DocumentSession> currentDocumentAccessor,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Action updateEditorInteractivity,
            Func<bool, string> resyncPathEditSession = null)
        {
            _view = view;
            _previewService = previewService;
            _inspectorPanelController = inspectorPanelController;
            _currentDocumentAccessor = currentDocumentAccessor;
            _workspaceCoordinatorAccessor = workspaceCoordinatorAccessor;
            _updateEditorInteractivity = updateEditorInteractivity;
            _resyncPathEditSession = resyncPathEditSession;
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
            ResolvePathEditStatus(previewIsCurrent: false);
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

            bool previewIsCurrent = _previewService.RefreshLivePreviewAndReportState(keepExistingPreviewOnFailure);
            string pathEditStatus = ResolvePathEditStatus(previewIsCurrent);
            _inspectorPanelController.RefreshTargets();
            WorkspaceCoordinator?.RefreshStructureViews();
            _updateEditorInteractivity?.Invoke();
            _view.SetStatus(BuildStatusMessage(string.IsNullOrWhiteSpace(pathEditStatus) ? status : pathEditStatus));
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

        private string ResolvePathEditStatus(bool previewIsCurrent)
        {
            if (_resyncPathEditSession != null)
            {
                return _resyncPathEditSession(previewIsCurrent) ?? string.Empty;
            }

            return WorkspaceCoordinator?.ResyncPathEditSession(previewIsCurrent) ?? string.Empty;
        }
    }
}
