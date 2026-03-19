using System;
using UnityEngine.UIElements;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.UI.Inspector
{
    internal sealed class PanelViewRotationDragBinder
    {
        private const string DRAG_NUMBER_FIELD_PREFIX_CLASS_NAME = "drag-number-field__prefix";

        private readonly FormControls _form;
        private readonly Action<PanelView.TransformHelperChange> _onTransformHelperChanged;
        private readonly Action _onRotationDragStarted;
        private readonly Action _onRotationDragEnded;

        private bool _isRotationDragging;
        private float _rotationDragStartValue;

        public PanelViewRotationDragBinder(
            FormControls form,
            Action<PanelView.TransformHelperChange> onTransformHelperChanged,
            Action onRotationDragStarted,
            Action onRotationDragEnded)
        {
            _form = form;
            _onTransformHelperChanged = onTransformHelperChanged;
            _onRotationDragStarted = onRotationDragStarted;
            _onRotationDragEnded = onRotationDragEnded;
        }

        public void Bind()
        {
            ToggleRotateChangedCallback(register: true);
            RegisterRotateDragCallbacks();
        }

        public void Unbind()
        {
            UnregisterRotateDragCallbacks();
            ToggleRotateChangedCallback(register: false);
            _isRotationDragging = false;
            _rotationDragStartValue = 0f;
        }

        private void OnRotateChanged(ChangeEvent<float> evt)
        {
            float delta = evt.newValue - evt.previousValue;
            if (_isRotationDragging && _form.RotateField is FloatField rotateField)
            {
                rotateField.SetValueWithoutNotify(evt.newValue);
                delta = evt.newValue - _rotationDragStartValue;
            }

            _onTransformHelperChanged?.Invoke(new PanelView.TransformHelperChange(PanelView.TransformHelperField.Rotate, delta));
        }

        private void RegisterRotateDragCallbacks()
        {
            if (_form.RotateField == null)
            {
                return;
            }

            CallbackBindingUtility.ToggleCallback<PointerDownEvent>(_form.RotateField, OnRotatePointerDown, register: true, TrickleDown.TrickleDown);
            CallbackBindingUtility.ToggleCallback<PointerUpEvent>(_form.RotateField, OnRotatePointerUp, register: true, TrickleDown.TrickleDown);
            CallbackBindingUtility.ToggleCallback<PointerCancelEvent>(_form.RotateField, OnRotatePointerCancel, register: true, TrickleDown.TrickleDown);
            CallbackBindingUtility.ToggleCallback<PointerCaptureOutEvent>(_form.RotateField, OnRotatePointerCaptureOut, register: true, TrickleDown.TrickleDown);
        }

        private void UnregisterRotateDragCallbacks()
        {
            if (_form.RotateField == null)
            {
                return;
            }

            CallbackBindingUtility.ToggleCallback<PointerDownEvent>(_form.RotateField, OnRotatePointerDown, register: false, TrickleDown.TrickleDown);
            CallbackBindingUtility.ToggleCallback<PointerUpEvent>(_form.RotateField, OnRotatePointerUp, register: false, TrickleDown.TrickleDown);
            CallbackBindingUtility.ToggleCallback<PointerCancelEvent>(_form.RotateField, OnRotatePointerCancel, register: false, TrickleDown.TrickleDown);
            CallbackBindingUtility.ToggleCallback<PointerCaptureOutEvent>(_form.RotateField, OnRotatePointerCaptureOut, register: false, TrickleDown.TrickleDown);
        }

        private void ToggleRotateChangedCallback(bool register)
        {
            CallbackBindingUtility.ToggleValueChangedCallback(_form.RotateField, OnRotateChanged, register);
        }

        private void OnRotatePointerDown(PointerDownEvent evt)
        {
            if (_form.RotateField == null || evt.button != 0 || evt.target is not VisualElement targetElement)
            {
                return;
            }

            for (VisualElement current = targetElement; current != null && current != _form.RotateField; current = current.parent)
            {
                if (!current.ClassListContains(DRAG_NUMBER_FIELD_PREFIX_CLASS_NAME))
                {
                    continue;
                }

                _isRotationDragging = true;
                _rotationDragStartValue = _form.RotateField.value;
                _onRotationDragStarted?.Invoke();
                return;
            }
        }

        private void OnRotatePointerUp(PointerUpEvent evt) => EndRotationDrag();
        private void OnRotatePointerCancel(PointerCancelEvent evt) => EndRotationDrag();
        private void OnRotatePointerCaptureOut(PointerCaptureOutEvent evt) => EndRotationDrag();

        private void EndRotationDrag()
        {
            if (!_isRotationDragging)
            {
                return;
            }

            _isRotationDragging = false;
            _onRotationDragEnded?.Invoke();
        }
    }
}
