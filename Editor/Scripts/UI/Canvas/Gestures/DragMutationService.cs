using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Shared;
using SvgEditor.UI.Workspace.Transforms;
using Core.UI.Extensions;

namespace SvgEditor.UI.Canvas
{
    internal sealed class DragMutationService
    {
        private readonly DragMoveMutationPipeline _movePipeline;
        private readonly DragRotateMutationPipeline _rotatePipeline;
        private readonly DragResizeMutationPipeline _resizePipeline;

        public DragMutationService(SceneProjector sceneProjector)
        {
            _movePipeline = new DragMoveMutationPipeline(sceneProjector);
            _rotatePipeline = new DragRotateMutationPipeline();
            _resizePipeline = new DragResizeMutationPipeline(sceneProjector);
        }

        public bool TryUpdateMoveTransientState(
            ICanvasPointerDragHost host,
            DragSelectionState selection,
            TransientDocumentSession transientDocumentModelSession,
            Vector2 viewportDelta,
            bool axisLock,
            bool snapEnabled)
        {
            return TryUpdateTransientState(
                host,
                (out SvgDocumentModel previewDocumentModel) => _movePipeline.TryBuildPreview(
                    host,
                    selection,
                    transientDocumentModelSession,
                    viewportDelta,
                    axisLock,
                    snapEnabled,
                    out previewDocumentModel));
        }

        public bool TryUpdateRotateTransientState(
            ICanvasPointerDragHost host,
            DragSelectionState selection,
            DragRotationState rotation,
            ElementRotationSession rotationSession,
            Vector2 localPosition,
            bool snapEnabled)
        {
            return TryUpdateTransientState(
                host,
                (out SvgDocumentModel previewDocumentModel) => _rotatePipeline.TryBuildPreview(
                    host,
                    selection,
                    rotation,
                    rotationSession,
                    localPosition,
                    snapEnabled,
                    out previewDocumentModel));
        }

        public bool TryUpdateResizeTransientState(
            ICanvasPointerDragHost host,
            DragSelectionState selection,
            TransientDocumentSession transientDocumentModelSession,
            SelectionHandle activeHandle,
            bool snapEnabled)
        {
            return TryUpdateTransientState(
                host,
                (out SvgDocumentModel previewDocumentModel) => _resizePipeline.TryBuildPreview(
                    host,
                    selection,
                    transientDocumentModelSession,
                    activeHandle,
                    snapEnabled,
                    out previewDocumentModel));
        }

        public Result<Unit> CommitMove(
            CommitDragRequest request,
            DragSelectionState selection,
            TransientDocumentSession transientDocumentModelSession)
        {
            if (request.CanvasDelta.sqrMagnitude < 4f)
            {
                return Result.Failure<Unit>("Canvas drag delta is too small.");
            }

            if (!_movePipeline.TryResolveCommitSceneDelta(selection, request.CanvasDelta, out Vector2 sceneDelta) ||
                sceneDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return Result.Failure<Unit>("Canvas scene delta is unavailable.");
            }

            if (request.Host.CurrentDocument == null)
            {
                return Result.Failure<Unit>("Canvas document is unavailable.");
            }

            return CommitMutation(
                request,
                selection,
                transientDocumentModelSession.TryBuildCommittedSource,
                (out string updatedSource, out string error) => _movePipeline.TryBuildCommittedSource(
                    request.Host.CurrentDocument,
                    selection,
                    sceneDelta,
                    out updatedSource,
                    out error));
        }

        public Result<Unit> CommitRotate(
            CommitDragRequest request,
            DragSelectionState selection,
            DragRotationState rotation,
            ElementRotationSession rotationSession)
        {
            if (Mathf.Approximately(rotation.CurrentAngle, 0f))
            {
                return Result.Failure<Unit>("Canvas rotation delta is too small.");
            }

            if (request.Host.CurrentDocument == null)
            {
                return Result.Failure<Unit>("Canvas document is unavailable.");
            }

            return CommitMutation(
                request,
                selection,
                rotationSession.TryBuildCommittedSource,
                (out string updatedSource, out string error) => _rotatePipeline.TryBuildCommittedSource(
                    request.Host.CurrentDocument,
                    selection,
                    rotation,
                    out updatedSource,
                    out error));
        }

