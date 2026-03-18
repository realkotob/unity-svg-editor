using Core.UI.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal class ViewportFrameState
    {
        public float Zoom { get; private set; } = 1f;
        public Vector2 Pan { get; private set; } = Vector2.zero;
        public Rect FrameRect { get; private set; }

        public bool HasFrame => FrameRect.width > Mathf.Epsilon && FrameRect.height > Mathf.Epsilon;

        public void Clear()
        {
            Zoom = 1f;
            Pan = Vector2.zero;
            FrameRect = default;
        }

        public void ResetToFit(Rect canvasBounds, Rect sceneRect, float margin, float padding, float headerHeight)
        {
            Zoom = 1f;
            Pan = Vector2.zero;

            Vector2 availableSize = new(
                Mathf.Max(1f, canvasBounds.width - (margin * 2f)),
                Mathf.Max(1f, canvasBounds.height - (margin * 2f) - headerHeight));

            Vector2 fittedContentSize = FitSizeWithin(sceneRect.size, availableSize);
            FrameRect = new Rect(
                (canvasBounds.width - (fittedContentSize.x + (padding * 2f))) * 0.5f,
                (canvasBounds.height - (fittedContentSize.y + (padding * 2f) + headerHeight)) * 0.5f,
                fittedContentSize.x + (padding * 2f),
                fittedContentSize.y + (padding * 2f) + headerHeight);
        }

        public void EnsureFrame(Rect canvasBounds, Rect sceneRect, float margin, float padding, float headerHeight)
        {
            if (!HasFrame)
            {
                ResetToFit(canvasBounds, sceneRect, margin, padding, headerHeight);
            }
        }

        public void PanBy(Vector2 viewportDelta)
        {
            Pan += viewportDelta;
        }

        public void SetPan(Vector2 pan)
        {
            Pan = pan;
        }

        public void SetZoomPercent(float zoomPercent)
        {
            Zoom = Mathf.Max(0.1f, zoomPercent);
        }

        public void SetFrameRect(Rect frameRect)
        {
            FrameRect = frameRect;
        }

        public void ZoomAtPoint(
            Rect canvasBounds,
            Rect sceneRect,
            Vector2 viewportPoint,
            float wheelDeltaY,
            float minZoom,
            float maxZoom,
            float padding,
            float headerHeight)
        {
            if (!HasFrame || Mathf.Approximately(wheelDeltaY, 0f))
            {
                return;
            }

            Rect previousFrameViewportRect = CanvasToViewport(FrameRect);
            Rect previousContentViewportRect = FitRectInto(previousFrameViewportRect, sceneRect.size, padding, headerHeight);
            float previousContentScale = previousContentViewportRect.width / Mathf.Max(sceneRect.width, Mathf.Epsilon);
            if (previousContentScale <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 scenePoint = new(
                sceneRect.xMin + ((viewportPoint.x - previousContentViewportRect.xMin) / previousContentScale),
                sceneRect.yMin + ((viewportPoint.y - previousContentViewportRect.yMin) / previousContentScale));

            float zoomFactor = Mathf.Pow(1.1f, -wheelDeltaY / WheelEvent.scrollDeltaPerTick);
            float nextZoom = Mathf.Clamp(Zoom * zoomFactor, minZoom, maxZoom);
            if (Mathf.Approximately(nextZoom, Zoom))
            {
                return;
            }

            Zoom = nextZoom;

            Rect nextFrameViewportRect = CanvasToViewport(FrameRect);
            Rect nextContentViewportRect = FitRectInto(nextFrameViewportRect, sceneRect.size, padding, headerHeight);
            float nextContentScale = nextContentViewportRect.width / Mathf.Max(sceneRect.width, Mathf.Epsilon);
            Vector2 desiredContentOrigin = viewportPoint - ((scenePoint - sceneRect.min) * nextContentScale);
            Vector2 adjustedFrameOrigin = desiredContentOrigin - (nextContentViewportRect.position - nextFrameViewportRect.position);

            Pan += adjustedFrameOrigin - nextFrameViewportRect.position;
        }

        public void MoveFrame(Vector2 viewportDelta)
        {
            if (!HasFrame)
            {
                return;
            }

            FrameRect = new Rect(FrameRect.position + ViewportToCanvasDelta(viewportDelta), FrameRect.size);
        }

        public Rect CanvasToViewport(Rect canvasRect)
        {
            return new Rect(CanvasToViewport(canvasRect.position), canvasRect.size * Zoom);
        }

        public Vector2 CanvasToViewport(Vector2 canvasPoint)
        {
            return (canvasPoint * Zoom) + Pan;
        }

        public Vector2 ViewportToCanvas(Vector2 viewportPoint)
        {
            return (viewportPoint - Pan) / Mathf.Max(Zoom, Mathf.Epsilon);
        }

        public Vector2 ViewportToCanvasDelta(Vector2 viewportDelta)
        {
            return viewportDelta / Mathf.Max(Zoom, Mathf.Epsilon);
        }

        public Rect GetFrameContentViewportRect(Rect sceneRect, float padding, float headerHeight)
        {
            return FitRectInto(CanvasToViewport(FrameRect), sceneRect.size, padding, headerHeight);
        }

        private static Rect FitRectInto(Rect frameViewportRect, Vector2 contentSize, float padding, float headerHeight)
        {
            Rect innerRect = new(
                frameViewportRect.xMin + padding,
                frameViewportRect.yMin + headerHeight + padding,
                Mathf.Max(1f, frameViewportRect.width - (padding * 2f)),
                Mathf.Max(1f, frameViewportRect.height - headerHeight - (padding * 2f)));

            return FitRectWithin(innerRect, contentSize);
        }

        private static Vector2 FitSizeWithin(Vector2 sourceSize, Vector2 maxSize)
        {
            float safeWidth = Mathf.Max(sourceSize.x, Mathf.Epsilon);
            float safeHeight = Mathf.Max(sourceSize.y, Mathf.Epsilon);
            float scale = Mathf.Min(maxSize.x / safeWidth, maxSize.y / safeHeight);
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            {
                scale = 1f;
            }

            return new Vector2(safeWidth * scale, safeHeight * scale);
        }

        private static Rect FitRectWithin(Rect containerRect, Vector2 contentSize)
        {
            Vector2 fittedSize = FitSizeWithin(contentSize, containerRect.size);
            return new Rect(
                containerRect.xMin + ((containerRect.width - fittedSize.x) * 0.5f),
                containerRect.yMin + ((containerRect.height - fittedSize.y) * 0.5f),
                fittedSize.x,
                fittedSize.y);
        }
    }
}
