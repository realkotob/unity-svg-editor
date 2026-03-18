using System.Globalization;
using Core.UI.Extensions;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor
{
    [UxmlElement(libraryPath = LibraryPath.COMPONENT_PATH)]
    public partial class SvgColorField : BaseField<Color>
    {
        #region Constants

        public static class UssClassName
        {
            public const string BASE = "svg-color-field";
            private const string ELEMENT_PREFIX = BASE + "__";

            public const string INPUT = ELEMENT_PREFIX + "input";
            public const string SWATCH_FIELD = ELEMENT_PREFIX + "swatch-field";
            public const string HEX_FIELD = ELEMENT_PREFIX + "hex-field";
            public const string ALPHA_CONTAINER = ELEMENT_PREFIX + "alpha-container";
            public const string ALPHA_DIVIDER = ELEMENT_PREFIX + "alpha-divider";
            public const string ALPHA_FIELD = ELEMENT_PREFIX + "alpha-field";
        }

        private const string DEFAULT_HEX_VALUE = "000000";
        private const string HEX_PREFIX = "#";
        private const string ALPHA_FORMAT = "0";

        #endregion Constants

        #region Variables

        private readonly ColorField _swatchField;
        private readonly TextField _hexField;
        private readonly VisualElement _inputRoot;
        private readonly VisualElement _alphaContainer;
        private readonly VisualElement _alphaDivider;
        private readonly SvgDragNumberField _alphaField;

        private bool _isSynchronizingControls;

        #endregion Variables

        #region Constructor

        public SvgColorField()
            : this(string.Empty)
        {
        }

        public SvgColorField(string label)
            : base(label, new VisualElement())
        {
            this.AddClass(UssClassName.BASE);
            _inputRoot = BuildInputRoot();
            _swatchField = BuildSwatchField();
            _hexField = BuildHexField();
            _alphaContainer = BuildAlphaContainer();
            _alphaDivider = BuildAlphaDivider();
            _alphaField = BuildAlphaField();

            ComposeInputLayout();
            RegisterCallbacks();

            SetValueWithoutNotify(new Color(0f, 0f, 0f, 1f));
        }

        #endregion Constructor

        #region Public Methods

        public override void SetValueWithoutNotify(Color newValue)
        {
            base.SetValueWithoutNotify(NormalizeColor(newValue));
            SyncControlsFromValue();
        }

        #endregion Public Methods

        #region Help Methods

        private void OnSwatchChanged(ChangeEvent<Color> evt)
        {
            if (_isSynchronizingControls)
            {
                return;
            }

            ApplySwatchChanged(evt.newValue);
        }

        private void OnHexChanged(ChangeEvent<string> evt)
        {
            if (_isSynchronizingControls)
            {
                return;
            }

            ApplyHexChanged(evt.newValue);
        }

        private void OnAlphaChanged(ChangeEvent<float> evt)
        {
            if (_isSynchronizingControls)
            {
                return;
            }

            ApplyAlphaChanged(evt.newValue);
        }

        private VisualElement BuildInputRoot()
        {
            VisualElement inputRoot = this.Q(className: inputUssClassName) ?? new VisualElement();
            if (inputRoot.parent == null)
            {
                Add(inputRoot);
            }

            inputRoot.AddClass(UssClassName.INPUT);
            return inputRoot;
        }

        private ColorField BuildSwatchField()
        {
            ColorField swatchField = new()
            {
                showAlpha = false,
                showEyeDropper = false
            };
            return swatchField.AddClass(UssClassName.SWATCH_FIELD);
        }

        private TextField BuildHexField()
        {
            TextField hexField = new()
            {
                isDelayed = true,
                maxLength = 7
            };
            return hexField.AddClass(UssClassName.HEX_FIELD);
        }

        private static VisualElement BuildAlphaContainer()
        {
            VisualElement alphaContainer = new();
            return alphaContainer.AddClass(UssClassName.ALPHA_CONTAINER);
        }

        private static VisualElement BuildAlphaDivider()
        {
            VisualElement alphaDivider = new();
            return alphaDivider.AddClass(UssClassName.ALPHA_DIVIDER);
        }

        private SvgDragNumberField BuildAlphaField()
        {
            SvgDragNumberField alphaField = new()
            {
                MinValue = 0f,
                MaxValue = 100f,
                DragStep = 1f,
                DragSensitivity = 0.25f,
                Prefix = "%",
                formatString = ALPHA_FORMAT
            };
            return alphaField.AddClass(UssClassName.ALPHA_FIELD);
        }

        private void ComposeInputLayout()
        {
            _inputRoot.Add(_swatchField);
            _inputRoot.Add(_hexField);
            _inputRoot.Add(_alphaContainer);
            _alphaContainer.Add(_alphaDivider);
            _alphaContainer.Add(_alphaField);
        }

        private void RegisterCallbacks()
        {
            _swatchField.RegisterValueChangedCallback(OnSwatchChanged);
            _hexField.RegisterValueChangedCallback(OnHexChanged);
            _alphaField.RegisterValueChangedCallback(OnAlphaChanged);
        }

        private void ApplySwatchChanged(Color swatchColor)
        {
            Color nextColor = value;
            nextColor.r = swatchColor.r;
            nextColor.g = swatchColor.g;
            nextColor.b = swatchColor.b;
            ApplyInputValue(nextColor);
        }

        private void ApplyHexChanged(string rawHexValue)
        {
            if (!TryParseHex(rawHexValue, out Color parsedColor))
            {
                SyncControlsFromValue();
                return;
            }

            Color nextColor = value;
            nextColor.r = parsedColor.r;
            nextColor.g = parsedColor.g;
            nextColor.b = parsedColor.b;
            ApplyInputValue(nextColor);
        }

        private void ApplyAlphaChanged(float rawAlphaPercent)
        {
            int alphaPercent = Mathf.Clamp(Mathf.RoundToInt(rawAlphaPercent), 0, 100);
            if (!Mathf.Approximately(rawAlphaPercent, alphaPercent))
            {
                SynchronizeControls(() => _alphaField.SetValueWithoutNotify(alphaPercent));
            }

            Color nextColor = value;
            nextColor.a = alphaPercent / 100f;
            ApplyInputValue(nextColor);
        }

        private void ApplyInputValue(Color nextColor)
        {
            Color normalizedColor = NormalizeColor(nextColor);
            if (ApproximatelyEquals(value, normalizedColor))
            {
                SyncControlsFromValue();
                return;
            }

            value = normalizedColor;
        }

        private void SyncControlsFromValue()
        {
            SynchronizeControls(() =>
            {
                SyncSwatchFromValue();
                SyncHexFromValue();
                SyncAlphaFromValue();
            });
        }

        private void SyncSwatchFromValue()
        {
            _swatchField.SetValueWithoutNotify(ToOpaqueColor(value));
        }

        private void SyncHexFromValue()
        {
            _hexField.SetValueWithoutNotify(ToDisplayHex(value));
        }

        private void SyncAlphaFromValue()
        {
            _alphaField.SetValueWithoutNotify(Mathf.RoundToInt(value.a * 100f));
        }

        private void SynchronizeControls(System.Action synchronizeAction)
        {
            _isSynchronizingControls = true;
            try
            {
                synchronizeAction?.Invoke();
            }
            finally
            {
                _isSynchronizingControls = false;
            }
        }

        private static bool TryParseHex(string rawValue, out Color parsedColor)
        {
            parsedColor = default;
            string normalizedHex = NormalizeHex(rawValue);
            if (normalizedHex.Length != 6)
            {
                return false;
            }

            return ColorUtility.TryParseHtmlString(HEX_PREFIX + normalizedHex, out parsedColor);
        }

        private static string NormalizeHex(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            string trimmed = rawValue.Trim();
            if (trimmed.StartsWith(HEX_PREFIX, true, CultureInfo.InvariantCulture))
            {
                trimmed = trimmed.Substring(1);
            }

            return trimmed.ToUpperInvariant();
        }

        private static string ToDisplayHex(Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGB(ToOpaqueColor(color));
            return string.IsNullOrEmpty(hex) ? DEFAULT_HEX_VALUE : hex.ToUpperInvariant();
        }

        private static Color ToOpaqueColor(Color color)
        {
            return new Color(color.r, color.g, color.b, 1f);
        }

        private static Color NormalizeColor(Color color)
        {
            return new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                Mathf.Clamp01(color.a));
        }

        private static bool ApproximatelyEquals(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r) &&
                   Mathf.Approximately(left.g, right.g) &&
                   Mathf.Approximately(left.b, right.b) &&
                   Mathf.Approximately(left.a, right.a);
        }

        #endregion Help Methods
    }
}
