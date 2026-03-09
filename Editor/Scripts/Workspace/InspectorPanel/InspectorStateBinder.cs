using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal static class InspectorStateBinder
    {
        public static void CaptureState(InspectorFormControls form, InspectorPanelState inspectorPanelState)
        {
            if (form == null || inspectorPanelState == null)
                return;

            inspectorPanelState.SelectTargetLabel(form.SelectedTargetLabel);
            inspectorPanelState.FillEnabled = form.FillColorField != null;
            inspectorPanelState.FillColor = form.FillColorField?.value ?? Color.white;
            inspectorPanelState.StrokeEnabled = form.StrokeColorField != null;
            inspectorPanelState.StrokeColor = form.StrokeColorField?.value ?? Color.black;
            inspectorPanelState.StrokeWidthEnabled = form.StrokeWidthField != null;
            inspectorPanelState.StrokeWidth = form.StrokeWidthField?.value ?? 1f;
            inspectorPanelState.OpacityEnabled = form.OpacitySlider != null;
            inspectorPanelState.Opacity = form.OpacitySlider?.value ?? 1f;
            inspectorPanelState.StrokeLinecap = form.LinecapPopup?.value ?? string.Empty;
            inspectorPanelState.StrokeLinejoin = form.LinejoinPopup?.value ?? string.Empty;
            inspectorPanelState.DasharrayEnabled = form.DashLengthField != null || form.DashGapField != null;
            inspectorPanelState.DashLength = form.DashLengthField?.value ?? 4f;
            inspectorPanelState.DashGap = form.DashGapField?.value ?? 2f;
            inspectorPanelState.TransformEnabled = form.TransformField != null;
            inspectorPanelState.Transform = form.TransformField?.value ?? string.Empty;
            inspectorPanelState.TranslateX = form.TranslateXField?.value ?? 0f;
            inspectorPanelState.TranslateY = form.TranslateYField?.value ?? 0f;
            inspectorPanelState.Rotate = form.RotateField?.value ?? 0f;
            inspectorPanelState.ScaleX = form.ScaleXField?.value ?? 1f;
            inspectorPanelState.ScaleY = form.ScaleYField?.value ?? 1f;
        }

        public static void ApplyState(InspectorFormControls form, InspectorPanelState inspectorPanelState)
        {
            if (form == null || inspectorPanelState == null)
                return;

            form.SetSelectedTargetLabel(inspectorPanelState.SelectedTargetLabel, notify: false);
            form.FillColorField?.SetValueWithoutNotify(inspectorPanelState.FillColor);
            form.StrokeColorField?.SetValueWithoutNotify(inspectorPanelState.StrokeColor);
            form.StrokeWidthField?.SetValueWithoutNotify(inspectorPanelState.StrokeWidth);
            form.OpacitySlider?.SetValueWithoutNotify(inspectorPanelState.Opacity);
            SetPopupValue(form.LinecapPopup, inspectorPanelState.StrokeLinecap);
            SetPopupValue(form.LinejoinPopup, inspectorPanelState.StrokeLinejoin);
            form.DashLengthField?.SetValueWithoutNotify(inspectorPanelState.DashLength);
            form.DashGapField?.SetValueWithoutNotify(inspectorPanelState.DashGap);
            form.TransformField?.SetValueWithoutNotify(inspectorPanelState.Transform);
            form.TranslateXField?.SetValueWithoutNotify(inspectorPanelState.TranslateX);
            form.TranslateYField?.SetValueWithoutNotify(inspectorPanelState.TranslateY);
            form.RotateField?.SetValueWithoutNotify(inspectorPanelState.Rotate);
            form.ScaleXField?.SetValueWithoutNotify(inspectorPanelState.ScaleX);
            form.ScaleYField?.SetValueWithoutNotify(inspectorPanelState.ScaleY);
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
