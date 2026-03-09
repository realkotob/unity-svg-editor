using System;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentPreviewService : IDisposable
    {
        private readonly PreviewSnapshotBuilder _previewSnapshotBuilder;
        private readonly DocumentLifecycleView _view;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;

        public DocumentPreviewService(
            PreviewSnapshotBuilder previewSnapshotBuilder,
            DocumentLifecycleView view,
            Func<DocumentSession> currentDocumentAccessor,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor)
        {
            _previewSnapshotBuilder = previewSnapshotBuilder;
            _view = view;
            _currentDocumentAccessor = currentDocumentAccessor;
            _workspaceCoordinatorAccessor = workspaceCoordinatorAccessor;
        }

        public PreviewSnapshot PreviewSnapshot { get; private set; }

        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinatorAccessor?.Invoke();
        private DocumentSession CurrentDocument => _currentDocumentAccessor?.Invoke();

        public void Dispose()
        {
            DisposePreviewSnapshot();
        }

        public void ApplyCurrentPreviewState()
        {
            _view.SetPreviewVectorImage(PreviewSnapshot?.PreviewVectorImage ?? CurrentDocument?.VectorImageAsset);
        }

        public void ResetPreviewToDocumentAsset()
        {
            DisposePreviewSnapshot();
            _view.SetPreviewVectorImage(CurrentDocument?.VectorImageAsset);
            WorkspaceCoordinator?.UpdateCanvasVisualState();
        }

        public void ClearPreview()
        {
            DisposePreviewSnapshot();
            _view.SetPreviewVectorImage(null);
            WorkspaceCoordinator?.UpdateCanvasVisualState();
        }

        public void RefreshLivePreview(bool keepExistingPreviewOnFailure)
        {
            if (_view.PreviewImage == null)
            {
                return;
            }

            var currentDocument = CurrentDocument;
            if (currentDocument == null)
            {
                DisposePreviewSnapshot();
                _view.SetPreviewVectorImage(null);
                WorkspaceCoordinator?.UpdateCanvasVisualState();
                return;
            }

            if (!_previewSnapshotBuilder.TryBuildSnapshot(currentDocument.WorkingSourceText, out PreviewSnapshot snapshot, out _))
            {
                if (!keepExistingPreviewOnFailure)
                {
                    DisposePreviewSnapshot();
                    _view.SetPreviewVectorImage(currentDocument.VectorImageAsset);
                    WorkspaceCoordinator?.UpdateCanvasVisualState();
                }

                return;
            }

            ReplacePreviewSnapshot(snapshot);
        }

        public bool TryRefreshTransientPreview(string sourceText)
        {
            if (_view.PreviewImage == null || string.IsNullOrWhiteSpace(sourceText))
            {
                return false;
            }

            if (!_previewSnapshotBuilder.TryBuildSnapshot(sourceText, out PreviewSnapshot snapshot, out _))
            {
                return false;
            }

            ReplacePreviewSnapshot(snapshot);
            return true;
        }

        private void ReplacePreviewSnapshot(PreviewSnapshot snapshot)
        {
            DisposePreviewSnapshot();
            PreviewSnapshot = snapshot;
            _view.SetPreviewVectorImage(snapshot?.PreviewVectorImage ?? CurrentDocument?.VectorImageAsset);
            WorkspaceCoordinator?.SyncCanvasFrameToPreview();
            WorkspaceCoordinator?.UpdateCanvasVisualState();
        }

        private void DisposePreviewSnapshot()
        {
            PreviewSnapshot?.Dispose();
            PreviewSnapshot = null;
        }
    }
}
