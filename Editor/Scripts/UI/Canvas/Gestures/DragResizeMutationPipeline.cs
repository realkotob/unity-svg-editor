using System.Collections.Generic;
using UnityEngine;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Shared;
using SvgEditor.UI.Workspace.Transforms;

namespace SvgEditor.UI.Canvas
{
    internal sealed class DragResizeMutationPipeline
    {
        private readonly SceneProjector _sceneProjector;

        public DragResizeMutationPipeline(SceneProjector sceneProjector)
        {
            _sceneProjector = sceneProjector;
        }

        public bool TryBuildPreview(
            ICanvasPointerDragHost host,
            DragSelectionState selection,
            TransientDocumentSession transientDocumentModelSession,
            SelectionHandle activeHandle,
            bool snapEnabled,
            out SvgDocumentModel previewDocumentModel)
        {
            PreviewMutation preview = new(
                _sceneProjector,
                host,
                selection,
                transientDocumentModelSession,
                activeHandle,
                snapEnabled);
            return preview.TryBuild(out previewDocumentModel);
        }

        public bool TryBuildCommittedSource(
            DocumentSession currentDocument,
            DragSelectionState selection,
            out string updatedSource,
            out string error)
        {
            return new CommitMutation(_sceneProjector, currentDocument, selection)
                .TryBuildSource(out updatedSource, out error);
        }

        private static bool HasGroupTargets(DragSelectionState selection)
        {
            return selection.MoveTargets != null && selection.MoveTargets.Count > 1;
        }

        private sealed class PreviewMutation
        {
            private readonly SceneProjector _sceneProjector;
            private readonly ICanvasPointerDragHost _host;
            private readonly DragSelectionState _selection;
            private readonly TransientDocumentSession _transientDocumentModelSession;
            private readonly SelectionHandle _activeHandle;
            private readonly bool _snapEnabled;
            private Vector2 _scale;
            private Vector2 _pivotWorld;

            public PreviewMutation(
                SceneProjector sceneProjector,
                ICanvasPointerDragHost host,
                DragSelectionState selection,
                TransientDocumentSession transientDocumentModelSession,
                SelectionHandle activeHandle,
                bool snapEnabled)
            {
                _sceneProjector = sceneProjector;
                _host = host;
                _selection = selection;
                _transientDocumentModelSession = transientDocumentModelSession;
                _activeHandle = activeHandle;
                _snapEnabled = snapEnabled;
                _scale = Vector2.one;
                _pivotWorld = Vector2.zero;
            }

            public bool TryBuild(out SvgDocumentModel previewDocumentModel)
            {
                previewDocumentModel = null;
                if (!CanBuild() || !TryResolveTransform())
                {
                    return false;
                }

                return HasGroupTargets(_selection)
                    ? new GroupResizeMutation(_host.CurrentDocument, _selection, _scale, _pivotWorld)
                        .TryBuildPreview(out previewDocumentModel)
                    : TryBuildSinglePreview(out previewDocumentModel);
            }

            private bool CanBuild()
            {
                return _host.CurrentDocument != null && !string.IsNullOrWhiteSpace(_selection.ElementKey);
            }

            private bool TryResolveTransform()
            {
                if (!_snapEnabled || _host.PreviewSnapshot == null)
                {
                    return _sceneProjector.TryBuildScaleTransform(
                        _selection.StartSelectionViewportRect,
                        _selection.StartElementSceneRect,
                        _selection.CurrentSelectionViewportRect,
                        _activeHandle,
                        _selection.ResizeCenterAnchor,
                        out _scale,
                        out _pivotWorld);
                }

                Rect snappedSceneRect = SnapUtility.SnapRect(BuildScaledSceneRect(_selection.CurrentSelectionViewportRect));
                UpdateSelectionViewportRect(snappedSceneRect);
                return _sceneProjector.TryBuildScaleTransformFromSceneRect(
                    _selection.StartElementSceneRect,
                    snappedSceneRect,
                    _activeHandle,
                    _selection.ResizeCenterAnchor,
                    out _scale,
                    out _pivotWorld);
            }

            private Rect BuildScaledSceneRect(Rect currentSelectionViewportRect)
            {
                return _sceneProjector.BuildScaledSceneRect(
                    _selection.StartSelectionViewportRect,
                    _selection.StartElementSceneRect,
                    currentSelectionViewportRect,
                    _activeHandle,
                    _selection.ResizeCenterAnchor);
            }

            private void UpdateSelectionViewportRect(Rect sceneRect)
            {
                PreviewSnapshot previewSnapshot = _host.PreviewSnapshot;
                if (previewSnapshot != null &&
                    _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, sceneRect, out Rect viewportRect))
                {
                    _selection.CurrentSelectionViewportRect = viewportRect;
                }
            }

