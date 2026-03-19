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
    internal sealed class DragMoveMutationPipeline
    {
        private readonly SceneProjector _sceneProjector;

        public DragMoveMutationPipeline(SceneProjector sceneProjector)
        {
            _sceneProjector = sceneProjector;
        }

        public bool TryBuildPreview(
            ICanvasPointerDragHost host,
            DragSelectionState selection,
            TransientDocumentSession transientDocumentModelSession,
            Vector2 viewportDelta,
            bool axisLock,
            bool snapEnabled,
            out SvgDocumentModel previewDocumentModel)
        {
            PreviewMutation preview = new(
                _sceneProjector,
                host,
                selection,
                transientDocumentModelSession,
                viewportDelta,
                axisLock,
                snapEnabled);
            return preview.TryBuild(out previewDocumentModel);
        }

        public bool TryBuildCommittedSource(
            DocumentSession currentDocument,
            DragSelectionState selection,
            Vector2 sceneDelta,
            out string updatedSource,
            out string error)
        {
            return new GroupMoveMutation(currentDocument, selection, sceneDelta)
                .TryBuildSource(out updatedSource, out error);
        }

        public bool TryResolveCommitSceneDelta(
            DragSelectionState selection,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            return _sceneProjector.TryViewportDeltaToScene(
                selection.StartProjectionSceneRect,
                selection.StartPreserveAspectRatioMode,
                viewportDelta,
                out sceneDelta);
        }

        private static Vector2 ApplyAxisLock(Vector2 sceneDelta)
        {
            return Mathf.Abs(sceneDelta.x) >= Mathf.Abs(sceneDelta.y)
                ? new Vector2(sceneDelta.x, 0f)
                : new Vector2(0f, sceneDelta.y);
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
            private readonly Vector2 _viewportDelta;
            private readonly bool _axisLock;
            private readonly bool _snapEnabled;
            private Vector2 _sceneDelta;

            public PreviewMutation(
                SceneProjector sceneProjector,
                ICanvasPointerDragHost host,
                DragSelectionState selection,
                TransientDocumentSession transientDocumentModelSession,
                Vector2 viewportDelta,
                bool axisLock,
                bool snapEnabled)
            {
                _sceneProjector = sceneProjector;
                _host = host;
                _selection = selection;
                _transientDocumentModelSession = transientDocumentModelSession;
                _viewportDelta = viewportDelta;
                _axisLock = axisLock;
                _snapEnabled = snapEnabled;
                _sceneDelta = Vector2.zero;
            }

            public bool TryBuild(out SvgDocumentModel previewDocumentModel)
            {
                previewDocumentModel = null;
                if (!CanBuild() || !TryResolveSceneDelta())
                {
                    return false;
                }

                Rect movedSceneRect = BuildMovedSceneRect();
                UpdateSelectionViewportRect(movedSceneRect);
                return HasGroupTargets(_selection)
                    ? new GroupMoveMutation(_host.CurrentDocument, _selection, _sceneDelta)
                        .TryBuildPreview(out previewDocumentModel)
                    : TryBuildSinglePreview(out previewDocumentModel);
            }

            private bool CanBuild()
            {
                return _host.CurrentDocument != null && !string.IsNullOrWhiteSpace(_selection.ElementKey);
            }

            private bool TryResolveSceneDelta()
            {
                if (!_sceneProjector.TryViewportDeltaToScene(
                        _selection.StartProjectionSceneRect,
                        _selection.StartPreserveAspectRatioMode,
                        _viewportDelta,
                        out _sceneDelta))
                {
                    return false;
                }

                if (_axisLock)
                {
                    _sceneDelta = ApplyAxisLock(_sceneDelta);
                }

                return true;
            }

            private Rect BuildMovedSceneRect()
            {
                Rect movedSceneRect = new(
                    _selection.StartElementSceneRect.xMin + _sceneDelta.x,
                    _selection.StartElementSceneRect.yMin + _sceneDelta.y,
                    _selection.StartElementSceneRect.width,
                    _selection.StartElementSceneRect.height);

                if (!_snapEnabled)
                {
                    return movedSceneRect;
                }

                Rect snappedSceneRect = SnapUtility.SnapRect(movedSceneRect, snapPosition: true, snapSize: false);
                _sceneDelta = snappedSceneRect.position - _selection.StartElementSceneRect.position;
                return snappedSceneRect;
            }

            private void UpdateSelectionViewportRect(Rect movedSceneRect)
            {
                PreviewSnapshot previewSnapshot = _host.PreviewSnapshot;
                if (previewSnapshot != null &&
                    _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, movedSceneRect, out Rect viewportRect))
                {
                    _selection.CurrentSelectionViewportRect = viewportRect;
                }
            }

            private bool TryBuildSinglePreview(out SvgDocumentModel previewDocumentModel)
            {
                previewDocumentModel = null;
                Vector2 svgTranslateDelta = _selection.StartParentWorldTransform.Inverse().MultiplyVector(_sceneDelta);
                return _transientDocumentModelSession.TryApplyTranslation(svgTranslateDelta) &&
                       _transientDocumentModelSession.TryBuildPreviewDocumentModel(out previewDocumentModel, out _);
            }
        }

        private sealed class GroupMoveMutation
        {
            private readonly DocumentSession _currentDocument;
            private readonly DragSelectionState _selection;
            private readonly Vector2 _sceneDelta;

            public GroupMoveMutation(
                DocumentSession currentDocument,
                DragSelectionState selection,
                Vector2 sceneDelta)
            {
                _currentDocument = currentDocument;
                _selection = selection;
                _sceneDelta = sceneDelta;
            }

            public bool TryBuildPreview(out SvgDocumentModel previewDocumentModel)
            {
                return TryTranslateTargets(out previewDocumentModel, out _);
            }

            public bool TryBuildSource(out string updatedSource, out string error)
            {
                updatedSource = string.Empty;
                error = string.Empty;
                if (!TryTranslateTargets(out SvgDocumentModel updatedDocumentModel, out error))
                {
                    return false;
                }

                updatedSource = updatedDocumentModel?.SourceText ?? string.Empty;
                return !string.IsNullOrWhiteSpace(updatedSource);
            }

            private bool TryTranslateTargets(
                out SvgDocumentModel updatedDocumentModel,
                out string error)
            {
                updatedDocumentModel = null;
                error = string.Empty;

                IReadOnlyList<ElementMoveTarget> moveTargets = _selection.MoveTargets;
                if (_currentDocument?.DocumentModel == null || moveTargets == null || moveTargets.Count == 0)
                {
                    error = "Transient document model is unavailable.";
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

                    Vector2 parentTranslateDelta = moveTarget.ParentWorldTransform.Inverse().MultiplyVector(_sceneDelta);
                    if (!svgMutator.TryPrependElementTranslation(
                            workingDocumentModel,
                            new TranslateElementRequest(moveTarget.ElementKey, parentTranslateDelta),
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
