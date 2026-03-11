using System;
using System.Collections.Generic;
using Core.UI.Foundation.Components.ColorPercentField;
using SelectElement = Core.UI.Foundation.Components.Select.Select;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorPanelView
    {
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

        private readonly InspectorFormControls _form = new();

        public event Action<ImmediateApplyField> ImmediateApplyRequested;
        public event Action FrameRectChanged;
        public event Action TransformHelperChanged;
        public event Action<PositionAction> PositionActionRequested;

        public bool IsBound => _form.IsBound;
        public bool FillEnabled => _form.FillEnabled;
        public bool StrokeEnabled => _form.StrokeEnabled;
        public bool StrokeWidthEnabled => _form.StrokeWidthEnabled;
        public bool OpacityEnabled => _form.OpacityEnabled;
        public bool DasharrayEnabled => _form.DasharrayEnabled;

        public VisualElement FillColorControl => _form.FillColorControl;
        public VisualElement StrokeColorControl => _form.StrokeColorControl;
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
        public VisualElement LinecapControl => _form.LinecapPopup;
        public VisualElement LinejoinControl => _form.LinejoinPopup;
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

        public void CaptureState(InspectorPanelState inspectorPanelState)
        {
            InspectorStateBinder.CaptureState(_form, inspectorPanelState);
        }

        public void ApplyState(InspectorPanelState inspectorPanelState)
        {
            InspectorStateBinder.ApplyState(_form, inspectorPanelState);
        }

        public void SetTransformText(string transform)
        {
            _form.SetTransformText(transform);
        }

        private void RegisterCallbacks()
        {
            RegisterImmediateApplyCallback(_form.FillColorField, OnFillColorChanged);
            RegisterImmediateApplyCallback(_form.FillColorLegacyField, OnFillColorChanged);
            RegisterImmediateApplyCallback(_form.StrokeColorField, OnStrokeColorChanged);
            RegisterImmediateApplyCallback(_form.StrokeColorLegacyField, OnStrokeColorChanged);
            RegisterImmediateApplyCallback(_form.StrokeWidthField, OnStrokeWidthChanged);
            RegisterImmediateApplyCallback(_form.OpacityField, OnOpacityChanged);
            RegisterImmediateApplyCallback(_form.CornerRadiusField, OnCornerRadiusChanged);
            RegisterImmediateApplyCallback(_form.DashLengthField, OnStrokeDasharrayChanged);
            RegisterImmediateApplyCallback(_form.DashGapField, OnStrokeDasharrayChanged);
            RegisterImmediateApplyCallback(_form.LinecapPopup, OnStrokeLinecapChanged);
            RegisterImmediateApplyCallback(_form.LinejoinPopup, OnStrokeLinejoinChanged);
            RegisterFrameRectCallback(_form.FrameXField, OnFrameRectChanged);
            RegisterFrameRectCallback(_form.FrameYField, OnFrameRectChanged);
            RegisterFrameRectCallback(_form.FrameWidthField, OnFrameRectChanged);
            RegisterFrameRectCallback(_form.FrameHeightField, OnFrameRectChanged);
            RegisterTransformHelperCallback(_form.TranslateXField, OnTransformHelperChanged);
            RegisterTransformHelperCallback(_form.TranslateYField, OnTransformHelperChanged);
            RegisterTransformHelperCallback(_form.RotateField, OnTransformHelperChanged);
            RegisterTransformHelperCallback(_form.ScaleXField, OnTransformHelperChanged);
            RegisterTransformHelperCallback(_form.ScaleYField, OnTransformHelperChanged);
            RegisterButtonClicked(_form.PositionAlignLeftButton, OnPositionAlignLeftRequested);
            RegisterButtonClicked(_form.PositionAlignCenterButton, OnPositionAlignCenterRequested);
            RegisterButtonClicked(_form.PositionAlignRightButton, OnPositionAlignRightRequested);
            RegisterButtonClicked(_form.PositionAlignTopButton, OnPositionAlignTopRequested);
            RegisterButtonClicked(_form.PositionAlignMiddleButton, OnPositionAlignMiddleRequested);
            RegisterButtonClicked(_form.PositionAlignBottomButton, OnPositionAlignBottomRequested);
            RegisterButtonClicked(_form.PositionRotateClockwise90Button, OnPositionRotateClockwise90Requested);
            RegisterButtonClicked(_form.PositionFlipHorizontalButton, OnPositionFlipHorizontalRequested);
            RegisterButtonClicked(_form.PositionFlipVerticalButton, OnPositionFlipVerticalRequested);
        }

        private void UnregisterCallbacks()
        {
            UnregisterImmediateApplyCallback(_form.FillColorField, OnFillColorChanged);
            UnregisterImmediateApplyCallback(_form.FillColorLegacyField, OnFillColorChanged);
            UnregisterImmediateApplyCallback(_form.StrokeColorField, OnStrokeColorChanged);
            UnregisterImmediateApplyCallback(_form.StrokeColorLegacyField, OnStrokeColorChanged);
            UnregisterImmediateApplyCallback(_form.StrokeWidthField, OnStrokeWidthChanged);
            UnregisterImmediateApplyCallback(_form.OpacityField, OnOpacityChanged);
            UnregisterImmediateApplyCallback(_form.CornerRadiusField, OnCornerRadiusChanged);
            UnregisterImmediateApplyCallback(_form.DashLengthField, OnStrokeDasharrayChanged);
            UnregisterImmediateApplyCallback(_form.DashGapField, OnStrokeDasharrayChanged);
            UnregisterImmediateApplyCallback(_form.LinecapPopup, OnStrokeLinecapChanged);
            UnregisterImmediateApplyCallback(_form.LinejoinPopup, OnStrokeLinejoinChanged);
            UnregisterFrameRectCallback(_form.FrameXField, OnFrameRectChanged);
            UnregisterFrameRectCallback(_form.FrameYField, OnFrameRectChanged);
            UnregisterFrameRectCallback(_form.FrameWidthField, OnFrameRectChanged);
            UnregisterFrameRectCallback(_form.FrameHeightField, OnFrameRectChanged);
            UnregisterTransformHelperCallback(_form.TranslateXField, OnTransformHelperChanged);
            UnregisterTransformHelperCallback(_form.TranslateYField, OnTransformHelperChanged);
            UnregisterTransformHelperCallback(_form.RotateField, OnTransformHelperChanged);
            UnregisterTransformHelperCallback(_form.ScaleXField, OnTransformHelperChanged);
            UnregisterTransformHelperCallback(_form.ScaleYField, OnTransformHelperChanged);
            UnregisterButtonClicked(_form.PositionAlignLeftButton, OnPositionAlignLeftRequested);
            UnregisterButtonClicked(_form.PositionAlignCenterButton, OnPositionAlignCenterRequested);
            UnregisterButtonClicked(_form.PositionAlignRightButton, OnPositionAlignRightRequested);
            UnregisterButtonClicked(_form.PositionAlignTopButton, OnPositionAlignTopRequested);
            UnregisterButtonClicked(_form.PositionAlignMiddleButton, OnPositionAlignMiddleRequested);
            UnregisterButtonClicked(_form.PositionAlignBottomButton, OnPositionAlignBottomRequested);
            UnregisterButtonClicked(_form.PositionRotateClockwise90Button, OnPositionRotateClockwise90Requested);
            UnregisterButtonClicked(_form.PositionFlipHorizontalButton, OnPositionFlipHorizontalRequested);
            UnregisterButtonClicked(_form.PositionFlipVerticalButton, OnPositionFlipVerticalRequested);
        }

        private void OnFillColorChanged(ChangeEvent<Color> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.FillColor);

        private void OnStrokeColorChanged(ChangeEvent<Color> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeColor);

        private void OnStrokeWidthChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeWidth);

        private void OnOpacityChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.Opacity);

        private void OnCornerRadiusChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.CornerRadius);

        private void OnStrokeLinecapChanged(ChangeEvent<string> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeLinecap);

        private void OnStrokeLinejoinChanged(ChangeEvent<string> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeLinejoin);

        private void OnStrokeDasharrayChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeDasharray);

        private void OnFrameRectChanged(ChangeEvent<float> evt) => FrameRectChanged?.Invoke();

        private void OnTransformHelperChanged(ChangeEvent<float> evt) => TransformHelperChanged?.Invoke();

        private void OnPositionAlignLeftRequested() => PositionActionRequested?.Invoke(PositionAction.AlignLeft);

        private void OnPositionAlignCenterRequested() => PositionActionRequested?.Invoke(PositionAction.AlignCenter);

        private void OnPositionAlignRightRequested() => PositionActionRequested?.Invoke(PositionAction.AlignRight);

        private void OnPositionAlignTopRequested() => PositionActionRequested?.Invoke(PositionAction.AlignTop);

        private void OnPositionAlignMiddleRequested() => PositionActionRequested?.Invoke(PositionAction.AlignMiddle);

        private void OnPositionAlignBottomRequested() => PositionActionRequested?.Invoke(PositionAction.AlignBottom);

        private void OnPositionRotateClockwise90Requested() => PositionActionRequested?.Invoke(PositionAction.RotateClockwise90);

        private void OnPositionFlipHorizontalRequested() => PositionActionRequested?.Invoke(PositionAction.FlipHorizontal);

        private void OnPositionFlipVerticalRequested() => PositionActionRequested?.Invoke(PositionAction.FlipVertical);

        private static void RegisterButtonClicked(Button button, Action callback)
        {
            if (button == null || callback == null)
                return;

            button.clicked -= callback;
            button.clicked += callback;
        }

        private static void UnregisterButtonClicked(Button button, Action callback)
        {
            if (button == null || callback == null)
                return;

            button.clicked -= callback;
        }

        private static void RegisterImmediateApplyCallback(
            ColorPercentField field,
            EventCallback<ChangeEvent<Color>> callback)
        {
            if (field == null)
                return;

            field.UnregisterCallback(callback);
            field.RegisterCallback(callback);
        }

        private static void RegisterImmediateApplyCallback(
            ColorField field,
            EventCallback<ChangeEvent<Color>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            field.RegisterValueChangedCallback(callback);
        }

        private static void RegisterImmediateApplyCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            field.RegisterValueChangedCallback(callback);
        }

        private static void RegisterImmediateApplyCallback(
            SelectElement field,
            EventCallback<ChangeEvent<string>> callback)
        {
            if (field == null)
                return;

            field.UnregisterCallback(callback);
            field.RegisterCallback(callback);
        }

        private static void UnregisterImmediateApplyCallback(
            ColorPercentField field,
            EventCallback<ChangeEvent<Color>> callback)
        {
            if (field == null)
                return;

            field.UnregisterCallback(callback);
        }

        private static void UnregisterImmediateApplyCallback(
            ColorField field,
            EventCallback<ChangeEvent<Color>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
        }

        private static void UnregisterImmediateApplyCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
        }

        private static void UnregisterImmediateApplyCallback(
            SelectElement field,
            EventCallback<ChangeEvent<string>> callback)
        {
            if (field == null)
                return;

            field.UnregisterCallback(callback);
        }

        private static void RegisterFrameRectCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            field.RegisterValueChangedCallback(callback);
        }

        private static void UnregisterFrameRectCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
        }

        private static void RegisterTransformHelperCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            field.RegisterValueChangedCallback(callback);
        }

        private static void UnregisterTransformHelperCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
        }

        private static void RegisterTransformTextCallback(
            TextField field,
            EventCallback<ChangeEvent<string>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            field.RegisterValueChangedCallback(callback);
        }

        private static void UnregisterTransformTextCallback(
            TextField field,
            EventCallback<ChangeEvent<string>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
        }
    }
}
