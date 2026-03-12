using System.Collections.Generic;
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
        private readonly CanvasDefinitionOverlayBuilder _definitionOverlayBuilder = new();
        private CanvasSelectionKind _selectionKind = CanvasSelectionKind.None;
        private string _hoveredElementKey = string.Empty;
        private CanvasStageView _canvasStageView;
        private IReadOnlyList<CanvasDefinitionOverlayVisual> _definitionOverlays = System.Array.Empty<CanvasDefinitionOverlayVisual>();
        private CanvasDefinitionProxySelection _selectedDefinitionProxy;

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
        public bool HasDefinitionProxySelection => _selectedDefinitionProxy != null;

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

        public void SetSelectionKind(CanvasSelectionKind selectionKind)
        {
            _selectionKind = selectionKind;
            if (selectionKind != CanvasSelectionKind.Element)
                ClearDefinitionProxySelection();
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
            ClearDefinitionProxySelection();
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
            if (PreviewSnapshot == null || SelectionKind == CanvasSelectionKind.None)
            {
                ClearDefinitionProxySelection();
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
                        CanvasSelectionKind.Element,
                        _pointerDragController.DragCurrentSelectionViewportRect,
                        _pointerDragController.DragStartElementSceneRect.size,
                        false));
                    _overlayController.SetDefinitionOverlays(BuildDraggedDefinitionOverlays());
                    return;
                }

                if (TryBuildDraggedSelectionVisual(out CanvasSelectionVisual draggedSelectionVisual))
                {
                    _overlayController.SetSelection(draggedSelectionVisual);
                    _overlayController.SetDefinitionOverlays(BuildDraggedDefinitionOverlays());
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
                _overlayController.SetDefinitionOverlays(BuildDraggedDefinitionOverlays());
                return;
            }

            UpdateDefinitionOverlayVisual();

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
                _overlayController.ClearDefinitionOverlays();
                return;
            }

            if (TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual selectedProxy) &&
                selectedProxy != null)
            {
                _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                    PreviewSnapshot,
                    CanvasSelectionKind.Element,
                    selectedProxy.ViewportBounds,
                    selectedProxy.SceneBounds.size,
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
                UpdateDefinitionOverlayVisual();
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

        private void UpdateDefinitionOverlayVisual()
        {
            DocumentSession currentDocument = _host.CurrentDocument;
            if (SelectionKind != CanvasSelectionKind.Element ||
                PreviewSnapshot == null ||
                currentDocument?.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(currentDocument.DocumentModelLoadError) ||
                !string.Equals(currentDocument.DocumentModel.SourceText, currentDocument.WorkingSourceText, System.StringComparison.Ordinal))
            {
                _definitionOverlays = System.Array.Empty<CanvasDefinitionOverlayVisual>();
                ClearDefinitionProxySelection();
                _overlayController.ClearDefinitionOverlays();
                return;
            }

            if (!_definitionOverlayBuilder.TryBuild(
                    currentDocument.DocumentModel,
                    _host.SelectedElementKey,
                    PreviewSnapshot,
                    _sceneProjector,
                    out IReadOnlyList<CanvasDefinitionOverlayVisual> overlays,
                    out _))
            {
                _definitionOverlays = System.Array.Empty<CanvasDefinitionOverlayVisual>();
                ClearDefinitionProxySelection();
                _overlayController.ClearDefinitionOverlays();
                return;
            }

            _definitionOverlays = overlays ?? System.Array.Empty<CanvasDefinitionOverlayVisual>();
            _overlayController.SetDefinitionOverlays(overlays);
            SyncDefinitionProxySelectionFromStructure();
            if (HasDefinitionProxySelection && !TryResolveSelectedDefinitionProxyVisual(out _))
            {
                ClearDefinitionProxySelection();
            }
        }
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
            TryGetSelectedDefinitionProxy(out overlay);
        void ICanvasPointerDragHost.SelectDefinitionProxy(CanvasDefinitionOverlayVisual overlay) => SelectDefinitionProxy(overlay);
        void ICanvasPointerDragHost.ClearDefinitionProxySelection() => ClearDefinitionProxySelection();

        internal bool TryNudgeSelectedElement(Vector2 sceneDelta)
        {
            if (PreviewSnapshot == null || _selectionKind != CanvasSelectionKind.Element)
            {
                return false;
            }

            if (TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual selectedProxy) &&
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
                return false;

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

        private void SelectDefinitionProxy(CanvasDefinitionOverlayVisual overlay)
        {
            if (overlay == null || string.IsNullOrWhiteSpace(overlay.ProxyElementKey))
                return;

            _selectedDefinitionProxy = new CanvasDefinitionProxySelection
            {
                SourceElementKey = _host.SelectedElementKey,
                ProxyElementKey = overlay.ProxyElementKey,
                DefinitionElementKey = overlay.DefinitionElementKey,
                Kind = overlay.Kind,
                ReferenceId = overlay.ReferenceId
            };
            _host.SelectStructureElementFromCanvas(overlay.ProxyElementKey, syncPatchTarget: false);
            _selectionKind = CanvasSelectionKind.Element;
            UpdateSelectionVisual();
        }

        private void ClearDefinitionProxySelection()
        {
            _selectedDefinitionProxy = null;
        }

        private bool TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual overlay)
        {
            return TryResolveSelectedDefinitionProxyVisual(out overlay);
        }

        private bool TryResolveSelectedDefinitionProxyVisual(out CanvasDefinitionOverlayVisual overlay)
        {
            overlay = null;
            if (_selectedDefinitionProxy == null ||
                string.IsNullOrWhiteSpace(_selectedDefinitionProxy.SourceElementKey) ||
                !string.Equals(_selectedDefinitionProxy.SourceElementKey, _host.SelectedElementKey, System.StringComparison.Ordinal) ||
                _definitionOverlays == null)
            {
                return false;
            }

            for (int index = 0; index < _definitionOverlays.Count; index++)
            {
                CanvasDefinitionOverlayVisual candidate = _definitionOverlays[index];
                if (candidate == null)
                    continue;

                if (candidate.Kind != _selectedDefinitionProxy.Kind ||
                    !string.Equals(candidate.ReferenceId, _selectedDefinitionProxy.ReferenceId, System.StringComparison.Ordinal) ||
                    !string.Equals(candidate.DefinitionElementKey, _selectedDefinitionProxy.DefinitionElementKey, System.StringComparison.Ordinal) ||
                    !string.Equals(candidate.ProxyElementKey, _selectedDefinitionProxy.ProxyElementKey, System.StringComparison.Ordinal))
                {
                    continue;
                }

                overlay = candidate;
                return true;
            }

            return false;
        }

        private void SyncDefinitionProxySelectionFromStructure()
        {
            StructureNode selectedNode = _host.SelectedStructureNode;
            if (selectedNode?.IsDefinitionProxy != true)
            {
                _selectedDefinitionProxy = null;
                return;
            }

            _selectedDefinitionProxy = new CanvasDefinitionProxySelection
            {
                SourceElementKey = selectedNode.SourceElementKey,
                ProxyElementKey = selectedNode.Key,
                DefinitionElementKey = selectedNode.DefinitionElementKey,
                Kind = selectedNode.DefinitionProxyKind,
                ReferenceId = selectedNode.DefinitionReferenceId
            };
        }

        private IReadOnlyList<CanvasDefinitionOverlayVisual> BuildDraggedDefinitionOverlays()
        {
            if (CanUseLiveDraggedPreview() &&
                TryBuildLiveDraggedDefinitionOverlays(out IReadOnlyList<CanvasDefinitionOverlayVisual> liveOverlays))
            {
                return liveOverlays;
            }

            Vector2 viewportDelta =
                _pointerDragController.DragCurrentSelectionViewportRect.position -
                _pointerDragController.DragStartSelectionViewportRect.position;

            if (viewportDelta.sqrMagnitude <= Mathf.Epsilon)
                return _definitionOverlays;

            List<CanvasDefinitionOverlayVisual> shifted = new(_definitionOverlays.Count);
            for (int index = 0; index < _definitionOverlays.Count; index++)
            {
                CanvasDefinitionOverlayVisual overlay = _definitionOverlays[index];
                if (overlay == null)
                    continue;
                shifted.Add(OffsetDefinitionOverlay(overlay, viewportDelta));
            }

            return shifted;
        }

        private bool TryBuildDraggedSelectionVisual(out CanvasSelectionVisual selectionVisual)
        {
            selectionVisual = null;

            if (PreviewSnapshot == null ||
                string.IsNullOrWhiteSpace(_host.SelectedElementKey) ||
                !_sceneProjector.TryResolveSelectedElementSceneRect(PreviewSnapshot, _host.SelectedElementKey, out Rect selectedElementSceneRect) ||
                !_sceneProjector.TrySceneRectToViewportRect(PreviewSnapshot, selectedElementSceneRect, out Rect selectedElementViewportRect))
            {
                return false;
            }

            if (!IsLiveDraggedPreviewReady(selectedElementViewportRect))
            {
                return false;
            }

            bool showHandles = _pointerDragController.DragMode != CanvasDragMode.RotateElement;
            selectionVisual = _sceneProjector.BuildSelectionVisual(
                PreviewSnapshot,
                CanvasSelectionKind.Element,
                selectedElementViewportRect,
                selectedElementSceneRect.size,
                showHandles);

            PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(PreviewSnapshot, _host.SelectedElementKey);
            if (selectedGeometry != null &&
                _sceneProjector.TryScenePointToViewportPoint(PreviewSnapshot, selectedGeometry.RotationPivotWorld, out Vector2 rotationPivotViewport))
            {
                selectionVisual.HasRotationPivot = true;
                selectionVisual.RotationPivotViewport = rotationPivotViewport;
            }

            return true;
        }

        private bool TryBuildLiveDraggedDefinitionOverlays(out IReadOnlyList<CanvasDefinitionOverlayVisual> overlays)
        {
            overlays = System.Array.Empty<CanvasDefinitionOverlayVisual>();

            DocumentSession currentDocument = _host.CurrentDocument;
            return currentDocument?.DocumentModel != null &&
                   PreviewSnapshot != null &&
                   !string.IsNullOrWhiteSpace(_host.SelectedElementKey) &&
                   _definitionOverlayBuilder.TryBuild(
                       currentDocument.DocumentModel,
                       _host.SelectedElementKey,
                       PreviewSnapshot,
                       _sceneProjector,
                       out overlays,
                       out _);
        }

        private bool CanUseLiveDraggedPreview()
        {
            if (PreviewSnapshot == null ||
                string.IsNullOrWhiteSpace(_host.SelectedElementKey) ||
                !_sceneProjector.TryResolveSelectedElementSceneRect(PreviewSnapshot, _host.SelectedElementKey, out Rect liveSceneRect) ||
                !_sceneProjector.TrySceneRectToViewportRect(PreviewSnapshot, liveSceneRect, out Rect liveViewportRect))
            {
                return false;
            }

            return IsLiveDraggedPreviewReady(liveViewportRect);
        }

        private bool IsLiveDraggedPreviewReady(Rect liveViewportRect)
        {
            if (_pointerDragController.DragMode == CanvasDragMode.RotateElement)
            {
                return true;
            }

            return RectsApproximatelyEqual(
                       _pointerDragController.DragCurrentSelectionViewportRect,
                       _pointerDragController.DragStartSelectionViewportRect) ||
                   !RectsApproximatelyEqual(
                       liveViewportRect,
                       _pointerDragController.DragStartSelectionViewportRect);
        }

        private static bool RectsApproximatelyEqual(Rect left, Rect right)
        {
            const float epsilon = 0.01f;
            return Mathf.Abs(left.xMin - right.xMin) <= epsilon &&
                   Mathf.Abs(left.yMin - right.yMin) <= epsilon &&
                   Mathf.Abs(left.width - right.width) <= epsilon &&
                   Mathf.Abs(left.height - right.height) <= epsilon;
        }

        private static CanvasDefinitionOverlayVisual OffsetDefinitionOverlay(CanvasDefinitionOverlayVisual overlay, Vector2 viewportDelta)
        {
            List<CanvasLineSegment> shiftedSegments = new();
            if (overlay.OutlineSegments != null)
            {
                for (int index = 0; index < overlay.OutlineSegments.Count; index++)
                {
                    CanvasLineSegment segment = overlay.OutlineSegments[index];
                    shiftedSegments.Add(new CanvasLineSegment(segment.Start + viewportDelta, segment.End + viewportDelta));
                }
            }

            Rect viewportBounds = overlay.ViewportBounds;
            viewportBounds.position += viewportDelta;

            return new CanvasDefinitionOverlayVisual
            {
                Kind = overlay.Kind,
                ReferenceId = overlay.ReferenceId,
                ProxyElementKey = overlay.ProxyElementKey,
                DefinitionElementKey = overlay.DefinitionElementKey,
                SceneBounds = overlay.SceneBounds,
                ParentWorldTransform = overlay.ParentWorldTransform,
                ViewportBounds = viewportBounds,
                OutlineSegments = shiftedSegments
            };
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
