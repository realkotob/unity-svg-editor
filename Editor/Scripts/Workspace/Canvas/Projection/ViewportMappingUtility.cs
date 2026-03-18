using UnityEngine;
using Core.UI.Extensions;

using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class ViewportMappingUtility
    {
        public static bool TrySceneRectToViewportRect(
            CanvasViewportLayoutUtility.ProjectionContext context,
            Rect sceneRect,
            out Rect viewportRect)
        {
            viewportRect = default;
            if (!TryGetSceneViewportMapping(context, out SceneViewportMapping mapping))
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
            CanvasViewportLayoutUtility.ProjectionContext context,
            Vector2 scenePoint,
            out Vector2 viewportPoint)
        {
            viewportPoint = default;
            if (!TryGetSceneViewportMapping(context, out SceneViewportMapping mapping))
            {
                return false;
            }

            viewportPoint = new Vector2(
                mapping.Layout.ImageViewportRect.xMin + ((scenePoint.x - mapping.SceneRect.xMin) * mapping.Scale.x),
                mapping.Layout.ImageViewportRect.yMin + ((scenePoint.y - mapping.SceneRect.yMin) * mapping.Scale.y));
            return true;
        }

        public static bool TryViewportDeltaToScene(
            CanvasViewportLayoutUtility.ProjectionContext context,
            Vector2 viewportDelta,
            out Vector2 sceneDelta)
        {
            sceneDelta = default;
            if (!TryGetSceneViewportMapping(context, out SceneViewportMapping mapping))
            {
                return false;
            }

            sceneDelta = new Vector2(
                viewportDelta.x / mapping.Scale.x,
                viewportDelta.y / mapping.Scale.y);
            return true;
        }

        public static bool TryViewportPointToScenePoint(
            CanvasViewportLayoutUtility.ProjectionContext context,
            Vector2 viewportPoint,
            out Vector2 scenePoint)
        {
            scenePoint = default;
            if (!TryGetSceneViewportMapping(context, out SceneViewportMapping mapping))
            {
                return false;
            }

            scenePoint = new Vector2(
                mapping.SceneRect.xMin + ((viewportPoint.x - mapping.Layout.ImageViewportRect.xMin) / mapping.Scale.x),
                mapping.SceneRect.yMin + ((viewportPoint.y - mapping.Layout.ImageViewportRect.yMin) / mapping.Scale.y));
            return true;
        }

        public static bool TryGetDisplayedZoomScale(
            CanvasViewportLayoutUtility.ProjectionContext context,
            out float displayedZoomScale)
        {
            displayedZoomScale = 1f;
            if (!TryGetSceneViewportMapping(context, out SceneViewportMapping mapping))
            {
                return false;
            }

            displayedZoomScale = ResolveDisplayedZoomScale(mapping.Scale);
            return displayedZoomScale > Mathf.Epsilon;
        }

        private static bool TryGetSceneViewportMapping(
            CanvasViewportLayoutUtility.ProjectionContext context,
            out SceneViewportMapping mapping)
        {
            mapping = default;
            if (!ViewportFrameLayoutCalculator.TryGetFrameContentLayout(
                    context.ViewportState,
                    context.ProjectionSceneRect,
                    context.PreserveAspectRatioMode,
                    context.FramePadding,
                    context.FrameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
            {
                return false;
            }

            Vector2 scale = new(
                layout.ImageViewportRect.width / context.ProjectionSceneRect.width,
                layout.ImageViewportRect.height / context.ProjectionSceneRect.height);
            if (scale.x <= Mathf.Epsilon || scale.y <= Mathf.Epsilon)
            {
                return false;
            }

            mapping = new SceneViewportMapping(context.ProjectionSceneRect, layout, scale);
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
