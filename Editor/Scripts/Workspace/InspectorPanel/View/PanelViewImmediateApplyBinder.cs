using System;
using Core.UI.Foundation.Components.ColorPercentField;
using SelectElement = Core.UI.Foundation.Components.Select.Select;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PanelViewImmediateApplyBinder
    {
        private readonly FormControls _form;
        private readonly Action<PanelView.ImmediateApplyField> _onImmediateApplyRequested;
        private readonly Action _onFrameRectChanged;
        private readonly Action<PanelView.TransformHelperChange> _onTransformHelperChanged;

        public PanelViewImmediateApplyBinder(
            FormControls form,
            Action<PanelView.ImmediateApplyField> onImmediateApplyRequested,
            Action onFrameRectChanged,
            Action<PanelView.TransformHelperChange> onTransformHelperChanged)
        {
            _form = form;
            _onImmediateApplyRequested = onImmediateApplyRequested;
            _onFrameRectChanged = onFrameRectChanged;
            _onTransformHelperChanged = onTransformHelperChanged;
        }

        public void Bind()
        {
            ToggleImmediateApplyCallbacks(register: true);
            ToggleFrameRectCallbacks(register: true);
            ToggleTransformHelperCallbacks(register: true);
        }

        public void Unbind()
        {
            ToggleImmediateApplyCallbacks(register: false);
            ToggleFrameRectCallbacks(register: false);
            ToggleTransformHelperCallbacks(register: false);
        }

        private void OnFillColorChanged(ChangeEvent<Color> evt) => RequestImmediateApply(PanelView.ImmediateApplyField.FillColor);
        private void OnStrokeColorChanged(ChangeEvent<Color> evt) => RequestImmediateApply(PanelView.ImmediateApplyField.StrokeColor);
        private void OnStrokeWidthChanged(ChangeEvent<float> evt) => RequestImmediateApply(PanelView.ImmediateApplyField.StrokeWidth);
        private void OnOpacityChanged(ChangeEvent<float> evt) => RequestImmediateApply(PanelView.ImmediateApplyField.Opacity);
        private void OnCornerRadiusChanged(ChangeEvent<float> evt) => RequestImmediateApply(PanelView.ImmediateApplyField.CornerRadius);
        private void OnStrokeLinecapChanged(ChangeEvent<string> evt) => RequestImmediateApply(PanelView.ImmediateApplyField.StrokeLinecap);
        private void OnStrokeLinejoinChanged(ChangeEvent<string> evt) => RequestImmediateApply(PanelView.ImmediateApplyField.StrokeLinejoin);
        private void OnStrokeDasharrayChanged(ChangeEvent<float> evt) => RequestImmediateApply(PanelView.ImmediateApplyField.StrokeDasharray);

        private void OnFrameRectChanged(ChangeEvent<float> evt) => _onFrameRectChanged?.Invoke();
        private void OnTranslateXChanged(ChangeEvent<float> evt) => NotifyTransformHelperChanged(PanelView.TransformHelperField.TranslateX);
        private void OnTranslateYChanged(ChangeEvent<float> evt) => NotifyTransformHelperChanged(PanelView.TransformHelperField.TranslateY);
        private void OnScaleXChanged(ChangeEvent<float> evt) => NotifyTransformHelperChanged(PanelView.TransformHelperField.ScaleX);
        private void OnScaleYChanged(ChangeEvent<float> evt) => NotifyTransformHelperChanged(PanelView.TransformHelperField.ScaleY);

        private void RequestImmediateApply(PanelView.ImmediateApplyField field)
        {
            _onImmediateApplyRequested?.Invoke(field);
        }

        private void NotifyTransformHelperChanged(PanelView.TransformHelperField field)
        {
            _onTransformHelperChanged?.Invoke(new PanelView.TransformHelperChange(field));
        }

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

        private void ToggleFrameRectCallbacks(bool register)
        {
            ToggleValueChangedCallback(_form.FrameXField, OnFrameRectChanged, register);
            ToggleValueChangedCallback(_form.FrameYField, OnFrameRectChanged, register);
            ToggleValueChangedCallback(_form.FrameWidthField, OnFrameRectChanged, register);
            ToggleValueChangedCallback(_form.FrameHeightField, OnFrameRectChanged, register);
        }

        private void ToggleTransformHelperCallbacks(bool register)
        {
            ToggleValueChangedCallback(_form.TranslateXField, OnTranslateXChanged, register);
            ToggleValueChangedCallback(_form.TranslateYField, OnTranslateYChanged, register);
            ToggleValueChangedCallback(_form.ScaleXField, OnScaleXChanged, register);
            ToggleValueChangedCallback(_form.ScaleYField, OnScaleYChanged, register);
        }

        private static void ToggleImmediateApplyCallback(
            ColorPercentField field,
            EventCallback<ChangeEvent<Color>> callback,
            bool register)
        {
            if (field == null)
            {
                return;
            }

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
            {
                return;
            }

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
            ToggleValueChangedCallback(field, callback, register);
        }

        private static void ToggleImmediateApplyCallback(
            SelectElement field,
            EventCallback<ChangeEvent<string>> callback,
            bool register)
        {
            if (field == null)
            {
                return;
            }

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
            {
                return;
            }

            field.UnregisterValueChangedCallback(callback);
            if (register)
            {
                field.RegisterValueChangedCallback(callback);
            }
        }

        private static void ToggleValueChangedCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback,
            bool register)
        {
            if (field == null)
            {
                return;
            }

            field.UnregisterValueChangedCallback(callback);
            if (register)
            {
                field.RegisterValueChangedCallback(callback);
            }
        }
    }
}
