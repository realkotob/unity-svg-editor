using System;
using UnityEngine.UIElements;
using SvgEditor.Preview;
using SvgEditor.Preview.Build;
using SvgEditor.Workspace.Coordination;
using SvgEditor.Workspace.InspectorPanel;
using SvgEditor.DocumentModel;
using SvgEditor.Document;

namespace SvgEditor.Workspace.Document
{
    internal sealed class DocumentLifecycleController
    {
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly DocumentWorkspaceSyncService _workspaceSyncService;
        private readonly DocumentEditHistoryService _editHistory = new();
        private readonly DocumentLifecycleCommandService _commandService;

        public DocumentLifecycleController(
            DocumentRepository documentRepository,
            SnapshotBuilder previewSnapshotBuilder,
            PanelController inspectorPanelController,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Action updateEditorInteractivity)
        {
            _view = new DocumentLifecycleView();
            _previewService = new DocumentPreviewService(
                previewSnapshotBuilder,
                _view,
                () => CurrentDocument,
                workspaceCoordinatorAccessor);
            _workspaceSyncService = new DocumentWorkspaceSyncService(
                _view,
                _previewService,
                inspectorPanelController,
                () => CurrentDocument,
                workspaceCoordinatorAccessor,
                updateEditorInteractivity);
            _commandService = new DocumentLifecycleCommandService(
                documentRepository,
                _previewService,
                _workspaceSyncService,
                _editHistory,
                () => CurrentDocument,
                document => CurrentDocument = document,
                status => _view.SetStatus(status));
        }

        public DocumentSession CurrentDocument { get; private set; }
        public PreviewSnapshot PreviewSnapshot => _previewService.PreviewSnapshot;
        public Image PreviewImage => _view.PreviewImage;
        public bool CanUndo => _editHistory.CanUndo;
        public bool CanRedo => _editHistory.CanRedo;

        public void Bind(VisualElement root)
        {
            _view.Bind(root);
            if (root == null)
            {
                return;
            }

            _previewService.ApplyCurrentPreviewState();
            _workspaceSyncService.ApplyBoundState();
        }

        public void Unbind() => _view.Unbind();

        public void Dispose()
        {
            Unbind();
            _previewService.Dispose();
        }

        public bool CanSwitchDocument() => _commandService.CanSwitchDocument();

        public void LoadAsset(string assetPath) => _commandService.LoadAsset(assetPath);

        public void ApplyUpdatedSource(string updatedSource, string successStatus) =>
            _commandService.ApplyUpdatedSource(updatedSource, successStatus);

        public void ApplyUpdatedSource(string updatedSource, string successStatus, HistoryRecordingMode recordingMode) =>
            _commandService.ApplyUpdatedSource(updatedSource, successStatus, recordingMode);

        public bool TryUndo() => _commandService.TryUndo();

        public bool TryRedo() => _commandService.TryRedo();

        public void ReloadCurrentDocument() => _commandService.ReloadCurrentDocument();

        public void SaveCurrentDocument() => _commandService.SaveCurrentDocument();

        public void RefreshLivePreview(bool keepExistingPreviewOnFailure) => _previewService.RefreshLivePreview(keepExistingPreviewOnFailure);

        public bool TryRefreshTransientPreview(SvgDocumentModel documentModel) => _previewService.TryRefreshTransientPreview(documentModel);

        public void UpdateInteractivity()
        {
            // Document panel controls were removed, so this controller no longer
            // owns any direct interactivity toggles.
        }

        public void UpdateSourceStatus(string status) => _view.SetStatus(status);
    }
}
