using System;
using System.Collections.Generic;
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
            FillColor,
            StrokeColor,
            StrokeWidth,
            StrokeLinecap,
            StrokeLinejoin,
            StrokeDasharray
        }

        private readonly InspectorFormControls _form = new();

        public event Action<string> TargetChanged;
        public event Action<ImmediateApplyField> ImmediateApplyRequested;
        public event Action TransformHelperChanged;
        public event Action TransformTextChanged;
        public event Action ReadRequested;
        public event Action BuildTransformRequested;
        public event Action ApplyRequested;

        public bool IsBound => _form.IsBound;
        public string SelectedTargetLabel => _form.SelectedTargetLabel;
        public bool FillEnabled => _form.FillEnabled;
        public bool StrokeEnabled => _form.StrokeEnabled;
        public bool StrokeWidthEnabled => _form.StrokeWidthEnabled;
        public bool OpacityEnabled => _form.OpacityEnabled;
        public bool DasharrayEnabled => _form.DasharrayEnabled;
        public bool TransformEnabled => _form.TransformEnabled;

        public VisualElement TargetControl => _form.TargetPopup;
        public VisualElement FillColorControl => _form.FillColorField;
        public VisualElement StrokeColorControl => _form.StrokeColorField;
        public VisualElement StrokeWidthControl => _form.StrokeWidthField;
        public VisualElement OpacityControl => _form.OpacitySlider;
        public VisualElement DashLengthControl => _form.DashLengthField;
        public VisualElement DashGapControl => _form.DashGapField;
        public VisualElement TransformControl => _form.TransformField;
        public VisualElement LinecapControl => _form.LinecapPopup;
        public VisualElement LinejoinControl => _form.LinejoinPopup;
        public VisualElement TranslateXControl => _form.TranslateXField;
        public VisualElement TranslateYControl => _form.TranslateYField;
        public VisualElement RotateControl => _form.RotateField;
        public VisualElement ScaleXControl => _form.ScaleXField;
        public VisualElement ScaleYControl => _form.ScaleYField;
        public VisualElement ReadButtonControl => _form.ReadButton;
        public VisualElement BuildTransformButtonControl => _form.BuildTransformButton;
        public VisualElement ApplyButtonControl => _form.ApplyButton;

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

        public void SetTargetChoices(IReadOnlyList<string> choices)
        {
            _form.SetTargetChoices(choices);
        }

        public void SetSelectedTargetLabel(string label, bool notify)
        {
            _form.SetSelectedTargetLabel(label, notify);
        }

        public void SetTransformText(string transform)
        {
            _form.SetTransformText(transform);
        }

        public void CaptureState(InspectorPanelState inspectorPanelState)
        {
            InspectorStateBinder.CaptureState(_form, inspectorPanelState);
        }

        public void ApplyState(InspectorPanelState inspectorPanelState)
        {
            InspectorStateBinder.ApplyState(_form, inspectorPanelState);
        }

        private void RegisterCallbacks()
        {
            if (_form.TargetPopup != null)
            {
                _form.TargetPopup.UnregisterValueChangedCallback(OnTargetChanged);
                _form.TargetPopup.RegisterValueChangedCallback(OnTargetChanged);
            }

            RegisterImmediateApplyCallback(_form.FillColorField, OnFillColorChanged);
            RegisterImmediateApplyCallback(_form.StrokeColorField, OnStrokeColorChanged);
            RegisterImmediateApplyCallback(_form.StrokeWidthField, OnStrokeWidthChanged);
            RegisterImmediateApplyCallback(_form.OpacitySlider, OnOpacityChanged);
            RegisterImmediateApplyCallback(_form.DashLengthField, OnStrokeDasharrayChanged);
            RegisterImmediateApplyCallback(_form.DashGapField, OnStrokeDasharrayChanged);
            RegisterImmediateApplyCallback(_form.LinecapPopup, OnStrokeLinecapChanged);
            RegisterImmediateApplyCallback(_form.LinejoinPopup, OnStrokeLinejoinChanged);
            RegisterTransformHelperCallback(_form.TranslateXField, OnTransformHelperChanged);
            RegisterTransformHelperCallback(_form.TranslateYField, OnTransformHelperChanged);
            RegisterTransformHelperCallback(_form.RotateField, OnTransformHelperChanged);
            RegisterTransformHelperCallback(_form.ScaleXField, OnTransformHelperChanged);
            RegisterTransformHelperCallback(_form.ScaleYField, OnTransformHelperChanged);
            RegisterTransformTextCallback(_form.TransformField, OnTransformTextChanged);

            if (_form.ReadButton != null)
                _form.ReadButton.clicked += OnReadRequested;
            if (_form.BuildTransformButton != null)
                _form.BuildTransformButton.clicked += OnBuildTransformRequested;
            if (_form.ApplyButton != null)
                _form.ApplyButton.clicked += OnApplyRequested;
        }

        private void UnregisterCallbacks()
        {
            if (_form.TargetPopup != null)
                _form.TargetPopup.UnregisterValueChangedCallback(OnTargetChanged);

            UnregisterImmediateApplyCallback(_form.FillColorField, OnFillColorChanged);
            UnregisterImmediateApplyCallback(_form.StrokeColorField, OnStrokeColorChanged);
            UnregisterImmediateApplyCallback(_form.StrokeWidthField, OnStrokeWidthChanged);
            UnregisterImmediateApplyCallback(_form.OpacitySlider, OnOpacityChanged);
            UnregisterImmediateApplyCallback(_form.DashLengthField, OnStrokeDasharrayChanged);
            UnregisterImmediateApplyCallback(_form.DashGapField, OnStrokeDasharrayChanged);
            UnregisterImmediateApplyCallback(_form.LinecapPopup, OnStrokeLinecapChanged);
            UnregisterImmediateApplyCallback(_form.LinejoinPopup, OnStrokeLinejoinChanged);
            UnregisterTransformHelperCallback(_form.TranslateXField, OnTransformHelperChanged);
            UnregisterTransformHelperCallback(_form.TranslateYField, OnTransformHelperChanged);
            UnregisterTransformHelperCallback(_form.RotateField, OnTransformHelperChanged);
            UnregisterTransformHelperCallback(_form.ScaleXField, OnTransformHelperChanged);
            UnregisterTransformHelperCallback(_form.ScaleYField, OnTransformHelperChanged);
            UnregisterTransformTextCallback(_form.TransformField, OnTransformTextChanged);

            if (_form.ReadButton != null)
                _form.ReadButton.clicked -= OnReadRequested;
            if (_form.BuildTransformButton != null)
                _form.BuildTransformButton.clicked -= OnBuildTransformRequested;
            if (_form.ApplyButton != null)
                _form.ApplyButton.clicked -= OnApplyRequested;
        }

        private void OnTargetChanged(ChangeEvent<string> evt) => TargetChanged?.Invoke(evt.newValue ?? string.Empty);

        private void OnFillColorChanged(ChangeEvent<Color> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.FillColor);

        private void OnStrokeColorChanged(ChangeEvent<Color> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeColor);

        private void OnStrokeWidthChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeWidth);

        private void OnOpacityChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.Opacity);

        private void OnStrokeLinecapChanged(ChangeEvent<string> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeLinecap);

        private void OnStrokeLinejoinChanged(ChangeEvent<string> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeLinejoin);

        private void OnStrokeDasharrayChanged(ChangeEvent<float> evt) => ImmediateApplyRequested?.Invoke(ImmediateApplyField.StrokeDasharray);

        private void OnTransformHelperChanged(ChangeEvent<float> evt) => TransformHelperChanged?.Invoke();

        private void OnTransformTextChanged(ChangeEvent<string> evt) => TransformTextChanged?.Invoke();

        private void OnReadRequested() => ReadRequested?.Invoke();

        private void OnBuildTransformRequested() => BuildTransformRequested?.Invoke();

        private void OnApplyRequested() => ApplyRequested?.Invoke();

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
            PopupField<string> field,
            EventCallback<ChangeEvent<string>> callback)
        {
            if (field == null)
                return;

            field.UnregisterValueChangedCallback(callback);
            field.RegisterValueChangedCallback(callback);
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
            PopupField<string> field,
            EventCallback<ChangeEvent<string>> callback)
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
