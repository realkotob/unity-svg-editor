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
        private readonly CanvasTransientDocumentModelSession _transientDocumentModelSession = new();

        private Rect _dragStartSelectionViewportRect;
        private Rect _dragCurrentSelectionViewportRect;
        private Rect _dragStartElementSceneRect;
        private Rect _dragStartProjectionSceneRect;
        private SvgPreserveAspectRatioMode _dragStartPreserveAspectRatioMode = SvgPreserveAspectRatioMode.Meet;
        private bool _dragResizeCenterAnchor;
        private string _dragElementKey = string.Empty;
        private Matrix2D _dragStartParentWorldTransform = Matrix2D.identity;

        public Rect DragCurrentSelectionViewportRect => _dragCurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _dragStartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _dragStartSelectionViewportRect;
        public Rect DragStartProjectionSceneRect => _dragStartProjectionSceneRect;
        public SvgPreserveAspectRatioMode DragStartPreserveAspectRatioMode => _dragStartPreserveAspectRatioMode;
        public bool DragResizeCenterAnchor => _dragResizeCenterAnchor;
        public string DragElementKey => _dragElementKey;

        public CanvasElementDragController(StructureEditor structureEditor, CanvasSceneProjector sceneProjector)
        {
            _structureEditor = structureEditor;
            _sceneProjector = sceneProjector;
        }

        public void BeginMove(
            DocumentSession currentDocument,
            PreviewSnapshot previewSnapshot,
            string elementKey,
            Vector2 localPosition,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform)
        {
            _dragElementKey = elementKey;
            _dragStartParentWorldTransform = parentWorldTransform;
            _dragStartProjectionSceneRect = previewSnapshot?.CanvasViewportRect ?? default;
            _dragStartPreserveAspectRatioMode = previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet;
            _dragStartSelectionViewportRect = _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, elementSceneRect, out Rect selectionViewportRect)
                ? selectionViewportRect
                : default;
            _dragCurrentSelectionViewportRect = _dragStartSelectionViewportRect;
            _dragStartElementSceneRect = elementSceneRect;
            _moveSession.Begin(elementKey, localPosition, _dragStartSelectionViewportRect, elementSceneRect);
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
            _dragElementKey = elementKey;
            _dragStartParentWorldTransform = parentWorldTransform;
            _dragStartProjectionSceneRect = projectionSceneRect;
            _dragStartPreserveAspectRatioMode = preserveAspectRatioMode;
            _dragStartSelectionViewportRect = selectionViewportRect;
            _dragCurrentSelectionViewportRect = selectionViewportRect;
            _dragStartElementSceneRect = selectionSceneRect;
            _dragResizeCenterAnchor = false;
            _transientDocumentModelSession.TryBegin(currentDocument, elementKey);
        }

        public Vector2 UpdateMove(Vector2 localPosition)
        {
            Vector2 viewportDelta = _moveSession.Update(localPosition);
            _dragCurrentSelectionViewportRect = _moveSession.CurrentSelectionViewportRect;
            return viewportDelta;
        }

        public void UpdateResize(Vector2 viewportDelta, CanvasHandle activeHandle, bool uniformScale, bool centerAnchor)
        {
            _dragResizeCenterAnchor = centerAnchor;
            Rect resizedViewportRect = RectResizeUtility.ResizeRect(
                _dragStartSelectionViewportRect,
                activeHandle,
                viewportDelta,
                12f);
            _dragCurrentSelectionViewportRect = CanvasProjectionMath.GetResizeViewportRect(
                _dragStartSelectionViewportRect,
                resizedViewportRect,
                activeHandle,
                uniformScale,
                centerAnchor);
        }

        public Rect BuildScaledSceneRect(CanvasHandle handle)
        {
            return _sceneProjector.BuildScaledSceneRect(
                _dragStartSelectionViewportRect,
                _dragStartElementSceneRect,
                _dragCurrentSelectionViewportRect,
                handle,
                _dragResizeCenterAnchor);
        }

        public void End()
        {
            _dragElementKey = string.Empty;
            _dragStartProjectionSceneRect = default;
            _dragStartPreserveAspectRatioMode = SvgPreserveAspectRatioMode.Meet;
            _dragResizeCenterAnchor = false;
            _dragStartParentWorldTransform = Matrix2D.identity;
            _transientDocumentModelSession.End();
            _moveSession.End();
        }

        public bool TryUpdateMoveTransientState(
            ICanvasPointerDragHost host,
            Vector2 viewportDelta)
        {
            if (host.CurrentDocument == null || string.IsNullOrWhiteSpace(_dragElementKey))
            {
                return false;
            }

            if (!_sceneProjector.TryConvertViewportDeltaToSceneDelta(
                    _dragStartProjectionSceneRect,
                    _dragStartPreserveAspectRatioMode,
                    viewportDelta,
                    out Vector2 sceneDelta))
            {
                return false;
            }

            Vector2 svgTranslateDelta = ToParentSpaceDelta(sceneDelta);
            if (!_transientDocumentModelSession.TryApplyTranslation(svgTranslateDelta) ||
                !_transientDocumentModelSession.TryBuildPreviewSource(out string previewSource, out _) ||
                !host.TryRefreshTransientPreview(previewSource))
            {
                return false;
            }

            host.RefreshInspectorFromSource(previewSource);
            return true;
        }

        public bool TryUpdateResizeTransientState(
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
                    _dragResizeCenterAnchor,
                    out Vector2 scale,
                    out Vector2 pivot))
            {
                return false;
            }

            Vector2 svgPivot = ToParentSpacePoint(pivot);
            if (!_transientDocumentModelSession.TryApplyScale(scale, svgPivot) ||
                !_transientDocumentModelSession.TryBuildPreviewSource(out string previewSource, out _) ||
                !host.TryRefreshTransientPreview(previewSource))
            {
                return false;
            }

            host.RefreshInspectorFromSource(previewSource);
            return true;
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
                (!_sceneProjector.TryConvertViewportDeltaToSceneDelta(
                     _dragStartProjectionSceneRect,
                     _dragStartPreserveAspectRatioMode,
                     canvasDelta,
                     out sceneDelta) ||
                 sceneDelta.sqrMagnitude <= Mathf.Epsilon))
            {
                return false;
            }

            if (host.CurrentDocument == null)
            {
                return false;
            }

            string updatedSource;
            string error;
            if (_transientDocumentModelSession.TryBuildCommittedSource(out updatedSource, out error))
            {
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
                        _dragResizeCenterAnchor,
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
