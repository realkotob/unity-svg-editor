using System;
using UnityEditor;
using SvgEditor.Core.Svg.Source;
using Core.UI.Extensions;

namespace SvgEditor.UI.Workspace.Document
{
    internal sealed class LifecycleCommandService
    {
        private readonly DocumentRepository _documentRepository;
        private readonly DocumentPreviewService _previewService;
        private readonly DocumentSyncService _workspaceSyncService;
        private readonly DocumentEditHistoryService _editHistory;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Action<DocumentSession> _setCurrentDocument;
        private readonly Action<string> _setStatus;

        public LifecycleCommandService(
            DocumentRepository documentRepository,
            DocumentPreviewService previewService,
            DocumentSyncService workspaceSyncService,
            DocumentEditHistoryService editHistory,
            Func<DocumentSession> currentDocumentAccessor,
            Action<DocumentSession> setCurrentDocument,
            Action<string> setStatus)
        {
            _documentRepository = documentRepository;
            _previewService = previewService;
            _workspaceSyncService = workspaceSyncService;
            _editHistory = editHistory;
            _currentDocumentAccessor = currentDocumentAccessor;
            _setCurrentDocument = setCurrentDocument;
            _setStatus = setStatus;
        }

        private DocumentSession CurrentDocument => _currentDocumentAccessor?.Invoke();

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
                _setCurrentDocument?.Invoke(null);
                _previewService.ClearPreview();
                _workspaceSyncService.HandleLoadFailure(error);
                return;
            }

            HandleLoadedDocument(document);
        }

        public void ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            ApplyUpdatedSource(updatedSource, successStatus, HistoryRecordingMode.Immediate);
        }

        public void ApplyUpdatedSource(string updatedSource, string successStatus, HistoryRecordingMode recordingMode)
        {
            CommitUpdatedSource(updatedSource, successStatus, recordHistory: true, recordingMode);
        }

        public bool TryUndo()
        {
            if (CurrentDocument == null ||
                !_editHistory.TryUndo(CurrentDocument.WorkingSourceText, out string restoredSource))
            {
                return false;
            }

            CommitUpdatedSource(restoredSource, "Undo.", recordHistory: false, HistoryRecordingMode.Immediate);
            return true;
        }

        public bool TryRedo()
        {
            if (CurrentDocument == null ||
                !_editHistory.TryRedo(CurrentDocument.WorkingSourceText, out string restoredSource))
            {
                return false;
            }

            CommitUpdatedSource(restoredSource, "Redo.", recordHistory: false, HistoryRecordingMode.Immediate);
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
            if (CurrentDocument == null)
            {
                return;
            }

            if (!_documentRepository.Save(CurrentDocument, out string error))
            {
                _setStatus?.Invoke($"Save failed: {error}");
                return;
            }

            _editHistory.SyncCurrent(CurrentDocument.WorkingSourceText);
            _workspaceSyncService.HandleSaveSucceeded();
        }

        private void HandleLoadedDocument(DocumentSession document)
        {
            _setCurrentDocument?.Invoke(document);
            _editHistory.Reset(document);
            _previewService.ResetPreviewState();
            _workspaceSyncService.ResetSelection();
            _workspaceSyncService.HandleDocumentLoaded();
        }

        private void CommitUpdatedSource(
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
    }
}
