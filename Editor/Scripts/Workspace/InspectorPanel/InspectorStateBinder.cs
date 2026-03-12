using UnityEngine;
using UnityEngine.UIElements;
using SelectElement = Core.UI.Foundation.Components.Select.Select;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal static class InspectorStateBinder
    {
        private const string RemovePlaceholder = "none";
        private const string LinecapActualDefaultValue = "butt";
        private const string LinejoinActualDefaultValue = "miter";

        public static void CaptureState(InspectorFormControls form, InspectorPanelState inspectorPanelState)
        {
            if (form == null || inspectorPanelState == null)
                return;

            inspectorPanelState.FillEnabled = form.FillEnabled;
            inspectorPanelState.FillColor = form.FillColorValue;
            inspectorPanelState.StrokeEnabled = form.StrokeEnabled;
            inspectorPanelState.StrokeColor = form.StrokeColorValue;
            inspectorPanelState.StrokeWidthEnabled = form.StrokeEnabled && form.StrokeWidthField != null;
            inspectorPanelState.StrokeWidth = form.StrokeWidthField?.value ?? 1f;
            inspectorPanelState.OpacityEnabled = form.OpacityControl != null;
            float opacityValue = form.OpacityValue;
            inspectorPanelState.Opacity = form.IsOpacitySlider
                ? Mathf.Clamp01(opacityValue)
                : Mathf.Clamp01(opacityValue / 100f);
            inspectorPanelState.CornerRadius = Mathf.Max(0f, form.CornerRadiusField?.value ?? 0f);
            inspectorPanelState.StrokeLinecap = NormalizeLinecapValue(form.LinecapPopup?.Value ?? form.LinecapLegacyPopup?.value);
            inspectorPanelState.StrokeLinejoin = NormalizeLinejoinValue(form.LinejoinPopup?.Value ?? form.LinejoinLegacyPopup?.value);
            inspectorPanelState.DasharrayEnabled = form.StrokeEnabled && (form.DashLengthField != null || form.DashGapField != null);
            inspectorPanelState.DashLength = form.DashLengthField?.value ?? 4f;
            inspectorPanelState.DashGap = form.DashGapField?.value ?? 2f;
            inspectorPanelState.TransformEnabled = form.TransformField != null;
            inspectorPanelState.Transform = form.TransformField?.value ?? string.Empty;
            inspectorPanelState.FrameX = form.FrameXField?.value ?? 0f;
            inspectorPanelState.FrameY = form.FrameYField?.value ?? 0f;
            inspectorPanelState.FrameWidth = form.FrameWidthField?.value ?? 0f;
            inspectorPanelState.FrameHeight = form.FrameHeightField?.value ?? 0f;
            if (form.TranslateXField != null)
                inspectorPanelState.TranslateX = form.TranslateXField.value;
            if (form.TranslateYField != null)
                inspectorPanelState.TranslateY = form.TranslateYField.value;
            if (form.RotateField != null)
                inspectorPanelState.Rotate = form.RotateField.value;
            if (form.ScaleXField != null)
                inspectorPanelState.ScaleX = form.ScaleXField.value;
            if (form.ScaleYField != null)
                inspectorPanelState.ScaleY = form.ScaleYField.value;
        }

        public static void ApplyState(InspectorFormControls form, InspectorPanelState inspectorPanelState)
        {
            if (form == null || inspectorPanelState == null)
                return;

            form.SetFillVisible(inspectorPanelState.FillEnabled);
            form.SetStrokeVisible(inspectorPanelState.StrokeEnabled);
            form.SetFillColorWithoutNotify(inspectorPanelState.FillColor);
            form.SetStrokeColorWithoutNotify(inspectorPanelState.StrokeColor);
            form.StrokeWidthField?.SetValueWithoutNotify(inspectorPanelState.StrokeWidth);
            form.OpacityField?.SetValueWithoutNotify(form.IsOpacitySlider
                ? inspectorPanelState.Opacity
                : inspectorPanelState.Opacity * 100f);
            form.CornerRadiusField?.SetValueWithoutNotify(inspectorPanelState.CornerRadius);
            SetPopupValue(form.LinecapPopup, string.IsNullOrEmpty(inspectorPanelState.StrokeLinecap) ? LinecapActualDefaultValue : inspectorPanelState.StrokeLinecap);
            SetPopupValue(form.LinecapLegacyPopup, string.IsNullOrEmpty(inspectorPanelState.StrokeLinecap) ? LinecapActualDefaultValue : inspectorPanelState.StrokeLinecap);
            SetPopupValue(form.LinejoinPopup, string.IsNullOrEmpty(inspectorPanelState.StrokeLinejoin) ? LinejoinActualDefaultValue : inspectorPanelState.StrokeLinejoin);
            SetPopupValue(form.LinejoinLegacyPopup, string.IsNullOrEmpty(inspectorPanelState.StrokeLinejoin) ? LinejoinActualDefaultValue : inspectorPanelState.StrokeLinejoin);
            form.DashLengthField?.SetValueWithoutNotify(inspectorPanelState.DashLength);
            form.DashGapField?.SetValueWithoutNotify(inspectorPanelState.DashGap);
            form.TransformField?.SetValueWithoutNotify(inspectorPanelState.Transform);
            form.FrameXField?.SetValueWithoutNotify(inspectorPanelState.FrameX);
            form.FrameYField?.SetValueWithoutNotify(inspectorPanelState.FrameY);
            form.FrameWidthField?.SetValueWithoutNotify(inspectorPanelState.FrameWidth);
            form.FrameHeightField?.SetValueWithoutNotify(inspectorPanelState.FrameHeight);
            form.TranslateXField?.SetValueWithoutNotify(inspectorPanelState.TranslateX);
            form.TranslateYField?.SetValueWithoutNotify(inspectorPanelState.TranslateY);
            form.RotateField?.SetValueWithoutNotify(inspectorPanelState.Rotate);
            form.ScaleXField?.SetValueWithoutNotify(inspectorPanelState.ScaleX);
            form.ScaleYField?.SetValueWithoutNotify(inspectorPanelState.ScaleY);
        }

        private static void SetPopupValue(SelectElement popup, string value)
        {
            if (popup == null)
                return;

            string[] choices = string.IsNullOrWhiteSpace(popup.Choices)
                ? System.Array.Empty<string>()
                : popup.Choices.Split(',');

            int index = System.Array.IndexOf(choices, value ?? string.Empty);
            popup.Index = index;
        }

        private static void SetPopupValue(DropdownField popup, string value)
        {
            if (popup == null)
                return;

            popup.SetValueWithoutNotify(string.IsNullOrEmpty(value) ? RemovePlaceholder : value);
        }

        private static string NormalizePopupValue(string value)
        {
            return string.Equals(value, RemovePlaceholder, System.StringComparison.Ordinal)
                ? string.Empty
                : value ?? string.Empty;
        }

        private static string NormalizeLinecapValue(string value)
        {
            return string.Equals(value, LinecapActualDefaultValue, System.StringComparison.Ordinal)
                ? string.Empty
                : NormalizePopupValue(value);
        }

        private static string NormalizeLinejoinValue(string value)
        {
            return string.Equals(value, LinejoinActualDefaultValue, System.StringComparison.Ordinal)
                ? string.Empty
                : NormalizePopupValue(value);
        }
    }
}
