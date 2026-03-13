using System;
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

            request.Host.ApplyUpdatedSource(updatedSource, BuildSuccessStatus(request.Host, state.ElementKey, request.DragMode));
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

        private static string BuildSuccessStatus(ICanvasPointerDragHost host, string elementKey, DragMode dragMode)
        {
            string tagName = host.FindHierarchyNode(elementKey)?.TagName ?? "element";
            return dragMode switch
            {
                DragMode.ResizeElement => $"Resized <{tagName}>.",
                DragMode.RotateElement => $"Rotated <{tagName}>.",
                _ => $"Moved <{tagName}>."
            };
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
