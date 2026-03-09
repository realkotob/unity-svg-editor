using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class PatchInspectorView
    {
        private readonly PatchInspectorFormControls _form = new();

        public event Action<string> TargetChanged;
        public event Action InteractivityToggleChanged;
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
        public VisualElement FillToggleControl => _form.FillEnabledToggle;
        public VisualElement StrokeToggleControl => _form.StrokeEnabledToggle;
        public VisualElement StrokeWidthToggleControl => _form.StrokeWidthEnabledToggle;
        public VisualElement OpacityToggleControl => _form.OpacityEnabledToggle;
        public VisualElement DasharrayToggleControl => _form.DasharrayEnabledToggle;
        public VisualElement TransformToggleControl => _form.TransformEnabledToggle;
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

        public void CaptureState(PatchPanelState patchPanelState)
        {
            PatchInspectorStateBinder.CaptureState(_form, patchPanelState);
        }

        public void ApplyState(PatchPanelState patchPanelState)
        {
            PatchInspectorStateBinder.ApplyState(_form, patchPanelState);
        }

        private void RegisterCallbacks()
        {
            if (_form.TargetPopup != null)
            {
                _form.TargetPopup.UnregisterValueChangedCallback(OnPatchTargetChanged);
                _form.TargetPopup.RegisterValueChangedCallback(OnPatchTargetChanged);
            }

            foreach (var toggle in _form.InteractivityToggles)
            {
                if (toggle == null)
                    continue;

                toggle.UnregisterValueChangedCallback(OnInteractivityToggleChanged);
                toggle.RegisterValueChangedCallback(OnInteractivityToggleChanged);
            }

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
                _form.TargetPopup.UnregisterValueChangedCallback(OnPatchTargetChanged);

            foreach (var toggle in _form.InteractivityToggles)
            {
                if (toggle == null)
                    continue;

                toggle.UnregisterValueChangedCallback(OnInteractivityToggleChanged);
            }

            if (_form.ReadButton != null)
                _form.ReadButton.clicked -= OnReadRequested;
            if (_form.BuildTransformButton != null)
                _form.BuildTransformButton.clicked -= OnBuildTransformRequested;
            if (_form.ApplyButton != null)
                _form.ApplyButton.clicked -= OnApplyRequested;
        }

        private void OnPatchTargetChanged(ChangeEvent<string> evt) => TargetChanged?.Invoke(evt.newValue ?? string.Empty);

        private void OnInteractivityToggleChanged(ChangeEvent<bool> evt) => InteractivityToggleChanged?.Invoke();

        private void OnReadRequested() => ReadRequested?.Invoke();

        private void OnBuildTransformRequested() => BuildTransformRequested?.Invoke();

        private void OnApplyRequested() => ApplyRequested?.Invoke();
    }
}
