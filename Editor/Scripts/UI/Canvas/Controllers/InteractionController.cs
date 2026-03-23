using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Hierarchy;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Preview;
using Core.UI.Extensions;

namespace SvgEditor.UI.Canvas
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
        private readonly CanvasSelectionVisualService _selectionVisualService;
        private readonly CanvasHoverVisualService _hoverVisualService;

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
            _selectionVisualService = new CanvasSelectionVisualService(
                host,
                sceneProjector,
                overlayController,
                _definitionProxyCoordinator,
                _pointerDragController);
            _hoverVisualService = new CanvasHoverVisualService(
                host,
                sceneProjector,
                overlayController);
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

            UpdateViewportVisualState();
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

            UpdateViewportVisualState();
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

        public void UpdateViewportVisualState()
        {
            _pointerDragController.ResyncPathEditSession(previewIsCurrent: true);
            UpdateCanvasVisualState();
        }

        public void UpdateCanvasVisualState()
        {
            _sceneProjector.UpdateFrameVisual(PreviewImage, PreviewSnapshot, _overlayController, _host.CurrentDocument, _pointerDragController.CanvasOverlay);
            UpdateZoomHud();
            UpdateSelectionVisual();
            UpdateHoverVisual();
        }

        public string ResyncPathEditSession(bool previewIsCurrent)
        {
            return _pointerDragController.ResyncPathEditSession(previewIsCurrent);
        }

        public void UpdateSelectionVisual()
        {
            _selectionVisualService.UpdateSelectionVisual(_selectionKind);
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
            _hoverVisualService.UpdateHoverVisual(_hoveredElementKey, _selectionKind);
        }

        DocumentSession ICanvasPointerDragHost.CurrentDocument => _host.CurrentDocument;
        PreviewSnapshot ICanvasPointerDragHost.PreviewSnapshot => _host.PreviewSnapshot;
        string ICanvasPointerDragHost.SelectedElementKey => _host.SelectedElementKey;
        IReadOnlyList<string> ICanvasPointerDragHost.SelectedElementKeys => _host.SelectedElementKeys;
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
        HierarchyNode ICanvasPointerDragHost.FindHierarchyNode(string elementKey) => _host.FindHierarchyNode(elementKey);
        void ICanvasPointerDragHost.SelectFrame() => _host.SelectFrameFromCanvas();
        void ICanvasPointerDragHost.SelectElement(string elementKey, bool syncPatchTarget) => _host.SelectStructureElementFromCanvas(elementKey, syncPatchTarget);
        void ICanvasPointerDragHost.ToggleElementSelection(string elementKey, bool syncPatchTarget) =>
            _host.ToggleStructureElementSelectionFromCanvas(elementKey, syncPatchTarget);
        void ICanvasPointerDragHost.ReplaceElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget) =>
            _host.ReplaceStructureElementSelectionFromCanvas(elementKeys, syncPatchTarget);
        void ICanvasPointerDragHost.AddElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget) =>
            _host.AddStructureElementSelectionFromCanvas(elementKeys, syncPatchTarget);
        void ICanvasPointerDragHost.ClearSelection() => _host.ClearStructureSelectionFromCanvas();
        void ICanvasPointerDragHost.UpdateStructureInteractivity(bool hasDocument) => _host.UpdateStructureInteractivity(hasDocument);
        void ICanvasPointerDragHost.UpdateViewportVisualState() => UpdateViewportVisualState();
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
            return CanvasNudgeService.TryNudgeSelectedElement(
                new CanvasNudgeRequest(
                    this,
                    _sceneProjector,
                    _pointerDragController,
                    sceneDelta));
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
