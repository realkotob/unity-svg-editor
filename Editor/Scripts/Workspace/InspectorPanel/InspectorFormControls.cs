using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorFormControls
    {
        private const string NumericDisplayFormat = "0.##";

        public ColorField FillColorField { get; private set; }
        public ColorField StrokeColorField { get; private set; }
        public FloatField StrokeWidthField { get; private set; }
        public Slider OpacityField { get; private set; }
        public FloatField CornerRadiusField { get; private set; }
        public PopupField<string> LinecapPopup { get; private set; }
        public PopupField<string> LinejoinPopup { get; private set; }
        public FloatField DashLengthField { get; private set; }
        public FloatField DashGapField { get; private set; }
        public TextField TransformField { get; private set; }
        public FloatField FrameXField { get; private set; }
        public FloatField FrameYField { get; private set; }
        public FloatField FrameWidthField { get; private set; }
        public FloatField FrameHeightField { get; private set; }
        public FloatField TranslateXField { get; private set; }
        public FloatField TranslateYField { get; private set; }
        public FloatField RotateField { get; private set; }
        public FloatField ScaleXField { get; private set; }
        public FloatField ScaleYField { get; private set; }
        public Button PositionAlignLeftButton { get; private set; }
        public Button PositionAlignCenterButton { get; private set; }
        public Button PositionAlignRightButton { get; private set; }
        public Button PositionAlignTopButton { get; private set; }
        public Button PositionAlignMiddleButton { get; private set; }
        public Button PositionAlignBottomButton { get; private set; }
        public Button PositionRotateResetButton { get; private set; }
        public Button PositionFlipHorizontalButton { get; private set; }
        public Button PositionFlipVerticalButton { get; private set; }
        public Button ReadButton { get; private set; }
        public Button BuildTransformButton { get; private set; }
        public Button ApplyButton { get; private set; }

        public bool IsBound =>
            FillColorField != null ||
            StrokeColorField != null ||
            OpacityField != null ||
            TransformField != null;

        public bool FillEnabled => FillColorField != null;
        public bool StrokeEnabled => StrokeColorField != null;
        public bool StrokeWidthEnabled => StrokeWidthField != null;
        public bool OpacityEnabled => OpacityField != null;
        public bool DasharrayEnabled => DashLengthField != null && DashGapField != null;
        public bool TransformEnabled => TransformField != null;

        public IEnumerable<Toggle> InteractivityToggles => Enumerable.Empty<Toggle>();

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            FillColorField = root.Q<ColorField>("inspector-fill-color");
            StrokeColorField = root.Q<ColorField>("inspector-stroke-color");
            StrokeWidthField = root.Q<FloatField>("inspector-stroke-width");
            OpacityField = root.Q<Slider>("inspector-opacity");
            CornerRadiusField = root.Q<FloatField>("inspector-corner-radius");
            LinecapPopup = root.Q<DropdownField>("inspector-linecap");
            LinejoinPopup = root.Q<DropdownField>("inspector-linejoin");
            DashLengthField = root.Q<FloatField>("inspector-dash-length");
            DashGapField = root.Q<FloatField>("inspector-dash-gap");
            TransformField = root.Q<TextField>("inspector-transform");
            FrameXField = root.Q<FloatField>("inspector-frame-x");
            FrameYField = root.Q<FloatField>("inspector-frame-y");
            FrameWidthField = root.Q<FloatField>("inspector-frame-width");
            FrameHeightField = root.Q<FloatField>("inspector-frame-height");
            TranslateXField = root.Q<FloatField>("inspector-translate-x");
            TranslateYField = root.Q<FloatField>("inspector-translate-y");
            RotateField = root.Q<FloatField>("inspector-rotate");
            ScaleXField = root.Q<FloatField>("inspector-scale-x");
            ScaleYField = root.Q<FloatField>("inspector-scale-y");
            PositionAlignLeftButton = root.Q<Button>("position-align-left");
            PositionAlignCenterButton = root.Q<Button>("position-align-center");
            PositionAlignRightButton = root.Q<Button>("position-align-right");
            PositionAlignTopButton = root.Q<Button>("position-align-top");
            PositionAlignMiddleButton = root.Q<Button>("position-align-middle");
            PositionAlignBottomButton = root.Q<Button>("position-align-bottom");
            PositionRotateResetButton = root.Q<Button>("position-rotate-reset");
            PositionFlipHorizontalButton = root.Q<Button>("position-flip-horizontal");
            PositionFlipVerticalButton = root.Q<Button>("position-flip-vertical");
            ReadButton = root.Q<Button>("inspector-read-target");
            BuildTransformButton = root.Q<Button>("inspector-build-transform");
            ApplyButton = root.Q<Button>("inspector-apply");

            ConfigureStrokePopup(LinecapPopup, new List<string> { string.Empty, "butt", "round", "square" });
            ConfigureStrokePopup(LinejoinPopup, new List<string> { string.Empty, "miter", "round", "bevel" });
            ConfigureNumericFieldFormat(StrokeWidthField);
            ConfigureNumericFieldFormat(CornerRadiusField);
            ConfigureNumericFieldFormat(DashLengthField);
            ConfigureNumericFieldFormat(DashGapField);
            ConfigureNumericFieldFormat(FrameXField);
            ConfigureNumericFieldFormat(FrameYField);
            ConfigureNumericFieldFormat(FrameWidthField);
            ConfigureNumericFieldFormat(FrameHeightField);
            ConfigureNumericFieldFormat(TranslateXField);
            ConfigureNumericFieldFormat(TranslateYField);
            ConfigureNumericFieldFormat(RotateField);
            ConfigureNumericFieldFormat(ScaleXField);
            ConfigureNumericFieldFormat(ScaleYField);

        }

        public void Unbind()
        {
            FillColorField = null;
            StrokeColorField = null;
            StrokeWidthField = null;
            OpacityField = null;
            CornerRadiusField = null;
            LinecapPopup = null;
            LinejoinPopup = null;
            DashLengthField = null;
            DashGapField = null;
            TransformField = null;
            FrameXField = null;
            FrameYField = null;
            FrameWidthField = null;
            FrameHeightField = null;
            TranslateXField = null;
            TranslateYField = null;
            RotateField = null;
            ScaleXField = null;
            ScaleYField = null;
            PositionAlignLeftButton = null;
            PositionAlignCenterButton = null;
            PositionAlignRightButton = null;
            PositionAlignTopButton = null;
            PositionAlignMiddleButton = null;
            PositionAlignBottomButton = null;
            PositionRotateResetButton = null;
            PositionFlipHorizontalButton = null;
            PositionFlipVerticalButton = null;
            ReadButton = null;
            BuildTransformButton = null;
            ApplyButton = null;
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

        private static void ConfigureNumericFieldFormat(FloatField field)
        {
            if (field == null)
                return;

            field.formatString = NumericDisplayFormat;
        }
    }
}
