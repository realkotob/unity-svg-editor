using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasPointerDragController
    {
        private const float CanvasFrameMargin = 72f;
        private const float CanvasFramePadding = 12f;
        private const float CanvasFrameHeaderHeight = 24f;

        private readonly ICanvasPointerDragHost _host;
        private readonly CanvasViewportState _viewportState;
        private readonly CanvasOverlayController _overlayController;
        private readonly CanvasSceneProjector _sceneProjector;
        private readonly CanvasToolController _toolController = new();
        private readonly CanvasElementDragController _elementDragController;
        private readonly PointerDragSession _dragSession = new();
        private readonly CanvasSelectionSyncService _selectionSyncService;
        private readonly CanvasGestureRouter _gestureRouter;

        private VisualElement _canvasOverlay;

        public CanvasPointerDragController(
            ICanvasPointerDragHost host,
            StructureEditor structureEditor,
            CanvasViewportState viewportState,
            CanvasOverlayController overlayController,
            CanvasSceneProjector sceneProjector)
        {
            _host = host;
            _viewportState = viewportState;
            _overlayController = overlayController;
            _sceneProjector = sceneProjector;
            _elementDragController = new CanvasElementDragController(structureEditor, sceneProjector);
            _selectionSyncService = new CanvasSelectionSyncService(_host, _overlayController, _elementDragController);
            _gestureRouter = new CanvasGestureRouter(new CanvasGestureRouterDependencies
            {
                host = _host,
                viewportState = _viewportState,
                overlayController = _overlayController,
                sceneProjector = _sceneProjector,
                toolController = _toolController,
                elementDragController = _elementDragController,
                selectionSyncService = _selectionSyncService,
                dragSession = _dragSession,
                overlayAccessor = GetCanvasOverlay,
                resetCanvasView = ResetCanvasViewInternal
            });
        }

        public VisualElement CanvasOverlay => _canvasOverlay;
        public bool IsDraggingSelectionPreview => _gestureRouter.IsDraggingSelectionPreview;
        public Rect DragCurrentSelectionViewportRect => _elementDragController.DragCurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _elementDragController.DragStartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _elementDragController.DragStartSelectionViewportRect;
        public CanvasDragMode DragMode => _gestureRouter.DragMode;
        public CanvasHandle ActiveHandle => _gestureRouter.ActiveHandle;

        public void ResetViewportToFit()
        {
            _viewportState.ResetToFit(
                _sceneProjector.GetCanvasBounds(_canvasOverlay),
                _sceneProjector.GetPreviewSceneRect(_host.PreviewSnapshot),
                CanvasFrameMargin,
                CanvasFramePadding,
                CanvasFrameHeaderHeight);
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
                CanvasFrameMargin,
                CanvasFramePadding,
                CanvasFrameHeaderHeight);
        }

        public void Bind(VisualElement stage, VisualElement frame, Toggle moveToolToggle)
        {
            Dispose();
            _toolController.BindMoveTool(moveToolToggle);
            BuildCanvasInteractionOverlay(stage, frame);
        }

        public void Dispose()
        {
            _gestureRouter.EndCanvasDrag();
            _toolController.Dispose();
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
            _host.UpdateCanvasVisualState();
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
                _host.UpdateCanvasVisualState);
        }

        private void OnCanvasKeyDown(KeyDownEvent evt)
        {
            _toolController.HandleKeyDown(
                evt,
                _canvasOverlay,
                _host.PreviewSnapshot,
                _sceneProjector,
                _viewportState,
                _host.UpdateCanvasVisualState);
        }

        private void OnCanvasKeyUp(KeyUpEvent evt)
        {
            _toolController.HandleKeyUp(evt);
        }

        private void ResetCanvasViewInternal()
        {
            if (_host.PreviewSnapshot == null)
            {
                _viewportState.Clear();
                _host.UpdateCanvasVisualState();
                return;
            }

            _viewportState.ResetToFit(
                _sceneProjector.GetCanvasBounds(_canvasOverlay),
                _sceneProjector.GetPreviewSceneRect(_host.PreviewSnapshot),
                CanvasFrameMargin,
                CanvasFramePadding,
                CanvasFrameHeaderHeight);
            _host.UpdateCanvasVisualState();
        }

        private VisualElement GetCanvasOverlay()
        {
            return _canvasOverlay;
        }
    }
}
