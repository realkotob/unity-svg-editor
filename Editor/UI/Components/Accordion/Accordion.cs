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
            private const string ELEMENT_PREFIX = BASE + "__";
            private const string MODIFIER_PREFIX = BASE + "--";

            public const string BORDERED = MODIFIER_PREFIX + "bordered";
            public const string ITEM = ELEMENT_PREFIX + "item";
            public const string ITEM_OPEN = ITEM + "--open";
            public const string ITEM_DISABLED = ITEM + "--disabled";
            public const string ITEM_FIRST = ITEM + "--first";
            public const string ITEM_LAST = ITEM + "--last";
            public const string ITEM_HEADER = ELEMENT_PREFIX + "item-header";
            public const string ITEM_HEADER_LABEL = ELEMENT_PREFIX + "item-header-label";
            public const string ITEM_HEADER_ICON = ELEMENT_PREFIX + "item-header-icon";
            public const string ITEM_CONTENT = ELEMENT_PREFIX + "item-content";
            public const string ITEM_CONTENT_INNER = ELEMENT_PREFIX + "item-content-inner";
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
                this.EnableClass(ClassName.BORDERED, value);
            }
        }

        public Accordion()
        {
            this.AddClass(ClassName.BASE)
                .Callback(OnGeometryChanged);
        }

        #region Help Methods

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

                item.RemoveClass(ClassName.ITEM_FIRST)
                    .RemoveClass(ClassName.ITEM_LAST);

                firstItem ??= item;
                lastItem = item;
            }

            firstItem?.AddClass(ClassName.ITEM_FIRST);
            lastItem?.AddClass(ClassName.ITEM_LAST);
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

        #endregion Help Methods
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
            set => _headerLabel.SetText(value ?? string.Empty);
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
                this.EnableClass(Accordion.ClassName.ITEM_OPEN, value);
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
                this.EnableClass(Accordion.ClassName.ITEM_DISABLED, value);
            }
        }

        public override VisualElement contentContainer => _contentInner;

        public AccordionItem()
        {
            this.AddClass(Accordion.ClassName.ITEM);

            _header = new VisualElement().AddClass(Accordion.ClassName.ITEM_HEADER)
                .Callback(OnHeaderClick);
            hierarchy.Add(_header);

            this.Callback(OnAttachToPanel)
                .Callback(OnDetachFromPanel);

            _headerLabel = new Label().AddClass(Accordion.ClassName.ITEM_HEADER_LABEL);
            _header.Add(_headerLabel);

            _headerIcon = new VisualElement().AddClass(Accordion.ClassName.ITEM_HEADER_ICON);
            _header.Add(_headerIcon);

            _content = new VisualElement().AddClass(Accordion.ClassName.ITEM_CONTENT)
                .SetHeight(0f)
                .Callback(OnContentTransitionEnd);
            hierarchy.Add(_content);

            _contentInner = new VisualElement().AddClass(Accordion.ClassName.ITEM_CONTENT_INNER);
            _content.Add(_contentInner);
        }

        #region Help Methods

        private void OnHeaderClick(ClickEvent evt)
        {
            if (_isDisabled)
            {
                return;
            }

            IsOpen = !IsOpen;
            Accordion accordion = GetFirstAncestorOfType<Accordion>();
            accordion?.OnItemToggled(this);
        }

        private void OnContentTransitionEnd(TransitionEndEvent evt)
        {
            if (_isOpen)
            {
                _content.SetHeight(StyleKeyword.Auto);
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            GetFirstAncestorOfType<Accordion>()?.UpdateFirstLastItem();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _header.UnregisterCallback<ClickEvent>(OnHeaderClick);
            GetFirstAncestorOfType<Accordion>()?.UpdateFirstLastItem();
        }

        private void AnimateContent(bool open)
        {
            if (open)
            {
                _content.SetHeight(StyleKeyword.Auto);
                _content.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
                {
                    float targetHeight = _content.resolvedStyle.height;
                    _content.SetHeight(0f);
                    schedule.Execute(() => _content.SetHeight(targetHeight));
                });
                return;
            }

            _content.SetHeight(_content.resolvedStyle.height);
            schedule.Execute(() => _content.SetHeight(0f));
        }

        #endregion Help Methods
    }
}
