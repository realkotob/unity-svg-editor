using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal static class CanvasProjectionMath
    {
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
            return previewSnapshot?.EffectiveViewport ?? default;
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
            Rect previewSceneRect = GetPreviewSceneRect(previewSnapshot);
            if (viewportState == null ||
                !viewportState.HasFrame ||
                previewSceneRect.width <= Mathf.Epsilon ||
                previewSceneRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            contentViewportRect = viewportState.GetFrameContentViewportRect(
                previewSceneRect,
                framePadding,
                frameHeaderHeight);
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
            if (!TryGetFrameContentViewportRect(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out Rect contentViewportRect))
            {
                return false;
            }

            Rect previewSceneRect = GetPreviewSceneRect(previewSnapshot);
            if (previewSceneRect.width <= Mathf.Epsilon || previewSceneRect.height <= Mathf.Epsilon)
                return false;

            float scale = contentViewportRect.width / previewSceneRect.width;
            viewportRect = new Rect(
                contentViewportRect.xMin + ((sceneRect.xMin - previewSceneRect.xMin) * scale),
                contentViewportRect.yMin + ((sceneRect.yMin - previewSceneRect.yMin) * scale),
                sceneRect.width * scale,
                sceneRect.height * scale);
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
            if (!TryGetFrameContentViewportRect(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out Rect contentViewportRect))
            {
                return false;
            }

            Rect previewSceneRect = GetPreviewSceneRect(previewSnapshot);
            if (previewSceneRect.width <= Mathf.Epsilon || previewSceneRect.height <= Mathf.Epsilon)
                return false;

            float scale = contentViewportRect.width / previewSceneRect.width;
            if (scale <= Mathf.Epsilon)
                return false;

            sceneDelta = viewportDelta / scale;
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
            if (!TryGetFrameContentViewportRect(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out Rect contentViewportRect))
            {
                return false;
            }

            Rect previewSceneRect = GetPreviewSceneRect(previewSnapshot);
            if (previewSceneRect.width <= Mathf.Epsilon || previewSceneRect.height <= Mathf.Epsilon)
                return false;

            float scale = contentViewportRect.width / previewSceneRect.width;
            if (scale <= Mathf.Epsilon)
                return false;

            scenePoint = new Vector2(
                previewSceneRect.xMin + ((viewportPoint.x - contentViewportRect.xMin) / scale),
                previewSceneRect.yMin + ((viewportPoint.y - contentViewportRect.yMin) / scale));
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
            Rect currentViewportRect)
        {
            if (dragStartSelectionViewportRect.width <= Mathf.Epsilon ||
                dragStartSelectionViewportRect.height <= Mathf.Epsilon)
            {
                return dragStartElementSceneRect;
            }

            Vector2 scale = new(
                currentViewportRect.width / dragStartSelectionViewportRect.width,
                currentViewportRect.height / dragStartSelectionViewportRect.height);

            return new Rect(
                dragStartElementSceneRect.position,
                new Vector2(
                    dragStartElementSceneRect.width * scale.x,
                    dragStartElementSceneRect.height * scale.y));
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
