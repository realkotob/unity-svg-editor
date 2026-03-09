using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Core.UI.Foundation;

namespace UnitySvgEditor.Editor
{
    [UxmlElement]
    public partial class AssetHierarchyListView : VisualElement
    {
        #region Constants
        internal static class Layout
        {
            public const float HIERARCHY_ITEM_HEIGHT = 28f;
            public const float ROW_PADDING_LEFT = 6f;
            public const float ROW_DEPTH_OFFSET = 20f;
        }

        private static class ViewDataKey
        {
            public const string TREE = "svg-editor-asset-hierarchy-tree";
        }

        internal static class UssClassName
        {
            public const string BASE = "svg-editor__asset-hierarchy";
            private const string ELEMENT_PREFIX = BASE + "__";
            private const string MODIFIER_PREFIX = BASE + "--";

            public const string TREE = ELEMENT_PREFIX + "tree";
            public const string PREVIEW_ENABLED = MODIFIER_PREFIX + "preview-enabled";
        }
        #endregion Constants

        #region Variables
        private readonly TreeView _hierarchyTreeView;
        private readonly AssetHierarchyPreviewRenderer _previewRenderer = new();
        private readonly List<TreeViewItemData<StructureNode>> _hierarchyItems = new();

        private StructureHierarchyInteractionController _interactionController;
        private Action<StructureNode> _selectionChangedHandler;
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
        public AssetHierarchyListView()
        {
            _hierarchyTreeView = CreateHierarchyTreeView();
            _hierarchyTreeView.makeItem = CreateHierarchyItemElement;
            _hierarchyTreeView.bindItem = BindHierarchyItem;
            _hierarchyTreeView.unbindItem = UnbindHierarchyItem;
            _hierarchyTreeView.selectedIndicesChanged += OnSelectedIndicesChanged;
            Add(_hierarchyTreeView);
            RefreshPreviewMode();
        }
        #endregion Constructor

        #region Internal Methods
        internal void BindRuntime(
            IStructureHierarchyHost host,
            StructureHierarchyInteractionController interactionController,
            Action<StructureNode> selectionChangedHandler)
        {
            _selectionChangedHandler = selectionChangedHandler;
            _interactionController = interactionController;
            ShowPreview = false;
            _interactionController?.Bind(_hierarchyTreeView, host);
        }

        internal void UnbindRuntime()
        {
            _interactionController?.Unbind();
            _interactionController = null;
            _selectionChangedHandler = null;
        }

        internal void SetHierarchyItems(IReadOnlyList<TreeViewItemData<StructureNode>> hierarchyItems)
        {
            _hierarchyItems.Clear();
            if (hierarchyItems != null)
            {
                _hierarchyItems.AddRange(hierarchyItems);
            }

            _hierarchyTreeView.SetRootItems<StructureNode>(_hierarchyItems);
            _hierarchyTreeView.Rebuild();
        }

        internal void ClearHierarchySelection()
        {
            _hierarchyTreeView.ClearSelection();
        }

        internal void SelectElementByKey(string elementKey)
        {
            StructureHierarchyTreeUtility.SelectElementByKey(_hierarchyTreeView, elementKey, _hierarchyItems);
        }
        #endregion Internal Methods

        #region Help Methods
        private static TreeView CreateHierarchyTreeView()
        {
            TreeView hierarchyTreeView = new()
            {
                selectionType = SelectionType.Single,
                fixedItemHeight = Layout.HIERARCHY_ITEM_HEIGHT,
                reorderable = false,
                viewDataKey = ViewDataKey.TREE
            };
            hierarchyTreeView.AddClass(UssClassName.TREE);
            if (hierarchyTreeView.Q<ScrollView>() is { } scrollView)
            {
                scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }
            return hierarchyTreeView;
        }

        private void RefreshPreviewMode()
        {
            if (_showPreview)
            {
                _previewRenderer.ApplyPreview(this, _hierarchyTreeView, _hierarchyItems, UssClassName.PREVIEW_ENABLED);
                return;
            }

            _previewRenderer.ClearPreview(this, _hierarchyTreeView, _hierarchyItems, UssClassName.PREVIEW_ENABLED);
        }

