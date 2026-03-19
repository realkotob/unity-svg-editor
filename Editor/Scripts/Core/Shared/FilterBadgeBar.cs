using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Core.UI.Extensions;

namespace SvgEditor.Core.Shared
{
    internal sealed class FilterBadgeBarClasses
    {
        public string rootClass = string.Empty;
        public string badgesContainerClass = string.Empty;
        public string actionsContainerClass = string.Empty;
        public string buttonClass = string.Empty;
        public string activeButtonClass = string.Empty;
        public string disabledButtonClass = string.Empty;
    }

    internal sealed class FilterBadgeOption
    {
        public string key = string.Empty;
        public string label = string.Empty;
        public string tooltip = string.Empty;
        public bool isSelected;
        public bool isEnabled = true;
        public IReadOnlyList<string> classes;
    }

    internal sealed class FilterBadgeBar : VisualElement
    {
        public static class ClassName
        {
            public const string BASE = "filter-badge-bar";
            private const string ElementPrefix = BASE + "__";

            public const string BADGES = ElementPrefix + "badges";
            public const string ACTIONS = ElementPrefix + "actions";
            public const string BADGE = ElementPrefix + "badge";
            public const string BADGE_ACTIVE = BADGE + "--active";
            public const string BADGE_DISABLED = BADGE + "--disabled";
        }

        private readonly List<Entry> _entries = new();
        private readonly FilterBadgeBarClasses _classes;
        private readonly VisualElement _badgesContainer;
        private readonly VisualElement _actionsContainer;
        private IReadOnlyList<FilterBadgeOption> _options = Array.Empty<FilterBadgeOption>();
        private Action<string> _onSelected;

        public FilterBadgeBar() : this(null)
        {
        }

        public FilterBadgeBar(FilterBadgeBarClasses classes)
        {
            _classes = classes ?? new FilterBadgeBarClasses();

            this.AddClass(ClassName.BASE)
                .SetFlexDirection(FlexDirection.Row)
                .SetAlignItems(Align.Center);
            AddOptionalClass(this, _classes.rootClass);

            _badgesContainer = new VisualElement();
            _badgesContainer.AddClass(ClassName.BADGES)
                .SetFlexDirection(FlexDirection.Row)
                .SetFlexWrap(Wrap.Wrap)
                .SetAlignItems(Align.Center);
            _badgesContainer.style.flexGrow = 1f;
            AddOptionalClass(_badgesContainer, _classes.badgesContainerClass);
            Add(_badgesContainer);

            _actionsContainer = new VisualElement();
            _actionsContainer.AddClass(ClassName.ACTIONS)
                .SetFlexDirection(FlexDirection.Row)
                .SetAlignItems(Align.Center);
            _actionsContainer.style.marginLeft = StyleKeyword.Auto;
            _actionsContainer.SetDisplay(false);
            AddOptionalClass(_actionsContainer, _classes.actionsContainerClass);
            Add(_actionsContainer);
        }

        public event Action<FilterBadgeOption> SelectionChanged;

        public string SelectedItemId { get; private set; } = string.Empty;
        public IReadOnlyList<FilterBadgeOption> Options => _options;
        public VisualElement ActionsContainer => _actionsContainer;

        public void Bind(IEnumerable<FilterBadgeOption> options, string selectedKey, Action<string> onSelected)
        {
            _onSelected = onSelected;
            _badgesContainer.Clear();
            _entries.Clear();

            List<FilterBadgeOption> buffer = options != null
                ? new List<FilterBadgeOption>(options)
                : new List<FilterBadgeOption>();
            _options = buffer;
            SelectedItemId = string.IsNullOrWhiteSpace(selectedKey)
                ? ResolveSelectedItemId(buffer)
                : selectedKey;

            for (int index = 0; index < buffer.Count; index++)
            {
                FilterBadgeOption option = buffer[index] ?? new FilterBadgeOption();
                option.key = ResolveItemId(option.key, index);
                option.isSelected = string.Equals(option.key, SelectedItemId, StringComparison.Ordinal);

                Button button = new(() => OnItemClicked(option))
                {
                    text = option.label ?? string.Empty,
                    tooltip = option.tooltip ?? string.Empty
                };
                button.AddClass(ClassName.BADGE);
                AddOptionalClass(button, _classes.buttonClass);

                if (option.classes != null)
                {
                    for (int classIndex = 0; classIndex < option.classes.Count; classIndex++)
                    {
                        string className = option.classes[classIndex];
                        if (!string.IsNullOrWhiteSpace(className))
                        {
                            button.AddClass(className);
                        }
                    }
                }

                Entry entry = new(option, button);
                _entries.Add(entry);
                RefreshEntry(entry);
                _badgesContainer.Add(button);
            }
        }

        public void SetActions(IEnumerable<VisualElement> actions)
        {
            _actionsContainer.Clear();

            bool hasAction = false;
            if (actions != null)
            {
                foreach (VisualElement action in actions)
                {
                    if (action == null)
                    {
                        continue;
                    }

                    _actionsContainer.Add(action);
                    hasAction = true;
                }
            }

            _actionsContainer.SetDisplay(hasAction);
        }

        public void SetActions(params VisualElement[] actions)
        {
            SetActions((IEnumerable<VisualElement>)actions);
        }

        public void SetSelected(string itemId)
        {
            SelectedItemId = itemId ?? string.Empty;
            RefreshEntries();
        }

        private void OnItemClicked(FilterBadgeOption option)
        {
            if (option == null || !option.isEnabled)
            {
                return;
            }

            SetSelected(option.key);
            _onSelected?.Invoke(option.key);
            SelectionChanged?.Invoke(option);
        }

        private void RefreshEntries()
        {
            for (int index = 0; index < _entries.Count; index++)
            {
                RefreshEntry(_entries[index]);
            }
        }

        private void RefreshEntry(Entry entry)
        {
            entry.Option.isSelected = string.Equals(entry.Option.key, SelectedItemId, StringComparison.Ordinal);
            entry.Button.SetEnabled(entry.Option.isEnabled);
            entry.Button.EnableClass(ClassName.BADGE_ACTIVE, entry.Option.isSelected);
            entry.Button.EnableClass(ClassName.BADGE_DISABLED, !entry.Option.isEnabled);

            if (!string.IsNullOrWhiteSpace(_classes.activeButtonClass))
            {
                entry.Button.EnableClass(_classes.activeButtonClass, entry.Option.isSelected);
            }

            if (!string.IsNullOrWhiteSpace(_classes.disabledButtonClass))
            {
                entry.Button.EnableClass(_classes.disabledButtonClass, !entry.Option.isEnabled);
            }
        }

        private static string ResolveSelectedItemId(IReadOnlyList<FilterBadgeOption> items)
        {
            for (int index = 0; index < items.Count; index++)
            {
                FilterBadgeOption item = items[index];
                if (item != null && item.isSelected)
                {
                    return ResolveItemId(item.key, index);
                }
            }

            return items.Count > 0 ? ResolveItemId(items[0]?.key, 0) : string.Empty;
        }

        private static string ResolveItemId(string itemId, int index)
        {
            return string.IsNullOrWhiteSpace(itemId) ? $"item-{index}" : itemId;
        }

        private static void AddOptionalClass(VisualElement element, string className)
        {
            if (!string.IsNullOrWhiteSpace(className))
            {
                element.AddClass(className);
            }
        }

        private sealed class Entry
        {
            public Entry(FilterBadgeOption option, Button button)
            {
                Option = option;
                Button = button;
            }

            public FilterBadgeOption Option { get; }
            public Button Button { get; }
        }
    }
}
