using UnityEngine;
using FoundationRectResizeUtility = Core.UI.Foundation.Tooling.RectResizeUtility;
using ResizeHandle = Core.UI.Foundation.Tooling.ResizeHandle;

namespace UnitySvgEditor.Editor
{
    internal static class RectResizeUtility
    {
        public static Rect ResizeRect(Rect sourceRect, CanvasHandle handle, Vector2 delta, float minSize)
        {
            return FoundationRectResizeUtility.ResizeRect(sourceRect, Map(handle), delta, minSize);
        }

        private static ResizeHandle Map(CanvasHandle handle)
        {
            return handle switch
            {
                CanvasHandle.TopLeft => ResizeHandle.TopLeft,
                CanvasHandle.TopRight => ResizeHandle.TopRight,
                CanvasHandle.BottomRight => ResizeHandle.BottomRight,
                CanvasHandle.BottomLeft => ResizeHandle.BottomLeft,
                _ => ResizeHandle.None,
            };
        }
    }
}
