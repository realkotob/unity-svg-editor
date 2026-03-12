using System;
using System.Collections.Generic;
using Core.UI.Foundation;
using Core.UI.Foundation.Tooling;
using UnityEngine.UIElements;

namespace SvgEditor
{
    [UxmlElement]
    public partial class AssetLibraryGridView : VisualElement
    {
        #region Constants
        public static readonly GridLayoutMetrics DefaultGridMetrics = new(51f, 58f, 19f);

        private static class UssClassName
        {
            public const string BASE = "svg-editor__asset-grid";
            private const string ELEMENT_PREFIX = BASE + "__";
            private const string MODIFIER_PREFIX = BASE + "--";

            public const string GRID = ELEMENT_PREFIX + "grid";
            public const string PREVIEW_ENABLED = MODIFIER_PREFIX + "preview-enabled";
        }

        private static class ToolingUssClassName
        {
            public const string COMPACT = "tooling-grid--compact";
        }
        #endregion Constants

        #region Variables
        private VirtualizedGridView _gridView;
        private readonly AssetLibraryGridPreviewRenderer _previewRenderer = new();
        private readonly List<GridViewItem> _gridItems = new();

        private Action<GridViewItem> _itemSelectedHandler;
        private Action<List<GridViewItem>> _selectionChangedHandler;
        private GridLayoutMetrics _gridMetrics = DefaultGridMetrics;
        private bool _isRuntimeBound;
        private bool _showPreview;
        #endregion Variables

        #region Properties
        [UxmlAttribute("show-preview")]
        public bool ShowPreview
        {
            get => _showPreview;
            set
            {
                if (_showPreview == value)
                {
                    return;
                }

                _showPreview = value;
                RefreshPreviewMode();
            }
        }

        internal GridLayoutMetrics GridMetrics => _gridMetrics;
        #endregion Properties

        #region Constructor
        public AssetLibraryGridView()
        {
            RebuildGridView();
        }
        #endregion Constructor

        #region Public Methods
        internal void SetGridMetrics(GridLayoutMetrics gridMetrics)
        {
            if (_gridMetrics.Equals(gridMetrics))
            {
                return;
            }

            _gridMetrics = gridMetrics;
            RebuildGridView();
        }

        internal void BindRuntime(Action<GridViewItem> itemSelectedHandler, Action<List<GridViewItem>> selectionChangedHandler)
        {
            _itemSelectedHandler = itemSelectedHandler;
            _selectionChangedHandler = selectionChangedHandler;
            _isRuntimeBound = true;
            ShowPreview = false;
        }

        internal void UnbindRuntime()
        {
            _itemSelectedHandler = null;
            _selectionChangedHandler = null;
            _isRuntimeBound = false;
        }

        internal void SetItems(IReadOnlyList<GridViewItem> gridItems)
        {
            _gridItems.Clear();
            if (gridItems != null)
            {
                _gridItems.AddRange(gridItems);
            }

            _gridView.SetItems(_gridItems);
        }

        internal void SelectById(string itemId)
        {
            _gridView.SelectById(itemId);
        }

        internal void ClearSelection()
        {
            _gridView.ClearSelection();
        }

        internal void SetEmptyText(string emptyText)
        {
            _gridView.EmptyText = emptyText;
        }
        #endregion Public Methods

        #region Help Methods
        private void RebuildGridView()
        {
            string emptyText = _gridView?.EmptyText ?? "No SVG assets found";
            if (_gridView != null)
            {
                _gridView.OnItemSelected -= OnGridItemSelected;
                _gridView.OnSelectionChanged -= OnGridSelectionChanged;
                _gridView.RemoveFromHierarchy();
            }

            _gridView = CreateGridView(_gridMetrics);
            _gridView.EmptyText = emptyText;
            _gridView.OnItemSelected += OnGridItemSelected;
            _gridView.OnSelectionChanged += OnGridSelectionChanged;
            Add(_gridView);
            RefreshPreviewMode();
        }

        private VirtualizedGridView CreateGridView(GridLayoutMetrics gridMetrics)
        {
            VirtualizedGridView gridView = new(gridMetrics)
            {
                GroupByAlpha = true,
                ShowActionButtons = false
            };
            gridView.AddClass(UssClassName.GRID).AddClass(ToolingUssClassName.COMPACT);
            if (gridView.Q<ScrollView>() is { } scrollView)
            {
                scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }
            return gridView;
        }

        private void RefreshPreviewMode()
        {
            if (_showPreview)
            {
                _previewRenderer.ApplyPreview(this, _gridView, _gridItems, UssClassName.PREVIEW_ENABLED);
                return;
            }

            _gridView.SetItems(_isRuntimeBound ? _gridItems : null);
            EnableInClassList(UssClassName.PREVIEW_ENABLED, false);
        }

        private void OnGridItemSelected(GridViewItem selectedItem)
        {
            _itemSelectedHandler?.Invoke(selectedItem);
        }

        private void OnGridSelectionChanged(List<GridViewItem> selectedItems)
        {
            _selectionChangedHandler?.Invoke(selectedItems);
        }
        #endregion Help Methods
    }
}
