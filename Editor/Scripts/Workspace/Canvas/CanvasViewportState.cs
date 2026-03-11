using Core.UI.Foundation.Tooling;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasViewportState : ViewportFrameState
    {
        public void ResetToActualSize(Rect canvasBounds, Rect sceneRect, float padding, float headerHeight)
        {
            SetZoomPercent(1f);
            SetPan(Vector2.zero);

            Vector2 frameSize = new(
                Mathf.Max(1f, sceneRect.width + (padding * 2f)),
                Mathf.Max(1f, sceneRect.height + (padding * 2f) + headerHeight));

            SetFrameRect(new Rect(
                canvasBounds.center.x - (frameSize.x * 0.5f),
                canvasBounds.center.y - (frameSize.y * 0.5f),
                frameSize.x,
                frameSize.y));
        }

        public void ResizeFrame(CanvasHandle handle, UnityEngine.Vector2 viewportDelta, float minSize)
        {
            SetFrameRect(RectResizeUtility.ResizeRect(FrameRect, handle, ViewportToCanvasDelta(viewportDelta), minSize));
        }
    }
}
