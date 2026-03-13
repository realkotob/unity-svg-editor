using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.DocumentModel;
using SvgEditor.Document;
using SvgEditor.Shared;
using SvgEditor.Workspace.Transforms;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class ElementDragMutationService
    {
        private readonly SceneProjector _sceneProjector;

        public ElementDragMutationService(SceneProjector sceneProjector)
        {
            _sceneProjector = sceneProjector;
        }

        public bool TryUpdateMoveTransientState(
            ICanvasPointerDragHost host,
            ElementDragState state,
            TransientDocumentSession transientDocumentModelSession,
            Vector2 viewportDelta,
            bool axisLock,
            bool snapEnabled)
        {
            if (host.CurrentDocument == null || string.IsNullOrWhiteSpace(state.ElementKey))
            {
                return false;
            }

            if (!_sceneProjector.TryViewportDeltaToScene(
                    state.StartProjectionSceneRect,
                    state.StartPreserveAspectRatioMode,
                    viewportDelta,
                    out Vector2 sceneDelta))
            {
                return false;
            }

            if (axisLock)
            {
                sceneDelta = Mathf.Abs(sceneDelta.x) >= Mathf.Abs(sceneDelta.y)
                    ? new Vector2(sceneDelta.x, 0f)
                    : new Vector2(0f, sceneDelta.y);
            }

            Rect movedSceneRect = new(
                state.StartElementSceneRect.xMin + sceneDelta.x,
                state.StartElementSceneRect.yMin + sceneDelta.y,
                state.StartElementSceneRect.width,
                state.StartElementSceneRect.height);
            if (snapEnabled)
            {
                movedSceneRect = SnapUtility.SnapRect(movedSceneRect, snapPosition: true, snapSize: false);
                sceneDelta = movedSceneRect.position - state.StartElementSceneRect.position;
            }

            if (host.PreviewSnapshot != null &&
                _sceneProjector.TrySceneRectToViewportRect(host.PreviewSnapshot, movedSceneRect, out Rect viewportRect))
            {
                state.CurrentSelectionViewportRect = viewportRect;
            }

            if (state.MoveTargets != null && state.MoveTargets.Count > 1)
            {
                return TryApplyMultiMoveTransientPreview(host, state.MoveTargets, sceneDelta);
            }

            Vector2 svgTranslateDelta = state.StartParentWorldTransform.Inverse().MultiplyVector(sceneDelta);
            return TryApplyTransientPreview(
                host,
                transientDocumentModelSession,
                session => session.TryApplyTranslation(svgTranslateDelta));
        }

        public bool TryUpdateRotateTransientState(
            ICanvasPointerDragHost host,
            ElementDragState state,
            ElementRotationSession rotationSession,
            Vector2 localPosition,
            bool snapEnabled)
        {
            if (host.CurrentDocument == null ||
                string.IsNullOrWhiteSpace(state.ElementKey) ||
                state.StartRotateVector.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Vector2 currentRotateVector = localPosition - state.StartRotationPivotViewport;
            if (currentRotateVector.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            float rotationAngle = Vector2.SignedAngle(state.StartRotateVector, currentRotateVector);
            state.CurrentRotationAngle = snapEnabled
                ? SnapUtility.SnapAngle(rotationAngle)
                : rotationAngle;

            if (state.MoveTargets != null && state.MoveTargets.Count > 1)
            {
                return TryApplyMultiRotateTransientPreview(host, state.MoveTargets, state.CurrentRotationAngle, state.StartRotationPivotWorld);
            }

            if (!rotationSession.TryBuildPreview(state.CurrentRotationAngle, out SvgDocumentModel previewDocumentModel, out _) ||
                !host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }

        public bool TryUpdateResizeTransientState(
            ICanvasPointerDragHost host,
            ElementDragState state,
            TransientDocumentSession transientDocumentModelSession,
            SelectionHandle activeHandle,
            bool snapEnabled)
        {
            if (host.CurrentDocument == null || string.IsNullOrWhiteSpace(state.ElementKey))
            {
                return false;
            }

            if (snapEnabled && host.PreviewSnapshot != null)
            {
                Rect snappedSceneRect = SnapUtility.SnapRect(
                    _sceneProjector.BuildScaledSceneRect(
                        state.StartSelectionViewportRect,
                        state.StartElementSceneRect,
                        state.CurrentSelectionViewportRect,
                        activeHandle,
                        state.ResizeCenterAnchor));
                if (_sceneProjector.TrySceneRectToViewportRect(host.PreviewSnapshot, snappedSceneRect, out Rect snappedViewportRect))
                {
                    state.CurrentSelectionViewportRect = snappedViewportRect;
                }

                if (!_sceneProjector.TryBuildScaleTransformFromSceneRect(
                        state.StartElementSceneRect,
                        snappedSceneRect,
                        activeHandle,
                        state.ResizeCenterAnchor,
                        out Vector2 scale,
                        out Vector2 pivot))
                {
                    return false;
                }

                Vector2 svgPivot = ElementRotationUtility.ToParentSpacePoint(state.StartParentWorldTransform, pivot);
                if (state.MoveTargets != null && state.MoveTargets.Count > 1)
                {
                    return TryApplyMultiScaleTransientPreview(host, state.MoveTargets, scale, pivot);
                }

                return TryApplyTransientPreview(
                    host,
                    transientDocumentModelSession,
                    session => session.TryApplyScale(scale, svgPivot));
            }

            if (!_sceneProjector.TryBuildScaleTransform(
                    state.StartSelectionViewportRect,
                    state.StartElementSceneRect,
                    state.CurrentSelectionViewportRect,
                    activeHandle,
                    state.ResizeCenterAnchor,
                    out Vector2 unsnappedScale,
                    out Vector2 unsnappedPivot))
            {
                return false;
            }

            Vector2 unsnappedSvgPivot = ElementRotationUtility.ToParentSpacePoint(state.StartParentWorldTransform, unsnappedPivot);
            if (state.MoveTargets != null && state.MoveTargets.Count > 1)
            {
                return TryApplyMultiScaleTransientPreview(host, state.MoveTargets, unsnappedScale, unsnappedPivot);
            }

            return TryApplyTransientPreview(
                host,
                transientDocumentModelSession,
                session => session.TryApplyScale(unsnappedScale, unsnappedSvgPivot));
        }

        public bool TryCommitDrag(CommitDragContext context)
        {
            CommitDragRequest request = context.Request;
            ElementDragState state = context.State;
            if (string.IsNullOrWhiteSpace(state.ElementKey))
            {
                return false;
            }

            if (request.DragMode == DragMode.RotateElement)
            {
                if (Mathf.Approximately(state.CurrentRotationAngle, 0f))
                {
                    return false;
                }
            }
            else if (request.CanvasDelta.sqrMagnitude < 4f)
            {
                return false;
            }

            if (request.DragMode == DragMode.MoveElement &&
                (!_sceneProjector.TryViewportDeltaToScene(
                     state.StartProjectionSceneRect,
                     state.StartPreserveAspectRatioMode,
                     request.CanvasDelta,
                     out Vector2 sceneDelta) ||
                 sceneDelta.sqrMagnitude <= Mathf.Epsilon))
            {
                return false;
            }

            if (request.Host.CurrentDocument == null)
            {
                return false;
            }

            if (request.DragMode == DragMode.MoveElement &&
                state.MoveTargets != null &&
                state.MoveTargets.Count > 1)
            {
                string multiError = string.Empty;
                if (!_sceneProjector.TryViewportDeltaToScene(
                        state.StartProjectionSceneRect,
                        state.StartPreserveAspectRatioMode,
                        request.CanvasDelta,
                        out Vector2 multiSceneDelta) ||
                    multiSceneDelta.sqrMagnitude <= Mathf.Epsilon ||
                    !TryBuildMultiMoveCommittedSource(request.Host.CurrentDocument, state.MoveTargets, multiSceneDelta, out string multiUpdatedSource, out multiError))
                {
                    if (!string.IsNullOrWhiteSpace(multiError))
                    {
                        request.Host.UpdateSourceStatus($"Drag commit failed: {multiError}");
                    }

                    return false;
                }

                request.Host.ApplyUpdatedSource(multiUpdatedSource, BuildSuccessStatus(request.Host, state, request.DragMode));
                return true;
            }

            if (request.DragMode == DragMode.RotateElement &&
                state.MoveTargets != null &&
                state.MoveTargets.Count > 1)
            {
                string multiError = string.Empty;
                if (!TryBuildMultiRotateCommittedSource(
                        request.Host.CurrentDocument,
                        state.MoveTargets,
                        state.CurrentRotationAngle,
                        state.StartRotationPivotWorld,
                        out string multiUpdatedSource,
                        out multiError))
                {
                    if (!string.IsNullOrWhiteSpace(multiError))
                    {
                        request.Host.UpdateSourceStatus($"Drag commit failed: {multiError}");
                    }

                    return false;
                }

                request.Host.ApplyUpdatedSource(multiUpdatedSource, BuildSuccessStatus(request.Host, state, request.DragMode));
                return true;
            }

            if (request.DragMode == DragMode.ResizeElement &&
                state.MoveTargets != null &&
                state.MoveTargets.Count > 1)
            {
                string multiError = string.Empty;
                Rect currentSceneRect = _sceneProjector.BuildScaledSceneRect(
                    state.StartSelectionViewportRect,
                    state.StartElementSceneRect,
                    state.CurrentSelectionViewportRect,
                    state.ActiveResizeHandle,
                    state.ResizeCenterAnchor);
                if (!_sceneProjector.TryBuildScaleTransformFromSceneRect(
                        state.StartElementSceneRect,
                        currentSceneRect,
                        state.ActiveResizeHandle,
                        state.ResizeCenterAnchor,
                        out Vector2 scale,
                        out Vector2 pivotWorld) ||
                    !TryBuildMultiScaleCommittedSource(
                        request.Host.CurrentDocument,
                        state.MoveTargets,
                        scale,
                        pivotWorld,
                        out string multiUpdatedSource,
                        out multiError))
                {
                    if (!string.IsNullOrWhiteSpace(multiError))
                    {
                        request.Host.UpdateSourceStatus($"Drag commit failed: {multiError}");
                    }

                    return false;
                }

                request.Host.ApplyUpdatedSource(multiUpdatedSource, BuildSuccessStatus(request.Host, state, request.DragMode));
                return true;
            }

            string updatedSource;
            string error;
            bool builtSource = request.DragMode == DragMode.RotateElement
                ? context.RotationSession.TryBuildCommittedSource(out updatedSource, out error)
                : context.TransientDocumentModelSession.TryBuildCommittedSource(out updatedSource, out error);
            if (!builtSource)
            {
                request.Host.UpdateSourceStatus(
                    string.IsNullOrWhiteSpace(error)
                        ? "Drag commit failed: transient model state is unavailable."
                        : $"Drag commit failed: {error}");
                return false;
            }

            request.Host.ApplyUpdatedSource(updatedSource, BuildSuccessStatus(request.Host, state, request.DragMode));
            return true;
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
            SvgDocumentModelMutationService mutationService = new();
            return mutationService.TryPrependElementTranslation(
                request.CurrentDocument.DocumentModel,
                new TranslateElementRequest(request.ElementKey, parentDelta),
                out MutationResult result) &&
                !string.IsNullOrWhiteSpace(updatedSource = result.UpdatedSourceText);
        }

        private static string BuildSuccessStatus(ICanvasPointerDragHost host, ElementDragState state, DragMode dragMode)
        {
            if (dragMode == DragMode.MoveElement &&
                state.MoveTargets != null &&
                state.MoveTargets.Count > 1)
            {
                return $"Moved {state.MoveTargets.Count} elements.";
            }

            string tagName = host.FindHierarchyNode(state.ElementKey)?.TagName ?? "element";
            return dragMode switch
            {
                DragMode.ResizeElement => $"Resized <{tagName}>.",
                DragMode.RotateElement => $"Rotated <{tagName}>.",
                _ => $"Moved <{tagName}>."
            };
        }

        private bool TryApplyMultiMoveTransientPreview(
            ICanvasPointerDragHost host,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            Vector2 sceneDelta)
        {
            if (!TryBuildMultiMoveDocumentModel(host.CurrentDocument, moveTargets, sceneDelta, out SvgDocumentModel previewDocumentModel, out _))
            {
                return false;
            }

            if (!host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }

        private bool TryBuildMultiMoveCommittedSource(
            DocumentSession currentDocument,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            Vector2 sceneDelta,
            out string updatedSource,
            out string error)
        {
            updatedSource = string.Empty;
            error = string.Empty;

            if (!TryBuildMultiMoveDocumentModel(currentDocument, moveTargets, sceneDelta, out SvgDocumentModel updatedDocumentModel, out error))
            {
                return false;
            }

            updatedSource = updatedDocumentModel?.SourceText ?? string.Empty;
            return !string.IsNullOrWhiteSpace(updatedSource);
        }

        private bool TryBuildMultiMoveDocumentModel(
            DocumentSession currentDocument,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            Vector2 sceneDelta,
            out SvgDocumentModel updatedDocumentModel,
            out string error)
        {
            updatedDocumentModel = null;
            error = string.Empty;

            if (currentDocument?.DocumentModel == null || moveTargets == null || moveTargets.Count == 0)
            {
                error = "Transient document model is unavailable.";
                return false;
            }

            SvgDocumentModelMutationService mutationService = new();
            SvgDocumentModel workingDocumentModel = currentDocument.DocumentModel;
            foreach (ElementMoveTarget moveTarget in moveTargets)
            {
                if (string.IsNullOrWhiteSpace(moveTarget.ElementKey))
                {
                    continue;
                }

                Vector2 parentTranslateDelta = moveTarget.ParentWorldTransform.Inverse().MultiplyVector(sceneDelta);
                if (!mutationService.TryPrependElementTranslation(
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

        private bool TryApplyMultiRotateTransientPreview(
            ICanvasPointerDragHost host,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            float angle,
            Vector2 pivotWorld)
        {
            if (!TryBuildMultiRotateDocumentModel(host.CurrentDocument, moveTargets, angle, pivotWorld, out SvgDocumentModel previewDocumentModel, out _))
            {
                return false;
            }

            if (!host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }

        private bool TryBuildMultiRotateCommittedSource(
            DocumentSession currentDocument,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            float angle,
            Vector2 pivotWorld,
            out string updatedSource,
            out string error)
        {
            updatedSource = string.Empty;
            error = string.Empty;

            if (!TryBuildMultiRotateDocumentModel(currentDocument, moveTargets, angle, pivotWorld, out SvgDocumentModel updatedDocumentModel, out error))
            {
                return false;
            }

            updatedSource = updatedDocumentModel?.SourceText ?? string.Empty;
            return !string.IsNullOrWhiteSpace(updatedSource);
        }

        private bool TryBuildMultiRotateDocumentModel(
            DocumentSession currentDocument,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            float angle,
            Vector2 pivotWorld,
            out SvgDocumentModel updatedDocumentModel,
            out string error)
        {
            updatedDocumentModel = null;
            error = string.Empty;

            if (currentDocument?.DocumentModel == null || moveTargets == null || moveTargets.Count == 0)
            {
                error = "Rotation session is unavailable.";
                return false;
            }

            SvgDocumentModelMutationService mutationService = new();
            SvgDocumentModel workingDocumentModel = currentDocument.DocumentModel;
            foreach (ElementMoveTarget moveTarget in moveTargets)
            {
                if (string.IsNullOrWhiteSpace(moveTarget.ElementKey))
                {
                    continue;
                }

                Vector2 parentPivot = ElementRotationUtility.ToParentSpacePoint(moveTarget.ParentWorldTransform, pivotWorld);
                if (!mutationService.TryPrependElementRotation(
                        workingDocumentModel,
                        new RotateElementRequest(moveTarget.ElementKey, angle, parentPivot),
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

        private bool TryApplyMultiScaleTransientPreview(
            ICanvasPointerDragHost host,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            Vector2 scale,
            Vector2 pivotWorld)
        {
            if (!TryBuildMultiScaleDocumentModel(host.CurrentDocument, moveTargets, scale, pivotWorld, out SvgDocumentModel previewDocumentModel, out _))
            {
                return false;
            }

            if (!host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }

        private bool TryBuildMultiScaleCommittedSource(
            DocumentSession currentDocument,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            Vector2 scale,
            Vector2 pivotWorld,
            out string updatedSource,
            out string error)
        {
            updatedSource = string.Empty;
            error = string.Empty;

            if (!TryBuildMultiScaleDocumentModel(currentDocument, moveTargets, scale, pivotWorld, out SvgDocumentModel updatedDocumentModel, out error))
            {
                return false;
            }

            updatedSource = updatedDocumentModel?.SourceText ?? string.Empty;
            return !string.IsNullOrWhiteSpace(updatedSource);
        }

        private bool TryBuildMultiScaleDocumentModel(
            DocumentSession currentDocument,
            IReadOnlyList<ElementMoveTarget> moveTargets,
            Vector2 scale,
            Vector2 pivotWorld,
            out SvgDocumentModel updatedDocumentModel,
            out string error)
        {
            updatedDocumentModel = null;
            error = string.Empty;

            if (currentDocument?.DocumentModel == null || moveTargets == null || moveTargets.Count == 0)
            {
                error = "Resize session is unavailable.";
                return false;
            }

            SvgDocumentModelMutationService mutationService = new();
            SvgDocumentModel workingDocumentModel = currentDocument.DocumentModel;
            foreach (ElementMoveTarget moveTarget in moveTargets)
            {
                if (string.IsNullOrWhiteSpace(moveTarget.ElementKey))
                {
                    continue;
                }

                Vector2 parentPivot = ElementRotationUtility.ToParentSpacePoint(moveTarget.ParentWorldTransform, pivotWorld);
                if (!mutationService.TryPrependElementScale(
                        workingDocumentModel,
                        new ScaleElementRequest(moveTarget.ElementKey, scale, parentPivot),
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

        private static bool TryApplyTransientPreview(
            ICanvasPointerDragHost host,
            TransientDocumentSession transientDocumentModelSession,
            Func<TransientDocumentSession, bool> applyMutation)
        {
            if (!applyMutation(transientDocumentModelSession) ||
                !transientDocumentModelSession.TryBuildPreviewDocumentModel(out SvgDocumentModel previewDocumentModel, out _) ||
                !host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }
    }
}
