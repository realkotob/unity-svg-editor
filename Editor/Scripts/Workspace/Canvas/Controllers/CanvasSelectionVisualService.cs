using UnityEngine;
using SvgEditor;
using SvgEditor.Preview;
using SvgEditor.Shared;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class CanvasSelectionVisualService
    {
        private readonly ICanvasWorkspaceHost _host;
        private readonly SceneProjector _sceneProjector;
        private readonly OverlayController _overlayController;
        private readonly DefinitionProxyCoordinator _definitionProxyCoordinator;
        private readonly PointerDragController _pointerDragController;

        public CanvasSelectionVisualService(
            ICanvasWorkspaceHost host,
            SceneProjector sceneProjector,
            OverlayController overlayController,
            DefinitionProxyCoordinator definitionProxyCoordinator,
            PointerDragController pointerDragController)
        {
            _host = host;
            _sceneProjector = sceneProjector;
            _overlayController = overlayController;
            _definitionProxyCoordinator = definitionProxyCoordinator;
            _pointerDragController = pointerDragController;
        }

        public void UpdateSelectionVisual(SelectionKind selectionKind)
        {
            PreviewSnapshot previewSnapshot = _host.PreviewSnapshot;
            if (previewSnapshot == null || selectionKind == SelectionKind.None)
            {
                _definitionProxyCoordinator.ClearSelection();
                _overlayController.ClearSelection();
                _overlayController.ClearDefinitionOverlays();
                return;
            }

            if (TrySetDraggingSelectionVisual(previewSnapshot))
            {
                return;
            }

            _definitionProxyCoordinator.UpdateDefinitionOverlayVisual(_host, selectionKind, _overlayController, _sceneProjector);
            if (TrySetFrameSelection(previewSnapshot, selectionKind) ||
                TrySetDefinitionProxySelection(previewSnapshot) ||
                TrySetElementSelection(previewSnapshot, selectionKind))
            {
                return;
            }

            _overlayController.ClearSelection();
            _overlayController.ClearDefinitionOverlays();
        }

        private bool TrySetDraggingSelectionVisual(PreviewSnapshot previewSnapshot)
        {
            if (!_pointerDragController.IsDraggingSelectionPreview)
            {
                return false;
            }

            if (_definitionProxyCoordinator.HasDefinitionProxySelection)
            {
                CanvasSelectionVisual selectionVisual = _sceneProjector.BuildSelectionVisual(
                    previewSnapshot,
                    new SelectionVisualRequest(
                        SelectionKind.Element,
                        _pointerDragController.DragCurrentSelectionViewportRect,
                        _pointerDragController.DragStartElementSceneRect.size,
                        false));
                ApplyDraggingRotationVisual(selectionVisual);
                _overlayController.SetSelection(selectionVisual);
                _overlayController.SetDefinitionOverlays(_definitionProxyCoordinator.BuildDraggedOverlays(_host, _pointerDragController, _sceneProjector));
                return true;
            }

            if (_definitionProxyCoordinator.TryBuildDraggedSelection(_host, _pointerDragController, _sceneProjector, out CanvasSelectionVisual draggedSelectionVisual))
            {
                _overlayController.SetSelection(draggedSelectionVisual);
                _overlayController.SetDefinitionOverlays(_definitionProxyCoordinator.BuildDraggedOverlays(_host, _pointerDragController, _sceneProjector));
                return true;
            }

            Rect sourceRect = _pointerDragController.DragMode == DragMode.ResizeElement
                ? _sceneProjector.BuildScaledSceneRect(
                    _pointerDragController.DragStartSelectionViewportRect,
                    _pointerDragController.DragStartElementSceneRect,
                    _pointerDragController.DragCurrentSelectionViewportRect,
                    _pointerDragController.DragMode == DragMode.ResizeElement
                        ? _pointerDragController.ActiveHandle
                        : SelectionHandle.None,
                    _pointerDragController.DragResizeCenterAnchor)
                : _pointerDragController.DragStartElementSceneRect;

            CanvasSelectionVisual dragSelectionVisual = _sceneProjector.BuildSelectionVisual(
                new SelectionVisualRequest(
                    SelectionKind.Element,
                    _pointerDragController.DragCurrentSelectionViewportRect,
                    sourceRect.size,
                    _pointerDragController.DragMode != DragMode.RotateElement)
                    .WithProjection(
                        _pointerDragController.DragStartProjectionSceneRect,
                        _pointerDragController.DragStartPreserveAspectRatioMode));
            ApplyDraggingRotationVisual(dragSelectionVisual);
            _overlayController.SetSelection(dragSelectionVisual);
            _overlayController.SetDefinitionOverlays(_definitionProxyCoordinator.BuildDraggedOverlays(_host, _pointerDragController, _sceneProjector));
            return true;
        }

        private bool TrySetFrameSelection(PreviewSnapshot previewSnapshot, SelectionKind selectionKind)
        {
            if (selectionKind != SelectionKind.Frame ||
                !_sceneProjector.TryGetFrameViewportRect(out Rect frameViewportRect))
            {
                return false;
            }

            Vector2 frameSourceSize = previewSnapshot?.ProjectionRect.size ?? frameViewportRect.size;
            _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                previewSnapshot,
                new SelectionVisualRequest(
                    SelectionKind.Frame,
                    frameViewportRect,
                    frameSourceSize,
                    false)));
            _overlayController.ClearDefinitionOverlays();
            return true;
        }

        private bool TrySetDefinitionProxySelection(PreviewSnapshot previewSnapshot)
        {
            if (!_definitionProxyCoordinator.TryGetSelectedDefinitionProxy(_host.SelectedElementKey, out CanvasDefinitionOverlayVisual selectedProxy) ||
                selectedProxy == null)
            {
                return false;
            }

            _overlayController.SetSelection(_sceneProjector.BuildSelectionVisual(
                previewSnapshot,
                new SelectionVisualRequest(
                    SelectionKind.Element,
                    selectedProxy.ViewportBounds,
                    selectedProxy.SceneBounds.size,
                    false)));
            return true;
        }

        private bool TrySetElementSelection(PreviewSnapshot previewSnapshot, SelectionKind selectionKind)
        {
            if (selectionKind != SelectionKind.Element)
            {
                return false;
            }

            bool multipleSelection = _host.SelectedElementKeys != null && _host.SelectedElementKeys.Count > 1;
            Rect selectedElementSceneRect;
            if (multipleSelection)
            {
                if (!CanvasProjectionMath.TryGetCombinedSelectionSceneRect(
                        previewSnapshot,
                        _host.SelectedElementKeys,
                        out selectedElementSceneRect))
                {
                    return false;
                }
            }
            else if (!_sceneProjector.TryResolveSelectedElementSceneRect(previewSnapshot, _host.SelectedElementKey, out selectedElementSceneRect))
            {
                return false;
            }

            if (!_sceneProjector.TrySceneRectToViewportRect(previewSnapshot, selectedElementSceneRect, out Rect elementViewportRect))
            {
                return false;
            }

            bool showSelectionHandles = multipleSelection || !IsResizeUnsupported(_host.SelectedElementKey);
            CanvasSelectionVisual selectionVisual = _sceneProjector.BuildSelectionVisual(
                previewSnapshot,
                new SelectionVisualRequest(
                    SelectionKind.Element,
                    elementViewportRect,
                    selectedElementSceneRect.size,
                    showSelectionHandles));
            selectionVisual.AllowResizeHandleInteraction = !multipleSelection && showSelectionHandles;
            selectionVisual.AllowRotateHandleInteraction = showSelectionHandles;
            PreviewElementGeometry selectedGeometry = !multipleSelection
                ? _sceneProjector.FindPreviewElement(previewSnapshot, _host.SelectedElementKey)
                : null;
            if (selectedGeometry != null &&
                _sceneProjector.TryScenePointToViewportPoint(previewSnapshot, selectedGeometry.RotationPivotWorld, out Vector2 rotationPivotViewport))
            {
                selectionVisual.HasRotationPivot = true;
                selectionVisual.RotationPivotViewport = rotationPivotViewport;
            }

            _overlayController.SetSelection(selectionVisual);
            return true;
        }

        private void ApplyDraggingRotationVisual(CanvasSelectionVisual selectionVisual)
        {
            if (selectionVisual == null || _pointerDragController.DragMode != DragMode.RotateElement)
            {
                return;
            }

            selectionVisual.HasRotationPivot = true;
            selectionVisual.RotationPivotViewport = _pointerDragController.DragRotationPivotViewport;
            selectionVisual.RotationDegrees = _pointerDragController.CurrentRotationAngle;
        }

        private bool IsResizeUnsupported(string elementKey)
        {
            string tagName = _host.FindHierarchyNode(elementKey)?.TagName;
            return string.Equals(tagName, SvgTagName.TSPAN, System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.TEXT_PATH, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
