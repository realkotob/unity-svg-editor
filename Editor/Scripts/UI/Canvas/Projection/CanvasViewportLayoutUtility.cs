using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Extensions;

using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal static class CanvasViewportLayoutUtility
    {
        internal readonly struct ProjectionContext
        {
            public ProjectionContext(
                ViewportState viewportState,
                Rect projectionSceneRect,
                SvgPreserveAspectRatioMode preserveAspectRatioMode,
                float framePadding,
                float frameHeaderHeight)
            {
                ViewportState = viewportState;
                ProjectionSceneRect = projectionSceneRect;
                PreserveAspectRatioMode = preserveAspectRatioMode;
                FramePadding = framePadding;
                FrameHeaderHeight = frameHeaderHeight;
            }

            public ViewportState ViewportState { get; }
            public Rect ProjectionSceneRect { get; }
            public SvgPreserveAspectRatioMode PreserveAspectRatioMode { get; }
            public float FramePadding { get; }
            public float FrameHeaderHeight { get; }
        }

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

        public static ProjectionContext CreateProjectionContext(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight)
        {
            return new ProjectionContext(
                viewportState,
                GetPreviewSceneRect(previewSnapshot),
                previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
                framePadding,
                frameHeaderHeight);
        }

        public static ProjectionContext CreateProjectionContext(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight)
        {
            return new ProjectionContext(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight);
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
            ProjectionContext context,
            Rect sceneRect,
            out Rect viewportRect)
        {
            return ViewportMappingUtility.TrySceneRectToViewportRect(
                context,
                sceneRect,
                out viewportRect);
        }

        public static bool TryScenePointToViewportPoint(
            ProjectionContext context,
            Vector2 scenePoint,
            out Vector2 viewportPoint)
        {
            return ViewportMappingUtility.TryScenePointToViewportPoint(
                context,
                scenePoint,
                out viewportPoint);
        }

        public static bool TryViewportDeltaToScene(
            ProjectionContext context,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            return ViewportMappingUtility.TryViewportDeltaToScene(
                context,
                viewportDelta,
                out sceneDelta);
        }

        public static bool TryViewportPointToScenePoint(
            ProjectionContext context,
            Vector2 viewportPoint,
            out Vector2 scenePoint)
        {
            return ViewportMappingUtility.TryViewportPointToScenePoint(
                context,
                viewportPoint,
                out scenePoint);
        }

        public static bool TryGetDisplayedZoomScale(
            ProjectionContext context,
            out float displayedZoomScale)
        {
            return ViewportMappingUtility.TryGetDisplayedZoomScale(
                context,
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
