using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Core.UI.Foundation;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;

namespace SvgEditor.Workspace.HierarchyPanel
{
    [UxmlElement]
    public partial class HierarchyListView : VisualElement
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
        private readonly HierarchyPreviewRenderer _previewRenderer = new();
        private readonly List<TreeViewItemData<HierarchyNode>> _hierarchyItems = new();

        private HierarchyInteractionController _interactionController;
        private Action<IReadOnlyList<HierarchyNode>, HierarchyNode> _selectionChangedHandler;
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
        public HierarchyListView()
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
            IHierarchyHost host,
            HierarchyInteractionController interactionController,
            Action<IReadOnlyList<HierarchyNode>, HierarchyNode> selectionChangedHandler)
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

        internal void SetHierarchyItems(IReadOnlyList<TreeViewItemData<HierarchyNode>> hierarchyItems)
        {
            _hierarchyItems.Clear();
            if (hierarchyItems != null)
            {
                _hierarchyItems.AddRange(hierarchyItems);
            }

            _hierarchyTreeView.SetRootItems<HierarchyNode>(_hierarchyItems);
            _hierarchyTreeView.Rebuild();
        }

        internal void ClearHierarchySelection()
        {
            _hierarchyTreeView.ClearSelection();
        }

        internal void SelectElementByKey(string elementKey)
        {
            SelectElementsByKey(
                string.IsNullOrWhiteSpace(elementKey) ? Array.Empty<string>() : new[] { elementKey },
                elementKey,
                elementKey);
        }

        internal void SelectElementsByKey(
            IReadOnlyList<string> elementKeys,
            string primaryElementKey,
            string selectionAnchorElementKey)
        {
            _interactionController?.SetSelectionState(primaryElementKey, selectionAnchorElementKey);
            HierarchyTreeUtility.SelectElementsByKey(_hierarchyTreeView, elementKeys, _hierarchyItems, primaryElementKey);
        }
        #endregion Internal Methods

        #region Help Methods
        private static TreeView CreateHierarchyTreeView()
        {
            TreeView hierarchyTreeView = new()
            {
                selectionType = SelectionType.Multiple,
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
            HierarchyTreeRow hierarchyItemRow = new();
            hierarchyItemRow.RegisterCallback<PointerDownEvent>(OnHierarchyRowPointerDown, TrickleDown.TrickleDown);
            hierarchyItemRow.RegisterCallback<ClickEvent>(OnHierarchyRowClicked);
            hierarchyItemRow.Expander.RegisterCallback<PointerDownEvent>(OnHierarchyExpanderPointerDown, TrickleDown.TrickleDown);
            return hierarchyItemRow;
        }

        private void BindHierarchyItem(VisualElement hierarchyItemRow, int index)
        {
            if (hierarchyItemRow is not HierarchyTreeRow row)
            {
                return;
            }

            if (!TryGetHierarchyNode(index, out HierarchyNode hierarchyNode))
            {
                return;
            }

            bool hasChildren = HierarchyTreeUtility.TryFindHierarchyItem(
                hierarchyNode.Key,
                _hierarchyItems,
                out TreeViewItemData<HierarchyNode> hierarchyItem) &&
                hierarchyItem.hasChildren;
            bool isExpanded =
                hasChildren &&
                HierarchyTreeUtility.TryFindHierarchyItemId(hierarchyNode.Key, _hierarchyItems, out int hierarchyItemId) &&
                _hierarchyTreeView.IsExpanded(hierarchyItemId);

            row.Bind(hierarchyNode, hasChildren, isExpanded);
        }

        private static void UnbindHierarchyItem(VisualElement hierarchyItemRow, int index)
        {
            if (hierarchyItemRow is not HierarchyTreeRow row)
            {
                return;
            }

            row.Unbind();
        }

        private bool TryGetHierarchyNode(int index, out HierarchyNode hierarchyNode)
        {
            try
            {
                hierarchyNode = _hierarchyTreeView.GetItemDataForIndex<HierarchyNode>(index);
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
            IReadOnlyList<HierarchyNode> selectedNodes = ResolveSelectedNodes(selectedIndices);
            _selectionChangedHandler?.Invoke(selectedNodes, ResolvePrimarySelectedNode(selectedNodes));
        }

        private IReadOnlyList<HierarchyNode> ResolveSelectedNodes(IEnumerable<int> selectedIndices)
        {
            if (selectedIndices == null)
            {
                return Array.Empty<HierarchyNode>();
            }

            List<HierarchyNode> selectedNodes = new();
            foreach (int index in selectedIndices)
            {
                if (index < 0)
                {
                    continue;
                }

                HierarchyNode selectedNode = _hierarchyTreeView.GetItemDataForIndex<HierarchyNode>(index);
                if (selectedNode != null)
                {
                    selectedNodes.Add(selectedNode);
                }
            }

            return selectedNodes;
        }

        private HierarchyNode ResolvePrimarySelectedNode(IReadOnlyList<HierarchyNode> selectedNodes)
        {
            if (selectedNodes == null || selectedNodes.Count == 0)
            {
                return null;
            }

            string preferredPrimaryElementKey = _interactionController?.PreferredPrimaryElementKey;
            if (!string.IsNullOrWhiteSpace(preferredPrimaryElementKey))
            {
                foreach (HierarchyNode selectedNode in selectedNodes)
                {
                    if (string.Equals(selectedNode.Key, preferredPrimaryElementKey, StringComparison.Ordinal))
                    {
                        return selectedNode;
                    }
                }
            }

            return selectedNodes[selectedNodes.Count - 1];
        }

        private void ToggleHierarchyItemExpansion(string elementKey)
        {
            if (!HierarchyTreeUtility.TryFindHierarchyItemId(elementKey, _hierarchyItems, out int hierarchyItemId))
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

            hierarchyItemRow.GetFirstAncestorOfType<HierarchyListView>()?._interactionController?.OnHierarchyRowPointerDown(evt);
        }

        private static void OnHierarchyExpanderPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement hierarchyExpander)
            {
                return;
            }

            HierarchyListView hierarchyListView = hierarchyExpander.GetFirstAncestorOfType<HierarchyListView>();
            hierarchyListView?._interactionController?.CancelPendingPress();
            if (hierarchyExpander.userData is not string elementKey)
            {
                evt.StopImmediatePropagation();
                return;
            }

            hierarchyListView?.ToggleHierarchyItemExpansion(elementKey);
            evt.StopImmediatePropagation();
        }

        private static void OnHierarchyRowClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not HierarchyTreeRow hierarchyItemRow)
            {
                return;
            }

            HierarchyListView hierarchyListView = hierarchyItemRow.GetFirstAncestorOfType<HierarchyListView>();
            if (hierarchyListView == null)
            {
                return;
            }

            if (hierarchyListView._interactionController?.ConsumeSuppressedRowClick() == true)
            {
                evt.StopPropagation();
            }
        }
        #endregion Help Methods
    }
}
