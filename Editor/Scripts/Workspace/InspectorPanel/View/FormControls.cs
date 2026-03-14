using System.Collections.Generic;
using System.Linq;
using Core.UI.Foundation.Components.ColorPercentField;
using SelectElement = Core.UI.Foundation.Components.Select.Select;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SvgEditor;
using SvgEditor.Shared;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class FormControls
    {
        private const string NumericDisplayFormat = "0.##";
        private const string LinecapActualDefaultValue = "butt";
        private const string LinecapDisplayDefaultValue = "none";
        private const string LinejoinActualDefaultValue = "miter";
        private const string LinejoinDisplayDefaultValue = "none";

        internal static class ElementName
        {
            public const string FILL_COLOR_ROW = "fill-color-row";
            public const string FILL_ADD_BUTTON = "fill-add-button";
            public const string FILL_REMOVE_BUTTON = "fill-remove-button";
            public const string FILL_COLOR = "inspector-fill-color";
            public const string STROKE_COLOR_ROW = "stroke-color-row";
            public const string STROKE_METRICS_ROW = "stroke-metrics-row";
            public const string STROKE_LINE_STYLE_ROW = "stroke-line-style-row";
            public const string STROKE_ADD_BUTTON = "stroke-add-button";
            public const string STROKE_REMOVE_BUTTON = "stroke-remove-button";
            public const string STROKE_COLOR = "inspector-stroke-color";
            public const string STROKE_WIDTH = "inspector-stroke-width";
            public const string OPACITY = "inspector-opacity";
            public const string CORNER_RADIUS = "inspector-corner-radius";
            public const string LINECAP = "inspector-linecap";
            public const string LINEJOIN = "inspector-linejoin";
            public const string DASH_LENGTH = "inspector-dash-length";
            public const string DASH_GAP = "inspector-dash-gap";
            public const string TRANSFORM = "inspector-transform";
            public const string FRAME_X = "inspector-frame-x";
            public const string FRAME_Y = "inspector-frame-y";
            public const string FRAME_WIDTH = "inspector-frame-width";
            public const string FRAME_HEIGHT = "inspector-frame-height";
            public const string TRANSLATE_X = "inspector-translate-x";
            public const string TRANSLATE_Y = "inspector-translate-y";
            public const string ROTATE = "inspector-rotate";
            public const string SCALE_X = "inspector-scale-x";
            public const string SCALE_Y = "inspector-scale-y";
            public const string POSITION_ALIGN_LEFT = "position-align-left";
            public const string POSITION_ALIGN_CENTER = "position-align-center";
            public const string POSITION_ALIGN_RIGHT = "position-align-right";
            public const string POSITION_ALIGN_TOP = "position-align-top";
            public const string POSITION_ALIGN_MIDDLE = "position-align-middle";
            public const string POSITION_ALIGN_BOTTOM = "position-align-bottom";
            public const string POSITION_ROTATE_CLOCKWISE_90 = "position-rotate-clockwise-90";
            public const string POSITION_FLIP_HORIZONTAL = "position-flip-horizontal";
            public const string POSITION_FLIP_VERTICAL = "position-flip-vertical";
        }

        private bool _isFillVisible = true;
        private bool _isStrokeVisible = true;
        private VisualElement _fillColorControl;
        private VisualElement _strokeColorControl;
        private BaseField<float> _opacityControl;
        private VisualElement _linecapControl;
        private VisualElement _linejoinControl;

        public VisualElement FillColorRow { get; private set; }
        public Button FillAddButton { get; private set; }
        public Button FillRemoveButton { get; private set; }
        public ColorPercentField FillColorField { get; private set; }
        public ColorField FillColorLegacyField { get; private set; }
        public VisualElement StrokeColorRow { get; private set; }
        public VisualElement StrokeMetricsRow { get; private set; }
        public VisualElement StrokeLineStyleRow { get; private set; }
        public Button StrokeAddButton { get; private set; }
        public Button StrokeRemoveButton { get; private set; }
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

        public VisualElement FillColorControl => _fillColorControl;
        public VisualElement StrokeColorControl => _strokeColorControl;
        public BaseField<float> OpacityControl => _opacityControl;
        public VisualElement LinecapControl => _linecapControl;
        public VisualElement LinejoinControl => _linejoinControl;
        public string LinecapValue => LinecapPopup?.Value ?? LinecapLegacyPopup?.value ?? string.Empty;
        public string LinejoinValue => LinejoinPopup?.Value ?? LinejoinLegacyPopup?.value ?? string.Empty;
        public Color FillColorValue => ResolveColorValue(_fillColorControl, FillColorField, FillColorLegacyField, Color.black);
        public Color StrokeColorValue => ResolveColorValue(_strokeColorControl, StrokeColorField, StrokeColorLegacyField, Color.black);
        public float OpacityValue => _opacityControl?.value ?? 1f;
        public bool IsOpacitySlider => _opacityControl is Slider;

        public bool IsBound =>
            FillColorControl != null ||
            StrokeColorControl != null ||
            OpacityField != null ||
            TransformField != null ||
            FrameXField != null;

        public bool FillEnabled => FillColorControl != null && _isFillVisible;
        public bool StrokeEnabled => StrokeColorControl != null && _isStrokeVisible;
        public bool StrokeWidthEnabled => StrokeWidthField != null;
        public bool OpacityEnabled => OpacityField != null;
        public bool DasharrayEnabled => DashLengthField != null && DashGapField != null;

        public IEnumerable<Toggle> InteractivityToggles => Enumerable.Empty<Toggle>();

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            FillColorRow = root.Q<VisualElement>(ElementName.FILL_COLOR_ROW);
            FillAddButton = root.Q<Button>(ElementName.FILL_ADD_BUTTON);
            FillRemoveButton = root.Q<Button>(ElementName.FILL_REMOVE_BUTTON);
            _fillColorControl = root.Q<VisualElement>(ElementName.FILL_COLOR);
            FillColorField = _fillColorControl as ColorPercentField;
            FillColorLegacyField = _fillColorControl as ColorField;
            StrokeColorRow = root.Q<VisualElement>(ElementName.STROKE_COLOR_ROW);
            StrokeMetricsRow = root.Q<VisualElement>(ElementName.STROKE_METRICS_ROW);
            StrokeLineStyleRow = root.Q<VisualElement>(ElementName.STROKE_LINE_STYLE_ROW);
            StrokeAddButton = root.Q<Button>(ElementName.STROKE_ADD_BUTTON);
            StrokeRemoveButton = root.Q<Button>(ElementName.STROKE_REMOVE_BUTTON);
            _strokeColorControl = root.Q<VisualElement>(ElementName.STROKE_COLOR);
            StrokeColorField = _strokeColorControl as ColorPercentField;
            StrokeColorLegacyField = _strokeColorControl as ColorField;
            StrokeWidthField = root.Q<FloatField>(ElementName.STROKE_WIDTH);
            _opacityControl = root.Q<VisualElement>(ElementName.OPACITY) as BaseField<float>;
            OpacityField = _opacityControl;
            CornerRadiusField = root.Q<FloatField>(ElementName.CORNER_RADIUS);
            _linecapControl = root.Q<VisualElement>(ElementName.LINECAP);
            LinecapPopup = _linecapControl as SelectElement;
            LinecapLegacyPopup = _linecapControl as DropdownField;
            ConfigureLinecapDropdown(LinecapLegacyPopup);
            _linejoinControl = root.Q<VisualElement>(ElementName.LINEJOIN);
            LinejoinPopup = _linejoinControl as SelectElement;
            LinejoinLegacyPopup = _linejoinControl as DropdownField;
            ConfigureLinejoinDropdown(LinejoinLegacyPopup);
            DashLengthField = root.Q<FloatField>(ElementName.DASH_LENGTH);
            DashGapField = root.Q<FloatField>(ElementName.DASH_GAP);
            TransformField = root.Q<TextField>(ElementName.TRANSFORM);
            FrameXField = root.Q<FloatField>(ElementName.FRAME_X);
            FrameYField = root.Q<FloatField>(ElementName.FRAME_Y);
            FrameWidthField = root.Q<FloatField>(ElementName.FRAME_WIDTH);
            FrameHeightField = root.Q<FloatField>(ElementName.FRAME_HEIGHT);
            TranslateXField = root.Q<FloatField>(ElementName.TRANSLATE_X);
            TranslateYField = root.Q<FloatField>(ElementName.TRANSLATE_Y);
            RotateField = root.Q<FloatField>(ElementName.ROTATE);
            ScaleXField = root.Q<FloatField>(ElementName.SCALE_X);
            ScaleYField = root.Q<FloatField>(ElementName.SCALE_Y);
            PositionAlignLeftButton = root.Q<Button>(ElementName.POSITION_ALIGN_LEFT);
            PositionAlignCenterButton = root.Q<Button>(ElementName.POSITION_ALIGN_CENTER);
            PositionAlignRightButton = root.Q<Button>(ElementName.POSITION_ALIGN_RIGHT);
            PositionAlignTopButton = root.Q<Button>(ElementName.POSITION_ALIGN_TOP);
            PositionAlignMiddleButton = root.Q<Button>(ElementName.POSITION_ALIGN_MIDDLE);
            PositionAlignBottomButton = root.Q<Button>(ElementName.POSITION_ALIGN_BOTTOM);
            PositionRotateClockwise90Button = root.Q<Button>(ElementName.POSITION_ROTATE_CLOCKWISE_90);
            PositionFlipHorizontalButton = root.Q<Button>(ElementName.POSITION_FLIP_HORIZONTAL);
            PositionFlipVerticalButton = root.Q<Button>(ElementName.POSITION_FLIP_VERTICAL);

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
            SetFillVisible(FillColorRow != null && FillColorRow.resolvedStyle.display != DisplayStyle.None);
            SetStrokeVisible(StrokeColorRow != null && StrokeColorRow.resolvedStyle.display != DisplayStyle.None);
        }

        public void Unbind()
        {
            FillColorRow = null;
            FillAddButton = null;
            FillRemoveButton = null;
            _fillColorControl = null;
            FillColorField = null;
            FillColorLegacyField = null;
            StrokeColorRow = null;
            StrokeMetricsRow = null;
            StrokeLineStyleRow = null;
            StrokeAddButton = null;
            StrokeRemoveButton = null;
            _strokeColorControl = null;
            StrokeColorField = null;
            StrokeColorLegacyField = null;
            _isFillVisible = true;
            _isStrokeVisible = true;
            StrokeWidthField = null;
            _opacityControl = null;
            OpacityField = null;
            CornerRadiusField = null;
            _linecapControl = null;
            LinecapPopup = null;
            LinecapLegacyPopup = null;
            _linejoinControl = null;
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

        public void SetLinecapWithoutNotify(string value)
        {
            SetPopupValue(LinecapPopup, value);
            SetPopupValue(LinecapLegacyPopup, value);
        }

        public void SetLinejoinWithoutNotify(string value)
        {
            SetPopupValue(LinejoinPopup, value);
            SetPopupValue(LinejoinLegacyPopup, value);
        }

        public void ToggleFillColorChanged(EventCallback<ChangeEvent<Color>> callback, bool register)
        {
            ToggleColorChanged(FillColorField, FillColorLegacyField, callback, register);
        }

        public void ToggleStrokeColorChanged(EventCallback<ChangeEvent<Color>> callback, bool register)
        {
            ToggleColorChanged(StrokeColorField, StrokeColorLegacyField, callback, register);
        }

        public void ToggleOpacityChanged(EventCallback<ChangeEvent<float>> callback, bool register)
        {
            ToggleValueChangedCallback(OpacityField, callback, register);
        }

        public void ToggleLinecapChanged(EventCallback<ChangeEvent<string>> callback, bool register)
        {
            TogglePopupChanged(LinecapPopup, LinecapLegacyPopup, callback, register);
        }

        public void ToggleLinejoinChanged(EventCallback<ChangeEvent<string>> callback, bool register)
        {
            TogglePopupChanged(LinejoinPopup, LinejoinLegacyPopup, callback, register);
        }

        public void SetFillVisible(bool visible)
        {
            _isFillVisible = visible;
            SetDisplay(FillColorRow, visible);
            SetDisplay(FillAddButton, !visible);
        }

        public void SetStrokeVisible(bool visible)
        {
            _isStrokeVisible = visible;
            SetDisplay(StrokeColorRow, visible);
            SetDisplay(StrokeMetricsRow, visible);
            SetDisplay(StrokeLineStyleRow, visible);
            SetDisplay(StrokeAddButton, !visible);
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

        private static Color ResolveColorValue(VisualElement colorControl, ColorPercentField colorPercentField, ColorField colorField, Color fallback)
        {
            if (colorPercentField != null)
            {
                return colorPercentField.ColorValue;
            }

            if (colorField != null)
            {
                return colorField.value;
            }

            if (colorControl is ColorPercentField queriedColorPercentField)
            {
                return queriedColorPercentField.ColorValue;
            }

            if (colorControl is ColorField queriedColorField)
            {
                return queriedColorField.value;
            }

            return fallback;
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

            popup.SetValueWithoutNotify(string.IsNullOrEmpty(value) ? "none" : value);
        }

        private static void ToggleColorChanged(
            ColorPercentField colorPercentField,
            ColorField colorField,
            EventCallback<ChangeEvent<Color>> callback,
            bool register)
        {
            CallbackBindingUtility.ToggleCallback(colorPercentField, callback, register);
            CallbackBindingUtility.ToggleValueChangedCallback(colorField, callback, register);
        }

        private static void TogglePopupChanged(
            SelectElement popup,
            DropdownField legacyPopup,
            EventCallback<ChangeEvent<string>> callback,
            bool register)
        {
            CallbackBindingUtility.ToggleCallback(popup, callback, register);
            CallbackBindingUtility.ToggleValueChangedCallback(legacyPopup, callback, register);
        }

        private static void ToggleValueChangedCallback(
            BaseField<float> field,
            EventCallback<ChangeEvent<float>> callback,
            bool register)
        {
            CallbackBindingUtility.ToggleValueChangedCallback(field, callback, register);
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            if (element == null)
                return;

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
