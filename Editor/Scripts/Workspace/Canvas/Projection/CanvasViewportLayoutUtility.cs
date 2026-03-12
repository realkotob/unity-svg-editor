using UnityEngine;
using UnityEngine.UIElements;

using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class CanvasViewportLayoutUtility
    {
        internal readonly struct FrameContentLayout
        {
            public FrameContentLayout(Rect visibleViewportRect, Rect imageViewportRect)
            {
                VisibleViewportRect = visibleViewportRect;
                ImageViewportRect = imageViewportRect;
            }

            public Rect VisibleViewportRect { get; }
            public Rect ImageViewportRect { get; }
        }

        public static Rect GetCanvasBounds(VisualElement canvasOverlay)
        {
            if (canvasOverlay == null)
            {
                return default;
            }

            float width = canvasOverlay.layout.width;
            float height = canvasOverlay.layout.height;

            if (width <= 0f)
            {
                width = canvasOverlay.resolvedStyle.width;
            }

            if (height <= 0f)
            {
                height = canvasOverlay.resolvedStyle.height;
            }

            width = Mathf.Max(1f, width);
            height = Mathf.Max(1f, height);
            return new Rect(0f, 0f, width, height);
        }

        public static Rect GetPreviewSceneRect(PreviewSnapshot previewSnapshot)
        {
            return previewSnapshot?.CanvasViewportRect ?? default;
        }

        public static bool TryGetFrameViewportRect(ViewportState viewportState, out Rect frameViewportRect)
        {
            frameViewportRect = default;
            if (viewportState == null || !viewportState.HasFrame)
            {
                return false;
            }

            frameViewportRect = viewportState.CanvasToViewport(viewportState.FrameRect);
            return true;
        }

        public static bool TryGetFrameVisibleViewportRect(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out Rect visibleViewportRect)
        {
            return ViewportFrameLayoutCalculator.TryGetFrameVisibleViewportRect(
                viewportState,
                previewSnapshot,
                framePadding,
                frameHeaderHeight,
                out visibleViewportRect);
        }

        public static bool TryGetFrameVisibleViewportRect(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out Rect visibleViewportRect)
        {
            return ViewportFrameLayoutCalculator.TryGetFrameVisibleViewportRect(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight,
                out visibleViewportRect);
        }

        public static bool TryGetFrameContentViewportRect(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out Rect contentViewportRect)
        {
            return ViewportFrameLayoutCalculator.TryGetFrameContentViewportRect(
                viewportState,
                previewSnapshot,
                framePadding,
                frameHeaderHeight,
                out contentViewportRect);
        }

        public static bool TryGetFrameContentViewportRect(
            ViewportState viewportState,
            Rect projectionSceneRect,
            float framePadding,
            float frameHeaderHeight,
            out Rect contentViewportRect)
        {
            return ViewportFrameLayoutCalculator.TryGetFrameContentViewportRect(
                viewportState,
                projectionSceneRect,
                framePadding,
                frameHeaderHeight,
                out contentViewportRect);
        }

        public static bool TryGetFrameContentViewportRect(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out Rect contentViewportRect)
        {
            return ViewportFrameLayoutCalculator.TryGetFrameContentViewportRect(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight,
                out contentViewportRect);
        }

        public static bool TrySceneRectToViewportRect(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Rect sceneRect,
            out Rect viewportRect)
        {
            return SceneViewportMappingUtility.TrySceneRectToViewportRect(
                viewportState,
                previewSnapshot,
                framePadding,
                frameHeaderHeight,
                sceneRect,
                out viewportRect);
        }

        public static bool TryScenePointToViewportPoint(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Vector2 scenePoint,
            out Vector2 viewportPoint)
        {
            return SceneViewportMappingUtility.TryScenePointToViewportPoint(
                viewportState,
                previewSnapshot,
                framePadding,
                frameHeaderHeight,
                scenePoint,
                out viewportPoint);
        }

        public static bool TryConvertViewportDeltaToSceneDelta(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            return SceneViewportMappingUtility.TryConvertViewportDeltaToSceneDelta(
                viewportState,
                previewSnapshot,
                framePadding,
                frameHeaderHeight,
                viewportDelta,
                out sceneDelta);
        }

        public static bool TryConvertViewportDeltaToSceneDelta(
            ViewportState viewportState,
            Rect projectionSceneRect,
            float framePadding,
            float frameHeaderHeight,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            return SceneViewportMappingUtility.TryConvertViewportDeltaToSceneDelta(
                viewportState,
                projectionSceneRect,
                framePadding,
                frameHeaderHeight,
                viewportDelta,
                out sceneDelta);
        }

        public static bool TryConvertViewportDeltaToSceneDelta(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            return SceneViewportMappingUtility.TryConvertViewportDeltaToSceneDelta(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight,
                viewportDelta,
                out sceneDelta);
        }

        public static bool TryViewportPointToScenePoint(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Vector2 viewportPoint,
            out Vector2 scenePoint)
        {
            return SceneViewportMappingUtility.TryViewportPointToScenePoint(
                viewportState,
                previewSnapshot,
                framePadding,
                frameHeaderHeight,
                viewportPoint,
                out scenePoint);
        }

        public static bool TryGetDisplayedZoomScale(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out float displayedZoomScale)
        {
            return SceneViewportMappingUtility.TryGetDisplayedZoomScale(
                viewportState,
                previewSnapshot,
                framePadding,
                frameHeaderHeight,
                out displayedZoomScale);
        }

        public static bool TryGetFrameContentLayout(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out FrameContentLayout layout)
        {
            return ViewportFrameLayoutCalculator.TryGetFrameContentLayout(
                viewportState,
                previewSnapshot,
                framePadding,
                frameHeaderHeight,
                out layout);
        }

        public static bool TryGetFrameContentLayout(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out FrameContentLayout layout)
        {
            return ViewportFrameLayoutCalculator.TryGetFrameContentLayout(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight,
                out layout);
        }
    }
}
