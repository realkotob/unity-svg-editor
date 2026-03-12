using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasInteractionController : ICanvasPointerDragHost
    {
        internal const string FrameHoverSentinel = "__canvas-frame-hover__";

        private readonly ICanvasWorkspaceHost _host;
        private readonly CanvasViewportState _viewportState;
        private readonly CanvasSceneProjector _sceneProjector;
        private readonly CanvasOverlayController _overlayController;
        private readonly CanvasPointerDragController _pointerDragController;
        private CanvasSelectionKind _selectionKind = CanvasSelectionKind.None;
        private string _hoveredElementKey = string.Empty;
        private CanvasStageView _canvasStageView;

        public CanvasInteractionController(
            ICanvasWorkspaceHost host,
            CanvasViewportState viewportState,
            CanvasOverlayController overlayController,
            CanvasSceneProjector sceneProjector)
        {
            _host = host;
            _viewportState = viewportState;
            _sceneProjector = sceneProjector;
            _overlayController = overlayController;
            _pointerDragController = new CanvasPointerDragController(
                this,
                viewportState,
                overlayController,
                sceneProjector);
        }

        public CanvasSelectionKind SelectionKind => _selectionKind;

        private PreviewSnapshot PreviewSnapshot => _host.PreviewSnapshot;
        private Image PreviewImage => _host.PreviewImage;

        public void Bind(CanvasStageView canvasStageView, Toggle moveToolToggle)
        {
            _canvasStageView = canvasStageView;
            _pointerDragController.Bind(canvasStageView, moveToolToggle);
            UpdateZoomHud();
        }

        public void Dispose()
        {
            _pointerDragController.Dispose();
            _canvasStageView = null;
        }

        public void SetSelectionKind(CanvasSelectionKind selectionKind)
        {
            _selectionKind = selectionKind;
        }

        public void ResetCanvasView(bool clearSelection = false)
        {
            if (PreviewSnapshot == null)
            {
                UpdateCanvasVisualState();
                return;
            }

            _pointerDragController.ResetViewportToActualSize();
            if (clearSelection)
            {
                ResetSelection();
            }

            UpdateCanvasVisualState();
        }

        public void FitCanvasView(bool clearSelection = false)
        {
            if (PreviewSnapshot == null)
            {
                UpdateCanvasVisualState();
                return;
            }

            _pointerDragController.ResetViewportToFit();
            if (clearSelection)
            {
                ResetSelection();
            }

            UpdateCanvasVisualState();
        }

        public void SyncCanvasFrameToPreview()
        {
            _pointerDragController.SyncFrameToPreview();
        }

        public void ResetSelection()
        {
            _selectionKind = CanvasSelectionKind.None;
            _overlayController.ClearSelection();
            UpdateHoverVisual();
        }

        public void UpdateCanvasVisualState()
        {
            _sceneProjector.UpdateFrameVisual(PreviewImage, PreviewSnapshot, _overlayController, _host.CurrentDocument, _pointerDragController.CanvasOverlay);
            UpdateZoomHud();
            UpdateSelectionVisual();
            UpdateHoverVisual();
        }

        public void UpdateSelectionVisual()
        {
            if (PreviewSnapshot == null || SelectionKind == CanvasSelectionKind.None)
            {
                _overlayController.ClearSelection();
                return;
            }

            if (_pointerDragController.IsDraggingSelectionPreview)
            {
                if (_pointerDragController.DragMode == CanvasDragMode.RotateElement &&
                    _sceneProjector.TryResolveSelectedElementSceneRect(PreviewSnapshot, _host.SelectedElementKey, out Rect rotatedElementSceneRect) &&
                    _sceneProjector.TrySceneRectToViewportRect(PreviewSnapshot, rotatedElementSceneRect, out Rect rotatedElementViewportRect))
                {
                    CanvasSelectionVisual rotatedSelectionVisual = _sceneProjector.BuildSelectionVisual(
                        PreviewSnapshot,
                        CanvasSelectionKind.Element,
                        rotatedElementViewportRect,
                        rotatedElementSceneRect.size,
                        false);
                    PreviewElementGeometry rotatedGeometry = _sceneProjector.FindPreviewElement(PreviewSnapshot, _host.SelectedElementKey);
                    if (rotatedGeometry != null &&
                        _sceneProjector.TryScenePointToViewportPoint(PreviewSnapshot, rotatedGeometry.RotationPivotWorld, out Vector2 rotationPivotViewport))
                    {
                        rotatedSelectionVisual.HasRotationPivot = true;
                        rotatedSelectionVisual.RotationPivotViewport = rotationPivotViewport;
                    }

                    _overlayController.SetSelection(rotatedSelectionVisual);
                    return;
                }

                Rect sourceRect = _pointerDragController.DragMode == CanvasDragMode.ResizeElement
                    ? _sceneProjector.BuildScaledSceneRect(
                        _pointerDragController.DragStartSelectionViewportRect,
                        _pointerDragController.DragStartElementSceneRect,
                        _pointerDragController.DragCurrentSelectionViewportRect,
                        _pointerDragController.DragMode == CanvasDragMode.ResizeElement
                            ? _pointerDragController.ActiveHandle
                            : CanvasHandle.None,
                        _pointerDragController.DragResizeCenterAnchor)
                    : _pointerDragController.DragStartElementSceneRect;

                _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                    _pointerDragController.DragStartProjectionSceneRect,
                    _pointerDragController.DragStartPreserveAspectRatioMode,
                    CanvasSelectionKind.Element,
                    _pointerDragController.DragCurrentSelectionViewportRect,
                    sourceRect.size,
                    _pointerDragController.DragMode != CanvasDragMode.RotateElement));
                return;
            }

            if (SelectionKind == CanvasSelectionKind.Frame &&
                _sceneProjector.TryGetFrameViewportRect(out Rect frameViewportRect))
            {
                Vector2 frameSourceSize = PreviewSnapshot?.ProjectionRect.size ?? frameViewportRect.size;
                _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                    PreviewSnapshot,
                    CanvasSelectionKind.Frame,
                    frameViewportRect,
                    frameSourceSize,
                    false));
                return;
            }

            if (SelectionKind == CanvasSelectionKind.Element &&
                _sceneProjector.TryResolveSelectedElementSceneRect(PreviewSnapshot, _host.SelectedElementKey, out Rect selectedElementSceneRect) &&
                _sceneProjector.TrySceneRectToViewportRect(PreviewSnapshot, selectedElementSceneRect, out Rect elementViewportRect))
            {
                bool showHandles = !IsResizeUnsupported(_host.SelectedElementKey);
                CanvasSelectionVisual selectionVisual = _sceneProjector.BuildSelectionVisual(
                    PreviewSnapshot,
                    CanvasSelectionKind.Element,
                    elementViewportRect,
                    selectedElementSceneRect.size,
                    showHandles);
                PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(PreviewSnapshot, _host.SelectedElementKey);
                if (selectedGeometry != null &&
                    _sceneProjector.TryScenePointToViewportPoint(PreviewSnapshot, selectedGeometry.RotationPivotWorld, out Vector2 rotationPivotViewport))
                {
                    selectionVisual.HasRotationPivot = true;
                    selectionVisual.RotationPivotViewport = rotationPivotViewport;
                }

                _overlayController.SetSelection(selectionVisual);
                return;
            }

            _overlayController.ClearSelection();
        }

        public void SetHoveredElement(string elementKey)
        {
            _hoveredElementKey = elementKey ?? string.Empty;
            UpdateHoverVisual();
        }

        public void ClearHover()
        {
            _hoveredElementKey = string.Empty;
            _overlayController.ClearHover();
        }

        public void UpdateHoverVisual()
        {
            if (PreviewSnapshot == null ||
                string.IsNullOrWhiteSpace(_hoveredElementKey) ||
                (SelectionKind == CanvasSelectionKind.Element &&
                 string.Equals(_hoveredElementKey, _host.SelectedElementKey, System.StringComparison.Ordinal)))
            {
                _overlayController.ClearHover();
                return;
            }

            if (string.Equals(_hoveredElementKey, FrameHoverSentinel, System.StringComparison.Ordinal))
            {
                if (SelectionKind != CanvasSelectionKind.Frame &&
                    _sceneProjector.TryGetFrameViewportRect(out Rect frameViewportRect))
                {
                    _overlayController.SetHover(frameViewportRect);
                    return;
                }

                _overlayController.ClearHover();
                return;
            }

            if (_sceneProjector.TryResolveSelectedElementSceneRect(PreviewSnapshot, _hoveredElementKey, out Rect hoveredElementSceneRect) &&
                _sceneProjector.TrySceneRectToViewportRect(PreviewSnapshot, hoveredElementSceneRect, out Rect hoveredElementViewportRect))
            {
                _overlayController.SetHover(hoveredElementViewportRect);
                return;
            }

            _overlayController.ClearHover();
        }

        DocumentSession ICanvasPointerDragHost.CurrentDocument => _host.CurrentDocument;
        PreviewSnapshot ICanvasPointerDragHost.PreviewSnapshot => _host.PreviewSnapshot;
        string ICanvasPointerDragHost.SelectedElementKey => _host.SelectedElementKey;
        CanvasSelectionKind ICanvasPointerDragHost.SelectionKind
        {
            get => _selectionKind;
            set => _selectionKind = value;
        }

        void ICanvasPointerDragHost.RefreshLivePreview(bool keepExistingPreviewOnFailure) => _host.RefreshLivePreview(keepExistingPreviewOnFailure);
        bool ICanvasPointerDragHost.TryRefreshTransientPreview(SvgDocumentModel documentModel) => _host.TryRefreshTransientPreview(documentModel);
        void ICanvasPointerDragHost.RefreshInspector() => _host.RefreshInspector();
        void ICanvasPointerDragHost.RefreshInspector(SvgDocumentModel documentModel) => _host.RefreshInspector(documentModel);
        void ICanvasPointerDragHost.ApplyUpdatedSource(string updatedSource, string successStatus) => _host.ApplyUpdatedSource(updatedSource, successStatus);
        void ICanvasPointerDragHost.UpdateSourceStatus(string status) => _host.UpdateSourceStatus(status);
        StructureNode ICanvasPointerDragHost.FindStructureNode(string elementKey) => _host.FindStructureNode(elementKey);
        void ICanvasPointerDragHost.SelectFrame() => _host.SelectFrameFromCanvas();
        void ICanvasPointerDragHost.SelectElement(string elementKey, bool syncPatchTarget) => _host.SelectStructureElementFromCanvas(elementKey, syncPatchTarget);
        void ICanvasPointerDragHost.ClearSelection() => _host.ClearStructureSelectionFromCanvas();
        void ICanvasPointerDragHost.UpdateStructureInteractivity(bool hasDocument) => _host.UpdateStructureInteractivity(hasDocument);
        void ICanvasPointerDragHost.UpdateCanvasVisualState() => UpdateCanvasVisualState();
        void ICanvasPointerDragHost.UpdateSelectionVisual() => UpdateSelectionVisual();
        void ICanvasPointerDragHost.SetHoveredElement(string elementKey) => SetHoveredElement(elementKey);
        void ICanvasPointerDragHost.ClearHover() => ClearHover();
        void ICanvasPointerDragHost.UpdateHoverVisual() => UpdateHoverVisual();

        internal bool TryNudgeSelectedElement(Vector2 sceneDelta)
        {
            if (PreviewSnapshot == null ||
                _selectionKind != CanvasSelectionKind.Element ||
                string.IsNullOrWhiteSpace(_host.SelectedElementKey))
            {
                return false;
            }

            PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(PreviewSnapshot, _host.SelectedElementKey);
            if (selectedGeometry == null ||
                !_pointerDragController.TryBuildNudgedSource(
                    _host.CurrentDocument,
                    _host.SelectedElementKey,
                    sceneDelta,
                    selectedGeometry.ParentWorldTransform,
                    out string updatedSource))
            {
                return false;
            }

            _host.ApplyUpdatedSource(updatedSource, $"Moved <{_host.FindStructureNode(_host.SelectedElementKey)?.TagName ?? "element"}>.");
            return true;
        }

        private bool IsResizeUnsupported(string elementKey)
        {
            string tagName = _host.FindStructureNode(elementKey)?.TagName;
            return string.Equals(tagName, "tspan", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, "textPath", System.StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateZoomHud()
        {
            if (_canvasStageView == null)
            {
                return;
            }

            float displayedZoomScale = 1f;
            if (PreviewSnapshot != null &&
                _sceneProjector.TryGetDisplayedZoomScale(PreviewSnapshot, out float resolvedZoomScale))
            {
                displayedZoomScale = resolvedZoomScale;
            }

            _canvasStageView.SetZoomPercent(displayedZoomScale);
            _canvasStageView.SetHudEnabled(PreviewSnapshot != null);
            _canvasStageView.SetDirtyBadgeVisible(_host.CurrentDocument?.IsDirty == true);
        }
    }
}
