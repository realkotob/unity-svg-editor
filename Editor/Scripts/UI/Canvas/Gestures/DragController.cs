using UnityEngine;
using Unity.VectorGraphics;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Shared;
using SvgEditor.UI.Workspace.Transforms;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class DragController
    {
        private readonly SceneProjector _sceneProjector;
        private readonly DragMutationService _mutationService;
        private readonly ElementMoveSession _moveSession = new();
        private readonly TransientDocumentSession _transientDocumentModelSession = new();
        private readonly ElementRotationSession _rotationSession = new();
        private readonly DragState _state = new();

        public Rect DragCurrentSelectionViewportRect => _state.Selection.CurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _state.Selection.StartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _state.Selection.StartSelectionViewportRect;
        public Rect DragStartProjectionSceneRect => _state.Selection.StartProjectionSceneRect;
        public SvgPreserveAspectRatioMode DragStartPreserveAspectRatioMode => _state.Selection.StartPreserveAspectRatioMode;
        public bool DragResizeCenterAnchor => _state.Selection.ResizeCenterAnchor;
        public string DragElementKey => _state.Selection.ElementKey;
        public bool IsGroupMove => _state.Selection.MoveTargets != null && _state.Selection.MoveTargets.Count > 1;
        public float CurrentRotationAngle => _state.Rotation.CurrentAngle;
        public Vector2 DragRotationPivotViewport => _state.Rotation.StartPivotViewport;

        public DragController(SceneProjector sceneProjector)
        {
            _sceneProjector = sceneProjector;
            _mutationService = new DragMutationService(sceneProjector);
        }

        public void BeginMove(DragBeginRequest request)
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
            _state.SetMoveTargets(request.MoveTargets);
            if (!IsGroupMove)
            {
                _transientDocumentModelSession.TryBegin(request.CurrentDocument, request.ElementKey);
            }
        }

        public void BeginRotate(RotateBeginRequest request)
        {
            DragBeginRequest dragBeginRequest = request.DragBeginRequest;
            Rect selectionViewportRect = BeginSelection(dragBeginRequest);
            Vector2 pivotViewport = _sceneProjector.TryScenePointToViewportPoint(dragBeginRequest.PreviewSnapshot, request.RotationPivotWorld, out Vector2 resolvedPivotViewport)
                ? resolvedPivotViewport
                : selectionViewportRect.center;
            _state.BeginRotation(pivotViewport, request.RotationPivotWorld, dragBeginRequest.LocalPosition - pivotViewport);
            if (_state.Selection.MoveTargets == null || _state.Selection.MoveTargets.Count <= 1)
            {
                _rotationSession.TryBegin(dragBeginRequest.CurrentDocument, dragBeginRequest.ElementKey, request.RotationPivotParentSpace);
            }
        }

        public Vector2 UpdateMove(Vector2 localPosition)
        {
            Vector2 viewportDelta = _moveSession.Update(localPosition);
            _state.Selection.CurrentSelectionViewportRect = _moveSession.CurrentSelectionViewportRect;
            return viewportDelta;
        }

        public void UpdateResize(Vector2 viewportDelta, SelectionHandle activeHandle, bool uniformScale, bool centerAnchor)
        {
            _state.Selection.ResizeCenterAnchor = centerAnchor;
            _state.Selection.ActiveResizeHandle = activeHandle;
            Rect resizedViewportRect = RectResizeUtility.ResizeRect(
                _state.Selection.StartSelectionViewportRect,
                activeHandle,
                viewportDelta,
                12f);
            _state.Selection.CurrentSelectionViewportRect = CanvasProjectionMath.GetResizeViewportRect(
                _state.Selection.StartSelectionViewportRect,
                resizedViewportRect,
                activeHandle,
                uniformScale,
                centerAnchor);
        }

        public Rect BuildScaledSceneRect(SelectionHandle handle)
        {
            return _sceneProjector.BuildScaledSceneRect(
                _state.Selection.StartSelectionViewportRect,
                _state.Selection.StartElementSceneRect,
                _state.Selection.CurrentSelectionViewportRect,
                handle,
                _state.Selection.ResizeCenterAnchor);
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
                _state.Selection,
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
                _state.Selection,
                _state.Rotation,
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
                _state.Selection,
                _transientDocumentModelSession,
                activeHandle,
                snapEnabled);
        }

        public Result<Unit> CommitDrag(CommitDragRequest request)
        {
            if (string.IsNullOrWhiteSpace(_state.Selection.ElementKey))
            {
                return Result.Failure<Unit>("Canvas drag state is unavailable.");
            }

            return request.DragMode switch
            {
                DragMode.MoveElement => _mutationService.CommitMove(request, _state.Selection, _transientDocumentModelSession),
                DragMode.RotateElement => _mutationService.CommitRotate(request, _state.Selection, _state.Rotation, _rotationSession),
                DragMode.ResizeElement => _mutationService.CommitResize(request, _state.Selection, _transientDocumentModelSession),
                _ => Result.Failure<Unit>("Canvas drag mode is unsupported.")
            };
        }

        public bool TryBuildNudgedSource(
            NudgeSourceRequest request,
            out string updatedSource)
        {
            return _mutationService.TryBuildNudgedSource(request, out updatedSource);
        }

        private Rect BeginSelection(DragBeginRequest request)
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
