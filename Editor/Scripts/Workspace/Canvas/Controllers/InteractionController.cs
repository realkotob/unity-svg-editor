using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.DocumentModel;
using SvgEditor.Shared;
using SvgEditor.Document;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class InteractionController : ICanvasPointerDragHost
    {
        internal const string FrameHoverSentinel = "__canvas-frame-hover__";

        private readonly ICanvasWorkspaceHost _host;
        private readonly ViewportState _viewportState;
        private readonly SceneProjector _sceneProjector;
        private readonly OverlayController _overlayController;
        private readonly PointerDragController _pointerDragController;
        private readonly DefinitionProxyCoordinator _definitionProxyCoordinator = new();
        private SelectionKind _selectionKind = SelectionKind.None;
        private string _hoveredElementKey = string.Empty;
        private CanvasStageView _canvasStageView;

        public InteractionController(
            ICanvasWorkspaceHost host,
            ViewportState viewportState,
            OverlayController overlayController,
            SceneProjector sceneProjector)
        {
            _host = host;
            _viewportState = viewportState;
            _sceneProjector = sceneProjector;
            _overlayController = overlayController;
            _pointerDragController = new PointerDragController(
                this,
                viewportState,
                overlayController,
                sceneProjector);
        }

        public SelectionKind SelectionKind => _selectionKind;
        public bool HasDefinitionProxySelection => _definitionProxyCoordinator.HasDefinitionProxySelection;

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

        public bool TryCancelActiveDrag()
        {
            return _pointerDragController.TryCancelActiveDrag();
        }

        public void SetSelectionKind(SelectionKind selectionKind)
        {
            _selectionKind = selectionKind;
            if (selectionKind != SelectionKind.Element)
            {
                _definitionProxyCoordinator.ClearSelection();
            }
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
            _selectionKind = SelectionKind.None;
            _definitionProxyCoordinator.ClearSelection();
            _overlayController.ClearSelection();
            _overlayController.ClearDefinitionOverlays();
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
            if (PreviewSnapshot == null || SelectionKind == SelectionKind.None)
            {
                _definitionProxyCoordinator.ClearSelection();
                _overlayController.ClearSelection();
                _overlayController.ClearDefinitionOverlays();
                return;
            }

            if (_pointerDragController.IsDraggingSelectionPreview)
            {
                if (HasDefinitionProxySelection)
                {
                    _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                        PreviewSnapshot,
                        SelectionKind.Element,
                        _pointerDragController.DragCurrentSelectionViewportRect,
                        _pointerDragController.DragStartElementSceneRect.size,
                        false));
                    _overlayController.SetDefinitionOverlays(_definitionProxyCoordinator.BuildDraggedDefinitionOverlays(_host, _pointerDragController, _sceneProjector));
                    return;
                }

                if (_definitionProxyCoordinator.TryBuildDraggedSelectionVisual(_host, _pointerDragController, _sceneProjector, out CanvasSelectionVisual draggedSelectionVisual))
                {
                    _overlayController.SetSelection(draggedSelectionVisual);
                    _overlayController.SetDefinitionOverlays(_definitionProxyCoordinator.BuildDraggedDefinitionOverlays(_host, _pointerDragController, _sceneProjector));
                    return;
                }

                var sourceRect = _pointerDragController.DragMode == DragMode.ResizeElement
                    ? _sceneProjector.BuildScaledSceneRect(
                        _pointerDragController.DragStartSelectionViewportRect,
                        _pointerDragController.DragStartElementSceneRect,
                        _pointerDragController.DragCurrentSelectionViewportRect,
                        _pointerDragController.DragMode == DragMode.ResizeElement
                            ? _pointerDragController.ActiveHandle
                            : SelectionHandle.None,
                        _pointerDragController.DragResizeCenterAnchor)
                    : _pointerDragController.DragStartElementSceneRect;

                _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                    _pointerDragController.DragStartProjectionSceneRect,
                    _pointerDragController.DragStartPreserveAspectRatioMode,
                    SelectionKind.Element,
                    _pointerDragController.DragCurrentSelectionViewportRect,
                    sourceRect.size,
                    _pointerDragController.DragMode != DragMode.RotateElement));
                _overlayController.SetDefinitionOverlays(_definitionProxyCoordinator.BuildDraggedDefinitionOverlays(_host, _pointerDragController, _sceneProjector));
                return;
            }

            _definitionProxyCoordinator.UpdateDefinitionOverlayVisual(_host, _selectionKind, _overlayController, _sceneProjector);

            if (SelectionKind == SelectionKind.Frame &&
                _sceneProjector.TryGetFrameViewportRect(out Rect frameViewportRect))
            {
                var frameSourceSize = PreviewSnapshot?.ProjectionRect.size ?? frameViewportRect.size;
                _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                    PreviewSnapshot,
                    SelectionKind.Frame,
                    frameViewportRect,
                    frameSourceSize,
                    false));
                _overlayController.ClearDefinitionOverlays();
                return;
            }

            if (_definitionProxyCoordinator.TryGetSelectedDefinitionProxy(_host.SelectedElementKey, out CanvasDefinitionOverlayVisual selectedProxy) &&
                selectedProxy != null)
            {
                _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                    PreviewSnapshot,
                    SelectionKind.Element,
                    selectedProxy.ViewportBounds,
                    selectedProxy.SceneBounds.size,
                    false));
                return;
            }

            if (SelectionKind == SelectionKind.Element &&
                _sceneProjector.TryResolveSelectedElementSceneRect(PreviewSnapshot, _host.SelectedElementKey, out Rect selectedElementSceneRect) &&
                _sceneProjector.TrySceneRectToViewportRect(PreviewSnapshot, selectedElementSceneRect, out Rect elementViewportRect))
            {
                var showSelectionHandles = !IsResizeUnsupported(_host.SelectedElementKey);
                var selectionVisual = _sceneProjector.BuildSelectionVisual(
                    PreviewSnapshot,
                    SelectionKind.Element,
                    elementViewportRect,
                    selectedElementSceneRect.size,
                    showSelectionHandles);
                var selectedGeometry = _sceneProjector.FindPreviewElement(PreviewSnapshot, _host.SelectedElementKey);
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
            _overlayController.ClearDefinitionOverlays();
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
                (SelectionKind == SelectionKind.Element &&
                 string.Equals(_hoveredElementKey, _host.SelectedElementKey, System.StringComparison.Ordinal)))
            {
                _overlayController.ClearHover();
                return;
            }

            if (string.Equals(_hoveredElementKey, FrameHoverSentinel, System.StringComparison.Ordinal))
            {
                if (SelectionKind != SelectionKind.Frame &&
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
        SelectionKind ICanvasPointerDragHost.SelectionKind
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
        bool ICanvasPointerDragHost.HasDefinitionProxySelection => HasDefinitionProxySelection;
        bool ICanvasPointerDragHost.TryHitTestDefinitionOverlay(Vector2 localPoint, out CanvasDefinitionOverlayVisual overlay) =>
            _overlayController.TryHitTestDefinitionOverlay(localPoint, out overlay);
        bool ICanvasPointerDragHost.TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual overlay) =>
            _definitionProxyCoordinator.TryGetSelectedDefinitionProxy(_host.SelectedElementKey, out overlay);
        void ICanvasPointerDragHost.SelectDefinitionProxy(CanvasDefinitionOverlayVisual overlay) => SelectDefinitionProxy(overlay);
        void ICanvasPointerDragHost.ClearDefinitionProxySelection() => ClearDefinitionProxySelection();

        internal bool TryNudgeSelectedElement(Vector2 sceneDelta)
        {
            if (PreviewSnapshot == null || _selectionKind != SelectionKind.Element)
            {
                return false;
            }

            if (_definitionProxyCoordinator.TryGetSelectedDefinitionProxy(_host.SelectedElementKey, out CanvasDefinitionOverlayVisual selectedProxy) &&
                selectedProxy != null &&
                _pointerDragController.TryBuildNudgedSource(
                    _host.CurrentDocument,
                    selectedProxy.DefinitionElementKey,
                    sceneDelta,
                    selectedProxy.ParentWorldTransform,
                    out string proxyUpdatedSource))
            {
                _host.ApplyUpdatedSource(proxyUpdatedSource, $"Moved <{_host.FindStructureNode(selectedProxy.DefinitionElementKey)?.TagName ?? "definition"}>.");
                return true;
            }

            if (string.IsNullOrWhiteSpace(_host.SelectedElementKey))
            {
                return false;
            }

            var selectedGeometry = _sceneProjector.FindPreviewElement(PreviewSnapshot, _host.SelectedElementKey);
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

        private void SelectDefinitionProxy(CanvasDefinitionOverlayVisual overlay)
        {
            if (!_definitionProxyCoordinator.SetSelectedDefinitionProxy(_host.SelectedElementKey, overlay))
            {
                return;
            }

            _host.SelectStructureElementFromCanvas(overlay.ProxyElementKey, syncPatchTarget: false);
            _selectionKind = SelectionKind.Element;
            UpdateSelectionVisual();
        }

        private void ClearDefinitionProxySelection()
        {
            _definitionProxyCoordinator.ClearSelection();
        }

        private bool IsResizeUnsupported(string elementKey)
        {
            string tagName = _host.FindStructureNode(elementKey)?.TagName;
            return string.Equals(tagName, SvgTagName.TSPAN, System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.TEXT_PATH, System.StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateZoomHud()
        {
            if (_canvasStageView == null)
            {
                return;
            }

            var displayedZoomScale = 1f;
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
