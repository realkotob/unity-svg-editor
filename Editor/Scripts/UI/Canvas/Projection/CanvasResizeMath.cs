using UnityEngine;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal static class CanvasResizeMath
    {
        public static Rect BuildScaledSceneRect(
            Rect dragStartSelectionViewportRect,
            Rect dragStartElementSceneRect,
            Rect currentViewportRect,
            SelectionHandle handle,
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
                SelectionHandle.TopLeft => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMax - newSize.x,
                    dragStartElementSceneRect.yMax - newSize.y,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMax),
                SelectionHandle.Top => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMax - newSize.y,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMax),
                SelectionHandle.TopRight => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMax - newSize.y,
                    dragStartElementSceneRect.xMin + newSize.x,
                    dragStartElementSceneRect.yMax),
                SelectionHandle.Right => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMin + newSize.x,
                    dragStartElementSceneRect.yMax),
                SelectionHandle.BottomRight => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMin + newSize.x,
                    dragStartElementSceneRect.yMin + newSize.y),
                SelectionHandle.Bottom => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMin,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMin + newSize.y),
                SelectionHandle.BottomLeft => Rect.MinMaxRect(
                    dragStartElementSceneRect.xMax - newSize.x,
                    dragStartElementSceneRect.yMin,
                    dragStartElementSceneRect.xMax,
                    dragStartElementSceneRect.yMin + newSize.y),
                SelectionHandle.Left => Rect.MinMaxRect(
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
            SelectionHandle handle,
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

            if (IsCornerSelectionHandle(handle))
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
                    SelectionHandle.TopLeft => Rect.MinMaxRect(
                        dragStartSelectionViewportRect.xMax - uniformSize.x,
                        dragStartSelectionViewportRect.yMax - uniformSize.y,
                        dragStartSelectionViewportRect.xMax,
                        dragStartSelectionViewportRect.yMax),
                    SelectionHandle.TopRight => Rect.MinMaxRect(
                        dragStartSelectionViewportRect.xMin,
                        dragStartSelectionViewportRect.yMax - uniformSize.y,
                        dragStartSelectionViewportRect.xMin + uniformSize.x,
                        dragStartSelectionViewportRect.yMax),
                    SelectionHandle.BottomRight => Rect.MinMaxRect(
                        dragStartSelectionViewportRect.xMin,
                        dragStartSelectionViewportRect.yMin,
                        dragStartSelectionViewportRect.xMin + uniformSize.x,
                        dragStartSelectionViewportRect.yMin + uniformSize.y),
                    SelectionHandle.BottomLeft => Rect.MinMaxRect(
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

            float edgeUniformScaleFactor = handle is SelectionHandle.Top or SelectionHandle.Bottom
                ? resizedViewportRect.height / dragStartSelectionViewportRect.height
                : resizedViewportRect.width / dragStartSelectionViewportRect.width;
            edgeUniformScaleFactor = Mathf.Max(GetMinimumUniformScaleFactor(dragStartSelectionViewportRect.size), edgeUniformScaleFactor);

            Vector2 edgeUniformSize = dragStartSelectionViewportRect.size * edgeUniformScaleFactor;

            return handle switch
            {
                SelectionHandle.Top => Rect.MinMaxRect(
                    dragStartSelectionViewportRect.center.x - (edgeUniformSize.x * 0.5f),
                    dragStartSelectionViewportRect.yMax - edgeUniformSize.y,
                    dragStartSelectionViewportRect.center.x + (edgeUniformSize.x * 0.5f),
                    dragStartSelectionViewportRect.yMax),
                SelectionHandle.Right => Rect.MinMaxRect(
                    dragStartSelectionViewportRect.xMin,
                    dragStartSelectionViewportRect.center.y - (edgeUniformSize.y * 0.5f),
                    dragStartSelectionViewportRect.xMin + edgeUniformSize.x,
                    dragStartSelectionViewportRect.center.y + (edgeUniformSize.y * 0.5f)),
                SelectionHandle.Bottom => Rect.MinMaxRect(
                    dragStartSelectionViewportRect.center.x - (edgeUniformSize.x * 0.5f),
                    dragStartSelectionViewportRect.yMin,
                    dragStartSelectionViewportRect.center.x + (edgeUniformSize.x * 0.5f),
                    dragStartSelectionViewportRect.yMin + edgeUniformSize.y),
                SelectionHandle.Left => Rect.MinMaxRect(
                    dragStartSelectionViewportRect.xMax - edgeUniformSize.x,
                    dragStartSelectionViewportRect.center.y - (edgeUniformSize.y * 0.5f),
                    dragStartSelectionViewportRect.xMax,
                    dragStartSelectionViewportRect.center.y + (edgeUniformSize.y * 0.5f)),
                _ => resizedViewportRect
            };
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

            pivot = GetScalePivot(dragStartElementSceneRect, handle, centerAnchor);

            return !Mathf.Approximately(scale.x, 1f) || !Mathf.Approximately(scale.y, 1f);
        }

        public static bool TryBuildScaleTransformFromSceneRect(
            Rect dragStartElementSceneRect,
            Rect currentSceneRect,
            SelectionHandle handle,
            bool centerAnchor,
            out Vector2 scale,
            out Vector2 pivot)
        {
            scale = Vector2.one;
            pivot = Vector2.zero;

            if (dragStartElementSceneRect.width <= Mathf.Epsilon ||
                dragStartElementSceneRect.height <= Mathf.Epsilon)
            {
                return false;
            }

            scale = new Vector2(
                currentSceneRect.width / dragStartElementSceneRect.width,
                currentSceneRect.height / dragStartElementSceneRect.height);

            pivot = GetScalePivot(dragStartElementSceneRect, handle, centerAnchor);

            return !Mathf.Approximately(scale.x, 1f) || !Mathf.Approximately(scale.y, 1f);
        }

        private static Rect GetCenterAnchoredResizeViewportRect(
            Rect dragStartSelectionViewportRect,
            Rect resizedViewportRect,
            SelectionHandle handle,
            bool uniformScale)
        {
            Vector2 center = dragStartSelectionViewportRect.center;
            float doubledWidth = Mathf.Max(12f, dragStartSelectionViewportRect.width + ((resizedViewportRect.width - dragStartSelectionViewportRect.width) * 2f));
            float doubledHeight = Mathf.Max(12f, dragStartSelectionViewportRect.height + ((resizedViewportRect.height - dragStartSelectionViewportRect.height) * 2f));

            if (IsCornerSelectionHandle(handle))
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
                float uniformScaleFactor = handle is SelectionHandle.Top or SelectionHandle.Bottom
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
                SelectionHandle.Top or SelectionHandle.Bottom => BuildCenteredResizeRect(
                    dragStartSelectionViewportRect.width,
                    doubledHeight,
                    center),
                SelectionHandle.Left or SelectionHandle.Right => BuildCenteredResizeRect(
                    doubledWidth,
                    dragStartSelectionViewportRect.height,
                    center),
                _ => resizedViewportRect
            };
        }

        private static Vector2 GetScalePivot(Rect dragStartElementSceneRect, SelectionHandle handle, bool centerAnchor)
        {
            if (centerAnchor)
            {
                return dragStartElementSceneRect.center;
            }

            return handle switch
            {
                SelectionHandle.TopLeft => new Vector2(dragStartElementSceneRect.xMax, dragStartElementSceneRect.yMax),
                SelectionHandle.Top => new Vector2(dragStartElementSceneRect.center.x, dragStartElementSceneRect.yMax),
                SelectionHandle.TopRight => new Vector2(dragStartElementSceneRect.xMin, dragStartElementSceneRect.yMax),
                SelectionHandle.Right => new Vector2(dragStartElementSceneRect.xMin, dragStartElementSceneRect.center.y),
                SelectionHandle.BottomRight => new Vector2(dragStartElementSceneRect.xMin, dragStartElementSceneRect.yMin),
                SelectionHandle.Bottom => new Vector2(dragStartElementSceneRect.center.x, dragStartElementSceneRect.yMin),
                SelectionHandle.BottomLeft => new Vector2(dragStartElementSceneRect.xMax, dragStartElementSceneRect.yMin),
                SelectionHandle.Left => new Vector2(dragStartElementSceneRect.xMax, dragStartElementSceneRect.center.y),
                _ => dragStartElementSceneRect.center
            };
        }

        private static bool IsCornerSelectionHandle(SelectionHandle handle)
        {
            return handle is SelectionHandle.TopLeft or SelectionHandle.TopRight or SelectionHandle.BottomRight or SelectionHandle.BottomLeft;
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
