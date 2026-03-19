using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Core.Shared
{
    internal sealed class PointerDragSession
    {
        public bool IsActive { get; private set; }
        public int PointerId { get; private set; } = -1;
        public Vector2 StartPosition { get; private set; }

        public void Begin(VisualElement captor, int pointerId, Vector2 startPosition)
        {
            IsActive = true;
            PointerId = pointerId;
            StartPosition = startPosition;

            if (captor != null && !captor.HasPointerCapture(pointerId))
            {
                captor.CapturePointer(pointerId);
            }
        }

        public bool Matches(int pointerId)
        {
            return IsActive && PointerId == pointerId;
        }

        public void End(VisualElement captor)
        {
            if (captor != null && PointerId >= 0 && captor.HasPointerCapture(PointerId))
            {
                captor.ReleasePointer(PointerId);
            }

            Reset();
        }

        public void Reset()
        {
            IsActive = false;
            PointerId = -1;
            StartPosition = default;
        }
    }
}
