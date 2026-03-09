using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class StructureHierarchyController
    {
        private readonly StructureHierarchyInteractionController _interactionController;
        private readonly StructureHierarchyViewController _viewController;

        private TreeView _treeView;
        private IStructureHierarchyHost _host;
        private Action<StructureNode> _selectionChangedHandler;

        public StructureHierarchyController(StructureEditor structureEditor)
        {
            _interactionController = new StructureHierarchyInteractionController(structureEditor);
            _viewController = new StructureHierarchyViewController(_interactionController);
        }

        public void Bind(TreeView treeView, IStructureHierarchyHost host, Action<StructureNode> selectionChangedHandler)
        {
            Unbind();
            _treeView = treeView;
            _host = host;
            _selectionChangedHandler = selectionChangedHandler;
            if (_treeView == null)
                return;

            _treeView.selectionType = SelectionType.Single;
            _treeView.fixedItemHeight = 28f;
            _treeView.reorderable = false;
            _treeView.viewDataKey = "svg-editor-asset-hierarchy-tree";
            _treeView.makeItem = _viewController.CreateHierarchyItemElement;
            _treeView.bindItem = _viewController.BindHierarchyItem;
            _treeView.unbindItem = _viewController.UnbindHierarchyItem;
            _treeView.selectedIndicesChanged += OnSelectedIndicesChanged;

            _viewController.Bind(_treeView, _host);
            _interactionController.Bind(_treeView, _host);
        }

        public void Unbind()
        {
            _interactionController.Unbind();
            _viewController.Unbind();

            if (_treeView != null)
                _treeView.selectedIndicesChanged -= OnSelectedIndicesChanged;

            _host = null;
            _treeView = null;
            _selectionChangedHandler = null;
        }

        public void SetItems(IReadOnlyList<TreeViewItemData<StructureNode>> items)
        {
            if (_treeView == null)
                return;

            _treeView.SetRootItems(items.ToList());
            _treeView.Rebuild();
        }

        public void SetEnabled(bool enabled)
        {
            _treeView?.SetEnabled(enabled);
        }

        public void ClearSelection()
        {
            _treeView?.ClearSelection();
        }

        public void SelectElementByKey(string elementKey, IReadOnlyList<TreeViewItemData<StructureNode>> items)
        {
            StructureHierarchyTreeUtility.SelectElementByKey(_treeView, elementKey, items);
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> selectedIndices)
        {
            _selectionChangedHandler?.Invoke(ResolveSelectedNode(selectedIndices));
        }

        private StructureNode ResolveSelectedNode(IEnumerable<int> selectedIndices)
        {
            if (_treeView == null || selectedIndices == null)
                return null;

            foreach (var index in selectedIndices)
            {
                if (index < 0)
                    continue;

                return _treeView.GetItemDataForIndex<StructureNode>(index);
            }

            return null;
        }
    }
}
