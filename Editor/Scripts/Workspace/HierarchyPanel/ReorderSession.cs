using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal sealed class ReorderSession
    {
        private readonly PointerDragSession _dragSession = new();
        private bool _shouldSuppressHierarchyRowClick;

        public bool IsHierarchyDragging { get; private set; }
        public string PressedHierarchyElementKey { get; private set; }
        public string PendingHierarchyDropParentKey { get; private set; }
        public int PendingHierarchyDropChildIndex { get; private set; } = -1;
        public Vector2 StartPosition => _dragSession.StartPosition;

        public void BeginPress(VisualElement captor, string elementKey, int pointerId, Vector2 position)
        {
            PressedHierarchyElementKey = elementKey;
            _dragSession.Reset();
            _dragSession.Begin(captor, pointerId, position);
            IsHierarchyDragging = false;
            ClearPendingDrop();
            _shouldSuppressHierarchyRowClick = false;
        }

        public void CancelPendingPress()
        {
            PressedHierarchyElementKey = null;
            IsHierarchyDragging = false;
            _shouldSuppressHierarchyRowClick = false;
            ClearPendingDrop();
            _dragSession.Reset();
        }

        public bool TryConsumeSuppressedRowClick()
        {
            if (!_shouldSuppressHierarchyRowClick)
            {
                return false;
            }

            _shouldSuppressHierarchyRowClick = false;
            return true;
        }

        public bool Matches(int pointerId)
        {
            return _dragSession.Matches(pointerId);
        }

        public bool TryBeginDrag(Vector2 currentPosition, float threshold)
        {
            if (IsHierarchyDragging)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(PressedHierarchyElementKey))
            {
                return false;
            }

            if ((currentPosition - _dragSession.StartPosition).magnitude < threshold)
            {
                return false;
            }

            IsHierarchyDragging = true;
            _shouldSuppressHierarchyRowClick = true;
            return true;
        }

        public void SetPendingDrop(string parentKey, int childIndex)
        {
            PendingHierarchyDropParentKey = parentKey;
            PendingHierarchyDropChildIndex = childIndex;
        }

        public void ClearPendingDrop()
        {
            PendingHierarchyDropParentKey = null;
            PendingHierarchyDropChildIndex = -1;
        }

        public void Reset(VisualElement captor)
        {
            ClearPendingDrop();
            _dragSession.End(captor);
            IsHierarchyDragging = false;
            PressedHierarchyElementKey = null;
            _shouldSuppressHierarchyRowClick = false;
        }
    }
}
