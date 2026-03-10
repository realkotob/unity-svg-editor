using Core.UI.Foundation.Tooling;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasViewportState : ViewportFrameState
    {
        public void ResizeFrame(CanvasHandle handle, UnityEngine.Vector2 viewportDelta, float minSize)
        {
            SetFrameRect(RectResizeUtility.ResizeRect(FrameRect, handle, ViewportToCanvasDelta(viewportDelta), minSize));
        }
    }
}
