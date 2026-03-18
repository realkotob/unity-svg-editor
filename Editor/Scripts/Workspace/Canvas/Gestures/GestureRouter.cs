using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;
using Core.UI.Extensions;
using SvgEditor.Shared;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class GestureRouter
    {
        private const float PendingGroupMoveThreshold = 5f;
        private const float AreaSelectionMinimumSize = 2f;

        private readonly ICanvasPointerDragHost _host;
        private readonly OverlayController _overlayController;
        private readonly SceneProjector _sceneProjector;
        private readonly ToolController _toolController;
        private readonly SelectionSyncService _selectionSyncService;
        private readonly PointerDragSession _dragSession;
        private readonly Func<VisualElement> _overlayAccessor;
        private readonly GestureState _gestureState = new();
        private readonly ViewportGestureHandler _viewportGestureHandler;
        private readonly ElementGestureHandler _elementGestureHandler;
        private readonly InteractionSelectionResolver _selectionResolver;
        private string _pendingGroupSelectionElementKey = string.Empty;
        private Rect _pendingGroupSelectionSceneRect;
        private IReadOnlyList<ElementMoveTarget> _pendingGroupMoveTargets = Array.Empty<ElementMoveTarget>();
        private bool _isAreaSelectionAdditive;
        private IReadOnlyList<string> _areaSelectionBaselineKeys = Array.Empty<string>();

        public GestureRouter(GestureRouterDependencies dependencies)
        {
            _host = dependencies.Host;
            _overlayController = dependencies.OverlayController;
            _sceneProjector = dependencies.SceneProjector;
            _toolController = dependencies.ToolController;
            _selectionSyncService = dependencies.SelectionSyncService;
            _dragSession = dependencies.DragSession;
            _overlayAccessor = dependencies.OverlayAccessor;
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
            else if (_gestureState.Mode == DragMode.SelectArea)
            {
                UpdateAreaSelection(localPosition);
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

            if (dragMode == DragMode.SelectArea)
            {
                CompleteAreaSelection(canvasDelta);
                EndCanvasDrag();
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
            if (_gestureState.Mode == DragMode.SelectArea)
            {
                RestoreAreaSelectionBaseline();
            }

            CancelCanvasDragPreview();
        }

        public void EndCanvasDrag()
        {
            _dragSession.End(_overlayAccessor());
            _elementGestureHandler.End();
            _gestureState.Reset();
            ResetPendingGroupSelection();
            _isAreaSelectionAdditive = false;
            _areaSelectionBaselineKeys = Array.Empty<string>();
            _overlayController.ClearMarquee();
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

                    evt.StopPropagation();
                    return;
                }
            }

            if (TryBeginMoveSelectedElement(localPosition, evt.pointerId, evt.modifiers))
            {
                evt.StopPropagation();
                return;
            }

            if (!_selectionResolver.TryResolveInteractionElement(localPosition, evt.modifiers, out PreviewElementGeometry interactionElement, out string interactionElementKey))
            {
                BeginAreaSelection(localPosition, evt.pointerId, evt.modifiers);
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

        private void BeginAreaSelection(Vector2 localPosition, int pointerId, EventModifiers modifiers)
        {
            _host.ClearDefinitionProxySelection();
            _isAreaSelectionAdditive = (modifiers & EventModifiers.Shift) != 0;
            _areaSelectionBaselineKeys = _host.SelectedElementKeys != null
                ? new List<string>(_host.SelectedElementKeys)
                : Array.Empty<string>();
            _gestureState.Begin(DragMode.SelectArea, SelectionHandle.None, default, default);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
            _overlayController.SetMarquee(new Rect(localPosition, Vector2.zero));
        }

        private void UpdateAreaSelection(Vector2 localPosition)
        {
            Rect viewportRect = BuildViewportRect(_dragSession.StartPosition, localPosition);
            _overlayController.SetMarquee(viewportRect);

            if (viewportRect.width < AreaSelectionMinimumSize &&
                viewportRect.height < AreaSelectionMinimumSize)
            {
                return;
            }

            ApplyAreaSelection(viewportRect);
        }

        private void CompleteAreaSelection(Vector2 canvasDelta)
        {
            Rect viewportRect = BuildViewportRect(_dragSession.StartPosition, _dragSession.StartPosition + canvasDelta);
            ApplyAreaSelection(viewportRect);
        }

        private void ApplyAreaSelection(Rect viewportRect)
        {
            if (viewportRect.width < AreaSelectionMinimumSize && viewportRect.height < AreaSelectionMinimumSize)
            {
                if (!_isAreaSelectionAdditive)
                {
                    _selectionSyncService.ClearCanvasSelection();
                }

                return;
            }

            if (!_sceneProjector.TryViewportPointToScenePoint(_host.PreviewSnapshot, viewportRect.min, out Vector2 minScenePoint) ||
                !_sceneProjector.TryViewportPointToScenePoint(_host.PreviewSnapshot, viewportRect.max, out Vector2 maxScenePoint))
            {
                return;
            }

            Rect sceneRect = Rect.MinMaxRect(
                Mathf.Min(minScenePoint.x, maxScenePoint.x),
                Mathf.Min(minScenePoint.y, maxScenePoint.y),
                Mathf.Max(minScenePoint.x, maxScenePoint.x),
                Mathf.Max(minScenePoint.y, maxScenePoint.y));
            IReadOnlyList<string> areaSelectionKeys = _selectionResolver.ResolveAreaSelectionKeys(sceneRect, EventModifiers.None);
            if (_isAreaSelectionAdditive)
            {
                _selectionSyncService.ReplaceCanvasElements(
                    BuildMergedAreaSelectionKeys(_areaSelectionBaselineKeys, areaSelectionKeys),
                    syncPatchTarget: false);
            }
            else
            {
                _selectionSyncService.ReplaceCanvasElements(areaSelectionKeys, syncPatchTarget: false);
            }
        }

        private void RestoreAreaSelectionBaseline()
        {
            _selectionSyncService.ReplaceCanvasElements(_areaSelectionBaselineKeys, syncPatchTarget: false);
        }

        private static Rect BuildViewportRect(Vector2 startPosition, Vector2 currentPosition)
        {
            return Rect.MinMaxRect(
                Mathf.Min(startPosition.x, currentPosition.x),
                Mathf.Min(startPosition.y, currentPosition.y),
                Mathf.Max(startPosition.x, currentPosition.x),
                Mathf.Max(startPosition.y, currentPosition.y));
        }

        private static List<string> BuildMergedAreaSelectionKeys(
            IReadOnlyList<string> baselineKeys,
            IReadOnlyList<string> areaSelectionKeys)
        {
            List<string> mergedKeys = baselineKeys != null
                ? new List<string>(baselineKeys)
                : new List<string>();

            if (areaSelectionKeys == null)
                return mergedKeys;

            foreach (string areaSelectionKey in areaSelectionKeys)
            {
                if (ContainsElementKey(mergedKeys, areaSelectionKey))
                {
                    RemoveElementKey(mergedKeys, areaSelectionKey);
                }
                else
                {
                    mergedKeys.Add(areaSelectionKey);
                }
            }

            return mergedKeys;
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

        private static void RemoveElementKey(List<string> elementKeys, string elementKey)
        {
            if (elementKeys == null || string.IsNullOrWhiteSpace(elementKey))
            {
                return;
            }

            for (int index = elementKeys.Count - 1; index >= 0; index--)
            {
                if (string.Equals(elementKeys[index], elementKey, StringComparison.Ordinal))
                {
                    elementKeys.RemoveAt(index);
                }
            }
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
            Func<VisualElement> overlayAccessor)
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
    }
}
