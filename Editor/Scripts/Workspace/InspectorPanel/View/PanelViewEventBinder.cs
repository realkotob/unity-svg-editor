using System;
using Core.UI.Foundation.Components.ColorPercentField;
using SelectElement = Core.UI.Foundation.Components.Select.Select;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PanelViewEventBinder
    {
        private const string DragNumberFieldPrefixClassName = "drag-number-field__prefix";

        private readonly FormControls _form;
        private readonly Action<PanelView.ImmediateApplyField> _onImmediateApplyRequested;
        private readonly Action _onFrameRectChanged;
        private readonly Action<PanelView.TransformHelperChange> _onTransformHelperChanged;
        private readonly Action _onRotationDragStarted;
        private readonly Action _onRotationDragEnded;
        private readonly Action<PanelView.PositionAction> _onPositionActionRequested;
        private readonly Action<PanelView.AttributeAction> _onAttributeActionRequested;

        private bool _isRotationDragging;
        private float _rotationDragStartValue;

        public PanelViewEventBinder(
            FormControls form,
            Action<PanelView.ImmediateApplyField> onImmediateApplyRequested,
            Action onFrameRectChanged,
            Action<PanelView.TransformHelperChange> onTransformHelperChanged,
            Action onRotationDragStarted,
            Action onRotationDragEnded,
            Action<PanelView.PositionAction> onPositionActionRequested,
            Action<PanelView.AttributeAction> onAttributeActionRequested)
        {
            _form = form;
            _onImmediateApplyRequested = onImmediateApplyRequested;
            _onFrameRectChanged = onFrameRectChanged;
            _onTransformHelperChanged = onTransformHelperChanged;
            _onRotationDragStarted = onRotationDragStarted;
            _onRotationDragEnded = onRotationDragEnded;
            _onPositionActionRequested = onPositionActionRequested;
            _onAttributeActionRequested = onAttributeActionRequested;
        }

        public void Bind()
        {
            ToggleImmediateApplyCallbacks(register: true);
            ToggleAttributeActionCallbacks(register: true);
            ToggleFrameRectCallbacks(register: true);
            ToggleTransformHelperCallbacks(register: true);
            RegisterRotateDragCallbacks();
            TogglePositionActionCallbacks(register: true);
        }

        public void Unbind()
        {
            ToggleImmediateApplyCallbacks(register: false);
            ToggleAttributeActionCallbacks(register: false);
            ToggleFrameRectCallbacks(register: false);
            ToggleTransformHelperCallbacks(register: false);
            UnregisterRotateDragCallbacks();
            TogglePositionActionCallbacks(register: false);
            _isRotationDragging = false;
            _rotationDragStartValue = 0f;
        }

        private void OnFillColorChanged(ChangeEvent<Color> evt) => _onImmediateApplyRequested?.Invoke(PanelView.ImmediateApplyField.FillColor);

        private void OnFillAddRequested() => _onAttributeActionRequested?.Invoke(PanelView.AttributeAction.AddFill);

        private void OnFillRemoveRequested() => _onAttributeActionRequested?.Invoke(PanelView.AttributeAction.RemoveFill);

        private void OnStrokeColorChanged(ChangeEvent<Color> evt) => _onImmediateApplyRequested?.Invoke(PanelView.ImmediateApplyField.StrokeColor);

        private void OnStrokeAddRequested() => _onAttributeActionRequested?.Invoke(PanelView.AttributeAction.AddStroke);

        private void OnStrokeRemoveRequested() => _onAttributeActionRequested?.Invoke(PanelView.AttributeAction.RemoveStroke);

        private void OnStrokeWidthChanged(ChangeEvent<float> evt) => _onImmediateApplyRequested?.Invoke(PanelView.ImmediateApplyField.StrokeWidth);

        private void OnOpacityChanged(ChangeEvent<float> evt) => _onImmediateApplyRequested?.Invoke(PanelView.ImmediateApplyField.Opacity);

        private void OnCornerRadiusChanged(ChangeEvent<float> evt) => _onImmediateApplyRequested?.Invoke(PanelView.ImmediateApplyField.CornerRadius);

        private void OnStrokeLinecapChanged(ChangeEvent<string> evt) => _onImmediateApplyRequested?.Invoke(PanelView.ImmediateApplyField.StrokeLinecap);

        private void OnStrokeLinejoinChanged(ChangeEvent<string> evt) => _onImmediateApplyRequested?.Invoke(PanelView.ImmediateApplyField.StrokeLinejoin);

        private void OnStrokeDasharrayChanged(ChangeEvent<float> evt) => _onImmediateApplyRequested?.Invoke(PanelView.ImmediateApplyField.StrokeDasharray);

        private void OnFrameRectChanged(ChangeEvent<float> evt) => _onFrameRectChanged?.Invoke();

        private void OnTranslateXChanged(ChangeEvent<float> evt) => _onTransformHelperChanged?.Invoke(new PanelView.TransformHelperChange(PanelView.TransformHelperField.TranslateX));

        private void OnTranslateYChanged(ChangeEvent<float> evt) => _onTransformHelperChanged?.Invoke(new PanelView.TransformHelperChange(PanelView.TransformHelperField.TranslateY));

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

        private void OnScaleXChanged(ChangeEvent<float> evt) => _onTransformHelperChanged?.Invoke(new PanelView.TransformHelperChange(PanelView.TransformHelperField.ScaleX));

        private void OnScaleYChanged(ChangeEvent<float> evt) => _onTransformHelperChanged?.Invoke(new PanelView.TransformHelperChange(PanelView.TransformHelperField.ScaleY));

        private void RegisterRotateDragCallbacks()
        {
            if (_form.RotateField == null)
            {
                return;
            }

            _form.RotateField.RegisterCallback<PointerDownEvent>(OnRotatePointerDown, TrickleDown.TrickleDown);
            _form.RotateField.RegisterCallback<PointerUpEvent>(OnRotatePointerUp, TrickleDown.TrickleDown);
            _form.RotateField.RegisterCallback<PointerCancelEvent>(OnRotatePointerCancel, TrickleDown.TrickleDown);
            _form.RotateField.RegisterCallback<PointerCaptureOutEvent>(OnRotatePointerCaptureOut, TrickleDown.TrickleDown);
        }

        private void UnregisterRotateDragCallbacks()
        {
            if (_form.RotateField == null)
            {
                return;
            }

            _form.RotateField.UnregisterCallback<PointerDownEvent>(OnRotatePointerDown, TrickleDown.TrickleDown);
            _form.RotateField.UnregisterCallback<PointerUpEvent>(OnRotatePointerUp, TrickleDown.TrickleDown);
            _form.RotateField.UnregisterCallback<PointerCancelEvent>(OnRotatePointerCancel, TrickleDown.TrickleDown);
            _form.RotateField.UnregisterCallback<PointerCaptureOutEvent>(OnRotatePointerCaptureOut, TrickleDown.TrickleDown);
        }

        private void OnRotatePointerDown(PointerDownEvent evt)
        {
            if (_form.RotateField == null || evt.button != 0 || evt.target is not VisualElement targetElement)
            {
                return;
            }

            for (VisualElement current = targetElement; current != null && current != _form.RotateField; current = current.parent)
            {
                if (!current.ClassListContains(DragNumberFieldPrefixClassName))
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

        private void OnPositionAlignLeftRequested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.AlignLeft);

        private void OnPositionAlignCenterRequested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.AlignCenter);

        private void OnPositionAlignRightRequested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.AlignRight);

        private void OnPositionAlignTopRequested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.AlignTop);

        private void OnPositionAlignMiddleRequested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.AlignMiddle);

        private void OnPositionAlignBottomRequested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.AlignBottom);

        private void OnPositionRotateClockwise90Requested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.RotateClockwise90);

        private void OnPositionFlipHorizontalRequested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.FlipHorizontal);

        private void OnPositionFlipVerticalRequested() => _onPositionActionRequested?.Invoke(PanelView.PositionAction.FlipVertical);

        private void ToggleImmediateApplyCallbacks(bool register)
        {
            ToggleImmediateApplyCallback(_form.FillColorField, OnFillColorChanged, register);
            ToggleImmediateApplyCallback(_form.FillColorLegacyField, OnFillColorChanged, register);
            ToggleImmediateApplyCallback(_form.StrokeColorField, OnStrokeColorChanged, register);
            ToggleImmediateApplyCallback(_form.StrokeColorLegacyField, OnStrokeColorChanged, register);
            ToggleImmediateApplyCallback(_form.StrokeWidthField, OnStrokeWidthChanged, register);
            ToggleImmediateApplyCallback(_form.OpacityField, OnOpacityChanged, register);
            ToggleImmediateApplyCallback(_form.CornerRadiusField, OnCornerRadiusChanged, register);
            ToggleImmediateApplyCallback(_form.DashLengthField, OnStrokeDasharrayChanged, register);
            ToggleImmediateApplyCallback(_form.DashGapField, OnStrokeDasharrayChanged, register);
            ToggleImmediateApplyCallback(_form.LinecapPopup, OnStrokeLinecapChanged, register);
            ToggleImmediateApplyCallback(_form.LinecapLegacyPopup, OnStrokeLinecapChanged, register);
            ToggleImmediateApplyCallback(_form.LinejoinPopup, OnStrokeLinejoinChanged, register);
            ToggleImmediateApplyCallback(_form.LinejoinLegacyPopup, OnStrokeLinejoinChanged, register);
        }

        private void ToggleAttributeActionCallbacks(bool register)
        {
            ToggleButtonClicked(_form.FillAddButton, OnFillAddRequested, register);
            ToggleButtonClicked(_form.FillRemoveButton, OnFillRemoveRequested, register);
            ToggleButtonClicked(_form.StrokeAddButton, OnStrokeAddRequested, register);
            ToggleButtonClicked(_form.StrokeRemoveButton, OnStrokeRemoveRequested, register);
        }

        private void ToggleFrameRectCallbacks(bool register)
        {
            ToggleFrameRectCallback(_form.FrameXField, OnFrameRectChanged, register);
            ToggleFrameRectCallback(_form.FrameYField, OnFrameRectChanged, register);
            ToggleFrameRectCallback(_form.FrameWidthField, OnFrameRectChanged, register);
            ToggleFrameRectCallback(_form.FrameHeightField, OnFrameRectChanged, register);
        }

        private void ToggleTransformHelperCallbacks(bool register)
        {
            ToggleTransformHelperCallback(_form.TranslateXField, OnTranslateXChanged, register);
            ToggleTransformHelperCallback(_form.TranslateYField, OnTranslateYChanged, register);
            ToggleTransformHelperCallback(_form.RotateField, OnRotateChanged, register);
            ToggleTransformHelperCallback(_form.ScaleXField, OnScaleXChanged, register);
            ToggleTransformHelperCallback(_form.ScaleYField, OnScaleYChanged, register);
        }

        private void TogglePositionActionCallbacks(bool register)
        {
            ToggleButtonClicked(_form.PositionAlignLeftButton, OnPositionAlignLeftRequested, register);
            ToggleButtonClicked(_form.PositionAlignCenterButton, OnPositionAlignCenterRequested, register);
            ToggleButtonClicked(_form.PositionAlignRightButton, OnPositionAlignRightRequested, register);
            ToggleButtonClicked(_form.PositionAlignTopButton, OnPositionAlignTopRequested, register);
            ToggleButtonClicked(_form.PositionAlignMiddleButton, OnPositionAlignMiddleRequested, register);
            ToggleButtonClicked(_form.PositionAlignBottomButton, OnPositionAlignBottomRequested, register);
            ToggleButtonClicked(_form.PositionRotateClockwise90Button, OnPositionRotateClockwise90Requested, register);
            ToggleButtonClicked(_form.PositionFlipHorizontalButton, OnPositionFlipHorizontalRequested, register);
            ToggleButtonClicked(_form.PositionFlipVerticalButton, OnPositionFlipVerticalRequested, register);
        }

        private static void ToggleButtonClicked(Button button, Action callback, bool register)
        {
            if (button == null || callback == null)
                return;

            button.clicked -= callback;
            if (register)
            {
                button.clicked += callback;
            }
        }

        private static void ToggleImmediateApplyCallback(
            ColorPercentField field,
            EventCallback<ChangeEvent<Color>> callback,
            bool register)
        {
            if (field == null)
                return;

            field.UnregisterCallback(callback);
            if (register)
            {
                field.RegisterCallback(callback);
            }
        }

        private static void ToggleImmediateApplyCallback(
            ColorField field,
            EventCallback<ChangeEvent<Color>> callback,
            bool register)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            if (register)
            {
                field.RegisterValueChangedCallback(callback);
            }
        }

        private static void ToggleImmediateApplyCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback,
            bool register)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            if (register)
            {
                field.RegisterValueChangedCallback(callback);
            }
        }

        private static void ToggleImmediateApplyCallback(
            SelectElement field,
            EventCallback<ChangeEvent<string>> callback,
            bool register)
        {
            if (field == null)
                return;

            field.UnregisterCallback(callback);
            if (register)
            {
                field.RegisterCallback(callback);
            }
        }

        private static void ToggleImmediateApplyCallback(
            DropdownField field,
            EventCallback<ChangeEvent<string>> callback,
            bool register)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            if (register)
            {
                field.RegisterValueChangedCallback(callback);
            }
        }

        private static void ToggleFrameRectCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback,
            bool register)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            if (register)
            {
                field.RegisterValueChangedCallback(callback);
            }
        }

        private static void ToggleTransformHelperCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback,
            bool register)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            if (register)
            {
                field.RegisterValueChangedCallback(callback);
            }
        }
    }
}
