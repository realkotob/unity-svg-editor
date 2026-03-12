using UnityEngine;

using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class CanvasViewportFrameLayoutCalculator
    {
        public static bool TryGetFrameVisibleViewportRect(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out Rect visibleViewportRect)
        {
            visibleViewportRect = default;
            if (!TryGetFrameContentLayout(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
            {
                return false;
            }

            visibleViewportRect = layout.VisibleViewportRect;
            return true;
        }

        public static bool TryGetFrameVisibleViewportRect(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out Rect visibleViewportRect)
        {
            visibleViewportRect = default;
            if (!TryGetFrameContentLayout(
                    viewportState,
                    projectionSceneRect,
                    preserveAspectRatioMode,
                    framePadding,
                    frameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
            {
                return false;
            }

            visibleViewportRect = layout.VisibleViewportRect;
            return true;
        }

        public static bool TryGetFrameContentViewportRect(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out Rect contentViewportRect)
        {
            contentViewportRect = default;
            if (!TryGetFrameContentLayout(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
            {
                return false;
            }

            contentViewportRect = layout.ImageViewportRect;
            return true;
        }

        public static bool TryGetFrameContentViewportRect(
            ViewportState viewportState,
            Rect projectionSceneRect,
            float framePadding,
            float frameHeaderHeight,
            out Rect contentViewportRect)
        {
            return TryGetFrameContentViewportRect(
                viewportState,
                projectionSceneRect,
                SvgPreserveAspectRatioMode.Meet,
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
            contentViewportRect = default;
            if (!TryGetFrameContentLayout(
                    viewportState,
                    projectionSceneRect,
                    preserveAspectRatioMode,
                    framePadding,
                    frameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
            {
                return false;
            }

            contentViewportRect = layout.ImageViewportRect;
            return true;
        }

        public static bool TryGetFrameContentLayout(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out CanvasViewportLayoutUtility.FrameContentLayout layout)
        {
            return TryGetFrameContentLayout(
                viewportState,
                CanvasViewportLayoutUtility.GetPreviewSceneRect(previewSnapshot),
                previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
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
            out CanvasViewportLayoutUtility.FrameContentLayout layout)
        {
            layout = default;
            if (viewportState == null || !viewportState.HasFrame)
            {
                return false;
            }

            Rect frameViewportRect = viewportState.CanvasToViewport(viewportState.FrameRect);
            Rect visibleViewportRect = new(
                frameViewportRect.xMin + framePadding,
                frameViewportRect.yMin + frameHeaderHeight + framePadding,
                Mathf.Max(1f, frameViewportRect.width - (framePadding * 2f)),
                Mathf.Max(1f, frameViewportRect.height - frameHeaderHeight - (framePadding * 2f)));

            if (projectionSceneRect.width <= Mathf.Epsilon || projectionSceneRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            Rect imageViewportRect = GetImageViewportRect(
                visibleViewportRect,
                projectionSceneRect.size,
                preserveAspectRatioMode);
            if (imageViewportRect.width <= Mathf.Epsilon || imageViewportRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            layout = new CanvasViewportLayoutUtility.FrameContentLayout(visibleViewportRect, imageViewportRect);
            return true;
        }

        private static Rect GetImageViewportRect(
            Rect visibleViewportRect,
            Vector2 contentSize,
            SvgPreserveAspectRatioMode preserveAspectRatioMode)
        {
            if (preserveAspectRatioMode.IsNone)
            {
                return visibleViewportRect;
            }

            float safeWidth = Mathf.Max(contentSize.x, Mathf.Epsilon);
            float safeHeight = Mathf.Max(contentSize.y, Mathf.Epsilon);
            float scale = preserveAspectRatioMode.IsSlice
                ? Mathf.Max(visibleViewportRect.width / safeWidth, visibleViewportRect.height / safeHeight)
                : Mathf.Min(visibleViewportRect.width / safeWidth, visibleViewportRect.height / safeHeight);

            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            {
                scale = 1f;
            }

            Vector2 fittedSize = new(safeWidth * scale, safeHeight * scale);
            float x = AlignWithin(visibleViewportRect.xMin, visibleViewportRect.width, fittedSize.x, preserveAspectRatioMode.AlignX);
            float y = AlignWithin(visibleViewportRect.yMin, visibleViewportRect.height, fittedSize.y, preserveAspectRatioMode.AlignY);
            return new Rect(x, y, fittedSize.x, fittedSize.y);
        }

        private static float AlignWithin(
            float start,
            float available,
            float size,
            SvgPreserveAspectRatioAlignX alignX)
        {
            return alignX switch
            {
                SvgPreserveAspectRatioAlignX.Min => start,
                SvgPreserveAspectRatioAlignX.Max => start + (available - size),
                _ => start + ((available - size) * 0.5f)
            };
        }

        private static float AlignWithin(
            float start,
            float available,
            float size,
            SvgPreserveAspectRatioAlignY alignY)
        {
            return alignY switch
            {
                SvgPreserveAspectRatioAlignY.Min => start,
                SvgPreserveAspectRatioAlignY.Max => start + (available - size),
                _ => start + ((available - size) * 0.5f)
            };
        }
    }
}