        public Result<Unit> CommitResize(
            CommitDragRequest request,
            DragSelectionState selection,
            TransientDocumentSession transientDocumentModelSession)
        {
            if (request.CanvasDelta.sqrMagnitude < 4f)
            {
                return Result.Failure<Unit>("Canvas drag delta is too small.");
            }

            if (request.Host.CurrentDocument == null)
            {
                return Result.Failure<Unit>("Canvas document is unavailable.");
            }

            return CommitMutation(
                request,
                selection,
                transientDocumentModelSession.TryBuildCommittedSource,
                (out string updatedSource, out string error) => _resizePipeline.TryBuildCommittedSource(
                    request.Host.CurrentDocument,
                    selection,
                    out updatedSource,
                    out error));
        }

        public bool TryBuildNudgedSource(
            NudgeSourceRequest request,
            out string updatedSource)
        {
            updatedSource = string.Empty;
            if (request.CurrentDocument?.DocumentModel == null ||
                string.IsNullOrWhiteSpace(request.ElementKey) ||
                request.SceneDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Vector2 parentDelta = request.ParentWorldTransform.Inverse().MultiplyVector(request.SceneDelta);
            SvgMutator svgMutator = new();
            return svgMutator.TryPrependElementTranslation(
                request.CurrentDocument.DocumentModel,
                new TranslateElementRequest(request.ElementKey, parentDelta),
                out MutationResult result) &&
                !string.IsNullOrWhiteSpace(updatedSource = result.UpdatedSourceText);
        }

        private static string BuildSuccessStatus(ICanvasPointerDragHost host, DragSelectionState selection, DragMode dragMode)
        {
            if (dragMode == DragMode.MoveElement && HasGroupTargets(selection))
            {
                return $"Moved {selection.MoveTargets.Count} elements.";
            }

            string tagName = host.FindHierarchyNode(selection.ElementKey)?.TagName ?? "element";
            return dragMode switch
            {
                DragMode.ResizeElement => $"Resized <{tagName}>.",
                DragMode.RotateElement => $"Rotated <{tagName}>.",
                _ => $"Moved <{tagName}>."
            };
        }

        private static Result<Unit> CommitBuiltSource(
            CommitDragRequest request,
            DragSelectionState selection,
            TryBuildCommittedSource buildCommittedSource)
        {
            if (!buildCommittedSource(out string updatedSource, out string error))
            {
                return ReportCommitFailure(request.Host, error);
            }

            request.Host.ApplyUpdatedSource(updatedSource, BuildSuccessStatus(request.Host, selection, request.DragMode));
            return Result.Success(Unit.Default);
        }

        private static Result<Unit> CommitMutation(
            CommitDragRequest request,
            DragSelectionState selection,
            TryBuildCommittedSource singleSource,
            TryBuildCommittedSource groupSource)
        {
            return CommitBuiltSource(
                request,
                selection,
                HasGroupTargets(selection) ? groupSource : singleSource);
        }

        private static Result<Unit> ReportCommitFailure(ICanvasPointerDragHost host, string error)
        {
            host.UpdateSourceStatus(
                string.IsNullOrWhiteSpace(error)
                    ? "Drag commit failed: transient model state is unavailable."
                    : $"Drag commit failed: {error}");
            return Result.Failure<Unit>(error);
        }

        private static bool HasGroupTargets(DragSelectionState selection)
        {
            return selection.MoveTargets != null && selection.MoveTargets.Count > 1;
        }

        private delegate bool TryBuildPreviewDocument(out SvgDocumentModel previewDocumentModel);
        private delegate bool TryBuildCommittedSource(out string updatedSource, out string error);

        private static bool TryUpdateTransientState(
            ICanvasPointerDragHost host,
            TryBuildPreviewDocument buildPreview)
        {
            return buildPreview(out SvgDocumentModel previewDocumentModel) &&
                   TryApplyPreview(host, previewDocumentModel);
        }

        private static bool TryApplyPreview(
            ICanvasPointerDragHost host,
            SvgDocumentModel previewDocumentModel)
        {
            if (!host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }
    }
}
