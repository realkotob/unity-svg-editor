using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
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
        private readonly SelectionTargetResolver _selectionTargetResolver;
        private readonly PathEditEntryController _pathEditEntryController;
        private readonly PathEditInteractionController _pathEditInteractionController;
        private PendingGroupMoveState _pendingGroupMoveState;
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
            _selectionTargetResolver = new SelectionTargetResolver(dependencies.Host, dependencies.SceneProjector);
            _viewportGestureHandler = new ViewportGestureHandler(
                dependencies.Host,
                dependencies.ViewportState,
                dependencies.DragSession,
                dependencies.OverlayAccessor);
            _elementGestureHandler = new ElementGestureHandler(
                dependencies.Host,
                dependencies.SceneProjector,
                dependencies.DragController,
                dependencies.SelectionSyncService,
                dependencies.DragSession,
                dependencies.OverlayAccessor);
            _pathEditEntryController = new PathEditEntryController(
                dependencies.ToolController,
                dependencies.OverlayController);
            _pathEditInteractionController = new PathEditInteractionController(
                dependencies.Host,
                dependencies.SceneProjector,
                dependencies.ToolController,
                dependencies.OverlayController);
        }

        public DragMode DragMode => _gestureState.Mode;
        public SelectionHandle ActiveHandle => _gestureState.ActiveHandle;
        public bool IsDraggingSelectionPreview =>
            _dragSession.IsActive && (_gestureState.IsElementGesture || _gestureState.Mode == DragMode.PathEdit);

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

            if (HandleCanvasPointerDown(
                    new CanvasPointerDownContext(
                        evt.pointerId,
                        evt.button,
                        evt.clickCount,
                        evt.modifiers,
                        evt.target),
                    localPosition))
            {
                evt.StopPropagation();
            }
        }

        internal bool HandleCanvasPointerDown(CanvasPointerDownContext context, Vector2 localPosition)
        {
            if (context.Button != (int)MouseButton.LeftMouse)
            {
                return false;
            }

            if (_toolController.ActiveTool == ToolKind.PathEdit)
            {
                return HandlePathEditPointerDown(context, localPosition);
            }

            if (_toolController.ActiveTool != ToolKind.Move)
            {
                return false;
            }

            if (TryHandleSelectionHandle(context, localPosition))
            {
                return true;
            }

            return HandleCanvasSelection(context, localPosition);
        }

        internal bool HandleCanvasPointerMove(int pointerId, Vector2 localPosition, EventModifiers modifiers)
        {
            if (!_dragSession.Matches(pointerId))
            {
                return false;
            }

            if (_gestureState.IsViewportGesture)
            {
                Vector2 viewportDelta = localPosition - _dragSession.StartPosition;
                _viewportGestureHandler.ApplyViewportDelta(_gestureState, viewportDelta);
                return true;
            }

            if (_gestureState.Mode == DragMode.SelectArea)
            {
                UpdateAreaSelection(localPosition);
                return true;
            }

            if (TryBeginPendingGroupMove(localPosition, pointerId, modifiers))
            {
                return true;
            }

            if (_gestureState.Mode == DragMode.PathEdit)
            {
                if (_pathEditInteractionController.UpdateDrag(localPosition))
                {
                    _host.UpdateSelectionVisual();
                }

                return true;
            }

            if (_gestureState.IsElementGesture)
            {
                ApplyActiveElementDelta(localPosition, modifiers);
                return true;
            }

            return false;
        }

        internal bool HandleCanvasPointerUp(int pointerId, Vector2 localPosition, bool hasLocalPosition)
        {
            if (!_dragSession.Matches(pointerId))
            {
                return false;
            }

            Vector2 canvasDelta = hasLocalPosition
                ? localPosition - _dragSession.StartPosition
                : Vector2.zero;
            bool wasViewportGesture = _gestureState.IsViewportGesture;
            bool wasElementGesture = _gestureState.IsElementGesture;
            DragMode dragMode = _gestureState.Mode;

            if (wasViewportGesture)
            {
                EndCanvasDrag();
                _viewportGestureHandler.Complete();
                return true;
            }

            if (dragMode == DragMode.SelectArea)
            {
                CompleteAreaSelection(canvasDelta);
                EndCanvasDrag();
                return true;
            }

            if (dragMode == DragMode.PathEdit)
            {
                bool committed = _pathEditInteractionController.CommitDrag();
                EndCanvasDrag();
                if (!committed)
                {
                    _host.UpdateCanvasVisualState();
                }

                return true;
            }

            if (wasElementGesture)
            {
                _elementGestureHandler.Complete(dragMode, canvasDelta);
                EndCanvasDrag();
                return true;
            }

            if (TryApplyPendingGroupSelection(pointerId))
            {
                EndCanvasDrag();
                return true;
            }

            EndCanvasDrag();
            return true;
        }

        internal bool HandleEscapeKey()
        {
            bool draggingPathEdit = _gestureState.Mode == DragMode.PathEdit;
            bool handled = _pathEditInteractionController.HandleEscapeKey();
            if (handled && draggingPathEdit && _dragSession.IsActive)
            {
                EndCanvasDrag();
                _host.UpdateCanvasVisualState();
            }

            return handled;
        }

        public void OnCanvasPointerMove(PointerMoveEvent evt)
        {
            if (!_sceneProjector.TryGetCanvasLocalPosition(_overlayAccessor(), evt.position, out Vector2 localPosition))
            {
                return;
            }

            if (!HandleCanvasPointerMove(evt.pointerId, localPosition, evt.modifiers))
            {
                HandleHover(localPosition, evt);
                return;
            }

            evt.StopPropagation();
        }

        public void OnCanvasPointerUp(PointerUpEvent evt)
        {
            bool hasLocalPosition = _sceneProjector.TryGetCanvasLocalPosition(_overlayAccessor(), evt.position, out Vector2 localPosition);
            if (HandleCanvasPointerUp(evt.pointerId, localPosition, hasLocalPosition))
            {
                evt.StopPropagation();
            }
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
            if (_gestureState.Mode == DragMode.PathEdit)
            {
                _pathEditInteractionController.CancelDrag();
                EndCanvasDrag();
                _host.UpdateCanvasVisualState();
                return;
            }

            bool shouldRefreshPreview = _gestureState.IsElementGesture;
            EndCanvasDrag();

            if (shouldRefreshPreview)
            {
                _host.RefreshLivePreview(keepExistingPreviewOnFailure: true);
            }

            _gestureState.Reset();
            _host.UpdateCanvasVisualState();
        }

        public void AbandonPathEditDrag()
        {
            if (_gestureState.Mode != DragMode.PathEdit)
            {
                return;
            }

            _pathEditInteractionController.AbandonDrag();
            EndCanvasDrag();
        }

        private void HandleHover(Vector2 localPosition, PointerMoveEvent evt)
        {
            if (_host.PreviewSnapshot == null)
            {
                _overlayController.ResetInteractionCursor();
                _host.ClearHover();
                return;
            }

            if (_toolController.ActiveTool == ToolKind.PathEdit)
            {
                bool hasPathEditHit = _pathEditInteractionController.TryHitPathEditTarget(localPosition, out _);
                _overlayController.UpdateInteractionCursor(hasPathEditHit);
                _host.ClearHover();
                return;
            }

            if (_toolController.ActiveTool != ToolKind.Move)
            {
                _overlayController.ResetInteractionCursor();
                _host.ClearHover();
                return;
            }

            _overlayController.UpdateInteractionCursor(false);

            if (_overlayController.IsFrameLabelTarget(evt.target))
            {
                _host.SetHoveredElement(InteractionController.FrameHoverSentinel);
                return;
            }

            if (_selectionTargetResolver.TryResolveInteractionElement(localPosition, evt.modifiers, out _, out string hoveredElementKey))
            {
                _host.SetHoveredElement(hoveredElementKey);
            }
            else
            {
                _host.ClearHover();
            }
        }

        private bool TryHandleSelectionHandle(CanvasPointerDownContext context, Vector2 localPosition)
        {
            if (!_overlayController.TryHitTestSelectionHandle(localPosition, out SelectionHandle handle))
            {
                return false;
            }

            if (handle == SelectionHandle.Rotate)
            {
                if (_elementGestureHandler.TryBeginRotateFromHandle(_gestureState, localPosition, context.PointerId))
                {
                    return true;
                }

                return false;
            }

            if (_host.SelectionKind == SelectionKind.Frame &&
                _sceneProjector.TryGetFrameViewportRect(out _))
            {
                _viewportGestureHandler.BeginFrameResize(_gestureState, handle, localPosition, context.PointerId);
                return true;
            }

            if (_elementGestureHandler.TryBeginResizeFromHandle(_gestureState, handle, localPosition, context.PointerId))
            {
                return true;
            }

            return false;
        }

        private bool HandleCanvasSelection(CanvasPointerDownContext context, Vector2 localPosition)
        {
            bool toggleSelection = (context.Modifiers & EventModifiers.Shift) != 0;

            if (_overlayController.IsFrameLabelTarget(context.Target))
            {
                _host.ClearDefinitionProxySelection();
                _selectionSyncService.SelectCanvasFrame();
                _viewportGestureHandler.BeginFrameMove(_gestureState, localPosition, context.PointerId);
                return true;
            }

            if (_host.TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual selectedProxy) &&
                selectedProxy != null &&
                selectedProxy.ViewportBounds.Contains(localPosition))
            {
                _elementGestureHandler.BeginMove(
                    _gestureState,
                    context.PointerId,
                    new DragBeginRequest(
                        _host.CurrentDocument,
                        _host.PreviewSnapshot,
                        selectedProxy.DefinitionElementKey,
                        localPosition,
                        selectedProxy.SceneBounds,
                        selectedProxy.ParentWorldTransform));
                return true;
            }

            if (toggleSelection)
            {
                if (_selectionTargetResolver.TryResolveInteractionElement(localPosition, context.Modifiers, out _, out string toggledElementKey))
                {
                    _host.ClearDefinitionProxySelection();
                    HierarchyNode toggledNode = _host.FindHierarchyNode(toggledElementKey);
                    _selectionSyncService.ToggleCanvasElement(
                        toggledElementKey,
                        syncPatchTarget: toggledNode?.CanUseAsTarget == true);

                    return true;
                }
            }

            if (TryHandlePathEditEntry(context, localPosition))
            {
                return true;
            }

            if (TryBeginMoveSelectedElement(localPosition, context.PointerId, context.Modifiers))
            {
                return true;
            }

            if (!_selectionTargetResolver.TryResolveInteractionElement(localPosition, context.Modifiers, out PreviewElementGeometry interactionElement, out string interactionElementKey))
            {
                BeginAreaSelection(localPosition, context.PointerId, context.Modifiers);
                return true;
            }

            SelectCanvasElement(interactionElementKey);
            _elementGestureHandler.BeginMove(
                _gestureState,
                context.PointerId,
                new DragBeginRequest(
                    _host.CurrentDocument,
                    _host.PreviewSnapshot,
                    interactionElementKey,
                    localPosition,
                    interactionElement.VisualBounds,
                    interactionElement.ParentWorldTransform));
            return true;
        }

        private bool TryHandlePathEditEntry(CanvasPointerDownContext context, Vector2 localPosition)
        {
            if (context.ClickCount < 2 ||
                !_selectionTargetResolver.TryResolveSelectionTarget(
                    localPosition,
                    context.Modifiers,
                    out _,
                    out string interactionElementKey,
                    out _,
                    out string directHitElementKey))
            {
                return false;
            }

            if (_selectionTargetResolver.IsDirectElementSelectionModifier(context.Modifiers) ||
                string.IsNullOrWhiteSpace(directHitElementKey) ||
                string.Equals(directHitElementKey, interactionElementKey, StringComparison.Ordinal))
            {
                return TryEnterPathEdit(interactionElementKey);
            }

            bool leafIsAlreadySelected =
                _host.SelectionKind == SelectionKind.Element &&
                string.Equals(_host.SelectedElementKey, directHitElementKey, StringComparison.Ordinal);
            if (!leafIsAlreadySelected)
            {
                SelectCanvasElement(directHitElementKey);
                return true;
            }

            if (TryEnterPathEdit(directHitElementKey))
            {
                return true;
            }

            SelectCanvasElement(directHitElementKey);
            return true;
        }

        private bool TryEnterPathEdit(string interactionElementKey)
        {
            if (_host.CurrentDocument?.DocumentModel == null ||
                _host.PreviewSnapshot == null ||
                string.IsNullOrWhiteSpace(interactionElementKey))
            {
                return false;
            }

            HierarchyNode selectedNode = _host.FindHierarchyNode(interactionElementKey);
            if (selectedNode?.CanUseAsTarget != true)
            {
                return false;
            }

            PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, interactionElementKey);
            PathEditEntryResult result = _pathEditEntryController.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: _host.CurrentDocument,
                elementKey: interactionElementKey,
                worldTransform: selectedGeometry?.WorldTransform ?? Matrix2D.identity,
                sceneToViewportPoint: scenePoint =>
                    _sceneProjector.TryScenePointToViewportPoint(_host.PreviewSnapshot, scenePoint, out Vector2 viewportPoint)
                        ? viewportPoint
                        : null));

            if (result.Kind == PathEditEntryResultKind.Ignored)
            {
                return false;
            }

            SelectCanvasElement(interactionElementKey);
            _toolController.UpdateVisualState(_overlayAccessor());

            if (result.Kind != PathEditEntryResultKind.Ignored)
            {
                _host.UpdateSourceStatus(result.StatusMessage);
            }

            return true;
        }

        private bool HandlePathEditPointerDown(CanvasPointerDownContext context, Vector2 localPosition)
        {
            if (!_overlayController.HasPathEditSession)
            {
                _toolController.SetActiveTool(ToolKind.Move);
                return false;
            }

            bool hitPathEditTarget = _pathEditInteractionController.TryHitPathEditTarget(localPosition, out _);
            if (!hitPathEditTarget &&
                _selectionTargetResolver.TryResolveInteractionElement(localPosition, context.Modifiers, out _, out string interactionElementKey) &&
                !string.IsNullOrWhiteSpace(interactionElementKey) &&
                !string.Equals(interactionElementKey, _overlayController.CurrentPathEditSession?.ElementKey, StringComparison.Ordinal))
            {
                _overlayController.ClearPathEditSession();
                _toolController.SetActiveTool(ToolKind.Move);
                SelectCanvasElement(interactionElementKey);
                _host.UpdateCanvasVisualState();
                return true;
            }

            bool handled = _pathEditInteractionController.TryHandlePointerDown(localPosition);
            if (!handled)
            {
                return false;
            }

            if (_pathEditInteractionController.IsDragging)
            {
                _gestureState.Begin(DragMode.PathEdit, SelectionHandle.None, default, default);
                _dragSession.Begin(_overlayAccessor(), context.PointerId, localPosition);
            }

            _host.UpdateSelectionVisual();
            return true;
        }

        private bool TryBeginMoveSelectedElement(Vector2 localPosition, int pointerId, EventModifiers modifiers)
        {
            bool multipleSelection = _host.SelectedElementKeys != null && _host.SelectedElementKeys.Count > 1;
            Rect selectedElementSceneRect;
            Rect selectedElementViewportRect;

            if (_host.HasDefinitionProxySelection ||
                _selectionTargetResolver.IsDirectElementSelectionModifier(modifiers) ||
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
                BeginPendingGroupSelection(CreatePendingGroupMoveState(localPosition, hitElement.Key, selectedElementSceneRect));
                _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
                return true;
            }

            _elementGestureHandler.BeginMove(
                _gestureState,
                pointerId,
                new DragBeginRequest(
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

        private PendingGroupMoveState CreatePendingGroupMoveState(
            Vector2 localPosition,
            string selectionElementKey,
            Rect selectionSceneRect)
        {
            return new PendingGroupMoveState(
                _host.CurrentDocument,
                _host.PreviewSnapshot,
                _host.SelectedElementKey,
                selectionElementKey,
                localPosition,
                selectionSceneRect,
                BuildMoveTargets(_host.SelectedElementKeys));
        }

        private void BeginPendingGroupSelection(PendingGroupMoveState pendingGroupMoveState)
        {
            _pendingGroupMoveState = pendingGroupMoveState;
        }

        private bool TryBeginPendingGroupMove(Vector2 localPosition, int pointerId, EventModifiers modifiers)
        {
            PendingGroupMoveState pendingGroupMoveState = _pendingGroupMoveState;
            if (pendingGroupMoveState == null ||
                !_dragSession.Matches(pointerId) ||
                (localPosition - pendingGroupMoveState.DragAnchorLocalPosition).magnitude < PendingGroupMoveThreshold ||
                string.IsNullOrWhiteSpace(pendingGroupMoveState.DragElementKey) ||
                pendingGroupMoveState.PreviewSnapshot == null)
            {
                return false;
            }

            PreviewElementGeometry primarySelectedGeometry = _sceneProjector.FindPreviewElement(
                pendingGroupMoveState.PreviewSnapshot,
                pendingGroupMoveState.DragElementKey);
            if (primarySelectedGeometry == null)
            {
                return false;
            }

            _elementGestureHandler.BeginMove(
                _gestureState,
                pointerId,
                new DragBeginRequest(
                    pendingGroupMoveState.CurrentDocument,
                    pendingGroupMoveState.PreviewSnapshot,
                    pendingGroupMoveState.DragElementKey,
                    pendingGroupMoveState.DragAnchorLocalPosition,
                    pendingGroupMoveState.SelectionSceneRect,
                    Matrix2D.identity,
                    pendingGroupMoveState.MoveTargets));
            _elementGestureHandler.ApplyElementDelta(
                _gestureState,
                BuildMoveElementDeltaRequest(localPosition, pendingGroupMoveState.DragAnchorLocalPosition, modifiers));
            return true;
        }

        private bool TryApplyPendingGroupSelection(int pointerId)
        {
            PendingGroupMoveState pendingGroupMoveState = _pendingGroupMoveState;
            if (pendingGroupMoveState == null ||
                string.IsNullOrWhiteSpace(pendingGroupMoveState.SelectionElementKey) ||
                !_dragSession.Matches(pointerId))
            {
                return false;
            }

            HierarchyNode selectedNode = _host.FindHierarchyNode(pendingGroupMoveState.SelectionElementKey);
            _selectionSyncService.SelectCanvasElement(
                pendingGroupMoveState.SelectionElementKey,
                syncPatchTarget: selectedNode?.CanUseAsTarget == true);
            return true;
        }

        private void ResetPendingGroupSelection()
        {
            _pendingGroupMoveState = null;
        }

        private void ApplyActiveElementDelta(Vector2 localPosition, EventModifiers modifiers)
        {
            switch (_gestureState.Mode)
            {
                case DragMode.MoveElement:
                    _elementGestureHandler.ApplyElementDelta(
                        _gestureState,
                        BuildMoveElementDeltaRequest(localPosition, _dragSession.StartPosition, modifiers));
                    return;
                case DragMode.ResizeElement:
                {
                    bool shiftPressed = (modifiers & EventModifiers.Shift) != 0;
                    bool centerAnchor = (modifiers & EventModifiers.Alt) != 0;
                    bool snapEnabled = (modifiers & (EventModifiers.Command | EventModifiers.Control)) != 0;
                    _elementGestureHandler.ApplyElementDelta(
                        _gestureState,
                        new ElementDeltaRequest(
                            localPosition,
                            localPosition - _dragSession.StartPosition,
                            uniformScale: shiftPressed,
                            centerAnchor,
                            axisLock: false,
                            snapEnabled));
                    return;
                }
                case DragMode.RotateElement:
                {
                    bool snapEnabled = (modifiers & EventModifiers.Shift) != 0;
                    _elementGestureHandler.ApplyElementDelta(
                        _gestureState,
                        new ElementDeltaRequest(
                            localPosition,
                            localPosition - _dragSession.StartPosition,
                            uniformScale: false,
                            centerAnchor: false,
                            axisLock: false,
                            snapEnabled));
                    return;
                }
            }
        }

        private static ElementDeltaRequest BuildMoveElementDeltaRequest(
            Vector2 localPosition,
            Vector2 dragAnchorLocalPosition,
            EventModifiers modifiers)
        {
            bool axisLock = (modifiers & EventModifiers.Shift) != 0;
            bool snapEnabled = (modifiers & (EventModifiers.Command | EventModifiers.Control)) != 0;
            return new ElementDeltaRequest(
                localPosition,
                localPosition - dragAnchorLocalPosition,
                uniformScale: false,
                centerAnchor: false,
                axisLock,
                snapEnabled);
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
            IReadOnlyList<string> areaSelectionKeys = _selectionTargetResolver.ResolveAreaSelectionKeys(sceneRect, EventModifiers.None);
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

        private void SelectCanvasElement(string elementKey)
        {
            _host.ClearDefinitionProxySelection();
            HierarchyNode selectedNode = _host.FindHierarchyNode(elementKey);
            _selectionSyncService.SelectCanvasElement(elementKey, syncPatchTarget: selectedNode?.CanUseAsTarget == true);
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

        private sealed class PendingGroupMoveState
        {
            public PendingGroupMoveState(
                DocumentSession currentDocument,
                PreviewSnapshot previewSnapshot,
                string dragElementKey,
                string selectionElementKey,
                Vector2 dragAnchorLocalPosition,
                Rect selectionSceneRect,
                IReadOnlyList<ElementMoveTarget> moveTargets)
            {
                CurrentDocument = currentDocument;
                PreviewSnapshot = previewSnapshot;
                DragElementKey = dragElementKey ?? string.Empty;
                SelectionElementKey = selectionElementKey ?? string.Empty;
                DragAnchorLocalPosition = dragAnchorLocalPosition;
                SelectionSceneRect = selectionSceneRect;
                MoveTargets = moveTargets ?? Array.Empty<ElementMoveTarget>();
            }

            public DocumentSession CurrentDocument { get; }
            public PreviewSnapshot PreviewSnapshot { get; }
            public string DragElementKey { get; }
            public string SelectionElementKey { get; }
            public Vector2 DragAnchorLocalPosition { get; }
            public Rect SelectionSceneRect { get; }
            public IReadOnlyList<ElementMoveTarget> MoveTargets { get; }
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
            DragController elementDragController,
            SelectionSyncService selectionSyncService,
            PointerDragSession dragSession,
            Func<VisualElement> overlayAccessor)
        {
            Host = host;
            ViewportState = viewportState;
            OverlayController = overlayController;
            SceneProjector = sceneProjector;
            ToolController = toolController;
            DragController = elementDragController;
            SelectionSyncService = selectionSyncService;
            DragSession = dragSession;
            OverlayAccessor = overlayAccessor;
        }

        public ICanvasPointerDragHost Host { get; }
        public ViewportState ViewportState { get; }
        public OverlayController OverlayController { get; }
        public SceneProjector SceneProjector { get; }
        public ToolController ToolController { get; }
        public DragController DragController { get; }
        public SelectionSyncService SelectionSyncService { get; }
        public PointerDragSession DragSession { get; }
        public Func<VisualElement> OverlayAccessor { get; }
    }

    internal readonly struct CanvasPointerDownContext
    {
        public CanvasPointerDownContext(
            int pointerId,
            int button,
            int clickCount,
            EventModifiers modifiers,
            object target)
        {
            PointerId = pointerId;
            Button = button;
            ClickCount = clickCount;
            Modifiers = modifiers;
            Target = target;
        }

        public int PointerId { get; }
        public int Button { get; }
        public int ClickCount { get; }
        public EventModifiers Modifiers { get; }
        public object Target { get; }
    }
}
