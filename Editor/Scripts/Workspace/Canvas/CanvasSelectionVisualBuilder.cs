using UnityEngine;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class CanvasSelectionVisualBuilder
    {
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
                showHandles);
        }

        public static CanvasSelectionVisual BuildSelectionVisual(
            CanvasViewportState viewportState,
            Rect projectionSceneRect,
            float framePadding,
            float frameHeaderHeight,
            float alignmentGuideThreshold,
            CanvasSelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showHandles)
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
                showHandles);
        }

        public static CanvasSelectionVisual BuildSelectionVisual(
            CanvasViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
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
                ShowHandles = showHandles,
                SizeText = $"{Mathf.RoundToInt(sourceSize.x)} × {Mathf.RoundToInt(sourceSize.y)}",
                ShowVerticalGuide = showVerticalGuide,
                VerticalGuideX = verticalGuideX,
                ShowHorizontalGuide = showHorizontalGuide,
                HorizontalGuideY = horizontalGuideY
            };
        }
    }
}
