using UnityEngine;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class SelectionVisualBuilder
    {
        public static CanvasSelectionVisual BuildSelectionVisual(
            ViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            float alignmentGuideThreshold,
            SelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showSelectionHandles)
        {
            return BuildSelectionVisual(
                viewportState,
                CanvasViewportLayoutUtility.GetPreviewSceneRect(previewSnapshot),
                previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet,
                framePadding,
                frameHeaderHeight,
                alignmentGuideThreshold,
                kind,
                viewportRect,
                sourceSize,
                showSelectionHandles);
        }

        public static CanvasSelectionVisual BuildSelectionVisual(
            ViewportState viewportState,
            Rect projectionSceneRect,
            float framePadding,
            float frameHeaderHeight,
            float alignmentGuideThreshold,
            SelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showSelectionHandles)
        {
            return BuildSelectionVisual(
                viewportState,
                projectionSceneRect,
                SvgPreserveAspectRatioMode.Meet,
                framePadding,
                frameHeaderHeight,
                alignmentGuideThreshold,
                kind,
                viewportRect,
                sourceSize,
                showSelectionHandles);
        }

        public static CanvasSelectionVisual BuildSelectionVisual(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            float alignmentGuideThreshold,
            SelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showSelectionHandles)
        {
            bool showVerticalGuide = false;
            bool showHorizontalGuide = false;
            float verticalGuideX = 0f;
            float horizontalGuideY = 0f;
            if (kind == SelectionKind.Element &&
                CanvasViewportLayoutUtility.TryGetFrameContentLayout(
                    viewportState,
                    projectionSceneRect,
                    preserveAspectRatioMode,
                    framePadding,
                    frameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
            {
                showVerticalGuide = Mathf.Abs(viewportRect.center.x - layout.ImageViewportRect.center.x) <= alignmentGuideThreshold;
                showHorizontalGuide = Mathf.Abs(viewportRect.center.y - layout.ImageViewportRect.center.y) <= alignmentGuideThreshold;
                verticalGuideX = layout.ImageViewportRect.center.x;
                horizontalGuideY = layout.ImageViewportRect.center.y;
            }

            return new CanvasSelectionVisual
            {
                Kind = kind,
                Rect = viewportRect,
                ShowSelectionHandles = showSelectionHandles,
                SizeText = $"{Mathf.RoundToInt(sourceSize.x)} × {Mathf.RoundToInt(sourceSize.y)}",
                ShowVerticalGuide = showVerticalGuide,
                VerticalGuideX = verticalGuideX,
                ShowHorizontalGuide = showHorizontalGuide,
                HorizontalGuideY = horizontalGuideY
            };
        }
    }
}
