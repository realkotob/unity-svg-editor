using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Shared;

namespace SvgEditor
{
    internal sealed class DragNumberFieldManipulator : PointerDragManipulatorBase
    {
        private readonly SvgDragNumberField _owner;
        private float _startValue;

        protected override float DragThreshold => 2f;
        internal bool IsDragging => DragSession.IsActive;

        public DragNumberFieldManipulator(SvgDragNumberField owner)
        {
            _owner = owner;
        }

        internal bool CancelDrag()
        {
            return TryCancelActiveDrag();
        }

        protected override bool CanStartDrag(PointerDownEvent evt)
        {
            if (!base.CanStartDrag(evt) || !_owner.CanDrag)
            {
                return false;
            }

            return evt.target is VisualElement element && _owner.IsDragHandle(element);
        }

        protected override void HandlePointerDown(PointerDownEvent evt)
        {
            if (!TryInitializeDrag(evt, _owner.CanDrag, () =>
                {
                    _startValue = _owner.value;
                    _owner.Blur();
                }))
            {
                return;
            }
        }

        protected override void HandleDragStarted(PointerMoveEvent evt)
        {
            _owner.SetDragging(true);
        }

        protected override void HandleDragMove(PointerMoveEvent evt, Vector2 delta)
        {
            _owner.ApplyDragDelta(_startValue, delta.x);
            evt.StopPropagation();
        }

        protected override void OnStopped()
        {
            _owner.SetDragging(false);
        }
    }
}
