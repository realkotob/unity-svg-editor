using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorFormControls
    {
        public PopupField<string> TargetPopup { get; private set; }
        public ColorField FillColorField { get; private set; }
        public ColorField StrokeColorField { get; private set; }
        public FloatField StrokeWidthField { get; private set; }
        public Slider OpacitySlider { get; private set; }
        public PopupField<string> LinecapPopup { get; private set; }
        public PopupField<string> LinejoinPopup { get; private set; }
        public FloatField DashLengthField { get; private set; }
        public FloatField DashGapField { get; private set; }
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
            FillColorField != null ||
            StrokeColorField != null ||
            OpacitySlider != null ||
            TransformField != null;

        public string SelectedTargetLabel => TargetPopup?.value ?? string.Empty;
        public bool FillEnabled => FillColorField != null;
        public bool StrokeEnabled => StrokeColorField != null;
        public bool StrokeWidthEnabled => StrokeWidthField != null;
        public bool OpacityEnabled => OpacitySlider != null;
        public bool DasharrayEnabled => DashLengthField != null && DashGapField != null;
        public bool TransformEnabled => TransformField != null;

        public IEnumerable<Toggle> InteractivityToggles => Enumerable.Empty<Toggle>();

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            TargetPopup = root.Q<DropdownField>("patch-target");
            FillColorField = root.Q<ColorField>("inspector-fill-color");
            StrokeColorField = root.Q<ColorField>("inspector-stroke-color");
            StrokeWidthField = root.Q<FloatField>("inspector-stroke-width");
            OpacitySlider = root.Q<Slider>("inspector-opacity");
            LinecapPopup = root.Q<DropdownField>("inspector-linecap");
            LinejoinPopup = root.Q<DropdownField>("inspector-linejoin");
            DashLengthField = root.Q<FloatField>("inspector-dash-length");
            DashGapField = root.Q<FloatField>("inspector-dash-gap");
            TransformField = root.Q<TextField>("inspector-transform");
            TranslateXField = root.Q<FloatField>("inspector-translate-x");
            TranslateYField = root.Q<FloatField>("inspector-translate-y");
            RotateField = root.Q<FloatField>("inspector-rotate");
            ScaleXField = root.Q<FloatField>("inspector-scale-x");
            ScaleYField = root.Q<FloatField>("inspector-scale-y");
            ReadButton = root.Q<Button>("inspector-read-target");
            BuildTransformButton = root.Q<Button>("inspector-build-transform");
            ApplyButton = root.Q<Button>("inspector-apply");

            ConfigureStrokePopup(LinecapPopup, new List<string> { string.Empty, "butt", "round", "square" });
            ConfigureStrokePopup(LinejoinPopup, new List<string> { string.Empty, "miter", "round", "bevel" });
        }

        public void Unbind()
        {
            TargetPopup = null;
            FillColorField = null;
            StrokeColorField = null;
            StrokeWidthField = null;
            OpacitySlider = null;
            LinecapPopup = null;
            LinejoinPopup = null;
            DashLengthField = null;
            DashGapField = null;
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
