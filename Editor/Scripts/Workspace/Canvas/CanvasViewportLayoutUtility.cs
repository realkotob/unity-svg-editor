using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
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

        private readonly struct SceneViewportMapping
        {
            public SceneViewportMapping(Rect sceneRect, FrameContentLayout layout, Vector2 scale)
            {
                SceneRect = sceneRect;
                Layout = layout;
                Scale = scale;
            }

            public Rect SceneRect { get; }
            public FrameContentLayout Layout { get; }
            public Vector2 Scale { get; }
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

        public static bool TryGetFrameViewportRect(CanvasViewportState viewportState, out Rect frameViewportRect)
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
            CanvasViewportState viewportState,
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
                    out FrameContentLayout layout))
            {
                return false;
            }

            visibleViewportRect = layout.VisibleViewportRect;
            return true;
        }

        public static bool TryGetFrameVisibleViewportRect(
            CanvasViewportState viewportState,
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
                    out FrameContentLayout layout))
            {
                return false;
            }

            visibleViewportRect = layout.VisibleViewportRect;
            return true;
        }

        public static bool TryGetFrameContentViewportRect(
            CanvasViewportState viewportState,
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
                    out FrameContentLayout layout))
            {
                return false;
            }

            contentViewportRect = layout.ImageViewportRect;
            return true;
        }

        public static bool TryGetFrameContentViewportRect(
            CanvasViewportState viewportState,
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
            CanvasViewportState viewportState,
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
                    out FrameContentLayout layout))
            {
                return false;
            }

            contentViewportRect = layout.ImageViewportRect;
            return true;
        }

        public static bool TrySceneRectToViewportRect(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Rect sceneRect,
            out Rect viewportRect)
        {
            viewportRect = default;
            if (!TryGetSceneViewportMapping(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out SceneViewportMapping mapping))
            {
                return false;
            }

            viewportRect = new Rect(
                mapping.Layout.ImageViewportRect.xMin + ((sceneRect.xMin - mapping.SceneRect.xMin) * mapping.Scale.x),
                mapping.Layout.ImageViewportRect.yMin + ((sceneRect.yMin - mapping.SceneRect.yMin) * mapping.Scale.y),
                sceneRect.width * mapping.Scale.x,
                sceneRect.height * mapping.Scale.y);
            return true;
        }

        public static bool TryScenePointToViewportPoint(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Vector2 scenePoint,
            out Vector2 viewportPoint)
        {
            viewportPoint = default;
            if (!TryGetSceneViewportMapping(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out SceneViewportMapping mapping))
            {
                return false;
            }

            viewportPoint = new Vector2(
                mapping.Layout.ImageViewportRect.xMin + ((scenePoint.x - mapping.SceneRect.xMin) * mapping.Scale.x),
                mapping.Layout.ImageViewportRect.yMin + ((scenePoint.y - mapping.SceneRect.yMin) * mapping.Scale.y));
            return true;
        }

        public static bool TryConvertViewportDeltaToSceneDelta(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            sceneDelta = default;
            if (!TryGetSceneViewportMapping(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out SceneViewportMapping mapping))
            {
                return false;
            }

            sceneDelta = new Vector2(
                viewportDelta.x / mapping.Scale.x,
                viewportDelta.y / mapping.Scale.y);
            return true;
        }

        public static bool TryConvertViewportDeltaToSceneDelta(
            CanvasViewportState viewportState,
            Rect projectionSceneRect,
            float framePadding,
            float frameHeaderHeight,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            return TryConvertViewportDeltaToSceneDelta(
                viewportState,
                projectionSceneRect,
                SvgPreserveAspectRatioMode.Meet,
                framePadding,
                frameHeaderHeight,
                viewportDelta,
                out sceneDelta);
        }

        public static bool TryConvertViewportDeltaToSceneDelta(
            CanvasViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            sceneDelta = default;
            if (!TryGetSceneViewportMapping(
                    viewportState,
                    projectionSceneRect,
                    preserveAspectRatioMode,
                    framePadding,
                    frameHeaderHeight,
                    out SceneViewportMapping mapping))
            {
                return false;
            }

            sceneDelta = new Vector2(
                viewportDelta.x / mapping.Scale.x,
                viewportDelta.y / mapping.Scale.y);
            return true;
        }

        public static bool TryViewportPointToScenePoint(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Vector2 viewportPoint,
            out Vector2 scenePoint)
        {
            scenePoint = default;
            if (!TryGetSceneViewportMapping(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out SceneViewportMapping mapping))
            {
                return false;
            }

            scenePoint = new Vector2(
                mapping.SceneRect.xMin + ((viewportPoint.x - mapping.Layout.ImageViewportRect.xMin) / mapping.Scale.x),
                mapping.SceneRect.yMin + ((viewportPoint.y - mapping.Layout.ImageViewportRect.yMin) / mapping.Scale.y));
            return true;
        }

        public static bool TryGetDisplayedZoomScale(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out float displayedZoomScale)
        {
            displayedZoomScale = 1f;
            if (!TryGetSceneViewportMapping(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out SceneViewportMapping mapping))
            {
                return false;
            }

            displayedZoomScale = ResolveDisplayedZoomScale(mapping.Scale);
            return displayedZoomScale > Mathf.Epsilon;
        }

        public static bool TryGetFrameContentLayout(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out FrameContentLayout layout)
        {
            return TryGetFrameContentLayout(
                viewportState,
                GetPreviewSceneRect(previewSnapshot),
                previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
                framePadding,
                frameHeaderHeight,
                out layout);
        }

        public static bool TryGetFrameContentLayout(
            CanvasViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out FrameContentLayout layout)
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

            layout = new FrameContentLayout(visibleViewportRect, imageViewportRect);
            return true;
        }

        private static bool TryGetSceneViewportMapping(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out SceneViewportMapping mapping)
        {
            return TryGetSceneViewportMapping(
                viewportState,
                GetPreviewSceneRect(previewSnapshot),
                previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
                framePadding,
                frameHeaderHeight,
                out mapping);
        }

        private static bool TryGetSceneViewportMapping(
            CanvasViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out SceneViewportMapping mapping)
        {
            mapping = default;
            if (!TryGetFrameContentLayout(
                    viewportState,
                    projectionSceneRect,
                    preserveAspectRatioMode,
                    framePadding,
                    frameHeaderHeight,
                    out FrameContentLayout layout))
            {
                return false;
            }

            Vector2 scale = new(
                layout.ImageViewportRect.width / projectionSceneRect.width,
                layout.ImageViewportRect.height / projectionSceneRect.height);
            if (scale.x <= Mathf.Epsilon || scale.y <= Mathf.Epsilon)
            {
                return false;
            }

            mapping = new SceneViewportMapping(projectionSceneRect, layout, scale);
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

        private static float ResolveDisplayedZoomScale(Vector2 scale)
        {
            if (scale.x <= Mathf.Epsilon || scale.y <= Mathf.Epsilon)
            {
                return 0f;
            }

            return Mathf.Abs(scale.x - scale.y) <= 0.0001f
                ? scale.x
                : Mathf.Min(scale.x, scale.y);
        }
    }
}
