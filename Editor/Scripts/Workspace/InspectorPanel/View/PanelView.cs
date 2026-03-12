using System;
using System.Collections.Generic;
using Core.UI.Foundation.Components.ColorPercentField;
using SelectElement = Core.UI.Foundation.Components.Select.Select;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PanelView
    {
        private const string DragNumberFieldPrefixClassName = "drag-number-field__prefix";

        internal enum ImmediateApplyField
        {
            Opacity,
            CornerRadius,
            FillColor,
            StrokeColor,
            StrokeWidth,
            StrokeLinecap,
            StrokeLinejoin,
            StrokeDasharray
        }

        internal enum AttributeAction
        {
            AddFill,
            RemoveFill,
            AddStroke,
            RemoveStroke
        }

        internal enum PositionAction
        {
            AlignLeft,
            AlignCenter,
            AlignRight,
            AlignTop,
            AlignMiddle,
            AlignBottom,
            RotateClockwise90,
            FlipHorizontal,
            FlipVertical
        }

        internal enum TransformHelperField
        {
            TranslateX,
            TranslateY,
            Rotate,
            ScaleX,
            ScaleY
        }

        internal readonly struct TransformHelperChange
        {
            public TransformHelperChange(TransformHelperField field, float delta = 0f)
            {
                Field = field;
                Delta = delta;
            }

            public TransformHelperField Field { get; }
            public float Delta { get; }
        }

        private readonly FormControls _form = new();

        public event Action<ImmediateApplyField> ImmediateApplyRequested;
        public event Action FrameRectChanged;
        public event Action<TransformHelperChange> TransformHelperChanged;
        public event Action RotationDragStarted;
        public event Action RotationDragEnded;
        public event Action<PositionAction> PositionActionRequested;
        public event Action<AttributeAction> AttributeActionRequested;

        private bool _isRotationDragging;
        private float _rotationDragStartValue;

        public bool IsBound => _form.IsBound;
        public bool FillEnabled => _form.FillEnabled;
        public bool StrokeEnabled => _form.StrokeEnabled;
        public bool StrokeWidthEnabled => _form.StrokeWidthEnabled;
        public bool OpacityEnabled => _form.OpacityEnabled;
        public bool DasharrayEnabled => _form.DasharrayEnabled;

        public VisualElement FillColorControl => _form.FillColorControl;
        public VisualElement FillAddControl => _form.FillAddButton;
        public VisualElement FillRemoveControl => _form.FillRemoveButton;
        public VisualElement StrokeColorControl => _form.StrokeColorControl;
        public VisualElement StrokeAddControl => _form.StrokeAddButton;
        public VisualElement StrokeRemoveControl => _form.StrokeRemoveButton;
        public VisualElement StrokeWidthControl => _form.StrokeWidthField;
        public VisualElement OpacityControl => _form.OpacityControl;
        public VisualElement CornerRadiusControl => _form.CornerRadiusField;
        public VisualElement DashLengthControl => _form.DashLengthField;
        public VisualElement DashGapControl => _form.DashGapField;
        public VisualElement TransformControl => _form.TransformField;
        public VisualElement FrameXControl => _form.FrameXField;
        public VisualElement FrameYControl => _form.FrameYField;
        public VisualElement FrameWidthControl => _form.FrameWidthField;
        public VisualElement FrameHeightControl => _form.FrameHeightField;
        public VisualElement LinecapControl => (VisualElement)_form.LinecapPopup ?? _form.LinecapLegacyPopup;
        public VisualElement LinejoinControl => (VisualElement)_form.LinejoinPopup ?? _form.LinejoinLegacyPopup;
        public VisualElement TranslateXControl => _form.TranslateXField;
        public VisualElement TranslateYControl => _form.TranslateYField;
        public VisualElement RotateControl => _form.RotateField;
        public VisualElement ScaleXControl => _form.ScaleXField;
        public VisualElement ScaleYControl => _form.ScaleYField;
        public VisualElement PositionAlignLeftControl => _form.PositionAlignLeftButton;
        public VisualElement PositionAlignCenterControl => _form.PositionAlignCenterButton;
        public VisualElement PositionAlignRightControl => _form.PositionAlignRightButton;
        public VisualElement PositionAlignTopControl => _form.PositionAlignTopButton;
        public VisualElement PositionAlignMiddleControl => _form.PositionAlignMiddleButton;
        public VisualElement PositionAlignBottomControl => _form.PositionAlignBottomButton;
        public VisualElement PositionRotateClockwise90Control => _form.PositionRotateClockwise90Button;
        public VisualElement PositionFlipHorizontalControl => _form.PositionFlipHorizontalButton;
        public VisualElement PositionFlipVerticalControl => _form.PositionFlipVerticalButton;

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            _form.Bind(root);
            RegisterCallbacks();
        }

        public void Unbind()
        {
            UnregisterCallbacks();
            _form.Unbind();
        }

        public void CaptureState(PanelState inspectorPanelState)
        {
            StateBinder.CaptureState(_form, inspectorPanelState);
        }

        public void ApplyState(PanelState inspectorPanelState)
        {
            StateBinder.ApplyState(_form, inspectorPanelState);
        }

        public void SetTransformText(string transform)
        {
            _form.SetTransformText(transform);
        }

        private void RegisterCallbacks()
        {
            ToggleImmediateApplyCallbacks(register: true);
            ToggleAttributeActionCallbacks(register: true);
            ToggleFrameRectCallbacks(register: true);
            ToggleTransformHelperCallbacks(register: true);
            RegisterRotateDragCallbacks();
            TogglePositionActionCallbacks(register: true);
        }

        private void UnregisterCallbacks()
        {
            ToggleImmediateApplyCallbacks(register: false);
            ToggleAttributeActionCallbacks(register: false);
            ToggleFrameRectCallbacks(register: false);
            ToggleTransformHelperCallbacks(register: false);
            UnregisterRotateDragCallbacks();
            TogglePositionActionCallbacks(register: false);
        }

        private void OnFillColorChanged(ChangeEvent<Color> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.FillColor);

        private void OnFillAddRequested() => AttributeActionRequested?.Invoke(AttributeAction.AddFill);

        private void OnFillRemoveRequested() => AttributeActionRequested?.Invoke(AttributeAction.RemoveFill);

        private void OnStrokeColorChanged(ChangeEvent<Color> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeColor);

        private void OnStrokeAddRequested() => AttributeActionRequested?.Invoke(AttributeAction.AddStroke);

        private void OnStrokeRemoveRequested() => AttributeActionRequested?.Invoke(AttributeAction.RemoveStroke);

        private void OnStrokeWidthChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeWidth);

        private void OnOpacityChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.Opacity);

        private void OnCornerRadiusChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.CornerRadius);

        private void OnStrokeLinecapChanged(ChangeEvent<string> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeLinecap);

        private void OnStrokeLinejoinChanged(ChangeEvent<string> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeLinejoin);

        private void OnStrokeDasharrayChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeDasharray);

        private void OnFrameRectChanged(ChangeEvent<float> evt) => FrameRectChanged?.Invoke();

        private void OnTranslateXChanged(ChangeEvent<float> evt) => TransformHelperChanged?.Invoke(new TransformHelperChange(TransformHelperField.TranslateX));

        private void OnTranslateYChanged(ChangeEvent<float> evt) => TransformHelperChanged?.Invoke(new TransformHelperChange(TransformHelperField.TranslateY));

        private void OnRotateChanged(ChangeEvent<float> evt)
        {
            float delta = evt.newValue - evt.previousValue;
            if (_isRotationDragging && _form.RotateField is FloatField rotateField)
            {
                rotateField.SetValueWithoutNotify(evt.newValue);
                delta = evt.newValue - _rotationDragStartValue;
            }

            TransformHelperChanged?.Invoke(new TransformHelperChange(TransformHelperField.Rotate, delta));
        }

        private void OnScaleXChanged(ChangeEvent<float> evt) => TransformHelperChanged?.Invoke(new TransformHelperChange(TransformHelperField.ScaleX));

        private void OnScaleYChanged(ChangeEvent<float> evt) => TransformHelperChanged?.Invoke(new TransformHelperChange(TransformHelperField.ScaleY));

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
                RotationDragStarted?.Invoke();
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
            RotationDragEnded?.Invoke();
        }

        private void OnPositionAlignLeftRequested() => PositionActionRequested?.Invoke(PositionAction.AlignLeft);

        private void OnPositionAlignCenterRequested() => PositionActionRequested?.Invoke(PositionAction.AlignCenter);

        private void OnPositionAlignRightRequested() => PositionActionRequested?.Invoke(PositionAction.AlignRight);

        private void OnPositionAlignTopRequested() => PositionActionRequested?.Invoke(PositionAction.AlignTop);

        private void OnPositionAlignMiddleRequested() => PositionActionRequested?.Invoke(PositionAction.AlignMiddle);

        private void OnPositionAlignBottomRequested() => PositionActionRequested?.Invoke(PositionAction.AlignBottom);

        private void OnPositionRotateClockwise90Requested() => PositionActionRequested?.Invoke(PositionAction.RotateClockwise90);

        private void OnPositionFlipHorizontalRequested() => PositionActionRequested?.Invoke(PositionAction.FlipHorizontal);

        private void OnPositionFlipVerticalRequested() => PositionActionRequested?.Invoke(PositionAction.FlipVertical);

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
