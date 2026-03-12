using UnityEngine;

using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class SceneViewportMappingUtility
    {
        public static bool TrySceneRectToViewportRect(
            ViewportState viewportState,
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
            ViewportState viewportState,
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
            ViewportState viewportState,
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
            ViewportState viewportState,
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
            ViewportState viewportState,
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
            ViewportState viewportState,
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
            ViewportState viewportState,
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

        private static bool TryGetSceneViewportMapping(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out SceneViewportMapping mapping)
        {
            return TryGetSceneViewportMapping(
                viewportState,
                CanvasViewportLayoutUtility.GetPreviewSceneRect(previewSnapshot),
                previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
                framePadding,
                frameHeaderHeight,
                out mapping);
        }

        private static bool TryGetSceneViewportMapping(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out SceneViewportMapping mapping)
        {
            mapping = default;
            if (!ViewportFrameLayoutCalculator.TryGetFrameContentLayout(
                    viewportState,
                    projectionSceneRect,
                    preserveAspectRatioMode,
                    framePadding,
                    frameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
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
