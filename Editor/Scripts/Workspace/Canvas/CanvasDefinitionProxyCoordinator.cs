using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySvgEditor.Editor.Workspace.Canvas
{
    internal sealed class CanvasDefinitionProxyCoordinator
    {
        private readonly CanvasDefinitionOverlayBuilder _definitionOverlayBuilder = new();

        private IReadOnlyList<CanvasDefinitionOverlayVisual> _definitionOverlays = Array.Empty<CanvasDefinitionOverlayVisual>();
        private CanvasDefinitionProxySelection _selectedDefinitionProxy;

        public bool HasDefinitionProxySelection => _selectedDefinitionProxy != null;

        public void ClearSelection()
        {
            _selectedDefinitionProxy = null;
        }

        public bool SetSelectedDefinitionProxy(string selectedElementKey, CanvasDefinitionOverlayVisual overlay)
        {
            if (overlay == null || string.IsNullOrWhiteSpace(overlay.ProxyElementKey))
            {
                return false;
            }

            _selectedDefinitionProxy = new CanvasDefinitionProxySelection
            {
                SourceElementKey = selectedElementKey,
                ProxyElementKey = overlay.ProxyElementKey,
                DefinitionElementKey = overlay.DefinitionElementKey,
                Kind = overlay.Kind,
                ReferenceId = overlay.ReferenceId
            };
            return true;
        }

        public bool TryGetSelectedDefinitionProxy(string selectedElementKey, out CanvasDefinitionOverlayVisual overlay)
        {
            return TryResolveSelectedDefinitionProxyVisual(selectedElementKey, out overlay);
        }

        public void UpdateDefinitionOverlayVisual(
            ICanvasWorkspaceHost host,
            CanvasSelectionKind selectionKind,
            CanvasOverlayController overlayController,
            CanvasSceneProjector sceneProjector)
        {
            var currentDocument = host.CurrentDocument;
            if (selectionKind != CanvasSelectionKind.Element ||
                host.PreviewSnapshot == null ||
                currentDocument?.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(currentDocument.DocumentModelLoadError) ||
                !string.Equals(currentDocument.DocumentModel.SourceText, currentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                _definitionOverlays = Array.Empty<CanvasDefinitionOverlayVisual>();
                ClearSelection();
                overlayController.ClearDefinitionOverlays();
                return;
            }

            if (!_definitionOverlayBuilder.TryBuild(
                    currentDocument.DocumentModel,
                    host.SelectedElementKey,
                    host.PreviewSnapshot,
                    sceneProjector,
                    out IReadOnlyList<CanvasDefinitionOverlayVisual> overlays,
                    out _))
            {
                _definitionOverlays = Array.Empty<CanvasDefinitionOverlayVisual>();
                ClearSelection();
                overlayController.ClearDefinitionOverlays();
                return;
            }

            _definitionOverlays = overlays ?? Array.Empty<CanvasDefinitionOverlayVisual>();
            overlayController.SetDefinitionOverlays(overlays);
            SyncDefinitionProxySelectionFromStructure(host.SelectedStructureNode);
            if (HasDefinitionProxySelection && !TryResolveSelectedDefinitionProxyVisual(host.SelectedElementKey, out _))
            {
                ClearSelection();
            }
        }

        public IReadOnlyList<CanvasDefinitionOverlayVisual> BuildDraggedDefinitionOverlays(
            ICanvasWorkspaceHost host,
            CanvasPointerDragController pointerDragController,
            CanvasSceneProjector sceneProjector)
        {
            if (CanUseLiveDraggedPreview(host, pointerDragController, sceneProjector) &&
                TryBuildLiveDraggedDefinitionOverlays(host, sceneProjector, out IReadOnlyList<CanvasDefinitionOverlayVisual> liveOverlays))
            {
                return liveOverlays;
            }

            Vector2 viewportDelta =
                pointerDragController.DragCurrentSelectionViewportRect.position -
                pointerDragController.DragStartSelectionViewportRect.position;

            if (viewportDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return _definitionOverlays;
            }

            var shifted = new List<CanvasDefinitionOverlayVisual>(_definitionOverlays.Count);
            for (var index = 0; index < _definitionOverlays.Count; index++)
            {
                var overlay = _definitionOverlays[index];
                if (overlay == null)
                {
                    continue;
                }

                shifted.Add(OffsetDefinitionOverlay(overlay, viewportDelta));
            }

            return shifted;
        }

        public bool TryBuildDraggedSelectionVisual(
            ICanvasWorkspaceHost host,
            CanvasPointerDragController pointerDragController,
            CanvasSceneProjector sceneProjector,
            out CanvasSelectionVisual selectionVisual)
        {
            selectionVisual = null;

            var previewSnapshot = host.PreviewSnapshot;
            if (previewSnapshot == null ||
                string.IsNullOrWhiteSpace(host.SelectedElementKey) ||
                !sceneProjector.TryResolveSelectedElementSceneRect(previewSnapshot, host.SelectedElementKey, out Rect selectedElementSceneRect) ||
                !sceneProjector.TrySceneRectToViewportRect(previewSnapshot, selectedElementSceneRect, out Rect selectedElementViewportRect))
            {
                return false;
            }

            if (!IsLiveDraggedPreviewReady(pointerDragController, selectedElementViewportRect))
            {
                return false;
            }

            var showHandles = pointerDragController.DragMode != CanvasDragMode.RotateElement;
            selectionVisual = sceneProjector.BuildSelectionVisual(
                previewSnapshot,
                CanvasSelectionKind.Element,
                selectedElementViewportRect,
                selectedElementSceneRect.size,
                showHandles);

            var selectedGeometry = sceneProjector.FindPreviewElement(previewSnapshot, host.SelectedElementKey);
            if (selectedGeometry != null &&
                sceneProjector.TryScenePointToViewportPoint(previewSnapshot, selectedGeometry.RotationPivotWorld, out Vector2 rotationPivotViewport))
            {
                selectionVisual.HasRotationPivot = true;
                selectionVisual.RotationPivotViewport = rotationPivotViewport;
            }

            return true;
        }

        private bool TryResolveSelectedDefinitionProxyVisual(string selectedElementKey, out CanvasDefinitionOverlayVisual overlay)
        {
            overlay = null;
            if (_selectedDefinitionProxy == null ||
                string.IsNullOrWhiteSpace(_selectedDefinitionProxy.SourceElementKey) ||
                !string.Equals(_selectedDefinitionProxy.SourceElementKey, selectedElementKey, StringComparison.Ordinal) ||
                _definitionOverlays == null)
            {
                return false;
            }

            for (var index = 0; index < _definitionOverlays.Count; index++)
            {
                var candidate = _definitionOverlays[index];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.Kind != _selectedDefinitionProxy.Kind ||
                    !string.Equals(candidate.ReferenceId, _selectedDefinitionProxy.ReferenceId, StringComparison.Ordinal) ||
                    !string.Equals(candidate.DefinitionElementKey, _selectedDefinitionProxy.DefinitionElementKey, StringComparison.Ordinal) ||
                    !string.Equals(candidate.ProxyElementKey, _selectedDefinitionProxy.ProxyElementKey, StringComparison.Ordinal))
                {
                    continue;
                }

                overlay = candidate;
                return true;
            }

            return false;
        }

        private void SyncDefinitionProxySelectionFromStructure(StructureNode selectedNode)
        {
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

        private bool TryBuildLiveDraggedDefinitionOverlays(
            ICanvasWorkspaceHost host,
            CanvasSceneProjector sceneProjector,
            out IReadOnlyList<CanvasDefinitionOverlayVisual> overlays)
        {
            overlays = Array.Empty<CanvasDefinitionOverlayVisual>();

            var currentDocument = host.CurrentDocument;
            return currentDocument?.DocumentModel != null &&
                   host.PreviewSnapshot != null &&
                   !string.IsNullOrWhiteSpace(host.SelectedElementKey) &&
                   _definitionOverlayBuilder.TryBuild(
                       currentDocument.DocumentModel,
                       host.SelectedElementKey,
                       host.PreviewSnapshot,
                       sceneProjector,
                       out overlays,
                       out _);
        }

        private bool CanUseLiveDraggedPreview(
            ICanvasWorkspaceHost host,
            CanvasPointerDragController pointerDragController,
            CanvasSceneProjector sceneProjector)
        {
            if (host.PreviewSnapshot == null ||
                string.IsNullOrWhiteSpace(host.SelectedElementKey) ||
                !sceneProjector.TryResolveSelectedElementSceneRect(host.PreviewSnapshot, host.SelectedElementKey, out Rect liveSceneRect) ||
                !sceneProjector.TrySceneRectToViewportRect(host.PreviewSnapshot, liveSceneRect, out Rect liveViewportRect))
            {
                return false;
            }

            return IsLiveDraggedPreviewReady(pointerDragController, liveViewportRect);
        }

        private static bool IsLiveDraggedPreviewReady(CanvasPointerDragController pointerDragController, Rect liveViewportRect)
        {
            if (pointerDragController.DragMode == CanvasDragMode.RotateElement)
            {
                return true;
            }

            return RectsApproximatelyEqual(
                       pointerDragController.DragCurrentSelectionViewportRect,
                       pointerDragController.DragStartSelectionViewportRect) ||
                   !RectsApproximatelyEqual(
                       liveViewportRect,
                       pointerDragController.DragStartSelectionViewportRect);
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
            var shiftedSegments = new List<CanvasLineSegment>();
            if (overlay.OutlineSegments != null)
            {
                for (var index = 0; index < overlay.OutlineSegments.Count; index++)
                {
                    var segment = overlay.OutlineSegments[index];
                    shiftedSegments.Add(new CanvasLineSegment(segment.Start + viewportDelta, segment.End + viewportDelta));
                }
            }

            var viewportBounds = overlay.ViewportBounds;
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
    }
}
