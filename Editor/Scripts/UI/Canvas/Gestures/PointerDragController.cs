using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class PointerDragController
    {
        private static readonly ViewportFrameLayoutSettings CanvasFrameLayout = new(72f, 0f, 0f);

        private readonly ICanvasPointerDragHost _host;
        private readonly ViewportState _viewportState;
        private readonly OverlayController _overlayController;
        private readonly SceneProjector _sceneProjector;
        private readonly ToolController _toolController = new();
        private readonly DragController _dragController;
        private readonly PointerDragSession _dragSession = new();
        private readonly SelectionSyncService _selectionSyncService;
        private readonly GestureRouter _gestureRouter;
        private readonly PathEditSessionSyncController _pathEditSessionSyncController;

        private VisualElement _canvasOverlay;
        private CanvasStageView _canvasStageView;

        public PointerDragController(
            ICanvasPointerDragHost host,
            ViewportState viewportState,
            OverlayController overlayController,
            SceneProjector sceneProjector)
        {
            _host = host;
            _viewportState = viewportState;
            _overlayController = overlayController;
            _sceneProjector = sceneProjector;
            _toolController.ActiveToolChanged += OnActiveToolChanged;
            _dragController = new DragController(sceneProjector);
            _selectionSyncService = new SelectionSyncService(_host, _overlayController, _dragController);
            _gestureRouter = new GestureRouter(new GestureRouterDependencies(
                _host,
                _viewportState,
                _overlayController,
                _sceneProjector,
                _toolController,
                _dragController,
                _selectionSyncService,
                _dragSession,
                GetCanvasOverlay));
            _pathEditSessionSyncController = new PathEditSessionSyncController(
                _host,
                _sceneProjector,
                _toolController,
                _overlayController);
        }

        public VisualElement CanvasOverlay => _canvasOverlay;
        public bool IsDraggingSelectionPreview => _gestureRouter.IsDraggingSelectionPreview;
        public Rect DragCurrentSelectionViewportRect => _dragController.DragCurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _dragController.DragStartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _dragController.DragStartSelectionViewportRect;
        public Rect DragStartProjectionSceneRect => _dragController.DragStartProjectionSceneRect;
        public SvgPreserveAspectRatioMode DragStartPreserveAspectRatioMode => _dragController.DragStartPreserveAspectRatioMode;
        public bool DragResizeCenterAnchor => _dragController.DragResizeCenterAnchor;
        public float CurrentRotationAngle => _dragController.CurrentRotationAngle;
        public Vector2 DragRotationPivotViewport => _dragController.DragRotationPivotViewport;
        public DragMode DragMode => _gestureRouter.DragMode;
        public SelectionHandle ActiveHandle => _gestureRouter.ActiveHandle;
        public float Zoom => _viewportState.Zoom;

        internal bool TryBuildNudgedSource(
            NudgeSourceRequest request,
            out string updatedSource)
        {
            return _dragController.TryBuildNudgedSource(request, out updatedSource);
        }

        public bool TryCancelActiveDrag()
        {
            if (!IsDraggingSelectionPreview)
            {
                return false;
            }

            _gestureRouter.CancelCanvasDragPreview();
            return true;
        }

        public void ResetViewportToFit()
        {
            _viewportState.ResetToFit(
                _sceneProjector.GetCanvasBounds(_canvasOverlay),
                _sceneProjector.GetPreviewSceneRect(_host.PreviewSnapshot),
                CanvasFrameLayout);
        }

        public void ResetViewportToActualSize()
        {
            if (_canvasOverlay == null || _host.PreviewSnapshot == null)
            {
                _viewportState.Clear();
                return;
            }

            _viewportState.ResetToActualSize(
                _sceneProjector.GetCanvasBounds(_canvasOverlay),
                _sceneProjector.GetPreviewSceneRect(_host.PreviewSnapshot),
                CanvasFrameLayout.Padding,
                CanvasFrameLayout.HeaderHeight);
        }

        public void SyncFrameToPreview()
        {
            if (_host.PreviewSnapshot == null)
            {
                _viewportState.Clear();
                return;
            }

            _viewportState.EnsureFrame(
                _sceneProjector.GetCanvasBounds(_canvasOverlay),
                _sceneProjector.GetPreviewSceneRect(_host.PreviewSnapshot),
                CanvasFrameLayout);
        }

        public string ResyncPathEditSession(bool previewIsCurrent)
        {
            _gestureRouter.AbandonPathEditDrag();
            return _pathEditSessionSyncController.ResyncActiveSession(previewIsCurrent);
        }

        public void SyncPathEditSelection()
        {
            if (_gestureRouter.DragMode == DragMode.PathEdit)
            {
                return;
            }

            _gestureRouter.AbandonPathEditDrag();
            _pathEditSessionSyncController.SyncActiveSessionToSelection();
        }

        public void Bind(CanvasStageView canvasStageView, Toggle moveToolToggle)
        {
            Dispose();
            _toolController.ActiveToolChanged += OnActiveToolChanged;
            _toolController.BindMoveTool(moveToolToggle);
            _canvasStageView = canvasStageView;
            if (canvasStageView != null)
            {
                canvasStageView.ResetRequested += OnCanvasResetRequested;
                BuildCanvasInteractionOverlay(canvasStageView.StageElement, canvasStageView.FrameElement);
            }
        }

        public void Dispose()
        {
            _gestureRouter.EndCanvasDrag();
            _toolController.ActiveToolChanged -= OnActiveToolChanged;
            _toolController.Dispose();
            if (_canvasStageView != null)
            {
                _canvasStageView.ResetRequested -= OnCanvasResetRequested;
                _canvasStageView = null;
            }

            UnbindCanvasInteractionOverlay();
        }

        private void BuildCanvasInteractionOverlay(VisualElement stage, VisualElement frame)
        {
            _overlayController.Attach(stage, frame);
            _canvasOverlay = _overlayController.Overlay;
            if (_canvasOverlay == null)
                return;

            _canvasOverlay.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);
            _canvasOverlay.RegisterCallback<PointerMoveEvent>(OnCanvasPointerMove);
            _canvasOverlay.RegisterCallback<PointerUpEvent>(OnCanvasPointerUp);
            _canvasOverlay.RegisterCallback<PointerCancelEvent>(OnCanvasPointerCancel);
            _canvasOverlay.RegisterCallback<WheelEvent>(OnCanvasWheel);
            _canvasOverlay.RegisterCallback<KeyDownEvent>(OnCanvasKeyDown);
            _canvasOverlay.RegisterCallback<KeyUpEvent>(OnCanvasKeyUp);
            _canvasOverlay.RegisterCallback<GeometryChangedEvent>(OnCanvasGeometryChanged);

            _toolController.UpdateVisualState(_canvasOverlay);
            _host.UpdateViewportVisualState();
        }

        private void UnbindCanvasInteractionOverlay()
        {
            if (_canvasOverlay != null)
            {
                _canvasOverlay.UnregisterCallback<PointerDownEvent>(OnCanvasPointerDown);
                _canvasOverlay.UnregisterCallback<PointerMoveEvent>(OnCanvasPointerMove);
                _canvasOverlay.UnregisterCallback<PointerUpEvent>(OnCanvasPointerUp);
                _canvasOverlay.UnregisterCallback<PointerCancelEvent>(OnCanvasPointerCancel);
                _canvasOverlay.UnregisterCallback<WheelEvent>(OnCanvasWheel);
                _canvasOverlay.UnregisterCallback<KeyDownEvent>(OnCanvasKeyDown);
                _canvasOverlay.UnregisterCallback<KeyUpEvent>(OnCanvasKeyUp);
                _canvasOverlay.UnregisterCallback<GeometryChangedEvent>(OnCanvasGeometryChanged);
            }

            _overlayController.Detach();
            _canvasOverlay = null;
        }

        private void OnCanvasPointerDown(PointerDownEvent evt)
        {
            _gestureRouter.OnCanvasPointerDown(evt);
        }

        private void OnCanvasPointerMove(PointerMoveEvent evt)
        {
            _gestureRouter.OnCanvasPointerMove(evt);
        }

        private void OnCanvasPointerUp(PointerUpEvent evt)
        {
            _gestureRouter.OnCanvasPointerUp(evt);
        }

        private void OnCanvasPointerCancel(PointerCancelEvent evt)
        {
            _gestureRouter.OnCanvasPointerCancel(evt);
        }

        private void OnCanvasGeometryChanged(GeometryChangedEvent evt)
        {
            _host.UpdateSelectionVisual();
        }

        private void OnCanvasWheel(WheelEvent evt)
        {
            _toolController.HandleWheel(
                evt,
                _canvasOverlay,
                _host.PreviewSnapshot,
                _sceneProjector,
                _viewportState,
                _host.UpdateViewportVisualState);
        }

        private void OnCanvasKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape && (_gestureRouter.HandleEscapeKey() || TryCancelActiveDrag()))
            {
                evt.StopPropagation();
                return;
            }

            bool handled = _toolController.HandleKeyDown(
                evt,
                _canvasOverlay,
                _host.PreviewSnapshot,
                _sceneProjector,
                _viewportState,
                _host.UpdateCanvasVisualState,
                NudgeSelectedElement);

            if (handled && evt.keyCode == KeyCode.Space)
            {
                _toolController.UpdateVisualState(_canvasOverlay);
            }
        }

        private void OnCanvasKeyUp(KeyUpEvent evt)
        {
            bool handled = _toolController.HandleKeyUp(evt);
            if (handled)
            {
                _toolController.UpdateVisualState(_canvasOverlay);
            }
        }

        private void OnActiveToolChanged(ToolKind toolKind)
        {
            if (toolKind == ToolKind.Move)
            {
                _overlayController.ClearPathEditSession();
            }

            _toolController.UpdateVisualState(_canvasOverlay);
            _host.UpdateSelectionVisual();
        }

        private void ResetCanvasViewInternal()
        {
            if (_host.PreviewSnapshot == null)
            {
                _viewportState.Clear();
                _host.UpdateViewportVisualState();
                return;
            }

            ResetViewportToActualSize();
            _host.UpdateViewportVisualState();
        }

        private VisualElement GetCanvasOverlay()
        {
            return _canvasOverlay;
        }

        private void OnCanvasResetRequested()
        {
            ResetCanvasViewInternal();
        }

        private bool NudgeSelectedElement(Vector2 delta)
        {
            return _host.SelectionKind == SelectionKind.Element &&
                   !string.IsNullOrWhiteSpace(_host.SelectedElementKey) &&
                   CanvasNudgeService.TryNudgeSelectedElement(
                       new CanvasNudgeRequest(
                           _host,
                           _sceneProjector,
                           this,
                           delta));
        }
    }
}