        private static VisualElement CreateHierarchyItemElement()
        {
            AssetHierarchyTreeRow hierarchyItemRow = new();
            hierarchyItemRow.RegisterCallback<PointerDownEvent>(OnHierarchyRowPointerDown, TrickleDown.TrickleDown);
            hierarchyItemRow.RegisterCallback<ClickEvent>(OnHierarchyRowClicked);
            hierarchyItemRow.Expander.RegisterCallback<PointerDownEvent>(OnHierarchyExpanderPointerDown, TrickleDown.TrickleDown);
            hierarchyItemRow.Expander.RegisterCallback<ClickEvent>(OnHierarchyExpanderClicked);
            return hierarchyItemRow;
        }

        private void BindHierarchyItem(VisualElement hierarchyItemRow, int index)
        {
            if (hierarchyItemRow is not AssetHierarchyTreeRow row)
            {
                return;
            }

            if (!TryGetHierarchyNode(index, out StructureNode hierarchyNode))
            {
                return;
            }

            bool hasChildren = StructureHierarchyTreeUtility.TryFindHierarchyItem(
                hierarchyNode.Key,
                _hierarchyItems,
                out TreeViewItemData<StructureNode> hierarchyItem) &&
                hierarchyItem.hasChildren;
            bool isExpanded =
                hasChildren &&
                StructureHierarchyTreeUtility.TryFindHierarchyItemId(hierarchyNode.Key, _hierarchyItems, out int hierarchyItemId) &&
                _hierarchyTreeView.IsExpanded(hierarchyItemId);

            row.Bind(hierarchyNode, hasChildren, isExpanded);
        }

        private static void UnbindHierarchyItem(VisualElement hierarchyItemRow, int index)
        {
            if (hierarchyItemRow is not AssetHierarchyTreeRow row)
            {
                return;
            }

            row.Unbind();
        }

        private bool TryGetHierarchyNode(int index, out StructureNode hierarchyNode)
        {
            try
            {
                hierarchyNode = _hierarchyTreeView.GetItemDataForIndex<StructureNode>(index);
                return true;
            }
            catch
            {
                hierarchyNode = null;
                return false;
            }
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> selectedIndices)
        {
            _selectionChangedHandler?.Invoke(ResolveSelectedNode(selectedIndices));
        }

        private StructureNode ResolveSelectedNode(IEnumerable<int> selectedIndices)
        {
            if (selectedIndices == null)
            {
                return null;
            }

            foreach (int index in selectedIndices)
            {
                if (index < 0)
                {
                    continue;
                }

                return _hierarchyTreeView.GetItemDataForIndex<StructureNode>(index);
            }

            return null;
        }

        private void ToggleHierarchyItemExpansion(string elementKey)
        {
            if (!StructureHierarchyTreeUtility.TryFindHierarchyItemId(elementKey, _hierarchyItems, out int hierarchyItemId))
            {
                return;
            }

            if (_hierarchyTreeView.IsExpanded(hierarchyItemId))
            {
                _hierarchyTreeView.CollapseItem(hierarchyItemId, false);
            }
            else
            {
                _hierarchyTreeView.ExpandItem(hierarchyItemId, false);
            }

            _hierarchyTreeView.Rebuild();
        }

        private static void OnHierarchyRowPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement hierarchyItemRow)
            {
                return;
            }

            hierarchyItemRow.GetFirstAncestorOfType<AssetHierarchyListView>()?._interactionController?.OnHierarchyRowPointerDown(evt);
        }

        private static void OnHierarchyExpanderPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement hierarchyExpander)
            {
                return;
            }

            hierarchyExpander.GetFirstAncestorOfType<AssetHierarchyListView>()?._interactionController?.CancelPendingPress();
            evt.StopPropagation();
        }

        private static void OnHierarchyExpanderClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not VisualElement hierarchyExpander || hierarchyExpander.userData is not string elementKey)
            {
                return;
            }

            hierarchyExpander.GetFirstAncestorOfType<AssetHierarchyListView>()?.ToggleHierarchyItemExpansion(elementKey);
            evt.StopPropagation();
        }

        private static void OnHierarchyRowClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not AssetHierarchyTreeRow hierarchyItemRow || hierarchyItemRow.ElementKey is not string elementKey)
            {
                return;
            }

            AssetHierarchyListView hierarchyListView = hierarchyItemRow.GetFirstAncestorOfType<AssetHierarchyListView>();
            if (hierarchyListView == null)
            {
                return;
            }

            if (hierarchyListView._interactionController?.TryConsumeSuppressedRowClick() == true)
            {
                evt.StopPropagation();
                return;
            }

            if (!hierarchyItemRow.HasExpandableChildren)
            {
                return;
            }

            hierarchyListView.ToggleHierarchyItemExpansion(elementKey);
        }
        #endregion Help Methods
    }
}
