using UnityEngine;
using Unity.VectorGraphics;
using SvgEditor.Document;
using SvgEditor.Shared;
using SvgEditor.Workspace.Transforms;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class ElementDragController
    {
        private readonly SceneProjector _sceneProjector;
        private readonly ElementDragMutationService _mutationService;
        private readonly ElementMoveSession _moveSession = new();
        private readonly TransientDocumentSession _transientDocumentModelSession = new();
        private readonly ElementRotationSession _rotationSession = new();
        private readonly ElementDragState _state = new();

        public Rect DragCurrentSelectionViewportRect => _state.CurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _state.StartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _state.StartSelectionViewportRect;
        public Rect DragStartProjectionSceneRect => _state.StartProjectionSceneRect;
        public SvgPreserveAspectRatioMode DragStartPreserveAspectRatioMode => _state.StartPreserveAspectRatioMode;
        public bool DragResizeCenterAnchor => _state.ResizeCenterAnchor;
        public string DragElementKey => _state.ElementKey;
        public bool IsGroupMove => _state.MoveTargets != null && _state.MoveTargets.Count > 1;

        public ElementDragController(SceneProjector sceneProjector)
        {
            _sceneProjector = sceneProjector;
            _mutationService = new ElementDragMutationService(sceneProjector);
        }

        public void BeginMove(ElementDragBeginRequest request)
        {
            Rect selectionViewportRect = BeginSelection(request);
            _moveSession.Begin(request.ElementKey, request.LocalPosition, selectionViewportRect, request.ElementSceneRect);
            if (!IsGroupMove)
            {
                _transientDocumentModelSession.TryBegin(request.CurrentDocument, request.ElementKey);
            }
        }

        public void BeginResize(ResizeBeginRequest request)
        {
            _state.BeginSelection(
                request.ElementKey,
                request.ProjectionSceneRect,
                request.PreserveAspectRatioMode,
                request.SelectionViewportRect,
                request.SelectionSceneRect,
                request.ParentWorldTransform);
            _transientDocumentModelSession.TryBegin(request.CurrentDocument, request.ElementKey);
        }

        public void BeginRotate(RotateBeginRequest request)
        {
            ElementDragBeginRequest dragBeginRequest = request.DragBeginRequest;
            Rect selectionViewportRect = BeginSelection(dragBeginRequest);
            Vector2 pivotViewport = _sceneProjector.TryScenePointToViewportPoint(dragBeginRequest.PreviewSnapshot, request.RotationPivotWorld, out Vector2 resolvedPivotViewport)
                ? resolvedPivotViewport
                : selectionViewportRect.center;
            _state.BeginRotation(pivotViewport, dragBeginRequest.LocalPosition - pivotViewport);
            _rotationSession.TryBegin(dragBeginRequest.CurrentDocument, dragBeginRequest.ElementKey, request.RotationPivotParentSpace);
        }

        public Vector2 UpdateMove(Vector2 localPosition)
        {
            Vector2 viewportDelta = _moveSession.Update(localPosition);
            _state.CurrentSelectionViewportRect = _moveSession.CurrentSelectionViewportRect;
            return viewportDelta;
        }

        public void UpdateResize(Vector2 viewportDelta, SelectionHandle activeHandle, bool uniformScale, bool centerAnchor)
        {
            _state.ResizeCenterAnchor = centerAnchor;
            Rect resizedViewportRect = RectResizeUtility.ResizeRect(
                _state.StartSelectionViewportRect,
                activeHandle,
                viewportDelta,
                12f);
            _state.CurrentSelectionViewportRect = CanvasProjectionMath.GetResizeViewportRect(
                _state.StartSelectionViewportRect,
                resizedViewportRect,
                activeHandle,
                uniformScale,
                centerAnchor);
        }

        public Rect BuildScaledSceneRect(SelectionHandle handle)
        {
            return _sceneProjector.BuildScaledSceneRect(
                _state.StartSelectionViewportRect,
                _state.StartElementSceneRect,
                _state.CurrentSelectionViewportRect,
                handle,
                _state.ResizeCenterAnchor);
        }

        public void End()
        {
            _state.Reset();
            _transientDocumentModelSession.End();
            _rotationSession.End();
            _moveSession.End();
        }

        public bool TryUpdateMoveTransientState(
            ICanvasPointerDragHost host,
            Vector2 viewportDelta,
            bool axisLock,
            bool snapEnabled)
        {
            return _mutationService.TryUpdateMoveTransientState(
                host,
                _state,
                _transientDocumentModelSession,
                viewportDelta,
                axisLock,
                snapEnabled);
        }

        public bool TryUpdateRotateTransientState(
            ICanvasPointerDragHost host,
            Vector2 localPosition,
            bool snapEnabled)
        {
            return _mutationService.TryUpdateRotateTransientState(
                host,
                _state,
                _rotationSession,
                localPosition,
                snapEnabled);
        }

        public bool TryUpdateResizeTransientState(
            ICanvasPointerDragHost host,
            SelectionHandle activeHandle,
            bool snapEnabled)
        {
            return _mutationService.TryUpdateResizeTransientState(
                host,
                _state,
                _transientDocumentModelSession,
                activeHandle,
                snapEnabled);
        }

        public bool TryCommitDrag(CommitDragRequest request)
        {
            return _mutationService.TryCommitDrag(
                new CommitDragContext(
                    request,
                    _state,
                    _transientDocumentModelSession,
                    _rotationSession));
        }

        public bool TryBuildNudgedSource(
            NudgeSourceRequest request,
            out string updatedSource)
        {
            return _mutationService.TryBuildNudgedSource(request, out updatedSource);
        }

        private Rect BeginSelection(ElementDragBeginRequest request)
        {
            Rect selectionViewportRect = _sceneProjector.TrySceneRectToViewportRect(request.PreviewSnapshot, request.ElementSceneRect, out Rect resolvedSelectionViewportRect)
                ? resolvedSelectionViewportRect
                : default;
            _state.BeginSelection(
                request.ElementKey,
                request.PreviewSnapshot?.CanvasViewportRect ?? default,
                request.PreviewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
                selectionViewportRect,
                request.ElementSceneRect,
                request.ParentWorldTransform);
            _state.SetMoveTargets(request.MoveTargets);
            return selectionViewportRect;
        }
    }
}
