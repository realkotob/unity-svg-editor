using System.Collections.Generic;
using UnityEngine;

namespace SvgEditor.UI.AssetLibrary.Grid
{
    internal sealed class GridLayoutEngine
    {
        private readonly List<LayoutEntry> _entries = new();
        private readonly HashSet<int> _hitTestBuffer = new();
        private readonly GridLayoutMetrics _metrics;

        private int _columns;
        private bool _isGrouped;
        private int _itemCount;
        private float _viewportWidth;

        public GridLayoutEngine(GridLayoutMetrics metrics)
        {
            _metrics = metrics;
        }

        public IReadOnlyList<LayoutEntry> Entries => _entries;
        public int Columns => _columns;
        public float TotalHeight { get; private set; }
        public float TotalWidth { get; private set; }

        public readonly struct LayoutEntry
        {
            public LayoutEntry(bool isHeader, int dataIndex, string headerText, float left, float top, float width, float height)
            {
                IsHeader = isHeader;
                DataIndex = dataIndex;
                HeaderText = headerText;
                Left = left;
                Top = top;
                Width = width;
                Height = height;
            }

            public bool IsHeader { get; }
            public int DataIndex { get; }
            public string HeaderText { get; }
            public float Left { get; }
            public float Top { get; }
            public float Width { get; }
            public float Height { get; }
        }

        public void Compute(int itemCount, bool isGrouped, float viewportWidth, IReadOnlyList<string> groupKeys = null)
        {
            _itemCount = itemCount;
            _isGrouped = isGrouped;
            _viewportWidth = viewportWidth;
            _columns = Mathf.Max(1, Mathf.FloorToInt(viewportWidth / _metrics.CellWidth));

            if (_isGrouped)
            {
                ComputeGrouped(groupKeys);
                return;
            }

            ComputeFlat();
        }

        public int DataIndexAtContent(float contentX, float contentY)
        {
            if (float.IsNaN(contentX) || float.IsInfinity(contentX) ||
                float.IsNaN(contentY) || float.IsInfinity(contentY))
            {
                return -1;
            }

            if (_isGrouped)
            {
                for (int index = 0; index < _entries.Count; index++)
                {
                    LayoutEntry entry = _entries[index];
                    if (entry.IsHeader)
                    {
                        continue;
                    }

                    if (contentX >= entry.Left && contentX < entry.Left + entry.Width &&
                        contentY >= entry.Top && contentY < entry.Top + entry.Height)
                    {
                        return entry.DataIndex;
                    }
                }

                return -1;
            }

            if (_columns <= 0)
            {
                return -1;
            }

            int row = Mathf.FloorToInt(contentY / _metrics.CellHeight);
            int col = Mathf.FloorToInt(contentX / _metrics.CellWidth);
            if (col < 0 || col >= _columns || row < 0)
            {
                return -1;
            }

            int itemIndex = (row * _columns) + col;
            return itemIndex >= 0 && itemIndex < _itemCount ? itemIndex : -1;
        }

        public HashSet<int> HitTestRect(Rect dragRect)
        {
            _hitTestBuffer.Clear();

            if (_isGrouped)
            {
                for (int index = 0; index < _entries.Count; index++)
                {
                    LayoutEntry entry = _entries[index];
                    if (entry.IsHeader)
                    {
                        continue;
                    }

                    Rect cellRect = new(entry.Left, entry.Top, entry.Width, entry.Height);
                    if (cellRect.Overlaps(dragRect))
                    {
                        _hitTestBuffer.Add(entry.DataIndex);
                    }
                }

                return _hitTestBuffer;
            }

            if (_columns <= 0)
            {
                return _hitTestBuffer;
            }

            int firstRow = Mathf.Max(0, Mathf.FloorToInt(dragRect.y / _metrics.CellHeight));
            int lastRow = Mathf.FloorToInt((dragRect.y + dragRect.height) / _metrics.CellHeight);
            int firstCol = Mathf.Max(0, Mathf.FloorToInt(dragRect.x / _metrics.CellWidth));
            int lastCol = Mathf.Min(_columns - 1, Mathf.FloorToInt((dragRect.x + dragRect.width) / _metrics.CellWidth));

            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int col = firstCol; col <= lastCol; col++)
                {
                    int itemIndex = (row * _columns) + col;
                    if (itemIndex < 0 || itemIndex >= _itemCount)
                    {
                        continue;
                    }

                    Rect cellRect = new(
                        col * _metrics.CellWidth,
                        row * _metrics.CellHeight,
                        _metrics.CellWidth,
                        _metrics.CellHeight);
                    if (cellRect.Overlaps(dragRect))
                    {
                        _hitTestBuffer.Add(itemIndex);
                    }
                }
            }

            return _hitTestBuffer;
        }

        private void ComputeFlat()
        {
            _entries.Clear();
            int totalRows = Mathf.CeilToInt((float)_itemCount / _columns);
            TotalHeight = totalRows * _metrics.CellHeight;
            TotalWidth = _columns * _metrics.CellWidth;
        }

        private void ComputeGrouped(IReadOnlyList<string> groupKeys)
        {
            _entries.Clear();

            float y = 0f;
            int col = 0;
            string currentHeader = null;

            for (int index = 0; index < _itemCount; index++)
            {
                string nextHeader = ResolveGroupKey(groupKeys, index);
                if (!string.Equals(nextHeader, currentHeader))
                {
                    if (col > 0)
                    {
                        y += _metrics.CellHeight;
                        col = 0;
                    }

                    _entries.Add(new LayoutEntry(
                        isHeader: true,
                        dataIndex: -1,
                        headerText: nextHeader,
                        left: 0f,
                        top: y,
                        width: _viewportWidth,
                        height: _metrics.HeaderHeight));
                    y += _metrics.HeaderHeight;
                    currentHeader = nextHeader;
                }

                _entries.Add(new LayoutEntry(
                    isHeader: false,
                    dataIndex: index,
                    headerText: string.Empty,
                    left: col * _metrics.CellWidth,
                    top: y,
                    width: _metrics.CellWidth,
                    height: _metrics.CellHeight));

                col++;
                if (col >= _columns)
                {
                    col = 0;
                    y += _metrics.CellHeight;
                }
            }

            if (col > 0)
            {
                y += _metrics.CellHeight;
            }

            TotalHeight = y;
            TotalWidth = _columns * _metrics.CellWidth;
        }

        private static string ResolveGroupKey(IReadOnlyList<string> groupKeys, int index)
        {
            if (groupKeys == null || index < 0 || index >= groupKeys.Count || string.IsNullOrWhiteSpace(groupKeys[index]))
            {
                return "#";
            }

            return groupKeys[index];
        }
    }
}
