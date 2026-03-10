using System;
using UnityEngine;

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
            if (PreviewSnapshot != null)
            {
                _view.SetPreviewVectorImage(PreviewSnapshot.PreviewVectorImage);
                return;
            }

            if (CurrentDocument != null)
            {
                RefreshLivePreview(keepExistingPreviewOnFailure: false);
                return;
            }

            _view.SetPreviewVectorImage(null);
        }

        public void ResetPreviewState()
        {
            DisposePreviewSnapshot();
            _view.SetPreviewVectorImage(null);
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

            Rect preferredViewportRect = PreviewSnapshot?.ProjectionRect ?? default;
            if (!_previewSnapshotBuilder.TryBuildSnapshot(
                    currentDocument.WorkingSourceText,
                    preferredViewportRect,
                    out PreviewSnapshot snapshot,
                    out _))
            {
                if (!keepExistingPreviewOnFailure)
                {
                    DisposePreviewSnapshot();
                    _view.SetPreviewVectorImage(null);
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

            var currentDocument = CurrentDocument;
            if (currentDocument == null)
            {
                return false;
            }

            Rect preferredViewportRect = PreviewSnapshot?.ProjectionRect ?? default;
            if (!_previewSnapshotBuilder.TryBuildSnapshot(
                    sourceText,
                    preferredViewportRect,
                    out PreviewSnapshot snapshot,
                    out _))
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
            _view.SetPreviewVectorImage(snapshot?.PreviewVectorImage);
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