            private bool TryBuildSinglePreview(out SvgDocumentModel previewDocumentModel)
            {
                previewDocumentModel = null;
                Vector2 svgPivot = ElementRotationUtility.ToParentSpacePoint(
                    _selection.StartParentWorldTransform,
                    _pivotWorld);
                return _transientDocumentModelSession.TryApplyScale(_scale, svgPivot) &&
                       _transientDocumentModelSession.TryBuildPreviewDocumentModel(out previewDocumentModel, out _);
            }
        }

        private sealed class CommitMutation
        {
            private readonly SceneProjector _sceneProjector;
            private readonly DocumentSession _currentDocument;
            private readonly DragSelectionState _selection;

            public CommitMutation(
                SceneProjector sceneProjector,
                DocumentSession currentDocument,
                DragSelectionState selection)
            {
                _sceneProjector = sceneProjector;
                _currentDocument = currentDocument;
                _selection = selection;
            }

            public bool TryBuildSource(out string updatedSource, out string error)
            {
                updatedSource = string.Empty;
                error = string.Empty;
                if (!TryResolveTransform(out Vector2 scale, out Vector2 pivotWorld))
                {
                    return false;
                }

                return new GroupResizeMutation(_currentDocument, _selection, scale, pivotWorld)
                    .TryBuildSource(out updatedSource, out error);
            }

            private bool TryResolveTransform(out Vector2 scale, out Vector2 pivotWorld)
            {
                Rect currentSceneRect = _sceneProjector.BuildScaledSceneRect(
                    _selection.StartSelectionViewportRect,
                    _selection.StartElementSceneRect,
                    _selection.CurrentSelectionViewportRect,
                    _selection.ActiveResizeHandle,
                    _selection.ResizeCenterAnchor);
                return _sceneProjector.TryBuildScaleTransformFromSceneRect(
                    _selection.StartElementSceneRect,
                    currentSceneRect,
                    _selection.ActiveResizeHandle,
                    _selection.ResizeCenterAnchor,
                    out scale,
                    out pivotWorld);
            }
        }

        private sealed class GroupResizeMutation
        {
            private readonly DocumentSession _currentDocument;
            private readonly DragSelectionState _selection;
            private readonly Vector2 _scale;
            private readonly Vector2 _pivotWorld;

            public GroupResizeMutation(
                DocumentSession currentDocument,
                DragSelectionState selection,
                Vector2 scale,
                Vector2 pivotWorld)
            {
                _currentDocument = currentDocument;
                _selection = selection;
                _scale = scale;
                _pivotWorld = pivotWorld;
            }

            public bool TryBuildPreview(out SvgDocumentModel previewDocumentModel)
            {
                return TryScaleTargets(out previewDocumentModel, out _);
            }

            public bool TryBuildSource(out string updatedSource, out string error)
            {
                updatedSource = string.Empty;
                error = string.Empty;
                if (!TryScaleTargets(out SvgDocumentModel updatedDocumentModel, out error))
                {
                    return false;
                }

                updatedSource = updatedDocumentModel?.SourceText ?? string.Empty;
                return !string.IsNullOrWhiteSpace(updatedSource);
            }

            private bool TryScaleTargets(
                out SvgDocumentModel updatedDocumentModel,
                out string error)
            {
                updatedDocumentModel = null;
                error = string.Empty;

                IReadOnlyList<ElementMoveTarget> moveTargets = _selection.MoveTargets;
                if (_currentDocument?.DocumentModel == null || moveTargets == null || moveTargets.Count == 0)
                {
                    error = "Resize session is unavailable.";
                    return false;
                }

                SvgMutator svgMutator = new();
                SvgDocumentModel workingDocumentModel = _currentDocument.DocumentModel;
                foreach (ElementMoveTarget moveTarget in moveTargets)
                {
                    if (string.IsNullOrWhiteSpace(moveTarget.ElementKey))
                    {
                        continue;
                    }

                    Vector2 parentPivot = ElementRotationUtility.ToParentSpacePoint(
                        moveTarget.ParentWorldTransform,
                        _pivotWorld);
                    if (!svgMutator.TryPrependElementScale(
                            workingDocumentModel,
                            new ScaleElementRequest(moveTarget.ElementKey, _scale, parentPivot),
                            out MutationResult result))
                    {
                        error = result.Error;
                        return false;
                    }

                    workingDocumentModel = result.UpdatedDocumentModel;
                }

                updatedDocumentModel = workingDocumentModel;
                return updatedDocumentModel != null;
            }
        }
    }
}
