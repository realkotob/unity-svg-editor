using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class PatchInspectorFormControls
    {
        public PopupField<string> TargetPopup { get; private set; }
        public Toggle FillEnabledToggle { get; private set; }
        public Toggle StrokeEnabledToggle { get; private set; }
        public ColorField FillColorField { get; private set; }
        public ColorField StrokeColorField { get; private set; }
        public Toggle StrokeWidthEnabledToggle { get; private set; }
        public FloatField StrokeWidthField { get; private set; }
        public Toggle OpacityEnabledToggle { get; private set; }
        public Slider OpacitySlider { get; private set; }
        public PopupField<string> LinecapPopup { get; private set; }
        public PopupField<string> LinejoinPopup { get; private set; }
        public Toggle DasharrayEnabledToggle { get; private set; }
        public FloatField DashLengthField { get; private set; }
        public FloatField DashGapField { get; private set; }
        public Toggle TransformEnabledToggle { get; private set; }
        public TextField TransformField { get; private set; }
        public FloatField TranslateXField { get; private set; }
        public FloatField TranslateYField { get; private set; }
        public FloatField RotateField { get; private set; }
        public FloatField ScaleXField { get; private set; }
        public FloatField ScaleYField { get; private set; }
        public Button ReadButton { get; private set; }
        public Button BuildTransformButton { get; private set; }
        public Button ApplyButton { get; private set; }

        public bool IsBound =>
            FillEnabledToggle != null ||
            StrokeEnabledToggle != null ||
            OpacityEnabledToggle != null ||
            TransformField != null;

        public string SelectedTargetLabel => TargetPopup?.value ?? string.Empty;
        public bool FillEnabled => FillEnabledToggle?.value ?? false;
        public bool StrokeEnabled => StrokeEnabledToggle?.value ?? false;
        public bool StrokeWidthEnabled => StrokeWidthEnabledToggle?.value ?? false;
        public bool OpacityEnabled => OpacityEnabledToggle?.value ?? false;
        public bool DasharrayEnabled => DasharrayEnabledToggle?.value ?? false;
        public bool TransformEnabled => TransformEnabledToggle?.value ?? false;

        public IEnumerable<Toggle> InteractivityToggles
        {
            get
            {
                yield return FillEnabledToggle;
                yield return StrokeEnabledToggle;
                yield return StrokeWidthEnabledToggle;
                yield return OpacityEnabledToggle;
                yield return DasharrayEnabledToggle;
                yield return TransformEnabledToggle;
            }
        }

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            TargetPopup = root.Q<DropdownField>("patch-target");
            FillEnabledToggle = root.Q<Toggle>("patch-fill-enabled");
            StrokeEnabledToggle = root.Q<Toggle>("patch-stroke-enabled");
            FillColorField = root.Q<ColorField>("patch-fill-color");
            StrokeColorField = root.Q<ColorField>("patch-stroke-color");
            StrokeWidthEnabledToggle = root.Q<Toggle>("patch-stroke-width-enabled");
            StrokeWidthField = root.Q<FloatField>("patch-stroke-width");
            OpacityEnabledToggle = root.Q<Toggle>("patch-opacity-enabled");
            OpacitySlider = root.Q<Slider>("patch-opacity");
            LinecapPopup = root.Q<DropdownField>("patch-linecap");
            LinejoinPopup = root.Q<DropdownField>("patch-linejoin");
            DasharrayEnabledToggle = root.Q<Toggle>("patch-dash-enabled");
            DashLengthField = root.Q<FloatField>("patch-dash-length");
            DashGapField = root.Q<FloatField>("patch-dash-gap");
            TransformEnabledToggle = root.Q<Toggle>("patch-transform-enabled");
            TransformField = root.Q<TextField>("patch-transform");
            TranslateXField = root.Q<FloatField>("patch-translate-x");
            TranslateYField = root.Q<FloatField>("patch-translate-y");
            RotateField = root.Q<FloatField>("patch-rotate");
            ScaleXField = root.Q<FloatField>("patch-scale-x");
            ScaleYField = root.Q<FloatField>("patch-scale-y");
            ReadButton = root.Q<Button>("patch-read-target");
            BuildTransformButton = root.Q<Button>("patch-build-transform");
            ApplyButton = root.Q<Button>("patch-apply");

            ConfigureStrokePopup(LinecapPopup, new List<string> { string.Empty, "butt", "round", "square" });
            ConfigureStrokePopup(LinejoinPopup, new List<string> { string.Empty, "miter", "round", "bevel" });
        }

        public void Unbind()
        {
            TargetPopup = null;
            FillEnabledToggle = null;
            StrokeEnabledToggle = null;
            FillColorField = null;
            StrokeColorField = null;
            StrokeWidthEnabledToggle = null;
            StrokeWidthField = null;
            OpacityEnabledToggle = null;
            OpacitySlider = null;
            LinecapPopup = null;
            LinejoinPopup = null;
            DasharrayEnabledToggle = null;
            DashLengthField = null;
            DashGapField = null;
            TransformEnabledToggle = null;
            TransformField = null;
            TranslateXField = null;
            TranslateYField = null;
            RotateField = null;
            ScaleXField = null;
            ScaleYField = null;
            ReadButton = null;
            BuildTransformButton = null;
            ApplyButton = null;
        }

        public void SetTargetChoices(IReadOnlyList<string> choices)
        {
            if (TargetPopup != null)
                TargetPopup.choices = choices?.ToList() ?? new List<string>();
        }

        public void SetSelectedTargetLabel(string label, bool notify)
        {
            if (TargetPopup == null)
                return;

            var value = label ?? string.Empty;
            if (notify)
                TargetPopup.value = value;
            else
                TargetPopup.SetValueWithoutNotify(value);
        }

        public void SetTransformText(string transform)
        {
            TransformField?.SetValueWithoutNotify(transform ?? string.Empty);
        }

        private static void ConfigureStrokePopup(PopupField<string> popup, List<string> choices)
        {
            if (popup == null)
                return;

            popup.choices = choices;
            popup.formatListItemCallback = FormatStrokePopupItem;
            popup.formatSelectedValueCallback = FormatStrokePopupItem;
        }

        private static string FormatStrokePopupItem(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(remove)" : value;
        }
    }
}
