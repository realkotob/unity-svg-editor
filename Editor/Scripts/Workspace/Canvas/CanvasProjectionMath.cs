using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal static class CanvasProjectionMath
    {
        private readonly struct SceneViewportMapping
        {
            public SceneViewportMapping(Rect sceneRect, Rect viewportRect, Vector2 scale)
            {
                SceneRect = sceneRect;
                ViewportRect = viewportRect;
                Scale = scale;
            }

            public Rect SceneRect { get; }
            public Rect ViewportRect { get; }
            public Vector2 Scale { get; }
        }

        public static Rect GetCanvasBounds(VisualElement canvasOverlay)
        {
            if (canvasOverlay == null)
                return default;

            float width = canvasOverlay.layout.width;
            float height = canvasOverlay.layout.height;

            if (width <= 0f)
                width = canvasOverlay.resolvedStyle.width;
            if (height <= 0f)
                height = canvasOverlay.resolvedStyle.height;

            width = Mathf.Max(1f, width);
            height = Mathf.Max(1f, height);
            return new Rect(0f, 0f, width, height);
        }

        public static Rect GetPreviewSceneRect(PreviewSnapshot previewSnapshot)
        {
            return previewSnapshot?.CanvasViewportRect ?? default;
        }

        private static bool TryGetSceneViewportMapping(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            out SceneViewportMapping mapping)
        {
            mapping = default;
            Rect previewSceneRect = GetPreviewSceneRect(previewSnapshot);
            if (viewportState == null ||
                !viewportState.HasFrame ||
                previewSceneRect.width <= Mathf.Epsilon ||
                previewSceneRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            Rect contentViewportRect = viewportState.GetFrameContentViewportRect(
                previewSceneRect,
                framePadding,
                frameHeaderHeight);
            if (contentViewportRect.width <= Mathf.Epsilon ||
                contentViewportRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            var scale = new Vector2(
                contentViewportRect.width / previewSceneRect.width,
                contentViewportRect.height / previewSceneRect.height);
            if (scale.x <= Mathf.Epsilon || scale.y <= Mathf.Epsilon)
                return false;

            mapping = new SceneViewportMapping(previewSceneRect, contentViewportRect, scale);
            return true;
        }

        public static bool TryGetFrameViewportRect(CanvasViewportState viewportState, out Rect frameViewportRect)
        {
            frameViewportRect = default;
            if (viewportState == null || !viewportState.HasFrame)
                return false;

            frameViewportRect = viewportState.CanvasToViewport(viewportState.FrameRect);
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
            if (!TryGetSceneViewportMapping(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out SceneViewportMapping mapping))
            {
                return false;
            }

            contentViewportRect = mapping.ViewportRect;
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
                mapping.ViewportRect.xMin + ((sceneRect.xMin - mapping.SceneRect.xMin) * mapping.Scale.x),
                mapping.ViewportRect.yMin + ((sceneRect.yMin - mapping.SceneRect.yMin) * mapping.Scale.y),
                sceneRect.width * mapping.Scale.x,
                sceneRect.height * mapping.Scale.y);
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
                mapping.SceneRect.xMin + ((viewportPoint.x - mapping.ViewportRect.xMin) / mapping.Scale.x),
                mapping.SceneRect.yMin + ((viewportPoint.y - mapping.ViewportRect.yMin) / mapping.Scale.y));
            return true;
        }

        public static CanvasSelectionVisual BuildSelectionVisual(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            float alignmentGuideThreshold,
            CanvasSelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showHandles)
        {
            bool showVerticalGuide = false;
            bool showHorizontalGuide = false;
            float verticalGuideX = 0f;
            float horizontalGuideY = 0f;
            if (kind == CanvasSelectionKind.Element &&
                TryGetFrameContentViewportRect(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out Rect contentViewportRect))
            {
                showVerticalGuide = Mathf.Abs(viewportRect.center.x - contentViewportRect.center.x) <= alignmentGuideThreshold;
                showHorizontalGuide = Mathf.Abs(viewportRect.center.y - contentViewportRect.center.y) <= alignmentGuideThreshold;
                verticalGuideX = contentViewportRect.center.x;
                horizontalGuideY = contentViewportRect.center.y;
            }

            return new CanvasSelectionVisual
            {
                Kind = kind,
                Rect = viewportRect,
                ShowHandles = showHandles,
                SizeText = $"{Mathf.RoundToInt(sourceSize.x)} × {Mathf.RoundToInt(sourceSize.y)}",
                ShowVerticalGuide = showVerticalGuide,
                VerticalGuideX = verticalGuideX,
                ShowHorizontalGuide = showHorizontalGuide,
                HorizontalGuideY = horizontalGuideY
            };
        }

        public static Rect BuildScaledSceneRect(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            CanvasHandle handle)
        {
            if (dragStartSelectionViewportRect.width <= Mathf.Epsilon ||
                dragStartSelectionViewportRect.height <= Mathf.Epsilon)
            {
                return dragStartElementSceneRect;
            }

            Vector2 scale = new(
                currentViewportRect.width / dragStartSelectionViewportRect.width,
                currentViewportRect.height / dragStartSelectionViewportRect.height);

            Vector2 newSize = new(
                dragStartElementSceneRect.width * scale.x,
                dragStartElementSceneRect.height * scale.y);

            return handle switch
            {
                CanvasHandle.TopLeft => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMax - newSize.x,
                    dragStartElementSceneRect.yMax - newSize.y,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMax),
                CanvasHandle.TopRight => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMax - newSize.y,
                    dragStartElementSceneRect.xMin + newSize.x,
                    dragStartElementSceneRect.yMax),
                CanvasHandle.BottomRight => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMin + newSize.x,
                    dragStartElementSceneRect.yMin + newSize.y),
                CanvasHandle.BottomLeft => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMax - newSize.x,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMin + newSize.y),
                _ => new Rect(dragStartElementSceneRect.position, newSize)
            };
        }

        public static bool TryBuildScaleTransform(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            CanvasHandle handle,
            out Vector2 scale,
            out Vector2 pivot)
        {
            scale = Vector2.one;
            pivot = Vector2.zero;

            if (dragStartSelectionViewportRect.width <= Mathf.Epsilon ||
                dragStartSelectionViewportRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            scale = new Vector2(
                currentViewportRect.width / dragStartSelectionViewportRect.width,
                currentViewportRect.height / dragStartSelectionViewportRect.height);

            pivot = handle switch
            {
                CanvasHandle.TopLeft => new Vector2(dragStartElementSceneRect.xMax, dragStartElementSceneRect.yMax),
                CanvasHandle.TopRight => new Vector2(dragStartElementSceneRect.xMin, dragStartElementSceneRect.yMax),
                CanvasHandle.BottomRight => new Vector2(dragStartElementSceneRect.xMin, dragStartElementSceneRect.yMin),
                CanvasHandle.BottomLeft => new Vector2(dragStartElementSceneRect.xMax, dragStartElementSceneRect.yMin),
                _ => dragStartElementSceneRect.center
            };

            return !Mathf.Approximately(scale.x, 1f) || !Mathf.Approximately(scale.y, 1f);
        }
    }
}
