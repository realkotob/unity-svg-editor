using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Preview.Build;
using SvgEditor.UI.Workspace.Coordination;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.UI.Workspace.Document
{
    internal sealed class DocumentPreviewService : IDisposable
    {
        private sealed class PendingPreviewDisposal
        {
            public PreviewSnapshot Snapshot { get; set; }
            public int RemainingTicks { get; set; }
        }

        private const int PreviewDisposeDelayTicks = 3;
        private readonly SnapshotBuilder _previewSnapshotBuilder;
        private readonly DocumentLifecycleView _view;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Func<EditorWorkspaceCoordinator> _workspaceCoordinatorAccessor;
        private readonly DeferredActionGate _disposeGate;
        private readonly List<PendingPreviewDisposal> _pendingDisposals = new();

        public DocumentPreviewService(
            SnapshotBuilder previewSnapshotBuilder,
            DocumentLifecycleView view,
            Func<DocumentSession> currentDocumentAccessor,
            Func<EditorWorkspaceCoordinator> workspaceCoordinatorAccessor)
        {
            _previewSnapshotBuilder = previewSnapshotBuilder;
            _view = view;
            _currentDocumentAccessor = currentDocumentAccessor;
            _workspaceCoordinatorAccessor = workspaceCoordinatorAccessor;
            _disposeGate = new DeferredActionGate(FlushPendingDisposals);
        }

        public PreviewSnapshot PreviewSnapshot { get; private set; }

        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinatorAccessor?.Invoke();
        private DocumentSession CurrentDocument => _currentDocumentAccessor?.Invoke();

        public void Dispose()
        {
            _view.SetPreviewVectorImage(null);
            DisposePreviewSnapshot(immediate: true);
            FlushPendingDisposals();
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
            _view.SetPreviewVectorImage(null);
            DisposePreviewSnapshot();
            WorkspaceCoordinator?.UpdateCanvasVisualState();
        }

        public void ClearPreview()
        {
            _view.SetPreviewVectorImage(null);
            DisposePreviewSnapshot();
            WorkspaceCoordinator?.UpdateCanvasVisualState();
        }

        public void RefreshLivePreview(bool keepExistingPreviewOnFailure)
        {
            RefreshLivePreviewCore(keepExistingPreviewOnFailure);
        }

        public bool RefreshLivePreviewAndReportState(bool keepExistingPreviewOnFailure)
        {
            return RefreshLivePreviewCore(keepExistingPreviewOnFailure);
        }

        private bool RefreshLivePreviewCore(bool keepExistingPreviewOnFailure)
        {
            if (_view.PreviewImage == null)
            {
                return false;
            }

            var currentDocument = CurrentDocument;
            if (currentDocument == null)
            {
                _view.SetPreviewVectorImage(null);
                DisposePreviewSnapshot();
                WorkspaceCoordinator?.UpdateCanvasVisualState();
                return false;
            }

            Rect preferredViewportRect = PreviewSnapshot?.ProjectionRect ?? default;
            PreviewSnapshot snapshot = null;
            bool builtSnapshot = currentDocument.DocumentModel != null &&
                                 string.IsNullOrWhiteSpace(currentDocument.DocumentModelLoadError) &&
                                 string.Equals(currentDocument.DocumentModel.SourceText, currentDocument.WorkingSourceText, StringComparison.Ordinal) &&
                                 _previewSnapshotBuilder.TryBuildSnapshot(
                                     currentDocument.DocumentModel,
                                     preferredViewportRect,
                                     out snapshot,
                                     out _);

            if (!builtSnapshot)
            {
                if (!keepExistingPreviewOnFailure)
                {
                    _view.SetPreviewVectorImage(null);
                    DisposePreviewSnapshot();
                    WorkspaceCoordinator?.UpdateCanvasVisualState();
                }

                return false;
            }

            ReplacePreviewSnapshot(snapshot);
            return true;
        }

        public bool TryRefreshTransientPreview(SvgDocumentModel documentModel)
        {
            if (_view.PreviewImage == null || documentModel == null)
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
                    documentModel,
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
            _view.SetPreviewVectorImage(null);
            DisposePreviewSnapshot();
            PreviewSnapshot = snapshot;
            _view.SetPreviewVectorImage(snapshot?.PreviewVectorImage);
            WorkspaceCoordinator?.SyncCanvasFrameToPreview();
            WorkspaceCoordinator?.UpdateCanvasVisualState();
        }

        private void DisposePreviewSnapshot(bool immediate = false)
        {
            if (PreviewSnapshot == null)
                return;

            if (immediate)
            {
                PreviewSnapshot.Dispose();
                PreviewSnapshot = null;
                return;
            }

            _pendingDisposals.Add(new PendingPreviewDisposal
            {
                Snapshot = PreviewSnapshot,
                RemainingTicks = PreviewDisposeDelayTicks
            });
            PreviewSnapshot = null;
            SchedulePendingDisposals();
        }

        private void SchedulePendingDisposals()
        {
            _disposeGate.Schedule();
        }

        private void FlushPendingDisposals()
        {
            _disposeGate.Cancel();

            for (int index = _pendingDisposals.Count - 1; index >= 0; index--)
            {
                PendingPreviewDisposal pending = _pendingDisposals[index];
                if (pending == null)
                {
                    _pendingDisposals.RemoveAt(index);
                    continue;
                }

                pending.RemainingTicks--;
                if (pending.RemainingTicks > 0)
                    continue;

                pending.Snapshot?.Dispose();
                _pendingDisposals.RemoveAt(index);
            }

            if (_pendingDisposals.Count > 0)
                SchedulePendingDisposals();
        }
    }
}
