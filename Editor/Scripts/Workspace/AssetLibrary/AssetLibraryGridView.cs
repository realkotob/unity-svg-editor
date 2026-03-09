using System;
using System.Collections.Generic;
using Core.UI.Foundation;
using Core.UI.Foundation.Tooling;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    [UxmlElement]
    public partial class AssetLibraryGridView : VisualElement
    {
        #region Constants
        private static readonly GridLayoutMetrics PREVIEW_GRID_METRICS = new(51f, 58f, 19f);

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
        private readonly VirtualizedGridView _gridView;
        private readonly AssetLibraryGridPreviewRenderer _previewRenderer = new();
        private readonly List<GridViewItem> _gridItems = new();

        private Action<GridViewItem> _itemSelectedHandler;
        private Action<List<GridViewItem>> _selectionChangedHandler;
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
        #endregion Properties

        #region Constructor
        public AssetLibraryGridView()
        {
            _gridView = CreateGridView();
            _gridView.OnItemSelected += OnGridItemSelected;
            _gridView.OnSelectionChanged += OnGridSelectionChanged;
            Add(_gridView);
            RefreshPreviewMode();
        }
        #endregion Constructor

        #region Public Methods
        internal void BindRuntime(Action<GridViewItem> itemSelectedHandler, Action<List<GridViewItem>> selectionChangedHandler)
        {
            _itemSelectedHandler = itemSelectedHandler;
            _selectionChangedHandler = selectionChangedHandler;
            ShowPreview = false;
        }

        internal void UnbindRuntime()
        {
            _itemSelectedHandler = null;
            _selectionChangedHandler = null;
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
        private VirtualizedGridView CreateGridView()
        {
            VirtualizedGridView gridView = new(PREVIEW_GRID_METRICS)
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

            _previewRenderer.ClearPreview(this, _gridView, _gridItems, UssClassName.PREVIEW_ENABLED);
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
