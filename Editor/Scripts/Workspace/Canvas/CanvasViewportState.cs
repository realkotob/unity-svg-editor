using Core.UI.Foundation.Tooling;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasViewportState : ViewportFrameState
    {
        public void ResizeFrame(CanvasHandle handle, UnityEngine.Vector2 viewportDelta, float minSize)
        {
            base.ResizeFrame(Map(handle), viewportDelta, minSize);
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
