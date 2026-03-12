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

        public bool TryGetFrameVisibleViewportRect(PreviewSnapshot previewSnapshot, out Rect visibleViewportRect)
        {
            return CanvasProjectionMath.TryGetFrameVisibleViewportRect(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                out visibleViewportRect);
        }

        public bool TryGetFrameContentViewportRect(Rect projectionSceneRect, out Rect contentViewportRect)
        {
            return CanvasProjectionMath.TryGetFrameContentViewportRect(
                _viewportState,
                projectionSceneRect,
                _framePadding,
                _frameHeaderHeight,
                out contentViewportRect);
        }

        public bool TryGetFrameContentViewportRect(
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            out Rect contentViewportRect)
        {
            return CanvasProjectionMath.TryGetFrameContentViewportRect(
                _viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
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

        public bool TryConvertViewportDeltaToSceneDelta(Rect projectionSceneRect, Vector2 viewportDelta, out Vector2 sceneDelta)
        {
            return CanvasProjectionMath.TryConvertViewportDeltaToSceneDelta(
                _viewportState,
                projectionSceneRect,
                _framePadding,
                _frameHeaderHeight,
                viewportDelta,
                out sceneDelta);
        }

        public bool TryConvertViewportDeltaToSceneDelta(
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            return CanvasProjectionMath.TryConvertViewportDeltaToSceneDelta(
                _viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
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

        public bool TryScenePointToViewportPoint(PreviewSnapshot previewSnapshot, Vector2 scenePoint, out Vector2 viewportPoint)
        {
            return CanvasProjectionMath.TryScenePointToViewportPoint(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                scenePoint,
                out viewportPoint);
        }

        public bool TryGetDisplayedZoomScale(PreviewSnapshot previewSnapshot, out float displayedZoomScale)
        {
            return CanvasProjectionMath.TryGetDisplayedZoomScale(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                out displayedZoomScale);
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

        public CanvasSelectionVisual BuildSelectionVisual(
            Rect projectionSceneRect,
            CanvasSelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showHandles)
        {
            return CanvasProjectionMath.BuildSelectionVisual(
                _viewportState,
                projectionSceneRect,
                _framePadding,
                _frameHeaderHeight,
                _alignmentGuideThreshold,
                kind,
                viewportRect,
                sourceSize,
                showHandles);
        }

        public CanvasSelectionVisual BuildSelectionVisual(
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            CanvasSelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showHandles)
        {
            return CanvasProjectionMath.BuildSelectionVisual(
                _viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                _framePadding,
                _frameHeaderHeight,
                _alignmentGuideThreshold,
                kind,
                viewportRect,
                sourceSize,
                showHandles);
        }

        public Rect BuildScaledSceneRect(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            CanvasHandle handle,
            bool centerAnchor = false)
        {
            return CanvasProjectionMath.BuildScaledSceneRect(
                dragStartSelectionViewportRect,
                dragStartElementSceneRect,
                currentViewportRect,
                handle,
                centerAnchor);
        }

        public bool TryBuildScaleTransform(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            CanvasHandle handle,
            bool centerAnchor,
            out Vector2 scale,
            out Vector2 pivot)
        {
            return CanvasProjectionMath.TryBuildScaleTransform(
                dragStartSelectionViewportRect,
                dragStartElementSceneRect,
                currentViewportRect,
                handle,
                centerAnchor,
                out scale,
                out pivot);
        }

        public bool TryBuildScaleTransformFromSceneRect(
            Rect dragStartElementSceneRect,
            Rect currentSceneRect,
            CanvasHandle handle,
            bool centerAnchor,
            out Vector2 scale,
            out Vector2 pivot)
        {
            return CanvasProjectionMath.TryBuildScaleTransformFromSceneRect(
                dragStartElementSceneRect,
                currentSceneRect,
                handle,
                centerAnchor,
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

            if (!CanvasProjectionMath.TryGetFrameContentLayout(
                    _viewportState,
                    previewSnapshot,
                    _framePadding,
                    _frameHeaderHeight,
                    out CanvasProjectionMath.FrameContentLayout layout))
            {
                overlayController.ClearFrame();
                return;
            }

            // The image rect is already aspect-resolved in viewport space, so the
            // visual canvas frame should follow it directly instead of the larger
            // visible viewport container.
            overlayController.SetFrame(layout.ImageViewportRect, GetCanvasFrameLabel(currentDocument));
            overlayController.SetTextOverlays(previewSnapshot, this);
            previewImage.scaleMode = ScaleMode.StretchToFill;
            previewImage.style.left = 0f;
            previewImage.style.top = 0f;
            previewImage.style.width = layout.ImageViewportRect.width;
            previewImage.style.height = layout.ImageViewportRect.height;
        }

        private static string GetCanvasFrameLabel(DocumentSession currentDocument)
        {
            if (currentDocument == null || string.IsNullOrWhiteSpace(currentDocument.AssetPath))
                return "Frame 1";

            return Path.GetFileNameWithoutExtension(currentDocument.AssetPath);
        }
    }
}
