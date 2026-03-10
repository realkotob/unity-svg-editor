using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasElementDragController
    {
        private readonly StructureEditor _structureEditor;
        private readonly CanvasSceneProjector _sceneProjector;
        private readonly ElementMoveSession _moveSession = new();

        private Rect _dragStartSelectionViewportRect;
        private Rect _dragCurrentSelectionViewportRect;
        private Rect _dragStartElementSceneRect;
        private Rect _dragStartProjectionSceneRect;
        private string _dragElementKey = string.Empty;
        private string _dragResizePreviewSourceText = string.Empty;
        private Matrix2D _dragStartParentWorldTransform = Matrix2D.identity;

        public Rect DragCurrentSelectionViewportRect => _dragCurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _dragStartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _dragStartSelectionViewportRect;
        public Rect DragStartProjectionSceneRect => _dragStartProjectionSceneRect;
        public string DragElementKey => _dragElementKey;
        public string DragPreviewSourceText => _moveSession.PreviewSourceText;
        public string DragResizePreviewSourceText => _dragResizePreviewSourceText;

        public CanvasElementDragController(StructureEditor structureEditor, CanvasSceneProjector sceneProjector)
        {
            _structureEditor = structureEditor;
            _sceneProjector = sceneProjector;
        }

        public void BeginMove(
            PreviewSnapshot previewSnapshot,
            string elementKey,
            Vector2 localPosition,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform)
        {
            _dragElementKey = elementKey;
            _dragStartParentWorldTransform = parentWorldTransform;
            _dragStartProjectionSceneRect = previewSnapshot?.CanvasViewportRect ?? default;
            _dragStartSelectionViewportRect = _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, elementSceneRect, out Rect selectionViewportRect)
                ? selectionViewportRect
                : default;
            _dragCurrentSelectionViewportRect = _dragStartSelectionViewportRect;
            _dragStartElementSceneRect = elementSceneRect;
            _moveSession.Begin(elementKey, localPosition, _dragStartSelectionViewportRect, elementSceneRect);
            _dragResizePreviewSourceText = string.Empty;
        }

        public void BeginResize(
            string elementKey,
            Rect projectionSceneRect,
            Rect selectionViewportRect,
            Rect selectionSceneRect,
            Matrix2D parentWorldTransform)
        {
            _dragElementKey = elementKey;
            _dragStartParentWorldTransform = parentWorldTransform;
            _dragStartProjectionSceneRect = projectionSceneRect;
            _dragStartSelectionViewportRect = selectionViewportRect;
            _dragCurrentSelectionViewportRect = selectionViewportRect;
            _dragStartElementSceneRect = selectionSceneRect;
            _dragResizePreviewSourceText = string.Empty;
        }

        public Vector2 UpdateMove(Vector2 localPosition)
        {
            Vector2 viewportDelta = _moveSession.Update(localPosition);
            _dragCurrentSelectionViewportRect = _moveSession.CurrentSelectionViewportRect;
            return viewportDelta;
        }

        public void UpdateResize(Vector2 viewportDelta, CanvasHandle activeHandle)
        {
            _dragCurrentSelectionViewportRect = RectResizeUtility.ResizeRect(
                _dragStartSelectionViewportRect,
                activeHandle,
                viewportDelta,
                12f);
        }

        public Rect BuildScaledSceneRect(CanvasHandle handle)
        {
            return _sceneProjector.BuildScaledSceneRect(
                _dragStartSelectionViewportRect,
                _dragStartElementSceneRect,
                _dragCurrentSelectionViewportRect,
                handle);
        }

        public void End()
        {
            _dragElementKey = string.Empty;
            _dragResizePreviewSourceText = string.Empty;
            _dragStartProjectionSceneRect = default;
            _dragStartParentWorldTransform = Matrix2D.identity;
            _moveSession.End();
        }

        public bool TryRefreshMovePreview(
            ICanvasPointerDragHost host,
            Vector2 viewportDelta)
        {
            if (host.CurrentDocument == null || string.IsNullOrWhiteSpace(_dragElementKey))
            {
                return false;
            }

            if (!_sceneProjector.TryConvertViewportDeltaToSceneDelta(_dragStartProjectionSceneRect, viewportDelta, out Vector2 sceneDelta) ||
                sceneDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Vector2 svgTranslateDelta = ToParentSpaceDelta(sceneDelta);
            if (_structureEditor.TryPrependElementTranslation(
                    host.CurrentDocument.WorkingSourceText,
                    _dragElementKey,
                    svgTranslateDelta,
                    out string previewSource,
                    out _))
            {
                _moveSession.SetPreviewSource(previewSource);
                return host.TryRefreshTransientPreview(previewSource);
            }

            return false;
        }

        public bool TryRefreshResizePreview(
            ICanvasPointerDragHost host,
            CanvasHandle activeHandle)
        {
            if (host.CurrentDocument == null || string.IsNullOrWhiteSpace(_dragElementKey))
            {
                return false;
            }

            if (!_sceneProjector.TryBuildScaleTransform(
                    _dragStartSelectionViewportRect,
                    _dragStartElementSceneRect,
                    _dragCurrentSelectionViewportRect,
                    activeHandle,
                    out Vector2 scale,
                    out Vector2 pivot))
            {
                return false;
            }

            Vector2 svgPivot = ToParentSpacePoint(pivot);
            if (_structureEditor.TryPrependElementScale(
                    host.CurrentDocument.WorkingSourceText,
                    _dragElementKey,
                    scale,
                    svgPivot,
                    out string previewSource,
                    out _))
            {
                _dragResizePreviewSourceText = previewSource;
                return host.TryRefreshTransientPreview(previewSource);
            }

            return false;
        }

        public bool TryCommitDrag(
            ICanvasPointerDragHost host,
            CanvasDragMode dragMode,
            CanvasHandle activeHandle,
            Vector2 canvasDelta)
        {
            if (string.IsNullOrWhiteSpace(_dragElementKey) || canvasDelta.sqrMagnitude < 4f)
            {
                return false;
            }

            Vector2 sceneDelta = Vector2.zero;
            if (dragMode == CanvasDragMode.MoveElement &&
                (!_sceneProjector.TryConvertViewportDeltaToSceneDelta(_dragStartProjectionSceneRect, canvasDelta, out sceneDelta) ||
                 sceneDelta.sqrMagnitude <= Mathf.Epsilon))
            {
                return false;
            }

            Rect committedSceneRect = dragMode == CanvasDragMode.ResizeElement
                ? BuildScaledSceneRect(activeHandle)
                : new Rect(_dragStartElementSceneRect.position + sceneDelta, _dragStartElementSceneRect.size);

            if (host.CurrentDocument == null)
            {
                return false;
            }

            string updatedSource;
            string error;
            string dragPreviewSourceText = dragMode == CanvasDragMode.MoveElement
                ? _moveSession.PreviewSourceText
                : _dragResizePreviewSourceText;

            if (!string.IsNullOrWhiteSpace(dragPreviewSourceText))
            {
                updatedSource = dragPreviewSourceText;
            }
            else if (dragMode == CanvasDragMode.MoveElement)
            {
                if (!_structureEditor.TryPrependElementTranslation(
                        host.CurrentDocument.WorkingSourceText,
                        _dragElementKey,
                        ToParentSpaceDelta(sceneDelta),
                        out updatedSource,
                        out error))
                {
                    host.UpdateSourceStatus($"Move failed: {error}");
                    return false;
                }
            }
            else
            {
                if (!_sceneProjector.TryBuildScaleTransform(
                        _dragStartSelectionViewportRect,
                        _dragStartElementSceneRect,
                        _dragCurrentSelectionViewportRect,
                        activeHandle,
                        out Vector2 scale,
                        out Vector2 pivot))
                {
                    return false;
                }

                if (!_structureEditor.TryPrependElementScale(
                        host.CurrentDocument.WorkingSourceText,
                        _dragElementKey,
                        scale,
                        ToParentSpacePoint(pivot),
                        out updatedSource,
                        out error))
                {
                    host.UpdateSourceStatus($"Resize failed: {error}");
                    return false;
                }
            }

            host.ApplyUpdatedSource(
                updatedSource,
                dragMode == CanvasDragMode.ResizeElement
                    ? $"Resized <{host.FindStructureNode(_dragElementKey)?.TagName ?? "element"}>."
                    : $"Moved <{host.FindStructureNode(_dragElementKey)?.TagName ?? "element"}>.");
            return true;
        }

        // Converts a world-space direction vector to the element's SVG parent coordinate space.
        // The SVG transform attribute operates in parent space, so translate() deltas must be
        // expressed in parent space to achieve the intended world-space movement.
        private Vector2 ToParentSpaceDelta(Vector2 worldDelta)
        {
            return _dragStartParentWorldTransform.Inverse().MultiplyVector(worldDelta);
        }

        // Converts a world-space point (e.g., scale pivot) to the element's SVG parent coordinate space.
        private Vector2 ToParentSpacePoint(Vector2 worldPoint)
        {
            return _dragStartParentWorldTransform.Inverse().MultiplyPoint(worldPoint);
        }
    }
}
