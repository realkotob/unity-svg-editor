using System;
using UnityEditor;
using UnityEngine.UIElements;
using SvgEditor.Preview;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.InspectorPanel;
using SvgEditor.DocumentModel;
using SvgEditor.Document;

namespace SvgEditor.Workspace
{
    internal sealed class DocumentLifecycleController
    {
        private readonly DocumentRepository _documentRepository;
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly DocumentWorkspaceSyncService _workspaceSyncService;
        private readonly DocumentEditHistoryService _editHistory = new();

        public DocumentLifecycleController(
            DocumentRepository documentRepository,
            PreviewSnapshotBuilder previewSnapshotBuilder,
            PanelController inspectorPanelController,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Action updateEditorInteractivity)
        {
            _documentRepository = documentRepository;
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

        public bool CanSwitchDocument()
        {
            if (CurrentDocument == null || !CurrentDocument.IsDirty)
            {
                return true;
            }

            return EditorUtility.DisplayDialog(
                "Discard SVG changes?",
                "The current SVG has unsaved edits. Discard changes and switch?",
                "Discard",
                "Cancel");
        }

        public void LoadAsset(string assetPath)
        {
            if (!_documentRepository.TryLoad(assetPath, out DocumentSession document, out string error))
            {
                CurrentDocument = null;
                _previewService.ClearPreview();
                _workspaceSyncService.SelectionHandleLoadFailure(error);
                return;
            }

            CurrentDocument = document;
            _editHistory.Reset(document);
            _previewService.ResetPreviewState();
            _workspaceSyncService.ResetSelection();
            _workspaceSyncService.SelectionHandleDocumentLoaded();
        }

        public void ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            ApplyUpdatedSource(updatedSource, successStatus, HistoryRecordingMode.Immediate);
        }

        public void ApplyUpdatedSource(string updatedSource, string successStatus, HistoryRecordingMode recordingMode)
        {
            ApplyUpdatedSource(updatedSource, successStatus, recordHistory: true, recordingMode);
        }

        public bool TryUndo()
        {
            if (CurrentDocument == null ||
                !_editHistory.TryUndo(CurrentDocument.WorkingSourceText, out string restoredSource))
            {
                return false;
            }

            ApplyUpdatedSource(restoredSource, "Undo.", recordHistory: false, HistoryRecordingMode.Immediate);
            return true;
        }

        public bool TryRedo()
        {
            if (CurrentDocument == null ||
                !_editHistory.TryRedo(CurrentDocument.WorkingSourceText, out string restoredSource))
            {
                return false;
            }

            ApplyUpdatedSource(restoredSource, "Redo.", recordHistory: false, HistoryRecordingMode.Immediate);
            return true;
        }

        public void ReloadCurrentDocument()
        {
            if (CurrentDocument == null || string.IsNullOrWhiteSpace(CurrentDocument.AssetPath))
            {
                return;
            }

            LoadAsset(CurrentDocument.AssetPath);
        }

        public void SaveCurrentDocument()
        {
            OnSaveClicked();
        }

        private void ApplyUpdatedSource(
            string updatedSource,
            string successStatus,
            bool recordHistory,
            HistoryRecordingMode recordingMode)
        {
            if (CurrentDocument == null)
            {
                return;
            }

            string previousSource = CurrentDocument.WorkingSourceText ?? string.Empty;
            string nextSource = updatedSource ?? string.Empty;
            if (recordHistory)
            {
                _editHistory.RecordChange(previousSource, nextSource, recordingMode);
            }
            else
            {
                _editHistory.SyncCurrent(nextSource);
            }

            CurrentDocument.WorkingSourceText = nextSource;
            _documentRepository.RefreshDocumentModel(CurrentDocument);
            _workspaceSyncService.RefreshDocumentState(successStatus, keepExistingPreviewOnFailure: true);
        }

        public void RefreshLivePreview(bool keepExistingPreviewOnFailure) => _previewService.RefreshLivePreview(keepExistingPreviewOnFailure);

        public bool TryRefreshTransientPreview(SvgDocumentModel documentModel) => _previewService.TryRefreshTransientPreview(documentModel);

        public void UpdateInteractivity()
        {
            // Document panel controls were removed, so this controller no longer
            // owns any direct interactivity toggles.
        }

        public void UpdateSourceStatus(string status) => _view.SetStatus(status);

        private void OnSaveClicked()
        {
            if (CurrentDocument == null)
            {
                return;
            }

            if (!_documentRepository.Save(CurrentDocument, out string error))
            {
                _view.SetStatus($"Save failed: {error}");
                return;
            }

            _editHistory.SyncCurrent(CurrentDocument.WorkingSourceText);
            _workspaceSyncService.SelectionHandleSaveSucceeded();
        }
    }
}
