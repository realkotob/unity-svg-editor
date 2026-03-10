using System;
using Core.UI.Foundation;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasElementGestureHandler
    {
        private readonly ICanvasPointerDragHost _host;
        private readonly CanvasSceneProjector _sceneProjector;
        private readonly CanvasElementDragController _elementDragController;
        private readonly CanvasSelectionSyncService _selectionSyncService;
        private readonly PointerDragSession _dragSession;
        private readonly Func<VisualElement> _overlayAccessor;

        public CanvasElementGestureHandler(
            ICanvasPointerDragHost host,
            CanvasSceneProjector sceneProjector,
            CanvasElementDragController elementDragController,
            CanvasSelectionSyncService selectionSyncService,
            PointerDragSession dragSession,
            Func<VisualElement> overlayAccessor)
        {
            _host = host;
            _sceneProjector = sceneProjector;
            _elementDragController = elementDragController;
            _selectionSyncService = selectionSyncService;
            _dragSession = dragSession;
            _overlayAccessor = overlayAccessor;
        }

        public bool TryBeginResizeFromHandle(CanvasGestureState state, CanvasHandle handle, Vector2 localPosition, int pointerId)
        {
            if (_host.SelectionKind != CanvasSelectionKind.Element ||
                !_sceneProjector.TryResolveSelectedElementSceneRect(_host.PreviewSnapshot, _host.SelectedElementKey, out Rect selectedElementSceneRect) ||
                !_sceneProjector.TryBuildCurrentSelectionViewportRect(_host.PreviewSnapshot, _host.SelectionKind, _host.SelectedElementKey, out Rect selectionViewportRect))
            {
                return false;
            }

            PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, _host.SelectedElementKey);
            Matrix2D parentWorldTransform = selectedGeometry?.ParentWorldTransform ?? Matrix2D.identity;
            BeginResize(
                state,
                handle,
                localPosition,
                pointerId,
                _host.CurrentDocument,
                _host.PreviewSnapshot.CanvasViewportRect,
                _host.PreviewSnapshot.PreserveAspectRatioMode,
                selectionViewportRect,
                selectedElementSceneRect,
                _host.SelectedElementKey,
                parentWorldTransform);
            return true;
        }

        public void BeginMove(CanvasGestureState state, string elementKey, Vector2 localPosition, int pointerId, Rect elementSceneRect, Matrix2D parentWorldTransform)
        {
            state.Begin(CanvasDragMode.MoveElement, CanvasHandle.None, default, default);
            _elementDragController.BeginMove(_host.CurrentDocument, _host.PreviewSnapshot, elementKey, localPosition, elementSceneRect, parentWorldTransform);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        public void ApplyElementDelta(
            CanvasGestureState state,
            Vector2 localPosition,
            Vector2 viewportDelta,
            bool uniformScale,
            bool centerAnchor)
        {
            switch (state.Mode)
            {
                case CanvasDragMode.MoveElement:
                    _elementDragController.UpdateMove(localPosition);
                    _host.UpdateSelectionVisual();
                    _elementDragController.TryUpdateMoveTransientState(_host, viewportDelta);
                    break;
                case CanvasDragMode.ResizeElement:
                    _elementDragController.UpdateResize(viewportDelta, state.ActiveHandle, uniformScale, centerAnchor);
                    _host.UpdateSelectionVisual();
                    _elementDragController.TryUpdateResizeTransientState(_host, state.ActiveHandle);
                    break;
            }
        }

        public void Complete(CanvasDragMode dragMode, CanvasHandle activeHandle, Vector2 canvasDelta)
        {
            if (_elementDragController.TryCommitDrag(_host, dragMode, activeHandle, canvasDelta))
            {
                _selectionSyncService.SelectCanvasElement(_elementDragController.DragElementKey, syncPatchTarget: false);
                return;
            }

            _host.RefreshLivePreview(keepExistingPreviewOnFailure: true);
            _host.RefreshInspectorFromSource(_host.CurrentDocument?.WorkingSourceText);
            _host.UpdateSelectionVisual();
        }

        public void End()
        {
            _elementDragController.End();
        }

        private void BeginResize(
            CanvasGestureState state,
            CanvasHandle handle,
            Vector2 localPosition,
            int pointerId,
            DocumentSession currentDocument,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Rect selectionViewportRect,
            Rect selectionSceneRect,
            string elementKey,
            Matrix2D parentWorldTransform)
        {
            state.Begin(CanvasDragMode.ResizeElement, handle, default, default);
            _elementDragController.BeginResize(
                currentDocument,
                elementKey,
                projectionSceneRect,
                preserveAspectRatioMode,
                selectionViewportRect,
                selectionSceneRect,
                parentWorldTransform);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }
    }
}
