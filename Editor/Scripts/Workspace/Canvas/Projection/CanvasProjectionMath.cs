using UnityEngine;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class CanvasProjectionMath
    {
        public static Rect GetPreviewSceneRect(PreviewSnapshot previewSnapshot)
        {
            return CanvasViewportLayoutUtility.GetPreviewSceneRect(previewSnapshot);
        }

        public static bool TryGetFrameContentViewportRect(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out Rect contentViewportRect)
        {
            return CanvasViewportLayoutUtility.TryGetFrameContentViewportRect(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight,
                out contentViewportRect);
        }

        public static bool TryGetFrameVisibleViewportRect(
            ViewportState viewportState,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            float framePadding,
            float frameHeaderHeight,
            out Rect visibleViewportRect)
        {
            return CanvasViewportLayoutUtility.TryGetFrameVisibleViewportRect(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight,
                out visibleViewportRect);
        }

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
            return SelectionVisualBuilder.BuildSelectionVisual(
                viewportState,
                previewSnapshot,
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
            return SelectionVisualBuilder.BuildSelectionVisual(
                viewportState,
                projectionSceneRect,
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
            return SelectionVisualBuilder.BuildSelectionVisual(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight,
                alignmentGuideThreshold,
                kind,
                viewportRect,
                sourceSize,
                showSelectionHandles);
        }

        public static Rect BuildScaledSceneRect(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            SelectionHandle handle,
            bool centerAnchor = false)
        {
            return CanvasResizeMath.BuildScaledSceneRect(
                dragStartSelectionViewportRect,
                dragStartElementSceneRect,
                currentViewportRect,
                handle,
                centerAnchor);
        }

        public static Rect GetResizeViewportRect(
            Rect dragStartSelectionViewportRect,
            Rect resizedViewportRect,
            SelectionHandle handle,
            bool uniformScale,
            bool centerAnchor)
        {
            return CanvasResizeMath.GetResizeViewportRect(
                dragStartSelectionViewportRect,
                resizedViewportRect,
                handle,
                uniformScale,
                centerAnchor);
        }

        public static bool TryBuildScaleTransform(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            SelectionHandle handle,
            bool centerAnchor,
            out Vector2 scale,
            out Vector2 pivot)
        {
            return CanvasResizeMath.TryBuildScaleTransform(
                dragStartSelectionViewportRect,
                dragStartElementSceneRect,
                currentViewportRect,
                handle,
                centerAnchor,
                out scale,
                out pivot);
        }

        public static bool TryBuildScaleTransformFromSceneRect(
            Rect dragStartElementSceneRect,
            Rect currentSceneRect,
            SelectionHandle handle,
            bool centerAnchor,
            out Vector2 scale,
            out Vector2 pivot)
        {
            return CanvasResizeMath.TryBuildScaleTransformFromSceneRect(
                dragStartElementSceneRect,
                currentSceneRect,
                handle,
                centerAnchor,
                out scale,
                out pivot);
        }
    }
}
