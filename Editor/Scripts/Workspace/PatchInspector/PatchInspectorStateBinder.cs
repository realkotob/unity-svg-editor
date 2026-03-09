using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal static class PatchInspectorStateBinder
    {
        public static void CaptureState(PatchInspectorFormControls form, PatchPanelState patchPanelState)
        {
            if (form == null || patchPanelState == null)
                return;

            patchPanelState.SelectTargetLabel(form.SelectedTargetLabel);
            patchPanelState.FillEnabled = form.FillEnabledToggle?.value ?? false;
            patchPanelState.FillColor = form.FillColorField?.value ?? Color.white;
            patchPanelState.StrokeEnabled = form.StrokeEnabledToggle?.value ?? false;
            patchPanelState.StrokeColor = form.StrokeColorField?.value ?? Color.black;
            patchPanelState.StrokeWidthEnabled = form.StrokeWidthEnabledToggle?.value ?? false;
            patchPanelState.StrokeWidth = form.StrokeWidthField?.value ?? 1f;
            patchPanelState.OpacityEnabled = form.OpacityEnabledToggle?.value ?? false;
            patchPanelState.Opacity = form.OpacitySlider?.value ?? 1f;
            patchPanelState.StrokeLinecap = form.LinecapPopup?.value ?? string.Empty;
            patchPanelState.StrokeLinejoin = form.LinejoinPopup?.value ?? string.Empty;
            patchPanelState.DasharrayEnabled = form.DasharrayEnabledToggle?.value ?? false;
            patchPanelState.DashLength = form.DashLengthField?.value ?? 4f;
            patchPanelState.DashGap = form.DashGapField?.value ?? 2f;
            patchPanelState.TransformEnabled = form.TransformEnabledToggle?.value ?? false;
            patchPanelState.Transform = form.TransformField?.value ?? string.Empty;
            patchPanelState.TranslateX = form.TranslateXField?.value ?? 0f;
            patchPanelState.TranslateY = form.TranslateYField?.value ?? 0f;
            patchPanelState.Rotate = form.RotateField?.value ?? 0f;
            patchPanelState.ScaleX = form.ScaleXField?.value ?? 1f;
            patchPanelState.ScaleY = form.ScaleYField?.value ?? 1f;
        }

        public static void ApplyState(PatchInspectorFormControls form, PatchPanelState patchPanelState)
        {
            if (form == null || patchPanelState == null)
                return;

            form.SetSelectedTargetLabel(patchPanelState.SelectedTargetLabel, notify: false);
            form.FillEnabledToggle?.SetValueWithoutNotify(patchPanelState.FillEnabled);
            form.FillColorField?.SetValueWithoutNotify(patchPanelState.FillColor);
            form.StrokeEnabledToggle?.SetValueWithoutNotify(patchPanelState.StrokeEnabled);
            form.StrokeColorField?.SetValueWithoutNotify(patchPanelState.StrokeColor);
            form.StrokeWidthEnabledToggle?.SetValueWithoutNotify(patchPanelState.StrokeWidthEnabled);
            form.StrokeWidthField?.SetValueWithoutNotify(patchPanelState.StrokeWidth);
            form.OpacityEnabledToggle?.SetValueWithoutNotify(patchPanelState.OpacityEnabled);
            form.OpacitySlider?.SetValueWithoutNotify(patchPanelState.Opacity);
            SetPopupValue(form.LinecapPopup, patchPanelState.StrokeLinecap);
            SetPopupValue(form.LinejoinPopup, patchPanelState.StrokeLinejoin);
            form.DasharrayEnabledToggle?.SetValueWithoutNotify(patchPanelState.DasharrayEnabled);
            form.DashLengthField?.SetValueWithoutNotify(patchPanelState.DashLength);
            form.DashGapField?.SetValueWithoutNotify(patchPanelState.DashGap);
            form.TransformEnabledToggle?.SetValueWithoutNotify(patchPanelState.TransformEnabled);
            form.TransformField?.SetValueWithoutNotify(patchPanelState.Transform);
            form.TranslateXField?.SetValueWithoutNotify(patchPanelState.TranslateX);
            form.TranslateYField?.SetValueWithoutNotify(patchPanelState.TranslateY);
            form.RotateField?.SetValueWithoutNotify(patchPanelState.Rotate);
            form.ScaleXField?.SetValueWithoutNotify(patchPanelState.ScaleX);
            form.ScaleYField?.SetValueWithoutNotify(patchPanelState.ScaleY);
        }

        private static void SetPopupValue(PopupField<string> popup, string value)
        {
            if (popup == null)
                return;

            if (!string.IsNullOrWhiteSpace(value) && popup.choices.Contains(value))
                popup.SetValueWithoutNotify(value);
            else
                popup.SetValueWithoutNotify(string.Empty);
        }
    }
}
