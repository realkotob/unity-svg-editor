using UnityEngine;
using UnitySvgEditor.Editor.Workspace.Canvas;

namespace UnitySvgEditor.Editor
{
    internal static class RectResizeUtility
    {
        public static Rect ResizeRect(Rect sourceRect, CanvasHandle handle, Vector2 delta, float minSize)
        {
            float left = sourceRect.xMin;
            float top = sourceRect.yMin;
            float right = sourceRect.xMax;
            float bottom = sourceRect.yMax;

            switch (handle)
            {
                case CanvasHandle.TopLeft:
                    left += delta.x;
                    top += delta.y;
                    break;
                case CanvasHandle.Top:
                    top += delta.y;
                    break;
                case CanvasHandle.TopRight:
                    right += delta.x;
                    top += delta.y;
                    break;
                case CanvasHandle.Right:
                    right += delta.x;
                    break;
                case CanvasHandle.BottomRight:
                    right += delta.x;
                    bottom += delta.y;
                    break;
                case CanvasHandle.Bottom:
                    bottom += delta.y;
                    break;
                case CanvasHandle.BottomLeft:
                    left += delta.x;
                    bottom += delta.y;
                    break;
                case CanvasHandle.Left:
                    left += delta.x;
                    break;
            }

            if (right - left < minSize)
            {
                if (handle is CanvasHandle.TopLeft or CanvasHandle.Left or CanvasHandle.BottomLeft)
                    left = right - minSize;
                else
                    right = left + minSize;
            }

            if (bottom - top < minSize)
            {
                if (handle is CanvasHandle.TopLeft or CanvasHandle.Top or CanvasHandle.TopRight)
                    top = bottom - minSize;
                else
                    bottom = top + minSize;
            }

            return Rect.MinMaxRect(left, top, right, bottom);
        }
    }
}
