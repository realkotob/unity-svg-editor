using System;
using System.Collections.Generic;
using Core.UI.Foundation;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class GestureRouter
    {
        private const float PendingGroupMoveThreshold = 5f;

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
        private readonly InteractionSelectionResolver _selectionResolver;
        private string _pendingGroupSelectionElementKey = string.Empty;
        private Rect _pendingGroupSelectionSceneRect;
        private IReadOnlyList<ElementMoveTarget> _pendingGroupMoveTargets = Array.Empty<ElementMoveTarget>();

        public GestureRouter(GestureRouterDependencies dependencies)
        {
            _host = dependencies.Host;
            _overlayController = dependencies.OverlayController;
            _sceneProjector = dependencies.SceneProjector;
            _toolController = dependencies.ToolController;
            _selectionSyncService = dependencies.SelectionSyncService;
            _dragSession = dependencies.DragSession;
            _overlayAccessor = dependencies.OverlayAccessor;
            _resetCanvasView = dependencies.ResetCanvasView;
            _selectionResolver = new InteractionSelectionResolver(dependencies.Host, dependencies.SceneProjector);
            _viewportGestureHandler = new ViewportGestureHandler(
                dependencies.Host,
                dependencies.ViewportState,
                dependencies.DragSession,
                dependencies.OverlayAccessor);
            _elementGestureHandler = new ElementGestureHandler(
                dependencies.Host,
                dependencies.SceneProjector,
                dependencies.ElementDragController,
                dependencies.SelectionSyncService,
                dependencies.DragSession,
                dependencies.OverlayAccessor);
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
            else if (TryBeginPendingGroupMove(localPosition, evt.pointerId, evt.modifiers))
            {
                evt.StopPropagation();
                return;
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
                _elementGestureHandler.ApplyElementDelta(
                    _gestureState,
                    new ElementDeltaRequest(localPosition, viewportDelta, uniformScale, centerAnchor, axisLock, snapEnabled));
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
                _elementGestureHandler.Complete(dragMode, canvasDelta);
                EndCanvasDrag();
                evt.StopPropagation();
                return;
            }

            if (TryApplyPendingGroupSelection(evt.pointerId))
            {
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
            ResetPendingGroupSelection();
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
            bool toggleSelection = (evt.modifiers & EventModifiers.Shift) != 0;

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
                    evt.pointerId,
                    new ElementDragBeginRequest(
                        _host.CurrentDocument,
                        _host.PreviewSnapshot,
                        selectedProxy.DefinitionElementKey,
                        localPosition,
                        selectedProxy.SceneBounds,
                        selectedProxy.ParentWorldTransform));
                evt.StopPropagation();
                return;
            }

            if (toggleSelection)
            {
                if (_selectionResolver.TryResolveInteractionElement(localPosition, evt.modifiers, out _, out string toggledElementKey))
                {
                    _host.ClearDefinitionProxySelection();
                    HierarchyNode toggledNode = _host.FindHierarchyNode(toggledElementKey);
                    _selectionSyncService.ToggleCanvasElement(
                        toggledElementKey,
                        syncPatchTarget: toggledNode?.CanUseAsTarget == true);
                }

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
                evt.pointerId,
                new ElementDragBeginRequest(
                    _host.CurrentDocument,
                    _host.PreviewSnapshot,
                    interactionElementKey,
                    localPosition,
                    interactionElement.VisualBounds,
                    interactionElement.ParentWorldTransform));
            evt.StopPropagation();
        }

        private bool TryBeginMoveSelectedElement(Vector2 localPosition, int pointerId, EventModifiers modifiers)
        {
            bool multipleSelection = _host.SelectedElementKeys != null && _host.SelectedElementKeys.Count > 1;
            Rect selectedElementSceneRect;
            Rect selectedElementViewportRect;

            if (_host.HasDefinitionProxySelection ||
                _selectionResolver.IsDirectElementSelectionModifier(modifiers) ||
                _host.SelectionKind != SelectionKind.Element ||
                string.IsNullOrWhiteSpace(_host.SelectedElementKey))
            {
                return false;
            }

            if (multipleSelection)
            {
                if (!CanvasProjectionMath.TryGetCombinedSelectionSceneRect(_host.PreviewSnapshot, _host.SelectedElementKeys, out selectedElementSceneRect) ||
                    !_sceneProjector.TrySceneRectToViewportRect(_host.PreviewSnapshot, selectedElementSceneRect, out selectedElementViewportRect) ||
                    !selectedElementViewportRect.Contains(localPosition))
                {
                    return false;
                }
            }
            else if (!_sceneProjector.TryResolveSelectedElementSceneRect(_host.PreviewSnapshot, _host.SelectedElementKey, out selectedElementSceneRect) ||
                     !_sceneProjector.TrySceneRectToViewportRect(_host.PreviewSnapshot, selectedElementSceneRect, out selectedElementViewportRect) ||
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
                (multipleSelection
                    ? _host.SelectedElementKeys == null || !ContainsElementKey(_host.SelectedElementKeys, hitElement.Key)
                    : !string.Equals(hitElement.Key, _host.SelectedElementKey, StringComparison.Ordinal)))
            {
                return false;
            }

            if (multipleSelection)
            {
                BeginPendingGroupSelection(localPosition, pointerId, hitElement.Key, selectedElementSceneRect, BuildMoveTargets(_host.SelectedElementKeys));
                return true;
            }

            _elementGestureHandler.BeginMove(
                _gestureState,
                pointerId,
                new ElementDragBeginRequest(
                    _host.CurrentDocument,
                    _host.PreviewSnapshot,
                    _host.SelectedElementKey,
                    localPosition,
                    multipleSelection ? selectedElementSceneRect : selectedElement.VisualBounds,
                    multipleSelection ? Matrix2D.identity : selectedElement.ParentWorldTransform,
                    multipleSelection ? BuildMoveTargets(_host.SelectedElementKeys) : null));
            return true;
        }

        private IReadOnlyList<ElementMoveTarget> BuildMoveTargets(IReadOnlyList<string> selectedElementKeys)
        {
            if (selectedElementKeys == null || selectedElementKeys.Count == 0)
            {
                return Array.Empty<ElementMoveTarget>();
            }

            var moveTargets = new List<ElementMoveTarget>(selectedElementKeys.Count);
            foreach (string selectedElementKey in selectedElementKeys)
            {
                PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, selectedElementKey);
                if (selectedGeometry == null)
                {
                    continue;
                }

                moveTargets.Add(new ElementMoveTarget(selectedElementKey, selectedGeometry.ParentWorldTransform));
            }

            return moveTargets;
        }

        private void BeginPendingGroupSelection(
            Vector2 localPosition,
            int pointerId,
            string selectedElementKey,
            Rect selectionSceneRect,
            IReadOnlyList<ElementMoveTarget> moveTargets)
        {
            _pendingGroupSelectionElementKey = selectedElementKey ?? string.Empty;
            _pendingGroupSelectionSceneRect = selectionSceneRect;
            _pendingGroupMoveTargets = moveTargets ?? Array.Empty<ElementMoveTarget>();
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        private bool TryBeginPendingGroupMove(Vector2 localPosition, int pointerId, EventModifiers modifiers)
        {
            if (string.IsNullOrWhiteSpace(_pendingGroupSelectionElementKey) ||
                !_dragSession.Matches(pointerId) ||
                (localPosition - _dragSession.StartPosition).magnitude < PendingGroupMoveThreshold)
            {
                return false;
            }

            PreviewElementGeometry primarySelectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, _host.SelectedElementKey);
            if (primarySelectedGeometry == null)
            {
                return false;
            }

            _elementGestureHandler.BeginMove(
                _gestureState,
                pointerId,
                new ElementDragBeginRequest(
                    _host.CurrentDocument,
                    _host.PreviewSnapshot,
                    _host.SelectedElementKey,
                    _dragSession.StartPosition,
                    _pendingGroupSelectionSceneRect,
                    Matrix2D.identity,
                    _pendingGroupMoveTargets));

            bool axisLock = (modifiers & EventModifiers.Shift) != 0;
            bool snapEnabled = (modifiers & (EventModifiers.Command | EventModifiers.Control)) != 0;
            _elementGestureHandler.ApplyElementDelta(
                _gestureState,
                new ElementDeltaRequest(
                    localPosition,
                    localPosition - _dragSession.StartPosition,
                    uniformScale: false,
                    centerAnchor: false,
                    axisLock,
                    snapEnabled));
            return true;
        }

        private bool TryApplyPendingGroupSelection(int pointerId)
        {
            if (string.IsNullOrWhiteSpace(_pendingGroupSelectionElementKey) || !_dragSession.Matches(pointerId))
            {
                return false;
            }

            HierarchyNode selectedNode = _host.FindHierarchyNode(_pendingGroupSelectionElementKey);
            _selectionSyncService.SelectCanvasElement(
                _pendingGroupSelectionElementKey,
                syncPatchTarget: selectedNode?.CanUseAsTarget == true);
            return true;
        }

        private void ResetPendingGroupSelection()
        {
            _pendingGroupSelectionElementKey = string.Empty;
            _pendingGroupSelectionSceneRect = default;
            _pendingGroupMoveTargets = Array.Empty<ElementMoveTarget>();
        }

        private static bool ContainsElementKey(IReadOnlyList<string> selectedElementKeys, string elementKey)
        {
            if (selectedElementKeys == null || string.IsNullOrWhiteSpace(elementKey))
            {
                return false;
            }

            foreach (string selectedElementKey in selectedElementKeys)
            {
                if (string.Equals(selectedElementKey, elementKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class GestureRouterDependencies
    {
        public GestureRouterDependencies(
            ICanvasPointerDragHost host,
            ViewportState viewportState,
            OverlayController overlayController,
            SceneProjector sceneProjector,
            ToolController toolController,
            ElementDragController elementDragController,
            SelectionSyncService selectionSyncService,
            PointerDragSession dragSession,
            Func<VisualElement> overlayAccessor,
            Action resetCanvasView)
        {
            Host = host;
            ViewportState = viewportState;
            OverlayController = overlayController;
            SceneProjector = sceneProjector;
            ToolController = toolController;
            ElementDragController = elementDragController;
            SelectionSyncService = selectionSyncService;
            DragSession = dragSession;
            OverlayAccessor = overlayAccessor;
            ResetCanvasView = resetCanvasView;
        }

        public ICanvasPointerDragHost Host { get; }
        public ViewportState ViewportState { get; }
        public OverlayController OverlayController { get; }
        public SceneProjector SceneProjector { get; }
        public ToolController ToolController { get; }
        public ElementDragController ElementDragController { get; }
        public SelectionSyncService SelectionSyncService { get; }
        public PointerDragSession DragSession { get; }
        public Func<VisualElement> OverlayAccessor { get; }
        public Action ResetCanvasView { get; }
    }
}
