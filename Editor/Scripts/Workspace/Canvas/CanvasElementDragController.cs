using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasElementDragController
    {
        private readonly StructureEditor _structureEditor;
        private readonly CanvasSceneProjector _sceneProjector;
        private readonly ElementMoveSession _moveSession = new();
        private string _committedElementKey = string.Empty;
        private Rect _committedSelectionViewportRect;
        private bool _hasCommittedSelectionViewportRect;

        private Rect _dragStartSelectionViewportRect;
        private Rect _dragCurrentSelectionViewportRect;
        private Rect _dragStartElementSceneRect;
        private string _dragElementKey = string.Empty;
        private string _dragResizePreviewSourceText = string.Empty;

        public Rect DragCurrentSelectionViewportRect => _dragCurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _dragStartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _dragStartSelectionViewportRect;
        public string DragElementKey => _dragElementKey;
        public string DragPreviewSourceText => _moveSession.PreviewSourceText;
        public string DragResizePreviewSourceText => _dragResizePreviewSourceText;

        public CanvasElementDragController(StructureEditor structureEditor, CanvasSceneProjector sceneProjector)
        {
            _structureEditor = structureEditor;
            _sceneProjector = sceneProjector;
        }

        public void ClearCommittedSelection()
        {
            _committedElementKey = string.Empty;
            _committedSelectionViewportRect = default;
            _hasCommittedSelectionViewportRect = false;
        }

        public bool TryGetCommittedSelectionViewportRect(string elementKey, out Rect viewportRect)
        {
            viewportRect = default;
            if (!_hasCommittedSelectionViewportRect ||
                string.IsNullOrWhiteSpace(elementKey) ||
                !string.Equals(_committedElementKey, elementKey, System.StringComparison.Ordinal))
            {
                return false;
            }

            viewportRect = _committedSelectionViewportRect;
            return true;
        }

        public void BeginMove(
            PreviewSnapshot previewSnapshot,
            string elementKey,
            Vector2 localPosition,
            Rect elementSceneRect)
        {
            _dragElementKey = elementKey;
            _dragStartSelectionViewportRect = TryGetCommittedSelectionViewportRect(elementKey, out Rect committedSelectionViewportRect)
                ? committedSelectionViewportRect
                : _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, elementSceneRect, out Rect selectionViewportRect)
                    ? selectionViewportRect
                    : default;
            _dragCurrentSelectionViewportRect = _dragStartSelectionViewportRect;
            _dragStartElementSceneRect = elementSceneRect;
            _moveSession.Begin(elementKey, localPosition, _dragStartSelectionViewportRect, elementSceneRect);
            _dragResizePreviewSourceText = string.Empty;
        }

        public void BeginResize(
            string elementKey,
            Rect selectionViewportRect,
            Rect selectionSceneRect)
        {
            _dragElementKey = elementKey;
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

        public Rect BuildScaledSceneRect()
        {
            return _sceneProjector.BuildScaledSceneRect(
                _dragStartSelectionViewportRect,
                _dragStartElementSceneRect,
                _dragCurrentSelectionViewportRect);
        }

        public void End()
        {
            _dragElementKey = string.Empty;
            _dragResizePreviewSourceText = string.Empty;
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

            if (!_sceneProjector.TryConvertViewportDeltaToSceneDelta(host.PreviewSnapshot, viewportDelta, out Vector2 sceneDelta) ||
                sceneDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            if (_structureEditor.TryPrependElementTranslation(
                    host.CurrentDocument.WorkingSourceText,
                    _dragElementKey,
                    sceneDelta,
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

            if (_structureEditor.TryPrependElementScale(
                    host.CurrentDocument.WorkingSourceText,
                    _dragElementKey,
                    scale,
                    pivot,
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
                (!_sceneProjector.TryConvertViewportDeltaToSceneDelta(host.PreviewSnapshot, canvasDelta, out sceneDelta) ||
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
                        sceneDelta,
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
                        pivot,
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
            CommitSelectionViewportRect(_dragElementKey, _dragCurrentSelectionViewportRect);
            return true;
        }

        private void CommitSelectionViewportRect(string elementKey, Rect viewportRect)
        {
            if (string.IsNullOrWhiteSpace(elementKey))
            {
                return;
            }

            _committedElementKey = elementKey;
            _committedSelectionViewportRect = viewportRect;
            _hasCommittedSelectionViewportRect = true;
        }
    }
}
