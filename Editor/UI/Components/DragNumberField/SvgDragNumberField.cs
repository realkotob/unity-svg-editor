using Core.UI.Extensions;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor
{
    [UxmlElement(libraryPath = LibraryPath.COMPONENT_PATH)]
    public partial class SvgDragNumberField : FloatField
    {
        public static class UssClassName
        {
            public const string BASE = "drag-number-field";
            private const string ELEMENT_PREFIX = BASE + "__";
            private const string MODIFIER_PREFIX = BASE + "--";

            public const string PREFIX = ELEMENT_PREFIX + "prefix";
            public const string PREFIX_LABEL = ELEMENT_PREFIX + "prefix-label";
            public const string PREFIX_ICON = ELEMENT_PREFIX + "prefix-icon";
            public const string PREFIX_DRAGGING = PREFIX + "--dragging";
            public const string PREFIX_WITH_ICON_TEXT = PREFIX + "--with-icon-text";
            public const string DRAGGING = MODIFIER_PREFIX + "dragging";
            public const string UNITY_INPUT = "unity-base-text-field__input";
        }

        private const float MinDragSensitivity = 0.0001f;
        private const float DefaultDragSensitivity = 0.25f;
        private const float DefaultDragStep = 1f;

        private readonly VisualElement _prefixContainer;
        private readonly Label _prefixLabel;
        private readonly VisualElement _prefixIcon;
        private readonly DragNumberFieldManipulator _dragManipulator;
        private VisualElement _inputElement;

        private string _prefix = string.Empty;
        private string _prefixIconClass = string.Empty;
        private float _dragStep = DefaultDragStep;
        private float _dragSensitivity = DefaultDragSensitivity;
        private float _minValue = float.NegativeInfinity;
        private float _maxValue = float.PositiveInfinity;

        [UxmlAttribute, CreateProperty]
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value ?? string.Empty;
                _prefixLabel.text = _prefix;
                _prefixLabel.style.display = string.IsNullOrEmpty(_prefix) ? DisplayStyle.None : DisplayStyle.Flex;
                RefreshDragHandleVisibility();
            }
        }

        [UxmlAttribute, CreateProperty]
        public string PrefixIconClass
        {
            get => _prefixIconClass;
            set
            {
                if (!string.IsNullOrEmpty(_prefixIconClass))
                {
                    _prefixIcon.RemoveFromClassList(_prefixIconClass);
                }

                _prefixIconClass = value ?? string.Empty;

                if (!string.IsNullOrEmpty(_prefixIconClass))
                {
                    _prefixIcon.AddToClassList(_prefixIconClass);
                }

                _prefixIcon.style.display = string.IsNullOrEmpty(_prefixIconClass) ? DisplayStyle.None : DisplayStyle.Flex;
                RefreshDragHandleVisibility();
            }
        }

        [UxmlAttribute]
        public float DragStep
        {
            get => _dragStep;
            set => _dragStep = Mathf.Approximately(value, 0f) ? DefaultDragStep : Mathf.Abs(value);
        }

        [UxmlAttribute]
        public float DragSensitivity
        {
            get => _dragSensitivity;
            set => _dragSensitivity = Mathf.Max(MinDragSensitivity, value);
        }

        [UxmlAttribute]
        public float MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;
                SetValueWithoutNotify(ClampValue(base.value));
            }
        }

        [UxmlAttribute]
        public float MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                SetValueWithoutNotify(ClampValue(base.value));
            }
        }

        internal bool CanDrag => enabledInHierarchy && HasPrefixContent();

        public SvgDragNumberField()
            : this(string.Empty, string.Empty)
        {
        }

        public SvgDragNumberField(string label)
            : this(label, string.Empty)
        {
        }

        public SvgDragNumberField(string label, string prefix)
            : base()
        {
            this.label = label ?? string.Empty;
            AddToClassList(UssClassName.BASE);

            _prefixContainer = new VisualElement();
            _prefixContainer.pickingMode = PickingMode.Position;
            _prefixContainer.AddToClassList(UssClassName.PREFIX);

            _prefixIcon = new VisualElement();
            _prefixIcon.pickingMode = PickingMode.Ignore;
            _prefixIcon.AddToClassList(UssClassName.PREFIX_ICON);

            _prefixLabel = new Label();
            _prefixLabel.pickingMode = PickingMode.Ignore;
            _prefixLabel.AddToClassList(UssClassName.PREFIX_LABEL);

            _prefixContainer.Add(_prefixIcon);
            _prefixContainer.Add(_prefixLabel);

            _dragManipulator = new DragNumberFieldManipulator(this);
            this.AddManipulator(_dragManipulator);

            DragStep = DefaultDragStep;
            DragSensitivity = DefaultDragSensitivity;
            Prefix = prefix;
            PrefixIconClass = string.Empty;

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        internal void SetDragging(bool isDragging)
        {
            EnableInClassList(UssClassName.DRAGGING, isDragging);
            _prefixContainer.EnableInClassList(UssClassName.PREFIX_DRAGGING, isDragging);
        }

        internal void ApplyDragDelta(float startValue, float deltaX)
        {
            float nextValue = ClampValue(startValue + (deltaX * _dragSensitivity * _dragStep));
            if (Mathf.Approximately(base.value, nextValue))
            {
                return;
            }

            value = nextValue;
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            panel?.visualTree?.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown, TrickleDown.TrickleDown);

            _inputElement ??= this.Q(className: UssClassName.UNITY_INPUT);
            if (_inputElement == null || _prefixContainer.parent == _inputElement)
            {
                return;
            }

            _inputElement.Insert(0, _prefixContainer);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            evt.originPanel?.visualTree?.UnregisterCallback<KeyDownEvent>(OnGlobalKeyDown, TrickleDown.TrickleDown);
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            float clampedValue = ClampValue(base.value);
            if (Mathf.Approximately(base.value, clampedValue))
            {
                return;
            }

            value = clampedValue;
        }

        private void OnGlobalKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape || !_dragManipulator.IsDragging)
            {
                return;
            }

            if (_dragManipulator.CancelDrag())
            {
                evt.StopPropagation();
            }
        }

        private float ClampValue(float input)
        {
            if (_minValue > _maxValue)
            {
                return input;
            }

            return Mathf.Clamp(input, _minValue, _maxValue);
        }

        internal bool IsDragHandle(VisualElement element)
        {
            return element != null &&
                   _prefixContainer.style.display == DisplayStyle.Flex &&
                   (element == _prefixContainer || _prefixContainer.Contains(element));
        }

        private void RefreshDragHandleVisibility()
        {
            bool hasPrefixText = !string.IsNullOrEmpty(_prefix);
            bool hasPrefixIcon = !string.IsNullOrEmpty(_prefixIconClass);
            bool hasPrefix = hasPrefixText || hasPrefixIcon;

            _prefixContainer.style.display = hasPrefix ? DisplayStyle.Flex : DisplayStyle.None;
            _prefixContainer.EnableInClassList(UssClassName.PREFIX_WITH_ICON_TEXT, hasPrefixText && hasPrefixIcon);
        }

        private bool HasPrefixContent()
        {
            return !string.IsNullOrEmpty(_prefix) || !string.IsNullOrEmpty(_prefixIconClass);
        }
    }
}
