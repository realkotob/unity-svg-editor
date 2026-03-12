using System.Collections.Generic;
using System.Linq;
using Core.UI.Foundation.Components.ColorPercentField;
using SelectElement = Core.UI.Foundation.Components.Select.Select;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorFormControls
    {
        private const string NumericDisplayFormat = "0.##";
        private const string LinecapActualDefaultValue = "butt";
        private const string LinecapDisplayDefaultValue = "none";
        private const string LinejoinActualDefaultValue = "miter";
        private const string LinejoinDisplayDefaultValue = "none";
        private VisualElement _root;

        public ColorPercentField FillColorField { get; private set; }
        public ColorField FillColorLegacyField { get; private set; }
        public ColorPercentField StrokeColorField { get; private set; }
        public ColorField StrokeColorLegacyField { get; private set; }
        public FloatField StrokeWidthField { get; private set; }
        public BaseField<float> OpacityField { get; private set; }
        public FloatField CornerRadiusField { get; private set; }
        public SelectElement LinecapPopup { get; private set; }
        public DropdownField LinecapLegacyPopup { get; private set; }
        public SelectElement LinejoinPopup { get; private set; }
        public DropdownField LinejoinLegacyPopup { get; private set; }
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
        public Button PositionRotateClockwise90Button { get; private set; }
        public Button PositionFlipHorizontalButton { get; private set; }
        public Button PositionFlipVerticalButton { get; private set; }

        public VisualElement FillColorControl => ResolveColorControl("inspector-fill-color", FillColorField, FillColorLegacyField);
        public VisualElement StrokeColorControl => ResolveColorControl("inspector-stroke-color", StrokeColorField, StrokeColorLegacyField);
        public VisualElement OpacityControl => ResolveOpacityField();
        public Color FillColorValue => ResolveColorValue("inspector-fill-color", FillColorField, FillColorLegacyField, Color.black);
        public Color StrokeColorValue => ResolveColorValue("inspector-stroke-color", StrokeColorField, StrokeColorLegacyField, Color.black);
        public float OpacityValue => ResolveOpacityField()?.value ?? 1f;
        public bool IsOpacitySlider => ResolveOpacityField() is Slider;

        public bool IsBound =>
            FillColorControl != null ||
            StrokeColorControl != null ||
            OpacityField != null ||
            TransformField != null ||
            FrameXField != null;

        public bool FillEnabled => FillColorControl != null;
        public bool StrokeEnabled => StrokeColorControl != null;
        public bool StrokeWidthEnabled => StrokeWidthField != null;
        public bool OpacityEnabled => OpacityField != null;
        public bool DasharrayEnabled => DashLengthField != null && DashGapField != null;

        public IEnumerable<Toggle> InteractivityToggles => Enumerable.Empty<Toggle>();

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            _root = root;
            VisualElement fillColorElement = root.Q<VisualElement>("inspector-fill-color");
            FillColorField = fillColorElement as ColorPercentField;
            FillColorLegacyField = fillColorElement as ColorField;
            VisualElement strokeColorElement = root.Q<VisualElement>("inspector-stroke-color");
            StrokeColorField = strokeColorElement as ColorPercentField;
            StrokeColorLegacyField = strokeColorElement as ColorField;
            StrokeWidthField = root.Q<FloatField>("inspector-stroke-width");
            VisualElement opacityElement = root.Q<VisualElement>("inspector-opacity");
            OpacityField = opacityElement as BaseField<float>;
            CornerRadiusField = root.Q<FloatField>("inspector-corner-radius");
            VisualElement linecapElement = root.Q<VisualElement>("inspector-linecap");
            LinecapPopup = linecapElement as SelectElement;
            LinecapLegacyPopup = linecapElement as DropdownField;
            ConfigureLinecapDropdown(LinecapLegacyPopup);
            VisualElement linejoinElement = root.Q<VisualElement>("inspector-linejoin");
            LinejoinPopup = linejoinElement as SelectElement;
            LinejoinLegacyPopup = linejoinElement as DropdownField;
            ConfigureLinejoinDropdown(LinejoinLegacyPopup);
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
            PositionRotateClockwise90Button = root.Q<Button>("position-rotate-clockwise-90");
            PositionFlipHorizontalButton = root.Q<Button>("position-flip-horizontal");
            PositionFlipVerticalButton = root.Q<Button>("position-flip-vertical");

            ConfigureNumericFieldFormat(StrokeWidthField);
            ConfigureNumericFieldFormat(OpacityField as FloatField);
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
            FillColorLegacyField = null;
            StrokeColorField = null;
            StrokeColorLegacyField = null;
            _root = null;
            StrokeWidthField = null;
            OpacityField = null;
            CornerRadiusField = null;
            LinecapPopup = null;
            LinecapLegacyPopup = null;
            LinejoinPopup = null;
            LinejoinLegacyPopup = null;
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
            PositionRotateClockwise90Button = null;
            PositionFlipHorizontalButton = null;
            PositionFlipVerticalButton = null;
        }

        public void SetFillColorWithoutNotify(Color color)
        {
            if (FillColorField != null)
            {
                FillColorField.SetValueWithoutNotify(color);
                return;
            }

            FillColorLegacyField?.SetValueWithoutNotify(color);
        }

        public void SetStrokeColorWithoutNotify(Color color)
        {
            if (StrokeColorField != null)
            {
                StrokeColorField.SetValueWithoutNotify(color);
                return;
            }

            StrokeColorLegacyField?.SetValueWithoutNotify(color);
        }

        public void SetTransformText(string transform)
        {
            TransformField?.SetValueWithoutNotify(transform ?? string.Empty);
        }

        private static void ConfigureNumericFieldFormat(FloatField field)
        {
            if (field == null)
                return;

            field.formatString = NumericDisplayFormat;
        }

        private static void ConfigureLinecapDropdown(DropdownField field)
        {
            if (field == null)
                return;

            field.formatListItemCallback = FormatLinecapValue;
            field.formatSelectedValueCallback = FormatLinecapValue;
        }

        private static string FormatLinecapValue(string value)
        {
            return string.Equals(value, LinecapActualDefaultValue, System.StringComparison.Ordinal)
                ? LinecapDisplayDefaultValue
                : value ?? string.Empty;
        }

        private static void ConfigureLinejoinDropdown(DropdownField field)
        {
            if (field == null)
                return;

            field.formatListItemCallback = FormatLinejoinValue;
            field.formatSelectedValueCallback = FormatLinejoinValue;
        }

        private static string FormatLinejoinValue(string value)
        {
            return string.Equals(value, LinejoinActualDefaultValue, System.StringComparison.Ordinal)
                ? LinejoinDisplayDefaultValue
                : value ?? string.Empty;
        }

        private VisualElement ResolveColorControl(string name, ColorPercentField colorPercentField, ColorField colorField)
        {
            return (VisualElement)colorPercentField ?? colorField ?? _root?.Q<VisualElement>(name);
        }

        private Color ResolveColorValue(string name, ColorPercentField colorPercentField, ColorField colorField, Color fallback)
        {
            if (colorPercentField != null)
            {
                return colorPercentField.ColorValue;
            }

            if (colorField != null)
            {
                return colorField.value;
            }

            VisualElement element = _root?.Q<VisualElement>(name);
            if (element is ColorPercentField queriedColorPercentField)
            {
                return queriedColorPercentField.ColorValue;
            }

            if (element is ColorField queriedColorField)
            {
                return queriedColorField.value;
            }

            return fallback;
        }

        private BaseField<float> ResolveOpacityField()
        {
            return OpacityField ?? _root?.Q<VisualElement>("inspector-opacity") as BaseField<float>;
        }
    }
}
