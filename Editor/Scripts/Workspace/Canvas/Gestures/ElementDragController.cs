using UnityEngine;
using Unity.VectorGraphics;
using SvgEditor.Document;
using SvgEditor.Shared;
using SvgEditor.Workspace.Transforms;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class ElementDragController
    {
        private readonly SceneProjector _sceneProjector;
        private readonly ElementDragMutationService _mutationService;
        private readonly ElementMoveSession _moveSession = new();
        private readonly CanvasTransientDocumentModelSession _transientDocumentModelSession = new();
        private readonly ElementRotationSession _rotationSession = new();
        private readonly ElementDragState _state = new();

        public Rect DragCurrentSelectionViewportRect => _state.CurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _state.StartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _state.StartSelectionViewportRect;
        public Rect DragStartProjectionSceneRect => _state.StartProjectionSceneRect;
        public SvgPreserveAspectRatioMode DragStartPreserveAspectRatioMode => _state.StartPreserveAspectRatioMode;
        public bool DragResizeCenterAnchor => _state.ResizeCenterAnchor;
        public string DragElementKey => _state.ElementKey;

        public ElementDragController(SceneProjector sceneProjector)
        {
            _sceneProjector = sceneProjector;
            _mutationService = new ElementDragMutationService(sceneProjector);
        }

        public void BeginMove(
            DocumentSession currentDocument,
            PreviewSnapshot previewSnapshot,
            string elementKey,
            Vector2 localPosition,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform)
        {
            Rect selectionViewportRect = _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, elementSceneRect, out Rect resolvedSelectionViewportRect)
                ? resolvedSelectionViewportRect
                : default;
            _state.BeginSelection(
                elementKey,
                previewSnapshot?.CanvasViewportRect ?? default,
                previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
                selectionViewportRect,
                elementSceneRect,
                parentWorldTransform);
            _moveSession.Begin(elementKey, localPosition, selectionViewportRect, elementSceneRect);
            _transientDocumentModelSession.TryBegin(currentDocument, elementKey);
        }

        public void BeginResize(
            DocumentSession currentDocument,
            string elementKey,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Rect selectionViewportRect,
            Rect selectionSceneRect,
            Matrix2D parentWorldTransform)
        {
            _state.BeginSelection(
                elementKey,
                projectionSceneRect,
                preserveAspectRatioMode,
                selectionViewportRect,
                selectionSceneRect,
                parentWorldTransform);
            _transientDocumentModelSession.TryBegin(currentDocument, elementKey);
        }

        public void BeginRotate(
            DocumentSession currentDocument,
            PreviewSnapshot previewSnapshot,
            string elementKey,
            Vector2 localPosition,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform,
            Vector2 rotationPivotWorld,
            Vector2 rotationPivotParentSpace)
        {
            Rect selectionViewportRect = _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, elementSceneRect, out Rect resolvedSelectionViewportRect)
                ? resolvedSelectionViewportRect
                : default;
            _state.BeginSelection(
                elementKey,
                previewSnapshot?.CanvasViewportRect ?? default,
                previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
                selectionViewportRect,
                elementSceneRect,
                parentWorldTransform);
            Vector2 pivotViewport = _sceneProjector.TryScenePointToViewportPoint(previewSnapshot, rotationPivotWorld, out Vector2 resolvedPivotViewport)
                ? resolvedPivotViewport
                : selectionViewportRect.center;
            _state.BeginRotation(pivotViewport, localPosition - pivotViewport);
            _rotationSession.TryBegin(currentDocument, elementKey, rotationPivotParentSpace);
        }

        public Vector2 UpdateMove(Vector2 localPosition)
        {
            Vector2 viewportDelta = _moveSession.Update(localPosition);
            _state.CurrentSelectionViewportRect = _moveSession.CurrentSelectionViewportRect;
            return viewportDelta;
        }

        public void UpdateResize(Vector2 viewportDelta, SelectionHandle activeHandle, bool uniformScale, bool centerAnchor)
        {
            _state.ResizeCenterAnchor = centerAnchor;
            Rect resizedViewportRect = RectResizeUtility.ResizeRect(
                _state.StartSelectionViewportRect,
                activeHandle,
                viewportDelta,
                12f);
            _state.CurrentSelectionViewportRect = CanvasProjectionMath.GetResizeViewportRect(
                _state.StartSelectionViewportRect,
                resizedViewportRect,
                activeHandle,
                uniformScale,
                centerAnchor);
        }

        public Rect BuildScaledSceneRect(SelectionHandle handle)
        {
            return _sceneProjector.BuildScaledSceneRect(
                _state.StartSelectionViewportRect,
                _state.StartElementSceneRect,
                _state.CurrentSelectionViewportRect,
                handle,
                _state.ResizeCenterAnchor);
        }

        public void End()
        {
            _state.Reset();
            _transientDocumentModelSession.End();
            _rotationSession.End();
            _moveSession.End();
        }

        public bool TryUpdateMoveTransientState(
            ICanvasPointerDragHost host,
            Vector2 viewportDelta,
            bool axisLock,
            bool snapEnabled)
        {
            return _mutationService.TryUpdateMoveTransientState(
                host,
                _state,
                _transientDocumentModelSession,
                viewportDelta,
                axisLock,
                snapEnabled);
        }

        public bool TryUpdateRotateTransientState(
            ICanvasPointerDragHost host,
            Vector2 localPosition,
            bool snapEnabled)
        {
            return _mutationService.TryUpdateRotateTransientState(
                host,
                _state,
                _rotationSession,
                localPosition,
                snapEnabled);
        }

        public bool TryUpdateResizeTransientState(
            ICanvasPointerDragHost host,
            SelectionHandle activeHandle,
            bool snapEnabled)
        {
            return _mutationService.TryUpdateResizeTransientState(
                host,
                _state,
                _transientDocumentModelSession,
                activeHandle,
                snapEnabled);
        }

        public bool TryCommitDrag(
            ICanvasPointerDragHost host,
            DragMode dragMode,
            SelectionHandle activeHandle,
            Vector2 canvasDelta)
        {
            return _mutationService.TryCommitDrag(
                host,
                _state,
                _transientDocumentModelSession,
                _rotationSession,
                dragMode,
                canvasDelta);
        }

        public bool TryBuildNudgedSource(
            DocumentSession currentDocument,
            string elementKey,
            Vector2 sceneDelta,
            Matrix2D parentWorldTransform,
            out string updatedSource)
        {
            return _mutationService.TryBuildNudgedSource(
                currentDocument,
                elementKey,
                sceneDelta,
                parentWorldTransform,
                out updatedSource);
        }
    }
}
