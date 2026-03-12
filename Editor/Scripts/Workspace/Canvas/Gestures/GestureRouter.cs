using System;
using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Document;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class GestureRouter
    {
        private readonly ICanvasPointerDragHost _host;
        private readonly OverlayController _overlayController;
        private readonly SceneProjector _sceneProjector;
        private readonly ToolController _toolController;
        private readonly SelectionSyncService _selectionSyncService;
        private readonly PointerDragSession _dragSession;
        private readonly Func<VisualElement> _overlayAccessor;
        private readonly Action _resetCanvasView;
        private readonly GestureState _gestureState = new();
        private readonly ViewportGestureHandler _viewportGestureHandler;
        private readonly ElementGestureHandler _elementGestureHandler;
        private readonly CanvasInteractionSelectionResolver _selectionResolver;

        public GestureRouter(GestureRouterDependencies dependencies)
        {
            _host = dependencies.host;
            _overlayController = dependencies.overlayController;
            _sceneProjector = dependencies.sceneProjector;
            _toolController = dependencies.toolController;
            _selectionSyncService = dependencies.selectionSyncService;
            _dragSession = dependencies.dragSession;
            _overlayAccessor = dependencies.overlayAccessor;
            _resetCanvasView = dependencies.resetCanvasView;
            _selectionResolver = new CanvasInteractionSelectionResolver(dependencies.host, dependencies.sceneProjector);
            _viewportGestureHandler = new ViewportGestureHandler(
                dependencies.host,
                dependencies.viewportState,
                dependencies.dragSession,
                dependencies.overlayAccessor);
            _elementGestureHandler = new ElementGestureHandler(
                dependencies.host,
                dependencies.sceneProjector,
                dependencies.elementDragController,
                dependencies.selectionSyncService,
                dependencies.dragSession,
                dependencies.overlayAccessor);
        }

        public DragMode DragMode => _gestureState.Mode;
        public SelectionHandle ActiveHandle => _gestureState.ActiveHandle;
        public bool IsDraggingSelectionPreview =>
            _dragSession.IsActive && _gestureState.IsElementGesture;

        public void OnCanvasPointerDown(PointerDownEvent evt)
        {
            if (!_sceneProjector.TryGetCanvasLocalPosition(_overlayAccessor(), evt.position, out Vector2 localPosition))
            {
                return;
            }

            _overlayAccessor()?.Focus();

            if (TryHandleCanvasReset(evt, localPosition))
            {
                return;
            }

            if (_toolController.IsPanGesture(evt))
            {
                _viewportGestureHandler.BeginPan(_gestureState, localPosition, evt.pointerId);
                evt.StopPropagation();
                return;
            }

            if (_toolController.ActiveTool != ToolKind.Move || evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            if (TryHandleSelectionHandle(evt, localPosition))
            {
                return;
            }

            HandleCanvasSelection(evt, localPosition);
        }

        public void OnCanvasPointerMove(PointerMoveEvent evt)
        {
            if (!_sceneProjector.TryGetCanvasLocalPosition(_overlayAccessor(), evt.position, out Vector2 localPosition))
            {
                return;
            }

            if (!_dragSession.Matches(evt.pointerId))
            {
                HandleHover(localPosition, evt);
                return;
            }

            Vector2 viewportDelta = localPosition - _dragSession.StartPosition;
            if (_gestureState.IsViewportGesture)
            {
                _viewportGestureHandler.ApplyViewportDelta(_gestureState, viewportDelta);
            }
            else if (_gestureState.IsElementGesture)
            {
                bool shiftPressed = (evt.modifiers & EventModifiers.Shift) != 0;
                bool uniformScale = _gestureState.Mode == DragMode.ResizeElement && shiftPressed;
                bool axisLock = _gestureState.Mode == DragMode.MoveElement && shiftPressed;
                bool centerAnchor = (evt.modifiers & EventModifiers.Alt) != 0;
                bool snapEnabled = _gestureState.Mode == DragMode.RotateElement
                    ? shiftPressed
                    : (evt.modifiers & (EventModifiers.Command | EventModifiers.Control)) != 0;
                _elementGestureHandler.ApplyElementDelta(_gestureState, localPosition, viewportDelta, uniformScale, centerAnchor, axisLock, snapEnabled);
            }

            evt.StopPropagation();
        }

        public void OnCanvasPointerUp(PointerUpEvent evt)
        {
            if (!_dragSession.Matches(evt.pointerId))
            {
                return;
            }

            bool hasLocalPosition = _sceneProjector.TryGetCanvasLocalPosition(_overlayAccessor(), evt.position, out Vector2 localPosition);
            Vector2 canvasDelta = hasLocalPosition
                ? localPosition - _dragSession.StartPosition
                : Vector2.zero;
            bool wasViewportGesture = _gestureState.IsViewportGesture;
            bool wasElementGesture = _gestureState.IsElementGesture;
            DragMode dragMode = _gestureState.Mode;
            SelectionHandle activeHandle = _gestureState.ActiveHandle;

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
            _overlayController.ResetInteractionCursor();
            _host.ClearHover();
            CancelCanvasDragPreview();
        }

        public void EndCanvasDrag()
        {
            _dragSession.End(_overlayAccessor());
            _elementGestureHandler.End();
            _gestureState.Reset();
            _overlayController.ResetInteractionCursor();
            _host.UpdateHoverVisual();
            _host.UpdateSelectionVisual();
        }

        public void CancelCanvasDragPreview()
        {
            bool shouldRefreshPreview = _gestureState.IsElementGesture;
            EndCanvasDrag();

            if (shouldRefreshPreview)
            {
                _host.RefreshLivePreview(keepExistingPreviewOnFailure: true);
            }

            _gestureState.Reset();
            _host.UpdateCanvasVisualState();
        }

        private void HandleHover(Vector2 localPosition, PointerMoveEvent evt)
        {
            if (_toolController.ActiveTool != ToolKind.Move || _host.PreviewSnapshot == null)
            {
                _overlayController.ResetInteractionCursor();
                _host.ClearHover();
                return;
            }

            _overlayController.UpdateInteractionCursor(localPosition);

            if (_overlayController.IsFrameLabelTarget(evt.target))
            {
                _host.SetHoveredElement(InteractionController.FrameHoverSentinel);
                return;
            }

            if (_selectionResolver.TryResolveInteractionElement(localPosition, evt.modifiers, out _, out string hoveredElementKey))
            {
                _host.SetHoveredElement(hoveredElementKey);
            }
            else
            {
                _host.ClearHover();
            }
        }

        private bool TryHandleCanvasReset(PointerDownEvent evt, Vector2 localPosition)
        {
            if (evt.clickCount != 2 || evt.button != (int)MouseButton.LeftMouse)
            {
                return false;
            }

            if (_sceneProjector.TryHitTestPreviewElement(_host.PreviewSnapshot, localPosition, out _))
            {
                return false;
            }

            if (_sceneProjector.TryHitTestFrameChrome(_host.PreviewSnapshot, localPosition, out _))
            {
                return false;
            }

            _resetCanvasView?.Invoke();
            evt.StopPropagation();
            return true;
        }

        private bool TryHandleSelectionHandle(PointerDownEvent evt, Vector2 localPosition)
        {
            if (!_overlayController.TryHitTestSelectionHandle(localPosition, out SelectionHandle handle))
            {
                return false;
            }

            if (handle == SelectionHandle.Rotate)
            {
                if (_elementGestureHandler.TryBeginRotateFromHandle(_gestureState, localPosition, evt.pointerId))
                {
                    evt.StopPropagation();
                    return true;
                }

                return false;
            }

            if (_host.SelectionKind == SelectionKind.Frame &&
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
            if (_overlayController.IsFrameLabelTarget(evt.target))
            {
                _host.ClearDefinitionProxySelection();
                _selectionSyncService.SelectCanvasFrame();
                _viewportGestureHandler.BeginFrameMove(_gestureState, localPosition, evt.pointerId);
                evt.StopPropagation();
                return;
            }

            if (_host.TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual selectedProxy) &&
                selectedProxy != null &&
                selectedProxy.ViewportBounds.Contains(localPosition))
            {
                _elementGestureHandler.BeginMove(
                    _gestureState,
                    selectedProxy.DefinitionElementKey,
                    localPosition,
                    evt.pointerId,
                    selectedProxy.SceneBounds,
                    selectedProxy.ParentWorldTransform);
                evt.StopPropagation();
                return;
            }

            if (TryBeginMoveSelectedElement(localPosition, evt.pointerId, evt.modifiers))
            {
                evt.StopPropagation();
                return;
            }

            if (!_selectionResolver.TryResolveInteractionElement(localPosition, evt.modifiers, out PreviewElementGeometry interactionElement, out string interactionElementKey))
            {
                _host.ClearDefinitionProxySelection();
                _selectionSyncService.ClearCanvasSelection();
                evt.StopPropagation();
                return;
            }

            _host.ClearDefinitionProxySelection();
            HierarchyNode selectedNode = _host.FindHierarchyNode(interactionElementKey);
            _selectionSyncService.SelectCanvasElement(interactionElementKey, syncPatchTarget: selectedNode?.CanUseAsTarget == true);
            _elementGestureHandler.BeginMove(
                _gestureState,
                interactionElementKey,
                localPosition,
                evt.pointerId,
                interactionElement.VisualBounds,
                interactionElement.ParentWorldTransform);
            evt.StopPropagation();
        }

        private bool TryBeginMoveSelectedElement(Vector2 localPosition, int pointerId, EventModifiers modifiers)
        {
            if (_host.HasDefinitionProxySelection ||
                _selectionResolver.IsDirectElementSelectionModifier(modifiers) ||
                _host.SelectionKind != SelectionKind.Element ||
                string.IsNullOrWhiteSpace(_host.SelectedElementKey) ||
                !_sceneProjector.TryResolveSelectedElementSceneRect(_host.PreviewSnapshot, _host.SelectedElementKey, out Rect selectedElementSceneRect) ||
                !_sceneProjector.TrySceneRectToViewportRect(_host.PreviewSnapshot, selectedElementSceneRect, out Rect selectedElementViewportRect) ||
                !selectedElementViewportRect.Contains(localPosition))
            {
                return false;
            }

            PreviewElementGeometry selectedElement = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, _host.SelectedElementKey);
            if (selectedElement == null)
            {
                return false;
            }

            if (!_sceneProjector.TryHitTestPreviewElement(_host.PreviewSnapshot, localPosition, out PreviewElementGeometry hitElement) ||
                hitElement == null ||
                !string.Equals(hitElement.Key, _host.SelectedElementKey, StringComparison.Ordinal))
            {
                return false;
            }

            _elementGestureHandler.BeginMove(
                _gestureState,
                _host.SelectedElementKey,
                localPosition,
                pointerId,
                selectedElement.VisualBounds,
                selectedElement.ParentWorldTransform);
            return true;
        }
    }

    internal sealed class GestureRouterDependencies
    {
        public ICanvasPointerDragHost host;
        public ViewportState viewportState;
        public OverlayController overlayController;
        public SceneProjector sceneProjector;
        public ToolController toolController;
        public ElementDragController elementDragController;
        public SelectionSyncService selectionSyncService;
        public PointerDragSession dragSession;
        public Func<VisualElement> overlayAccessor;
        public Action resetCanvasView;
    }
}
