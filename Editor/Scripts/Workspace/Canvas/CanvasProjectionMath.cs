using UnityEngine;

namespace UnitySvgEditor.Editor.Workspace.Canvas
{
    internal static class CanvasProjectionMath
    {
        public static Rect GetPreviewSceneRect(PreviewSnapshot previewSnapshot)
        {
            return CanvasViewportLayoutUtility.GetPreviewSceneRect(previewSnapshot);
        }

        public static bool TryGetFrameContentViewportRect(
            CanvasViewportState viewportState,
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
            CanvasViewportState viewportState,
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
            return CanvasSelectionVisualBuilder.BuildSelectionVisual(
                viewportState,
                previewSnapshot,
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
            return CanvasSelectionVisualBuilder.BuildSelectionVisual(
                viewportState,
                projectionSceneRect,
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
            return CanvasSelectionVisualBuilder.BuildSelectionVisual(
                viewportState,
                projectionSceneRect,
                preserveAspectRatioMode,
                framePadding,
                frameHeaderHeight,
                alignmentGuideThreshold,
                kind,
                viewportRect,
                sourceSize,
                showHandles);
        }

        public static Rect BuildScaledSceneRect(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            CanvasHandle handle,
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
            CanvasHandle handle,
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
            CanvasHandle handle,
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
            CanvasHandle handle,
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
