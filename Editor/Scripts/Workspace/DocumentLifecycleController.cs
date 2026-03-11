using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentLifecycleController
    {
        private readonly DocumentRepository _documentRepository;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly Action _updateEditorInteractivity;
        private readonly InspectorPanelController _inspectorPanelController;
        private readonly DocumentLifecycleView _view;
        private readonly DocumentPreviewService _previewService;
        private readonly DocumentEditHistoryService _editHistory = new();

        public DocumentLifecycleController(
            DocumentRepository documentRepository,
            PreviewSnapshotBuilder previewSnapshotBuilder,
            InspectorPanelController inspectorPanelController,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor,
            Action updateEditorInteractivity)
        {
            _documentRepository = documentRepository;
            _workspaceCoordinatorAccessor = workspaceCoordinatorAccessor;
            _updateEditorInteractivity = updateEditorInteractivity;
            _inspectorPanelController = inspectorPanelController;

            _view = new DocumentLifecycleView();
            _previewService = new DocumentPreviewService(
                previewSnapshotBuilder,
                _view,
                () => CurrentDocument,
                _workspaceCoordinatorAccessor);

            _view.SaveRequested += OnSaveClicked;
        }

        public DocumentSession CurrentDocument { get; private set; }
        public PreviewSnapshot PreviewSnapshot => _previewService.PreviewSnapshot;
        public Image PreviewImage => _view.PreviewImage;
        public bool CanUndo => _editHistory.CanUndo;
        public bool CanRedo => _editHistory.CanRedo;

        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinatorAccessor?.Invoke();

        public void Bind(VisualElement root)
        {
            _view.Bind(root);
            if (root == null)
            {
                return;
            }

            _previewService.ApplyCurrentPreviewState();
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
                HandleLoadFailure(error);
                return;
            }

            CurrentDocument = document;
            _editHistory.Reset(document);
            _previewService.ResetPreviewState();
            WorkspaceCoordinator?.ResetSelection();
            HandleDocumentLoaded();
        }

        public void ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            ApplyUpdatedSource(updatedSource, successStatus, recordHistory: true);
        }

        public bool TryUndo()
        {
            if (CurrentDocument == null ||
                !_editHistory.TryUndo(CurrentDocument.WorkingSourceText, out string restoredSource))
            {
                return false;
            }

            ApplyUpdatedSource(restoredSource, "Undo.", recordHistory: false);
            return true;
        }

        public bool TryRedo()
        {
            if (CurrentDocument == null ||
                !_editHistory.TryRedo(CurrentDocument.WorkingSourceText, out string restoredSource))
            {
                return false;
            }

            ApplyUpdatedSource(restoredSource, "Redo.", recordHistory: false);
            return true;
        }

        private void ApplyUpdatedSource(string updatedSource, string successStatus, bool recordHistory)
        {
            if (CurrentDocument == null)
            {
                return;
            }

            string previousSource = CurrentDocument.WorkingSourceText ?? string.Empty;
            string nextSource = updatedSource ?? string.Empty;
            if (recordHistory)
            {
                _editHistory.RecordChange(previousSource, nextSource);
            }
            else
            {
                _editHistory.SyncCurrent(nextSource);
            }

            CurrentDocument.WorkingSourceText = nextSource;
            _documentRepository.RefreshDocumentModel(CurrentDocument);
            SyncCurrentSource(
                successStatus,
                keepExistingPreviewOnFailure: true,
                updateSourceField: true);
        }

        public void RefreshLivePreview(bool keepExistingPreviewOnFailure) => _previewService.RefreshLivePreview(keepExistingPreviewOnFailure);

        public bool TryRefreshTransientPreview(SvgDocumentModel documentModel) => _previewService.TryRefreshTransientPreview(documentModel);

        public void UpdateInteractivity()
        {
            var currentDocument = CurrentDocument;
            var hasDocument = currentDocument != null;

            SetEnabledIfNotNull(_view.SaveButtonControl, hasDocument && currentDocument.IsDirty);
        }

        private static void SetEnabledIfNotNull(VisualElement element, bool enabled)
        {
            if (element != null)
                element.SetEnabled(enabled);
        }

        public void UpdateSourceStatus(string status) => _view.SetStatus(status);

        private void HandleLoadFailure(string error)
        {
            _view.ShowLoadFailure(error);
            _updateEditorInteractivity?.Invoke();
        }

        private void HandleDocumentLoaded()
        {
            if (CurrentDocument == null)
                return;

            SyncCurrentSource("Loaded SVG source.", keepExistingPreviewOnFailure: false, updateSourceField: true);
            LogRendererDiagnostics();
            WorkspaceCoordinator?.FitCanvasView(clearSelection: true);
        }

        private void SyncCurrentSource(
            string status,
            bool keepExistingPreviewOnFailure,
            bool updateSourceField,
            bool skipPreviewRefresh = false)
        {
            if (CurrentDocument == null)
                return;

            _inspectorPanelController.RefreshTargets();
            if (!skipPreviewRefresh)
                _previewService.RefreshLivePreview(keepExistingPreviewOnFailure);

            WorkspaceCoordinator?.RefreshStructureViews();
            _updateEditorInteractivity?.Invoke();
            _view.SetStatus(status);
        }

        private void LogRendererDiagnostics()
        {
            if (CurrentDocument == null)
                return;

            FeatureScanResult scanResult = FeatureScanner.Scan(CurrentDocument.WorkingSourceText);
            string warning = RendererSupportDiagnostics.BuildConsoleWarning(scanResult);
            if (!string.IsNullOrWhiteSpace(warning))
                Debug.LogWarning(warning);
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

            _previewService.ResetPreviewState();
            _editHistory.Reset(CurrentDocument);
            SyncCurrentSource(
                "Saved SVG and reimported asset.",
                keepExistingPreviewOnFailure: false,
                updateSourceField: false);
        }
    }
}
