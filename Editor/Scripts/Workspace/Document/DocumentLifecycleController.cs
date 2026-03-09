using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentLifecycleController
    {
        private readonly DocumentRepository _documentRepository;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Action _updateEditorInteractivity;
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly DocumentSourceSyncService _sourceSyncService;

        public DocumentLifecycleController(
            DocumentRepository documentRepository,
            PreviewSnapshotBuilder previewSnapshotBuilder,
            PatchInspectorController patchInspectorController,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Action updateEditorInteractivity)
        {
            _documentRepository = documentRepository;
            _workspaceCoordinatorAccessor = workspaceCoordinatorAccessor;
            _updateEditorInteractivity = updateEditorInteractivity;

            _view = new DocumentLifecycleView();
            _previewService = new DocumentPreviewService(
                previewSnapshotBuilder,
                _view,
                () => CurrentDocument,
                _workspaceCoordinatorAccessor);
            _sourceSyncService = new DocumentSourceSyncService(
                _view,
                _previewService,
                patchInspectorController,
                _workspaceCoordinatorAccessor,
                () => CurrentDocument,
                _updateEditorInteractivity);

            _view.ReloadRequested += OnReloadClicked;
            _view.ValidateRequested += OnValidateClicked;
            _view.SaveRequested += OnSaveClicked;
            _view.SourceChanged += OnSourceChanged;
        }

        public DocumentSession CurrentDocument { get; private set; }
        public PreviewSnapshot PreviewSnapshot => _previewService.PreviewSnapshot;
        public Image PreviewImage => _view.PreviewImage;

        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinatorAccessor?.Invoke();

        public void Bind(VisualElement root)
        {
            _view.Bind(root);
            if (root == null)
            {
                return;
            }

            _previewService.ApplyCurrentPreviewState();
            _sourceSyncService.ApplyCurrentDocumentToView();
            _updateEditorInteractivity?.Invoke();
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
                _sourceSyncService.HandleLoadFailure(error);
                return;
            }

            CurrentDocument = document;
            _previewService.ResetPreviewToDocumentAsset();
            WorkspaceCoordinator?.ResetSelection();
            _sourceSyncService.HandleDocumentLoaded();
        }

        public void ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            if (CurrentDocument == null)
            {
                return;
            }

            CurrentDocument.WorkingSourceText = updatedSource;
            _sourceSyncService.SyncCurrentSource(successStatus, keepExistingPreviewOnFailure: true, updateSourceField: true);
        }

        public void RefreshLivePreview(bool keepExistingPreviewOnFailure) => _previewService.RefreshLivePreview(keepExistingPreviewOnFailure);

        public bool TryRefreshTransientPreview(string sourceText) => _previewService.TryRefreshTransientPreview(sourceText);

        public void UpdateInteractivity()
        {
            var currentDocument = CurrentDocument;
            var hasDocument = currentDocument != null;

            SetEnabledIfNotNull(_view.SourceEditorControl, hasDocument);
            SetEnabledIfNotNull(_view.ReloadButtonControl, hasDocument);
            SetEnabledIfNotNull(_view.ValidateButtonControl, hasDocument);
            SetEnabledIfNotNull(_view.SaveButtonControl, hasDocument && currentDocument.IsDirty);
        }

        private static void SetEnabledIfNotNull(VisualElement element, bool enabled)
        {
            if (element != null)
                element.SetEnabled(enabled);
        }

        public void UpdateSourceStatus(string status) => _view.SetStatus(status);

        private void OnSourceChanged(string sourceText)
        {
            if (CurrentDocument == null)
            {
                return;
            }

            CurrentDocument.WorkingSourceText = sourceText;
            _sourceSyncService.SyncCurrentSource(
                CurrentDocument.IsDirty ? "Unsaved changes." : "No local changes.",
                keepExistingPreviewOnFailure: true,
                updateSourceField: false);
        }

        private void OnReloadClicked()
        {
            if (CurrentDocument == null)
            {
                return;
            }

            if (CurrentDocument.IsDirty)
            {
                bool shouldDiscard = EditorUtility.DisplayDialog(
                    "Discard SVG changes?",
                    "Reload will discard unsaved edits. Continue?",
                    "Reload",
                    "Cancel");
                if (!shouldDiscard)
                {
                    return;
                }
            }

            LoadAsset(CurrentDocument.AssetPath);
            UpdateSourceStatus("Reloaded SVG source from disk.");
        }

        private void OnValidateClicked()
        {
            if (CurrentDocument == null)
            {
                return;
            }

            if (_documentRepository.ValidateXml(CurrentDocument.WorkingSourceText, out string error))
            {
                UpdateSourceStatus("XML is valid.");
                return;
            }

            UpdateSourceStatus($"Invalid XML: {error}");
        }

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

            _previewService.ResetPreviewToDocumentAsset();
            _sourceSyncService.SyncCurrentSource(
                "Saved SVG and reimported asset.",
                keepExistingPreviewOnFailure: false,
                updateSourceField: false);
        }
    }
}
