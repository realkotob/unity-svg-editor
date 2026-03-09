using System;
using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasGestureRouter
    {
        private readonly ICanvasPointerDragHost _host;
        private readonly CanvasOverlayController _overlayController;
        private readonly CanvasSceneProjector _sceneProjector;
        private readonly CanvasToolController _toolController;
        private readonly CanvasSelectionSyncService _selectionSyncService;
        private readonly PointerDragSession _dragSession;
        private readonly Func<VisualElement> _overlayAccessor;
        private readonly Action _resetCanvasView;
        private readonly CanvasGestureState _gestureState = new();
        private readonly CanvasViewportGestureHandler _viewportGestureHandler;
        private readonly CanvasElementGestureHandler _elementGestureHandler;

        public CanvasGestureRouter(CanvasGestureRouterDependencies dependencies)
        {
            _host = dependencies.host;
            _overlayController = dependencies.overlayController;
            _sceneProjector = dependencies.sceneProjector;
            _toolController = dependencies.toolController;
            _selectionSyncService = dependencies.selectionSyncService;
            _dragSession = dependencies.dragSession;
            _overlayAccessor = dependencies.overlayAccessor;
            _resetCanvasView = dependencies.resetCanvasView;
            _viewportGestureHandler = new CanvasViewportGestureHandler(
                dependencies.host,
                dependencies.viewportState,
                dependencies.dragSession,
                dependencies.overlayAccessor);
            _elementGestureHandler = new CanvasElementGestureHandler(
                dependencies.host,
                dependencies.sceneProjector,
                dependencies.elementDragController,
                dependencies.selectionSyncService,
                dependencies.dragSession,
                dependencies.overlayAccessor);
        }

        public CanvasDragMode DragMode => _gestureState.Mode;
        public CanvasHandle ActiveHandle => _gestureState.ActiveHandle;
        public bool IsDraggingSelectionPreview =>
            _dragSession.IsActive && _gestureState.IsElementGesture;

        public void OnCanvasPointerDown(PointerDownEvent evt)
        {
            if (!_sceneProjector.TryGetCanvasLocalPosition(_overlayAccessor(), evt.position, out Vector2 localPosition))
                return;

            _overlayAccessor()?.Focus();

            if (TryHandleCanvasReset(evt, localPosition))
                return;

            if (_toolController.IsPanGesture(evt))
            {
                _viewportGestureHandler.BeginPan(_gestureState, localPosition, evt.pointerId);
                evt.StopPropagation();
                return;
            }

            if (_toolController.ActiveTool != CanvasTool.Move || evt.button != (int)MouseButton.LeftMouse)
                return;

            if (TryHandleSelectionHandle(evt, localPosition))
                return;

            HandleCanvasSelection(evt, localPosition);
        }

        public void OnCanvasPointerMove(PointerMoveEvent evt)
        {
            if (!_dragSession.Matches(evt.pointerId))
                return;

            if (!_sceneProjector.TryGetCanvasLocalPosition(_overlayAccessor(), evt.position, out Vector2 localPosition))
                return;

            Vector2 viewportDelta = localPosition - _dragSession.StartPosition;
            if (_gestureState.IsViewportGesture)
            {
                _viewportGestureHandler.ApplyViewportDelta(_gestureState, viewportDelta);
            }
            else if (_gestureState.IsElementGesture)
            {
                _elementGestureHandler.ApplyElementDelta(_gestureState, localPosition, viewportDelta);
            }

            evt.StopPropagation();
        }

        public void OnCanvasPointerUp(PointerUpEvent evt)
        {
            if (!_dragSession.Matches(evt.pointerId))
                return;

            bool hasLocalPosition = _sceneProjector.TryGetCanvasLocalPosition(_overlayAccessor(), evt.position, out Vector2 localPosition);
            Vector2 canvasDelta = hasLocalPosition
                ? localPosition - _dragSession.StartPosition
                : Vector2.zero;
            bool wasViewportGesture = _gestureState.IsViewportGesture;
            bool wasElementGesture = _gestureState.IsElementGesture;
            CanvasDragMode dragMode = _gestureState.Mode;
            CanvasHandle activeHandle = _gestureState.ActiveHandle;

            if (wasViewportGesture)
            {
                EndCanvasDrag();
                _viewportGestureHandler.Complete();
                evt.StopPropagation();
                return;
            }

            if (wasElementGesture)
            {
                _elementGestureHandler.Complete(dragMode, activeHandle, canvasDelta);
                EndCanvasDrag();
                evt.StopPropagation();
                return;
            }

            EndCanvasDrag();
        }

        public void OnCanvasPointerCancel(PointerCancelEvent evt)
        {
            CancelCanvasDragPreview();
        }

        public void EndCanvasDrag()
        {
            _dragSession.End(_overlayAccessor());
            _elementGestureHandler.End();
            _gestureState.Reset();
        }

        public void CancelCanvasDragPreview()
        {
            bool shouldRefreshPreview = _gestureState.IsElementGesture;
            EndCanvasDrag();

            if (shouldRefreshPreview)
                _host.RefreshLivePreview(keepExistingPreviewOnFailure: true);

            _gestureState.Reset();
            _host.UpdateCanvasVisualState();
        }

        private bool TryHandleCanvasReset(PointerDownEvent evt, Vector2 localPosition)
        {
            if (evt.clickCount != 2 || evt.button != (int)MouseButton.LeftMouse)
                return false;
            if (_sceneProjector.TryHitTestPreviewElement(_host.PreviewSnapshot, localPosition, out _))
                return false;
            if (_sceneProjector.TryHitTestFrameChrome(_host.PreviewSnapshot, localPosition, out _))
                return false;

            _resetCanvasView?.Invoke();
            evt.StopPropagation();
            return true;
        }

        private bool TryHandleSelectionHandle(PointerDownEvent evt, Vector2 localPosition)
        {
            if (!_overlayController.TryHitTestHandle(localPosition, out CanvasHandle handle))
                return false;

            if (_host.SelectionKind == CanvasSelectionKind.Frame &&
                _sceneProjector.TryGetFrameViewportRect(out _))
            {
                _viewportGestureHandler.BeginFrameResize(_gestureState, handle, localPosition, evt.pointerId);
                evt.StopPropagation();
                return true;
            }

            if (_elementGestureHandler.TryBeginResizeFromHandle(_gestureState, handle, localPosition, evt.pointerId))
            {
                evt.StopPropagation();
                return true;
            }

            return false;
        }

        private void HandleCanvasSelection(PointerDownEvent evt, Vector2 localPosition)
        {
            if (!_sceneProjector.TryHitTestPreviewElement(_host.PreviewSnapshot, localPosition, out PreviewElementGeometry hitElement))
            {
                if (_sceneProjector.TryHitTestFrameChrome(_host.PreviewSnapshot, localPosition, out _))
                {
                    _selectionSyncService.SelectCanvasFrame();
                    _viewportGestureHandler.BeginFrameMove(_gestureState, localPosition, evt.pointerId);
                }
                else
                {
                    _selectionSyncService.ClearCanvasSelection();
                }

                evt.StopPropagation();
                return;
            }

            _selectionSyncService.SelectCanvasElement(hitElement.Key, syncPatchTarget: !string.IsNullOrWhiteSpace(hitElement.TargetKey));
            _elementGestureHandler.BeginMove(_gestureState, hitElement.Key, localPosition, evt.pointerId, hitElement.SceneBounds);
            evt.StopPropagation();
        }
    }

    internal sealed class CanvasGestureRouterDependencies
    {
        public ICanvasPointerDragHost host;
        public CanvasViewportState viewportState;
        public CanvasOverlayController overlayController;
        public CanvasSceneProjector sceneProjector;
        public CanvasToolController toolController;
        public CanvasElementDragController elementDragController;
        public CanvasSelectionSyncService selectionSyncService;
        public PointerDragSession dragSession;
        public Func<VisualElement> overlayAccessor;
        public Action resetCanvasView;
    }
}
