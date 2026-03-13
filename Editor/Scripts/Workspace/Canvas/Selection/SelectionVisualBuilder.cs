using UnityEngine;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class SelectionVisualBuilder
    {
        public static CanvasSelectionVisual BuildSelectionVisual(
            ViewportState viewportState,
            CanvasViewportLayoutUtility.ProjectionContext projectionContext,
            float alignmentGuideThreshold,
            SelectionVisualRequest request)
        {
            bool showVerticalGuide = false;
            bool showHorizontalGuide = false;
            float verticalGuideX = 0f;
            float horizontalGuideY = 0f;
            if (request.Kind == SelectionKind.Element &&
                CanvasViewportLayoutUtility.TryGetFrameContentLayout(
                    viewportState,
                    projectionContext.ProjectionSceneRect,
                    projectionContext.PreserveAspectRatioMode,
                    projectionContext.FramePadding,
                    projectionContext.FrameHeaderHeight,
                    out CanvasViewportLayoutUtility.FrameContentLayout layout))
            {
                showVerticalGuide = Mathf.Abs(request.ViewportRect.center.x - layout.ImageViewportRect.center.x) <= alignmentGuideThreshold;
                showHorizontalGuide = Mathf.Abs(request.ViewportRect.center.y - layout.ImageViewportRect.center.y) <= alignmentGuideThreshold;
                verticalGuideX = layout.ImageViewportRect.center.x;
                horizontalGuideY = layout.ImageViewportRect.center.y;
            }

            return new CanvasSelectionVisual
            {
                Kind = request.Kind,
                Rect = request.ViewportRect,
                ShowSelectionHandles = request.ShowSelectionHandles,
                AllowSelectionHandleInteraction = request.ShowSelectionHandles,
                SizeText = $"{Mathf.RoundToInt(request.SourceSize.x)} × {Mathf.RoundToInt(request.SourceSize.y)}",
                ShowVerticalGuide = showVerticalGuide,
                VerticalGuideX = verticalGuideX,
                ShowHorizontalGuide = showHorizontalGuide,
                HorizontalGuideY = horizontalGuideY
            };
        }
    }
}
