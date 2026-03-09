using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasSceneProjector
    {
        private readonly CanvasViewportState _viewportState;
        private readonly CanvasSceneHitTestHelper _hitTestHelper;
        private readonly float _framePadding;
        private readonly float _frameHeaderHeight;
        private readonly float _alignmentGuideThreshold;

        public CanvasSceneProjector(
            CanvasViewportState viewportState,
            PreviewElementHitTester elementHitTester,
            float framePadding,
            float frameHeaderHeight,
            float alignmentGuideThreshold)
        {
            _viewportState = viewportState;
            _hitTestHelper = new CanvasSceneHitTestHelper(elementHitTester);
            _framePadding = framePadding;
            _frameHeaderHeight = frameHeaderHeight;
            _alignmentGuideThreshold = alignmentGuideThreshold;
        }

        public bool TryGetCanvasLocalPosition(VisualElement canvasOverlay, Vector2 worldPosition, out Vector2 localPosition)
        {
            localPosition = default;
            if (canvasOverlay == null)
                return false;

            localPosition = canvasOverlay.WorldToLocal(worldPosition);
            return true;
        }

        public Rect GetCanvasBounds(VisualElement canvasOverlay)
        {
            return CanvasProjectionMath.GetCanvasBounds(canvasOverlay);
        }

        public Rect GetPreviewSceneRect(PreviewSnapshot previewSnapshot)
        {
            return CanvasProjectionMath.GetPreviewSceneRect(previewSnapshot);
        }

        public bool TryGetFrameViewportRect(out Rect frameViewportRect)
        {
            return CanvasProjectionMath.TryGetFrameViewportRect(_viewportState, out frameViewportRect);
        }

        public bool TryGetFrameContentViewportRect(PreviewSnapshot previewSnapshot, out Rect contentViewportRect)
        {
            return CanvasProjectionMath.TryGetFrameContentViewportRect(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                out contentViewportRect);
        }

        public bool TrySceneRectToViewportRect(PreviewSnapshot previewSnapshot, Rect sceneRect, out Rect viewportRect)
        {
            return CanvasProjectionMath.TrySceneRectToViewportRect(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                sceneRect,
                out viewportRect);
        }

        public bool TryConvertViewportDeltaToSceneDelta(PreviewSnapshot previewSnapshot, Vector2 viewportDelta, out Vector2 sceneDelta)
        {
            return CanvasProjectionMath.TryConvertViewportDeltaToSceneDelta(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                viewportDelta,
                out sceneDelta);
        }

        public bool TryViewportPointToScenePoint(PreviewSnapshot previewSnapshot, Vector2 viewportPoint, out Vector2 scenePoint)
        {
            return CanvasProjectionMath.TryViewportPointToScenePoint(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                viewportPoint,
                out scenePoint);
        }

        public PreviewElementGeometry FindPreviewElement(PreviewSnapshot previewSnapshot, string elementKey)
        {
            return _hitTestHelper.FindPreviewElement(previewSnapshot, elementKey);
        }

        public bool TryHitTestFrame(Vector2 canvasLocalPoint, out Rect frameViewportRect)
        {
            return _hitTestHelper.TryHitTestFrame(_viewportState, canvasLocalPoint, out frameViewportRect);
        }

        public bool TryHitTestFrameChrome(PreviewSnapshot previewSnapshot, Vector2 canvasLocalPoint, out Rect frameViewportRect)
        {
            return _hitTestHelper.TryHitTestFrameChrome(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                canvasLocalPoint,
                out frameViewportRect);
        }

        public bool TryHitTestPreviewElement(PreviewSnapshot previewSnapshot, Vector2 canvasLocalPoint, out PreviewElementGeometry hitElement)
        {
            return _hitTestHelper.TryHitTestPreviewElement(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                canvasLocalPoint,
                out hitElement);
        }

        public bool TryResolveSelectedElementSceneRect(PreviewSnapshot previewSnapshot, string selectedElementKey, out Rect sceneRect)
        {
            return _hitTestHelper.TryResolveSelectedElementSceneRect(previewSnapshot, selectedElementKey, out sceneRect);
        }

        public bool TryBuildCurrentSelectionViewportRect(
            PreviewSnapshot previewSnapshot,
            CanvasSelectionKind selectionKind,
            string selectedElementKey,
            out Rect selectionViewportRect)
        {
            return _hitTestHelper.TryBuildCurrentSelectionViewportRect(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                selectionKind,
                selectedElementKey,
                out selectionViewportRect);
        }

        public CanvasSelectionVisual BuildSelectionVisual(
            PreviewSnapshot previewSnapshot,
            CanvasSelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showHandles)
        {
            return CanvasProjectionMath.BuildSelectionVisual(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                _alignmentGuideThreshold,
                kind,
                viewportRect,
                sourceSize,
                showHandles);
        }

        public Rect BuildScaledSceneRect(Rect dragStartSelectionViewportRect, Rect dragStartElementSceneRect, Rect currentViewportRect)
        {
            return CanvasProjectionMath.BuildScaledSceneRect(
                dragStartSelectionViewportRect,
                dragStartElementSceneRect,
                currentViewportRect);
        }

        public bool TryBuildScaleTransform(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            CanvasHandle handle,
            out Vector2 scale,
            out Vector2 pivot)
        {
            return CanvasProjectionMath.TryBuildScaleTransform(
                dragStartSelectionViewportRect,
                dragStartElementSceneRect,
                currentViewportRect,
                handle,
                out scale,
                out pivot);
        }

        public void UpdateFrameVisual(
            Image previewImage,
            PreviewSnapshot previewSnapshot,
            CanvasOverlayController overlayController,
            DocumentSession currentDocument,
            VisualElement canvasOverlay)
        {
            if (previewImage == null || previewSnapshot == null || !TryGetFrameViewportRect(out Rect frameViewportRect))
            {
                overlayController.ClearFrame();
                previewImage.style.left = 0f;
                previewImage.style.top = 0f;
                previewImage.style.width = 0f;
                previewImage.style.height = 0f;
                return;
            }

            overlayController.SetFrame(frameViewportRect, GetCanvasFrameLabel(currentDocument));
            if (!TryGetFrameContentViewportRect(previewSnapshot, out Rect contentViewportRect))
                return;

            previewImage.style.left = contentViewportRect.xMin - frameViewportRect.xMin;
            previewImage.style.top = contentViewportRect.yMin - frameViewportRect.yMin;
            previewImage.style.width = contentViewportRect.width;
            previewImage.style.height = contentViewportRect.height;
        }

        private static string GetCanvasFrameLabel(DocumentSession currentDocument)
        {
            if (currentDocument == null || string.IsNullOrWhiteSpace(currentDocument.AssetPath))
                return "Frame 1";

            return Path.GetFileNameWithoutExtension(currentDocument.AssetPath);
        }
    }
}
