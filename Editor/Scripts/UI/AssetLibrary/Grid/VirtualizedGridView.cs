using System;
using System.Collections.Generic;
using System.Linq;
using Core.UI.Extensions;
using SvgEditor.Core.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.UI.AssetLibrary.Grid
{
    internal class VirtualizedGridView : VisualElement
    {
        public static class UssClassName
        {
            public const string BASE = "tooling-grid";
            private const string ElementPrefix = BASE + "__";

            public const string SCROLL = ElementPrefix + "scroll";
            public const string VIEWPORT = ElementPrefix + "viewport";
            public const string EMPTY = ElementPrefix + "empty";
            public const string HEADER = ElementPrefix + "header";
            public const string CELL = ElementPrefix + "cell";
            public const string CELL_SELECTED = CELL + "--selected";
            public const string CELL_PRESSED = CELL + "--pressed";
            public const string CELL_ICON = ElementPrefix + "cell-icon";
            public const string CELL_CHECK = ElementPrefix + "cell-check";
            public const string CELL_BADGE = ElementPrefix + "cell-badge";
            public const string CELL_LABEL = ElementPrefix + "cell-label";
            public const string CELL_ACTION_BUTTON = ElementPrefix + "cell-action-btn";
            public const string CELL_ACTION_BUTTON_DELETE = CELL_ACTION_BUTTON + "--delete";
            public const string CELL_ACTION_BUTTON_IMPORT = CELL_ACTION_BUTTON + "--import";
        }

        public static readonly GridLayoutMetrics DefaultMetrics = GridLayoutMetrics.Default;

        private readonly ScrollView _scrollView;
        private readonly VisualElement _viewport;
        private readonly Label _emptyLabel;
        private readonly GridLayoutEngine _layout;
        private readonly VirtualizedGridViewVirtualizer _virtualizer;
        private readonly List<GridViewItem> _displayItems = new();
        private readonly List<string> _groupKeys = new();
        private readonly HashSet<int> _emptyIndices = new();

        private DragSelectionHandler _dragHandler;
        private List<GridViewItem> _selectedItemsCache;
        private bool _isSelectedItemsDirty = true;
        private string _pendingSelectedItemId = string.Empty;

        public VirtualizedGridView() : this(DefaultMetrics)
        {
        }

        public VirtualizedGridView(GridLayoutMetrics metrics)
        {
            Metrics = metrics;
            _layout = new GridLayoutEngine(metrics);

            this.AddClass(UssClassName.BASE);
            style.flexGrow = 1f;

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.AddClass(UssClassName.SCROLL);
            _scrollView.style.flexGrow = 1f;
            hierarchy.Add(_scrollView);

            _viewport = new VisualElement();
            _viewport.AddClass(UssClassName.VIEWPORT);
            _viewport.style.position = Position.Relative;
            _scrollView.Add(_viewport);

            _emptyLabel = new Label("No items found");
            _emptyLabel.AddClass(UssClassName.EMPTY);
            _emptyLabel.style.display = DisplayStyle.None;
            hierarchy.Add(_emptyLabel);

            _virtualizer = new VirtualizedGridViewVirtualizer(_viewport, _scrollView);
            _virtualizer.OnItemActionTriggered += ForwardItemActionTriggered;
            _scrollView.Callback(OnScrollViewGeometryChanged);
            _scrollView.verticalScroller.valueChanged += OnScrollValueChanged;

            focusable = true;
            this.Callback(OnKeyDown);
        }

        public event Action<GridViewItem> OnItemSelected = delegate { };
        public event Action<GridViewItem> OnItemDoubleClicked = delegate { };
        public event Action<GridViewItem> OnItemActionTriggered = delegate { };
        public event Action<int, int> OnVisibleRangeChanged = delegate { };
        public event Action<List<GridViewItem>> OnSelectionChanged = delegate { };

        public GridLayoutMetrics Metrics { get; }
        public bool GroupByAlpha { get; set; }
        public bool ShowActionButtons { get; set; }
        public int LabelMaxLength { get; set; } = 9;
        public int Columns => _layout.Columns;
        public IReadOnlyList<GridViewItem> Items => _displayItems;

        public List<GridViewItem> SelectedItems
        {
            get
            {
                if (_isSelectedItemsDirty)
                {
                    _selectedItemsCache = SelectedIndices
                        .Where(index => index >= 0 && index < _displayItems.Count)
                        .Select(index => _displayItems[index])
                        .ToList();
                    _isSelectedItemsDirty = false;
                }

                return _selectedItemsCache;
            }
        }

        public string EmptyText
        {
            get => _emptyLabel.text;
            set => _emptyLabel.text = value ?? string.Empty;
        }

        private HashSet<int> SelectedIndices => _dragHandler?.SelectedIndices ?? _emptyIndices;

        public void SetItems(IReadOnlyList<GridViewItem> items)
        {
            _displayItems.Clear();
            if (items != null)
            {
                _displayItems.AddRange(items);
            }

            if (GroupByAlpha)
            {
                _displayItems.Sort((left, right) => string.Compare(
                    ResolveSortKey(left),
                    ResolveSortKey(right),
                    StringComparison.OrdinalIgnoreCase));
            }

            RebuildGroupKeys();

            if (_dragHandler != null)
            {
                _dragHandler.SelectedIndices.Clear();
                _dragHandler.LastClickedIndex = -1;
            }

            _pendingSelectedItemId = string.Empty;
            InvalidateSelectionCache();
            RecomputeLayout();
            _virtualizer.ResetVisibleRange();
            _virtualizer.RecycleAll();

            if (_displayItems.Count == 0)
            {
                _emptyLabel.style.display = DisplayStyle.Flex;
                _scrollView.style.display = DisplayStyle.None;
                return;
            }

            _emptyLabel.style.display = DisplayStyle.None;
            _scrollView.style.display = DisplayStyle.Flex;
            _scrollView.scrollOffset = Vector2.zero;
            schedule.Execute(OnLayoutChanged);
        }

        public void RefreshItems()
        {
            SyncVirtualizerState();
            _virtualizer.RefreshItems(GetDataIndex);
        }

        public void Select(GridViewItem item)
        {
            if (item == null)
            {
                ClearSelection();
                return;
            }

            int index = FindItemIndex(item);
            SelectIndex(index, item.Id);
        }

        public void SelectById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                ClearSelection();
                return;
            }

            int index = FindItemIndexById(itemId);
            SelectIndex(index, itemId);
        }

        public void ClearSelection()
        {
            _pendingSelectedItemId = string.Empty;
            _dragHandler?.ClearSelection();

            SyncVirtualizerState();
            _virtualizer.RefreshSelectionVisuals(GetDataIndex);
            FireSelectionChanged();
        }

        public static string TruncateLabel(string text, int maxLength)
        {
            text ??= string.Empty;
            if (maxLength <= 0 || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength - 1) + "..";
        }

        protected virtual string FormatItemLabel(GridViewItem item)
        {
            return TruncateLabel(item?.Label ?? string.Empty, LabelMaxLength);
        }

        protected virtual string ResolveSortKey(GridViewItem item)
        {
            return string.IsNullOrWhiteSpace(item?.SortKey)
                ? item?.Label ?? string.Empty
                : item.SortKey;
        }

        protected virtual string ResolveGroupKey(GridViewItem item)
        {
            if (!string.IsNullOrWhiteSpace(item?.GroupKey))
            {
                return item.GroupKey;
            }

            string sortKey = ResolveSortKey(item);
            if (string.IsNullOrWhiteSpace(sortKey))
            {
                return "#";
            }

            return char.ToUpperInvariant(sortKey[0]).ToString();
        }

        private void RebuildGroupKeys()
        {
            _groupKeys.Clear();
            for (int index = 0; index < _displayItems.Count; index++)
            {
                _groupKeys.Add(ResolveGroupKey(_displayItems[index]));
            }
        }

        private void ForwardItemActionTriggered(GridViewItem item)
        {
            OnItemActionTriggered?.Invoke(item);
        }

        private void OnScrollViewGeometryChanged(GeometryChangedEvent _)
        {
            VisualElement viewport = _scrollView.contentViewport;
            if (_dragHandler == null)
            {
                _dragHandler = new DragSelectionHandler(viewport, _scrollView, _layout.DataIndexAtContent);
                _dragHandler.OnItemClicked += OnDragItemClicked;
                _dragHandler.OnItemDoubleClicked += OnDragItemDoubleClicked;
                _dragHandler.OnSelectionChanged += OnDragSelectionChanged;

                if (!string.IsNullOrWhiteSpace(_pendingSelectedItemId))
                {
                    SelectById(_pendingSelectedItemId);
                }
            }

            OnLayoutChanged();
        }

        private void OnScrollValueChanged(float _)
        {
            UpdateVisible();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape || SelectedIndices.Count <= 0)
            {
                return;
            }

            ClearSelection();
            evt.StopPropagation();
        }

        private void OnLayoutChanged()
        {
            int previousColumns = _layout.Columns;
            if (!RecomputeLayout())
            {
                return;
            }

            if (_layout.Columns != previousColumns)
            {
                _virtualizer.RecycleAll();
                _virtualizer.ResetVisibleRange();
            }

            UpdateVisible();
        }

        private void UpdateVisible()
        {
            SyncVirtualizerState();
            (int first, int last) visibleRange = _virtualizer.UpdateVisible(_layout);
            if (visibleRange.first >= 0)
            {
                OnVisibleRangeChanged?.Invoke(visibleRange.first, visibleRange.last);
            }
        }

        private void SyncVirtualizerState()
        {
            _virtualizer.SetState(
                _displayItems,
                SelectedIndices,
                ShowActionButtons,
                Metrics,
                FormatItemLabel);
        }

        private bool RecomputeLayout()
        {
            float scrollWidth = _scrollView.contentViewport.resolvedStyle.width;
            if (float.IsNaN(scrollWidth) || scrollWidth <= 0f)
            {
                scrollWidth = Metrics.CellWidth;
            }

            _layout.Compute(_displayItems.Count, GroupByAlpha, scrollWidth, _groupKeys);

            _viewport.style.height = _layout.TotalHeight;
            _viewport.style.width = Length.Percent(100);
            return _displayItems.Count > 0;
        }

        private int GetDataIndex(int layoutKey)
        {
            if (!GroupByAlpha)
            {
                return layoutKey;
            }

            IReadOnlyList<GridLayoutEngine.LayoutEntry> entries = _layout.Entries;
            if (layoutKey >= 0 && layoutKey < entries.Count && !entries[layoutKey].IsHeader)
            {
                return entries[layoutKey].DataIndex;
            }

            return -1;
        }

        private void InvalidateSelectionCache()
        {
            _isSelectedItemsDirty = true;
        }

        private void FireSelectionChanged()
        {
            InvalidateSelectionCache();
            OnSelectionChanged?.Invoke(SelectedItems);
        }

        private void SelectIndex(int index, string pendingItemId)
        {
            _pendingSelectedItemId = index >= 0 ? pendingItemId ?? string.Empty : string.Empty;

            _dragHandler?.SelectSingle(index);

            SyncVirtualizerState();
            _virtualizer.RefreshSelectionVisuals(GetDataIndex);
            FireSelectionChanged();
        }

        private void OnDragItemClicked(int dataIndex)
        {
            if (dataIndex < 0 || dataIndex >= _displayItems.Count)
            {
                return;
            }

            _pendingSelectedItemId = _displayItems[dataIndex].Id ?? string.Empty;
            SyncVirtualizerState();
            _virtualizer.RefreshSelectionVisuals(GetDataIndex);
            OnItemSelected?.Invoke(_displayItems[dataIndex]);
        }

        private void OnDragItemDoubleClicked(int dataIndex)
        {
            if (dataIndex < 0 || dataIndex >= _displayItems.Count)
            {
                return;
            }

            OnItemDoubleClicked?.Invoke(_displayItems[dataIndex]);
        }

        private void OnDragSelectionChanged()
        {
            if (SelectedItems.Count == 1)
            {
                _pendingSelectedItemId = SelectedItems[0]?.Id ?? string.Empty;
            }
            else if (SelectedItems.Count == 0)
            {
                _pendingSelectedItemId = string.Empty;
            }

            SyncVirtualizerState();
            _virtualizer.RefreshSelectionVisuals(GetDataIndex);
            FireSelectionChanged();
        }

        private int FindItemIndex(GridViewItem item)
        {
            if (item == null)
            {
                return -1;
            }

            if (!string.IsNullOrWhiteSpace(item.Id))
            {
                int indexById = FindItemIndexById(item.Id);
                if (indexById >= 0)
                {
                    return indexById;
                }
            }

            for (int index = 0; index < _displayItems.Count; index++)
            {
                if (ReferenceEquals(_displayItems[index], item))
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindItemIndexById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return -1;
            }

            for (int index = 0; index < _displayItems.Count; index++)
            {
                if (string.Equals(_displayItems[index]?.Id, itemId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
