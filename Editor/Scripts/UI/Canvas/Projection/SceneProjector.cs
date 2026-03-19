using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Source;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Preview.Geometry;

namespace SvgEditor.UI.Canvas
{
    internal sealed class SceneProjector
    {
        private readonly ViewportState _viewportState;
        private readonly CanvasSceneHitTestHelper _hitTestHelper;
        private readonly float _framePadding;
        private readonly float _frameHeaderHeight;
        private readonly float _alignmentGuideThreshold;

        public SceneProjector(
            ViewportState viewportState,
            ElementHitTester elementHitTester,
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
            return CanvasViewportLayoutUtility.GetCanvasBounds(canvasOverlay);
        }

        public Rect GetPreviewSceneRect(PreviewSnapshot previewSnapshot)
        {
            return CanvasViewportLayoutUtility.GetPreviewSceneRect(previewSnapshot);
        }

        public bool TryGetFrameViewportRect(out Rect frameViewportRect)
        {
            return CanvasViewportLayoutUtility.TryGetFrameViewportRect(_viewportState, out frameViewportRect);
        }

        public bool TryGetFrameContentViewportRect(PreviewSnapshot previewSnapshot, out Rect contentViewportRect)
        {
            return CanvasViewportLayoutUtility.TryGetFrameContentViewportRect(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                out contentViewportRect);
        }

        public bool TryGetFrameVisibleViewportRect(PreviewSnapshot previewSnapshot, out Rect visibleViewportRect)
        {
            return CanvasViewportLayoutUtility.TryGetFrameVisibleViewportRect(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight,
                out visibleViewportRect);
        }

        public bool TryGetFrameContentViewportRect(Rect projectionSceneRect, out Rect contentViewportRect)
        {
            return CanvasViewportLayoutUtility.TryGetFrameContentViewportRect(
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
            return CanvasViewportLayoutUtility.TryGetFrameContentViewportRect(
                _viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                _framePadding,
                _frameHeaderHeight,
                out contentViewportRect);
        }

        public bool TrySceneRectToViewportRect(PreviewSnapshot previewSnapshot, Rect sceneRect, out Rect viewportRect)
        {
            return CanvasViewportLayoutUtility.TrySceneRectToViewportRect(
                CreateProjectionContext(previewSnapshot),
                sceneRect,
                out viewportRect);
        }

        public bool TryViewportDeltaToScene(PreviewSnapshot previewSnapshot, Vector2 viewportDelta, out Vector2 sceneDelta)
        {
            return CanvasViewportLayoutUtility.TryViewportDeltaToScene(
                CreateProjectionContext(previewSnapshot),
                viewportDelta,
                out sceneDelta);
        }

        public bool TryViewportDeltaToScene(Rect projectionSceneRect, Vector2 viewportDelta, out Vector2 sceneDelta)
        {
            return CanvasViewportLayoutUtility.TryViewportDeltaToScene(
                CreateProjectionContext(projectionSceneRect),
                viewportDelta,
                out sceneDelta);
        }

        public bool TryViewportDeltaToScene(
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            return CanvasViewportLayoutUtility.TryViewportDeltaToScene(
                CreateProjectionContext(projectionSceneRect, preserveAspectRatioMode),
                viewportDelta,
                out sceneDelta);
        }

        public bool TryViewportPointToScenePoint(PreviewSnapshot previewSnapshot, Vector2 viewportPoint, out Vector2 scenePoint)
        {
            return CanvasViewportLayoutUtility.TryViewportPointToScenePoint(
                CreateProjectionContext(previewSnapshot),
                viewportPoint,
                out scenePoint);
        }

        public bool TryScenePointToViewportPoint(PreviewSnapshot previewSnapshot, Vector2 scenePoint, out Vector2 viewportPoint)
        {
            return CanvasViewportLayoutUtility.TryScenePointToViewportPoint(
                CreateProjectionContext(previewSnapshot),
                scenePoint,
                out viewportPoint);
        }

        public bool TryGetDisplayedZoomScale(PreviewSnapshot previewSnapshot, out float displayedZoomScale)
        {
            return CanvasViewportLayoutUtility.TryGetDisplayedZoomScale(
                CreateProjectionContext(previewSnapshot),
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
            SelectionKind selectionKind,
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
            SelectionVisualRequest request)
        {
            return CanvasProjectionMath.BuildSelectionVisual(
                _viewportState,
                CreateProjectionContext(previewSnapshot),
                _alignmentGuideThreshold,
                request);
        }

        public CanvasSelectionVisual BuildSelectionVisual(SelectionVisualRequest request)
        {
            return CanvasProjectionMath.BuildSelectionVisual(
                _viewportState,
                CreateProjectionContext(request.ProjectionSceneRect, request.PreserveAspectRatioMode),
                _alignmentGuideThreshold,
                request);
        }

        public Rect BuildScaledSceneRect(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            SelectionHandle handle,
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
            SelectionHandle handle,
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
            SelectionHandle handle,
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
            OverlayController overlayController,
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

            if (!CanvasViewportLayoutUtility.TryGetFrameContentLayout(
                    _viewportState,
                    previewSnapshot,
                    _framePadding,
                    _frameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
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

        private CanvasViewportLayoutUtility.ProjectionContext CreateProjectionContext(PreviewSnapshot previewSnapshot)
        {
            return CanvasViewportLayoutUtility.CreateProjectionContext(
                _viewportState,
                previewSnapshot,
                _framePadding,
                _frameHeaderHeight);
        }

        private CanvasViewportLayoutUtility.ProjectionContext CreateProjectionContext(
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode)
        {
            return CanvasViewportLayoutUtility.CreateProjectionContext(
                _viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                _framePadding,
                _frameHeaderHeight);
        }

        private CanvasViewportLayoutUtility.ProjectionContext CreateProjectionContext(Rect projectionSceneRect)
        {
            return CreateProjectionContext(projectionSceneRect, SvgPreserveAspectRatioMode.Meet);
        }
    }
}
