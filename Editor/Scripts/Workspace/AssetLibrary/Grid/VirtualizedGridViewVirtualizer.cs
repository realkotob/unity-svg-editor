using System;
using System.Collections.Generic;
using Core.UI.Extensions;
using SvgEditor.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Workspace.AssetLibrary.Grid
{
    internal sealed class VirtualizedGridViewVirtualizer
    {
        private const string ButtonActionPrimaryModifierClass = "button--action-primary";
        private const string ButtonActionDestructiveModifierClass = "button--action-destructive";
        private const string CellIconName = "cell-icon";
        private const string CellCheckName = "cell-check";
        private const string CellBadgeName = "cell-badge";
        private const string CellActionButtonName = "cell-action-btn";

        private readonly VisualElement _viewport;
        private readonly ScrollView _scrollView;
        private readonly List<VisualElement> _cellPool = new();
        private readonly List<VisualElement> _headerPool = new();
        private readonly Dictionary<int, VisualElement> _activeCells = new();
        private readonly List<int> _recycleBuffer = new();

        private List<GridViewItem> _items = new();
        private HashSet<int> _selectedIndices;
        private bool _showActions;
        private GridLayoutMetrics _metrics;
        private Func<GridViewItem, string> _labelFormatter;

        private int _visibleFirst = -1;
        private int _visibleLast = -1;

        public VirtualizedGridViewVirtualizer(VisualElement viewport, ScrollView scrollView)
        {
            _viewport = viewport;
            _scrollView = scrollView;
        }

        public event Action<GridViewItem> OnItemActionTriggered;

        public void SetState(
            List<GridViewItem> items,
            HashSet<int> selectedIndices,
            bool showActions,
            GridLayoutMetrics metrics,
            Func<GridViewItem, string> labelFormatter)
        {
            _items = items ?? new List<GridViewItem>();
            _selectedIndices = selectedIndices;
            _showActions = showActions;
            _metrics = metrics;
            _labelFormatter = labelFormatter;
        }

        public void ResetVisibleRange()
        {
            _visibleFirst = -1;
            _visibleLast = -1;
        }

        public void RecycleAll()
        {
            foreach ((int _, VisualElement element) in _activeCells)
            {
                element.style.display = DisplayStyle.None;
                if (element.ClassListContains(VirtualizedGridView.UssClassName.HEADER))
                {
                    _headerPool.Add(element);
                }
                else
                {
                    _cellPool.Add(element);
                }
            }

            _activeCells.Clear();
        }

        public void RefreshItems(Func<int, int> getDataIndex)
        {
            foreach ((int key, VisualElement element) in _activeCells)
            {
                int dataIndex = getDataIndex(key);
                if (dataIndex < 0 || dataIndex >= _items.Count)
                {
                    continue;
                }

                BindCellContent(element, _items[dataIndex], dataIndex);
            }
        }

        public void RefreshSelectionVisuals(Func<int, int> getDataIndex)
        {
            foreach ((int key, VisualElement cell) in _activeCells)
            {
                int dataIndex = getDataIndex(key);
                bool isSelected = _selectedIndices != null && _selectedIndices.Contains(dataIndex);
                cell.EnableClass(VirtualizedGridView.UssClassName.CELL_SELECTED, isSelected);

                if (!_showActions)
                {
                    continue;
                }

                Button button = cell.Q<Button>(CellActionButtonName);
                if (button == null)
                {
                    continue;
                }

                if (isSelected && _selectedIndices.Count <= 1)
                {
                    ShowCellAction(cell, hovered: false);
                }
                else
                {
                    button.style.display = DisplayStyle.None;
                }
            }
        }

        public (int first, int last) UpdateVisible(GridLayoutEngine layout)
        {
            return layout.Entries.Count > 0
                ? UpdateVisibleGrouped(layout)
                : UpdateVisibleFlat(layout);
        }

        private VisualElement GetOrCreateCell()
        {
            if (_cellPool.Count > 0)
            {
                VisualElement recycled = _cellPool[_cellPool.Count - 1];
                _cellPool.RemoveAt(_cellPool.Count - 1);
                recycled.style.display = DisplayStyle.Flex;
                return recycled;
            }

            VisualElement cell = new();
            cell.AddClass(VirtualizedGridView.UssClassName.CELL);
            cell.style.position = Position.Absolute;

            VisualElement icon = new() { name = CellIconName };
            icon.AddClass(VirtualizedGridView.UssClassName.CELL_ICON);
            cell.Add(icon);

            VisualElement check = new() { name = CellCheckName };
            check.AddClass(VirtualizedGridView.UssClassName.CELL_CHECK);
            check.style.display = DisplayStyle.None;
            cell.Add(check);

            Label badge = new() { name = CellBadgeName };
            badge.AddClass(VirtualizedGridView.UssClassName.CELL_BADGE);
            badge.style.display = DisplayStyle.None;
            cell.Add(badge);

            Label label = new();
            label.AddClass(VirtualizedGridView.UssClassName.CELL_LABEL);
            cell.Add(label);

            Button actionButton = new()
            {
                name = CellActionButtonName
            };
            actionButton.AddClass(VirtualizedGridView.UssClassName.CELL_ACTION_BUTTON);
            actionButton.style.display = DisplayStyle.None;
            actionButton.userData = cell;
            actionButton.Callback(OnActionButtonClicked);
            cell.Add(actionButton);

            cell.Callback(OnCellMouseEnter);
            cell.Callback(OnCellMouseLeave);

            _viewport.Add(cell);
            return cell;
        }

        private VisualElement GetOrCreateHeader()
        {
            if (_headerPool.Count > 0)
            {
                VisualElement recycled = _headerPool[_headerPool.Count - 1];
                _headerPool.RemoveAt(_headerPool.Count - 1);
                recycled.style.display = DisplayStyle.Flex;
                return recycled;
            }

            VisualElement header = new();
            header.AddClass(VirtualizedGridView.UssClassName.HEADER);
            header.style.position = Position.Absolute;
            header.Add(new Label());
            _viewport.Add(header);
            return header;
        }

        private void BindCellFlat(VisualElement cell, int index, int columns)
        {
            cell.userData = index;
            int row = index / columns;
            int col = index % columns;
            cell.style.left = col * _metrics.CellWidth;
            cell.style.top = row * _metrics.CellHeight;
            cell.style.width = _metrics.CellWidth;
            cell.style.height = _metrics.CellHeight;
            BindCellContent(cell, _items[index], index);
        }

        private void BindCellGrouped(VisualElement cell, GridLayoutEngine.LayoutEntry entry)
        {
            cell.userData = entry.DataIndex;
            cell.style.left = entry.Left;
            cell.style.top = entry.Top;
            cell.style.width = entry.Width;
            cell.style.height = entry.Height;
            BindCellContent(cell, _items[entry.DataIndex], entry.DataIndex);
        }

        private void BindCellContent(VisualElement cell, GridViewItem item, int dataIndex)
        {
            cell.userData = dataIndex;

            VisualElement icon = cell.Q<VisualElement>(CellIconName);
            if (icon != null)
            {
                PreviewImageHelper.Apply(icon, item?.PreviewSource ?? PreviewImageSource.None);
            }

            VisualElement check = cell.Q<VisualElement>(CellCheckName);
            if (check != null)
            {
                check.style.display = item != null && item.ShowStatusMark ? DisplayStyle.Flex : DisplayStyle.None;
            }

            Label badge = cell.Q<Label>(CellBadgeName);
            if (badge != null)
            {
                string badgeText = item?.BadgeText ?? string.Empty;
                if (string.IsNullOrWhiteSpace(badgeText))
                {
                    badge.style.display = DisplayStyle.None;
                }
                else
                {
                    badge.text = badgeText;
                    badge.style.display = DisplayStyle.Flex;
                }
            }

            Label label = cell.Q<Label>(className: VirtualizedGridView.UssClassName.CELL_LABEL);
            if (label != null)
            {
                label.text = _labelFormatter?.Invoke(item) ?? item?.Label ?? string.Empty;
                label.tooltip = item?.Label ?? string.Empty;
            }

            bool isSelected = _selectedIndices != null && _selectedIndices.Contains(dataIndex);
            cell.EnableClass(VirtualizedGridView.UssClassName.CELL_SELECTED, isSelected);

            Button button = cell.Q<Button>(CellActionButtonName);
            if (button != null)
            {
                button.style.display = DisplayStyle.None;
            }

            if (_showActions && isSelected && _selectedIndices.Count <= 1)
            {
                ShowCellAction(cell, hovered: false);
            }
        }

        private static void BindHeader(VisualElement header, GridLayoutEngine.LayoutEntry entry)
        {
            header.style.left = entry.Left;
            header.style.top = entry.Top;
            header.style.width = Length.Percent(100);
            header.style.height = entry.Height;
            header.Q<Label>().text = entry.HeaderText;
        }

        private (int, int) UpdateVisibleFlat(GridLayoutEngine layout)
        {
            if (_items.Count == 0 || layout.Columns <= 0)
            {
                return (-1, -1);
            }

            float scrollY = _scrollView.scrollOffset.y;
            float viewHeight = _scrollView.contentViewport.resolvedStyle.height;
            if (float.IsNaN(viewHeight) || viewHeight <= 0f)
            {
                return (-1, -1);
            }

            int firstRow = Mathf.Max(0, Mathf.FloorToInt(scrollY / _metrics.CellHeight) - 1);
            int lastRow = Mathf.FloorToInt((scrollY + viewHeight) / _metrics.CellHeight) + 1;
            int firstIndex = firstRow * layout.Columns;
            int lastIndex = Mathf.Min(_items.Count - 1, ((lastRow + 1) * layout.Columns) - 1);

            if (firstIndex == _visibleFirst && lastIndex == _visibleLast)
            {
                return (firstIndex, lastIndex);
            }

            RecycleOutOfRange(firstIndex, lastIndex);

            for (int index = firstIndex; index <= lastIndex; index++)
            {
                if (_activeCells.ContainsKey(index))
                {
                    continue;
                }

                VisualElement cell = GetOrCreateCell();
                BindCellFlat(cell, index, layout.Columns);
                _activeCells[index] = cell;
            }

            _visibleFirst = firstIndex;
            _visibleLast = lastIndex;
            return (firstIndex, lastIndex);
        }

        private (int, int) UpdateVisibleGrouped(GridLayoutEngine layout)
        {
            IReadOnlyList<GridLayoutEngine.LayoutEntry> entries = layout.Entries;
            if (entries.Count == 0 || layout.Columns <= 0)
            {
                return (-1, -1);
            }

            float scrollY = _scrollView.scrollOffset.y;
            float viewHeight = _scrollView.contentViewport.resolvedStyle.height;
            if (float.IsNaN(viewHeight) || viewHeight <= 0f)
            {
                return (-1, -1);
            }

            int firstVisible = -1;
            int lastVisible = -1;
            for (int index = 0; index < entries.Count; index++)
            {
                GridLayoutEngine.LayoutEntry entry = entries[index];
                float bottom = entry.Top + entry.Height;
                if (bottom >= scrollY && entry.Top <= scrollY + viewHeight)
                {
                    if (firstVisible == -1)
                    {
                        firstVisible = index;
                    }

                    lastVisible = index;
                }
                else if (firstVisible != -1)
                {
                    break;
                }
            }

            if (firstVisible == -1)
            {
                return (-1, -1);
            }

            firstVisible = Mathf.Max(0, firstVisible - layout.Columns);
            lastVisible = Mathf.Min(entries.Count - 1, lastVisible + layout.Columns);

            if (firstVisible == _visibleFirst && lastVisible == _visibleLast)
            {
                return (_visibleFirst, _visibleLast);
            }

            RecycleOutOfRange(firstVisible, lastVisible);

            for (int index = firstVisible; index <= lastVisible; index++)
            {
                if (_activeCells.ContainsKey(index))
                {
                    continue;
                }

                GridLayoutEngine.LayoutEntry entry = entries[index];
                if (entry.IsHeader)
                {
                    VisualElement header = GetOrCreateHeader();
                    BindHeader(header, entry);
                    _activeCells[index] = header;
                    continue;
                }

                if (entry.DataIndex < 0 || entry.DataIndex >= _items.Count)
                {
                    continue;
                }

                VisualElement cell = GetOrCreateCell();
                BindCellGrouped(cell, entry);
                _activeCells[index] = cell;
            }

            _visibleFirst = firstVisible;
            _visibleLast = lastVisible;

            int minData = int.MaxValue;
            int maxData = int.MinValue;
            for (int index = firstVisible; index <= lastVisible; index++)
            {
                if (entries[index].IsHeader)
                {
                    continue;
                }

                int dataIndex = entries[index].DataIndex;
                if (dataIndex < 0 || dataIndex >= _items.Count)
                {
                    continue;
                }

                if (dataIndex < minData)
                {
                    minData = dataIndex;
                }

                if (dataIndex > maxData)
                {
                    maxData = dataIndex;
                }
            }

            return minData != int.MaxValue ? (minData, maxData) : (-1, -1);
        }

        private void RecycleOutOfRange(int first, int last)
        {
            _recycleBuffer.Clear();
            foreach ((int key, _) in _activeCells)
            {
                if (key < first || key > last)
                {
                    _recycleBuffer.Add(key);
                }
            }

            for (int index = 0; index < _recycleBuffer.Count; index++)
            {
                int key = _recycleBuffer[index];
                VisualElement element = _activeCells[key];
                element.style.display = DisplayStyle.None;

                if (element.ClassListContains(VirtualizedGridView.UssClassName.HEADER))
                {
                    _headerPool.Add(element);
                }
                else
                {
                    _cellPool.Add(element);
                }

                _activeCells.Remove(key);
            }
        }

        private void ShowCellAction(VisualElement cell, bool hovered)
        {
            if (!_showActions)
            {
                return;
            }

            Button button = cell.Q<Button>(CellActionButtonName);
            if (button == null)
            {
                return;
            }

            if (cell.userData is not int index || index < 0 || index >= _items.Count)
            {
                button.style.display = DisplayStyle.None;
                return;
            }

            GridViewItem item = _items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.ActionText))
            {
                button.style.display = DisplayStyle.None;
                return;
            }

            bool hasSingleSelection = _selectedIndices.Count <= 1;
            bool isSelected = _selectedIndices.Contains(index);
            bool shouldShow = hasSingleSelection && (hovered || isSelected);
            if (!shouldShow)
            {
                button.style.display = DisplayStyle.None;
                return;
            }

            button.text = item.ActionText;
            button.EnableClass(ButtonActionDestructiveModifierClass, item.ActionIsDestructive);
            button.EnableClass(ButtonActionPrimaryModifierClass, !item.ActionIsDestructive);
            button.style.display = DisplayStyle.Flex;
        }

        private void OnActionButtonClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not VisualElement button)
            {
                return;
            }

            if (button.userData is not VisualElement cell)
            {
                return;
            }

            if (cell.userData is not int index || index < 0 || index >= _items.Count)
            {
                return;
            }

            OnItemActionTriggered?.Invoke(_items[index]);
        }

        private void OnCellMouseEnter(MouseEnterEvent evt)
        {
            if (evt.currentTarget is VisualElement cell)
            {
                ShowCellAction(cell, hovered: true);
            }
        }

        private void OnCellMouseLeave(MouseLeaveEvent evt)
        {
            if (evt.currentTarget is VisualElement cell)
            {
                ShowCellAction(cell, hovered: false);
            }
        }
    }
}
