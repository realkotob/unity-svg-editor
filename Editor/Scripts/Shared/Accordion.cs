using Core.UI.Extensions;
using Unity.Properties;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    [UxmlElement]
    public partial class Accordion : VisualElement
    {
        public static class ClassName
        {
            public const string BASE = "accordion";
            private const string ElementPrefix = BASE + "__";
            private const string ModifierPrefix = BASE + "--";

            public const string BORDERED = ModifierPrefix + "bordered";
            public const string ITEM = ElementPrefix + "item";
            public const string ITEM_OPEN = ITEM + "--open";
            public const string ITEM_DISABLED = ITEM + "--disabled";
            public const string ITEM_FIRST = ITEM + "--first";
            public const string ITEM_LAST = ITEM + "--last";
            public const string ITEM_HEADER = ElementPrefix + "item-header";
            public const string ITEM_HEADER_LABEL = ElementPrefix + "item-header-label";
            public const string ITEM_HEADER_ICON = ElementPrefix + "item-header-icon";
            public const string ITEM_CONTENT = ElementPrefix + "item-content";
            public const string ITEM_CONTENT_INNER = ElementPrefix + "item-content-inner";
        }

        private bool _shouldAllowMultiple;
        private bool _isBordered;

        [UxmlAttribute]
        public bool ShouldAllowMultiple
        {
            get => _shouldAllowMultiple;
            set => _shouldAllowMultiple = value;
        }

        [UxmlAttribute]
        public bool IsBordered
        {
            get => _isBordered;
            set
            {
                _isBordered = value;
                EnableInClassList(ClassName.BORDERED, value);
            }
        }

        public Accordion()
        {
            AddToClassList(ClassName.BASE);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateFirstLastItem();
        }

        internal void UpdateFirstLastItem()
        {
            AccordionItem firstItem = null;
            AccordionItem lastItem = null;

            foreach (VisualElement child in Children())
            {
                if (child is not AccordionItem item)
                {
                    continue;
                }

                item.RemoveFromClassList(ClassName.ITEM_FIRST);
                item.RemoveFromClassList(ClassName.ITEM_LAST);

                firstItem ??= item;
                lastItem = item;
            }

            firstItem?.AddToClassList(ClassName.ITEM_FIRST);
            lastItem?.AddToClassList(ClassName.ITEM_LAST);
        }

        internal void OnItemToggled(AccordionItem toggledItem)
        {
            if (_shouldAllowMultiple)
            {
                return;
            }

            foreach (VisualElement child in Children())
            {
                if (child is AccordionItem item && item != toggledItem)
                {
                    item.IsOpen = false;
                }
            }
        }
    }

    [UxmlElement]
    public partial class AccordionItem : VisualElement
    {
        private bool _isOpen;
        private bool _isDisabled;
        private readonly VisualElement _header;
        private readonly Label _headerLabel;
        private readonly VisualElement _headerIcon;
        private readonly VisualElement _content;
        private readonly VisualElement _contentInner;

        [UxmlAttribute, CreateProperty]
        public string Title
        {
            get => _headerLabel.text;
            set => _headerLabel.text = value ?? string.Empty;
        }

        [UxmlAttribute, CreateProperty]
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen == value)
                {
                    return;
                }

                _isOpen = value;
                EnableInClassList(Accordion.ClassName.ITEM_OPEN, value);
                AnimateContent(value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public bool IsDisabled
        {
            get => _isDisabled;
            set
            {
                _isDisabled = value;
                EnableInClassList(Accordion.ClassName.ITEM_DISABLED, value);
            }
        }

        public override VisualElement contentContainer => _contentInner;

        public AccordionItem()
        {
            AddToClassList(Accordion.ClassName.ITEM);

            _header = new VisualElement();
            _header.AddToClassList(Accordion.ClassName.ITEM_HEADER);
            _header.RegisterCallback<ClickEvent>(OnHeaderClick);
            hierarchy.Add(_header);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            _headerLabel = new Label();
            _headerLabel.AddToClassList(Accordion.ClassName.ITEM_HEADER_LABEL);
            _header.Add(_headerLabel);

            _headerIcon = new VisualElement();
            _headerIcon.AddToClassList(Accordion.ClassName.ITEM_HEADER_ICON);
            _header.Add(_headerIcon);

            _content = new VisualElement();
            _content.AddToClassList(Accordion.ClassName.ITEM_CONTENT);
            _content.style.height = 0f;
            _content.RegisterCallback<TransitionEndEvent>(OnContentTransitionEnd);
            hierarchy.Add(_content);

            _contentInner = new VisualElement();
            _contentInner.AddToClassList(Accordion.ClassName.ITEM_CONTENT_INNER);
            _content.Add(_contentInner);
        }

        private void OnHeaderClick(ClickEvent evt)
        {
            if (_isDisabled)
            {
                return;
            }

            IsOpen = !IsOpen;
            GetFirstAncestorOfType<Accordion>()?.OnItemToggled(this);
        }

        private void OnContentTransitionEnd(TransitionEndEvent evt)
        {
            if (_isOpen)
            {
                _content.style.height = StyleKeyword.Auto;
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            GetFirstAncestorOfType<Accordion>()?.UpdateFirstLastItem();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            GetFirstAncestorOfType<Accordion>()?.UpdateFirstLastItem();
        }

        private void AnimateContent(bool open)
        {
            if (open)
            {
                _content.style.height = StyleKeyword.Auto;
                _content.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
                {
                    float targetHeight = _content.resolvedStyle.height;
                    _content.style.height = 0f;
                    schedule.Execute(() => _content.style.height = targetHeight);
                });
                return;
            }

            float currentHeight = _content.resolvedStyle.height;
            _content.style.height = currentHeight;
            schedule.Execute(() => _content.style.height = 0f);
        }
    }
}
