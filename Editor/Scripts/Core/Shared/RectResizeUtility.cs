using UnityEngine;
using SvgEditor.UI.Canvas;
using Core.UI.Extensions;

namespace SvgEditor.Core.Shared
{
    internal static class RectResizeUtility
    {
        public static Rect ResizeRect(Rect sourceRect, SelectionHandle handle, Vector2 delta, float minSize)
        {
            float left = sourceRect.xMin;
            float top = sourceRect.yMin;
            float right = sourceRect.xMax;
            float bottom = sourceRect.yMax;

            switch (handle)
            {
                case SelectionHandle.TopLeft:
                    left += delta.x;
                    top += delta.y;
                    break;
                case SelectionHandle.Top:
                    top += delta.y;
                    break;
                case SelectionHandle.TopRight:
                    right += delta.x;
                    top += delta.y;
                    break;
                case SelectionHandle.Right:
                    right += delta.x;
                    break;
                case SelectionHandle.BottomRight:
                    right += delta.x;
                    bottom += delta.y;
                    break;
                case SelectionHandle.Bottom:
                    bottom += delta.y;
                    break;
                case SelectionHandle.BottomLeft:
                    left += delta.x;
                    bottom += delta.y;
                    break;
                case SelectionHandle.Left:
                    left += delta.x;
                    break;
            }

            if (right - left < minSize)
            {
                if (handle is SelectionHandle.TopLeft or SelectionHandle.Left or SelectionHandle.BottomLeft)
                    left = right - minSize;
                else
                    right = left + minSize;
            }

            if (bottom - top < minSize)
            {
                if (handle is SelectionHandle.TopLeft or SelectionHandle.Top or SelectionHandle.TopRight)
                    top = bottom - minSize;
                else
                    bottom = top + minSize;
            }

            return Rect.MinMaxRect(left, top, right, bottom);
        }
    }
}
