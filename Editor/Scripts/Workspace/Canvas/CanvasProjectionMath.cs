using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal static class CanvasProjectionMath
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

        public static bool TryGetFrameViewportRect(CanvasViewportState viewportState, out Rect frameViewportRect)
        {
            frameViewportRect = default;
            if (viewportState == null || !viewportState.HasFrame)
                return false;

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
                GetPreviewSceneRect(previewSnapshot),
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
                TryGetFrameContentLayout(
                    viewportState,
                    projectionSceneRect,
                    preserveAspectRatioMode,
                    framePadding,
                    frameHeaderHeight,
                    out FrameContentLayout layout))
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

        public static Rect BuildScaledSceneRect(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            CanvasHandle handle,
            bool centerAnchor = false)
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

            if (centerAnchor)
            {
                return Rect.MinMaxRect(
                    dragStartElementSceneRect.center.x - (newSize.x * 0.5f),
                    dragStartElementSceneRect.center.y - (newSize.y * 0.5f),
                    dragStartElementSceneRect.center.x + (newSize.x * 0.5f),
                    dragStartElementSceneRect.center.y + (newSize.y * 0.5f));
            }

            return handle switch
            {
                CanvasHandle.TopLeft => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMax - newSize.x,
                    dragStartElementSceneRect.yMax - newSize.y,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMax),
                CanvasHandle.Top => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMax - newSize.y,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMax),
                CanvasHandle.TopRight => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMax - newSize.y,
                    dragStartElementSceneRect.xMin + newSize.x,
                    dragStartElementSceneRect.yMax),
                CanvasHandle.Right => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMin + newSize.x,
                    dragStartElementSceneRect.yMax),
                CanvasHandle.BottomRight => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMin + newSize.x,
                    dragStartElementSceneRect.yMin + newSize.y),
                CanvasHandle.Bottom => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMin + newSize.y),
                CanvasHandle.BottomLeft => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMax - newSize.x,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMin + newSize.y),
                CanvasHandle.Left => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMax - newSize.x,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMax),
                _ => new Rect(dragStartElementSceneRect.position, newSize)
            };
        }

        public static Rect GetResizeViewportRect(
            Rect dragStartSelectionViewportRect,
            Rect resizedViewportRect,
            CanvasHandle handle,
            bool uniformScale,
            bool centerAnchor)
        {
            if (dragStartSelectionViewportRect.width <= Mathf.Epsilon ||
                dragStartSelectionViewportRect.height <= Mathf.Epsilon)
            {
                return resizedViewportRect;
            }

            if (centerAnchor)
            {
                return GetCenterAnchoredResizeViewportRect(
                    dragStartSelectionViewportRect,
                    resizedViewportRect,
                    handle,
                    uniformScale);
            }

            if (IsCornerHandle(handle))
            {
                float scaleX = resizedViewportRect.width / dragStartSelectionViewportRect.width;
                float scaleY = resizedViewportRect.height / dragStartSelectionViewportRect.height;
                float scaleDeltaX = Mathf.Abs(scaleX - 1f);
                float scaleDeltaY = Mathf.Abs(scaleY - 1f);
                float uniformScaleFactor = scaleDeltaX >= scaleDeltaY ? scaleX : scaleY;
                uniformScaleFactor = Mathf.Max(GetMinimumUniformScaleFactor(dragStartSelectionViewportRect.size), uniformScaleFactor);

                Vector2 uniformSize = dragStartSelectionViewportRect.size * uniformScaleFactor;

                return handle switch
                {
                    CanvasHandle.TopLeft => Rect.MinMaxRect(
                        dragStartSelectionViewportRect.xMax - uniformSize.x,
                        dragStartSelectionViewportRect.yMax - uniformSize.y,
                        dragStartSelectionViewportRect.xMax,
                        dragStartSelectionViewportRect.yMax),
                    CanvasHandle.TopRight => Rect.MinMaxRect(
                        dragStartSelectionViewportRect.xMin,
                        dragStartSelectionViewportRect.yMax - uniformSize.y,
                        dragStartSelectionViewportRect.xMin + uniformSize.x,
                        dragStartSelectionViewportRect.yMax),
                    CanvasHandle.BottomRight => Rect.MinMaxRect(
                        dragStartSelectionViewportRect.xMin,
                        dragStartSelectionViewportRect.yMin,
                        dragStartSelectionViewportRect.xMin + uniformSize.x,
                        dragStartSelectionViewportRect.yMin + uniformSize.y),
                    CanvasHandle.BottomLeft => Rect.MinMaxRect(
                        dragStartSelectionViewportRect.xMax - uniformSize.x,
                        dragStartSelectionViewportRect.yMin,
                        dragStartSelectionViewportRect.xMax,
                        dragStartSelectionViewportRect.yMin + uniformSize.y),
                    _ => resizedViewportRect
                };
            }

            if (!uniformScale ||
                dragStartSelectionViewportRect.width <= Mathf.Epsilon ||
                dragStartSelectionViewportRect.height <= Mathf.Epsilon)
            {
                return resizedViewportRect;
            }

            float edgeUniformScaleFactor = handle is CanvasHandle.Top or CanvasHandle.Bottom
                ? resizedViewportRect.height / dragStartSelectionViewportRect.height
                : resizedViewportRect.width / dragStartSelectionViewportRect.width;
            edgeUniformScaleFactor = Mathf.Max(GetMinimumUniformScaleFactor(dragStartSelectionViewportRect.size), edgeUniformScaleFactor);

            Vector2 edgeUniformSize = dragStartSelectionViewportRect.size * edgeUniformScaleFactor;

            return handle switch
            {
                CanvasHandle.Top => Rect.MinMaxRect(
                    dragStartSelectionViewportRect.center.x - (edgeUniformSize.x * 0.5f),
                    dragStartSelectionViewportRect.yMax - edgeUniformSize.y,
                    dragStartSelectionViewportRect.center.x + (edgeUniformSize.x * 0.5f),
                    dragStartSelectionViewportRect.yMax),
                CanvasHandle.Right => Rect.MinMaxRect(
                    dragStartSelectionViewportRect.xMin,
                    dragStartSelectionViewportRect.center.y - (edgeUniformSize.y * 0.5f),
                    dragStartSelectionViewportRect.xMin + edgeUniformSize.x,
                    dragStartSelectionViewportRect.center.y + (edgeUniformSize.y * 0.5f)),
                CanvasHandle.Bottom => Rect.MinMaxRect(
                    dragStartSelectionViewportRect.center.x - (edgeUniformSize.x * 0.5f),
                    dragStartSelectionViewportRect.yMin,
                    dragStartSelectionViewportRect.center.x + (edgeUniformSize.x * 0.5f),
                    dragStartSelectionViewportRect.yMin + edgeUniformSize.y),
                CanvasHandle.Left => Rect.MinMaxRect(
                    dragStartSelectionViewportRect.xMax - edgeUniformSize.x,
                    dragStartSelectionViewportRect.center.y - (edgeUniformSize.y * 0.5f),
                    dragStartSelectionViewportRect.xMax,
                    dragStartSelectionViewportRect.center.y + (edgeUniformSize.y * 0.5f)),
                _ => resizedViewportRect
            };
        }

        private static Rect GetCenterAnchoredResizeViewportRect(
            Rect dragStartSelectionViewportRect,
            Rect resizedViewportRect,
            CanvasHandle handle,
            bool uniformScale)
        {
            Vector2 center = dragStartSelectionViewportRect.center;
            float doubledWidth = Mathf.Max(12f, dragStartSelectionViewportRect.width + ((resizedViewportRect.width - dragStartSelectionViewportRect.width) * 2f));
            float doubledHeight = Mathf.Max(12f, dragStartSelectionViewportRect.height + ((resizedViewportRect.height - dragStartSelectionViewportRect.height) * 2f));

            if (IsCornerHandle(handle))
            {
                float scaleX = doubledWidth / dragStartSelectionViewportRect.width;
                float scaleY = doubledHeight / dragStartSelectionViewportRect.height;
                float scaleDeltaX = Mathf.Abs(scaleX - 1f);
                float scaleDeltaY = Mathf.Abs(scaleY - 1f);
                float uniformScaleFactor = scaleDeltaX >= scaleDeltaY ? scaleX : scaleY;
                uniformScaleFactor = Mathf.Max(GetMinimumUniformScaleFactor(dragStartSelectionViewportRect.size), uniformScaleFactor);

                Vector2 uniformSize = dragStartSelectionViewportRect.size * uniformScaleFactor;
                return Rect.MinMaxRect(
                    center.x - (uniformSize.x * 0.5f),
                    center.y - (uniformSize.y * 0.5f),
                    center.x + (uniformSize.x * 0.5f),
                    center.y + (uniformSize.y * 0.5f));
            }

            if (uniformScale)
            {
                float uniformScaleFactor = handle is CanvasHandle.Top or CanvasHandle.Bottom
                    ? doubledHeight / dragStartSelectionViewportRect.height
                    : doubledWidth / dragStartSelectionViewportRect.width;
                uniformScaleFactor = Mathf.Max(GetMinimumUniformScaleFactor(dragStartSelectionViewportRect.size), uniformScaleFactor);

                Vector2 uniformSize = dragStartSelectionViewportRect.size * uniformScaleFactor;
                return Rect.MinMaxRect(
                    center.x - (uniformSize.x * 0.5f),
                    center.y - (uniformSize.y * 0.5f),
                    center.x + (uniformSize.x * 0.5f),
                    center.y + (uniformSize.y * 0.5f));
            }

            return handle switch
            {
                CanvasHandle.Top or CanvasHandle.Bottom => BuildCenteredResizeRect(
                    dragStartSelectionViewportRect.width,
                    doubledHeight,
                    center),
                CanvasHandle.Left or CanvasHandle.Right => BuildCenteredResizeRect(
                    doubledWidth,
                    dragStartSelectionViewportRect.height,
                    center),
                _ => resizedViewportRect
            };
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

            if (centerAnchor)
            {
                pivot = dragStartElementSceneRect.center;
                return !Mathf.Approximately(scale.x, 1f) || !Mathf.Approximately(scale.y, 1f);
            }

            pivot = handle switch
            {
                CanvasHandle.TopLeft => new Vector2(dragStartElementSceneRect.xMax, dragStartElementSceneRect.yMax),
                CanvasHandle.Top => new Vector2(dragStartElementSceneRect.center.x, dragStartElementSceneRect.yMax),
                CanvasHandle.TopRight => new Vector2(dragStartElementSceneRect.xMin, dragStartElementSceneRect.yMax),
                CanvasHandle.Right => new Vector2(dragStartElementSceneRect.xMin, dragStartElementSceneRect.center.y),
                CanvasHandle.BottomRight => new Vector2(dragStartElementSceneRect.xMin, dragStartElementSceneRect.yMin),
                CanvasHandle.Bottom => new Vector2(dragStartElementSceneRect.center.x, dragStartElementSceneRect.yMin),
                CanvasHandle.BottomLeft => new Vector2(dragStartElementSceneRect.xMax, dragStartElementSceneRect.yMin),
                CanvasHandle.Left => new Vector2(dragStartElementSceneRect.xMax, dragStartElementSceneRect.center.y),
                _ => dragStartElementSceneRect.center
            };

            return !Mathf.Approximately(scale.x, 1f) || !Mathf.Approximately(scale.y, 1f);
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
                return false;

            Rect frameViewportRect = viewportState.CanvasToViewport(viewportState.FrameRect);
            Rect visibleViewportRect = new(
                frameViewportRect.xMin + framePadding,
                frameViewportRect.yMin + frameHeaderHeight + framePadding,
                Mathf.Max(1f, frameViewportRect.width - (framePadding * 2f)),
                Mathf.Max(1f, frameViewportRect.height - frameHeaderHeight - (framePadding * 2f)));

            if (projectionSceneRect.width <= Mathf.Epsilon || projectionSceneRect.height <= Mathf.Epsilon)
                return false;

            Rect imageViewportRect = GetImageViewportRect(
                visibleViewportRect,
                projectionSceneRect.size,
                preserveAspectRatioMode);
            if (imageViewportRect.width <= Mathf.Epsilon || imageViewportRect.height <= Mathf.Epsilon)
                return false;

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

            var scale = new Vector2(
                layout.ImageViewportRect.width / projectionSceneRect.width,
                layout.ImageViewportRect.height / projectionSceneRect.height);
            if (scale.x <= Mathf.Epsilon || scale.y <= Mathf.Epsilon)
                return false;

            mapping = new SceneViewportMapping(projectionSceneRect, layout, scale);
            return true;
        }

        private static Rect GetImageViewportRect(
            Rect visibleViewportRect,
            Vector2 contentSize,
            SvgPreserveAspectRatioMode preserveAspectRatioMode)
        {
            if (preserveAspectRatioMode.IsNone)
                return visibleViewportRect;

            float safeWidth = Mathf.Max(contentSize.x, Mathf.Epsilon);
            float safeHeight = Mathf.Max(contentSize.y, Mathf.Epsilon);
            float scale = preserveAspectRatioMode.IsSlice
                ? Mathf.Max(visibleViewportRect.width / safeWidth, visibleViewportRect.height / safeHeight)
                : Mathf.Min(visibleViewportRect.width / safeWidth, visibleViewportRect.height / safeHeight);

            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
                scale = 1f;

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

        private static bool IsCornerHandle(CanvasHandle handle)
        {
            return handle is CanvasHandle.TopLeft or CanvasHandle.TopRight or CanvasHandle.BottomRight or CanvasHandle.BottomLeft;
        }

        private static float ResolveDisplayedZoomScale(Vector2 scale)
        {
            if (scale.x <= Mathf.Epsilon || scale.y <= Mathf.Epsilon)
                return 0f;

            return Mathf.Abs(scale.x - scale.y) <= 0.0001f
                ? scale.x
                : Mathf.Min(scale.x, scale.y);
        }

        private static float GetMinimumUniformScaleFactor(Vector2 size)
        {
            float safeWidth = Mathf.Max(size.x, Mathf.Epsilon);
            float safeHeight = Mathf.Max(size.y, Mathf.Epsilon);
            return Mathf.Max(12f / safeWidth, 12f / safeHeight);
        }

        private static Rect BuildCenteredResizeRect(
            float width,
            float height,
            Vector2 center)
        {
            return Rect.MinMaxRect(
                center.x - (width * 0.5f),
                center.y - (height * 0.5f),
                center.x + (width * 0.5f),
                center.y + (height * 0.5f));
        }
    }
}
