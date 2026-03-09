using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class StructureHierarchyController
    {
        private readonly StructureHierarchyInteractionController _interactionController;

        private AssetHierarchyListView _hierarchyListView;

        public StructureHierarchyController(StructureEditor structureEditor)
        {
            _interactionController = new StructureHierarchyInteractionController(structureEditor);
        }

        public void Bind(AssetHierarchyListView hierarchyListView, IStructureHierarchyHost host, Action<StructureNode> selectionChangedHandler)
        {
            Unbind();
            _hierarchyListView = hierarchyListView;
            if (_hierarchyListView == null)
                return;

            _hierarchyListView.BindRuntime(host, _interactionController, selectionChangedHandler);
        }

        public void Unbind()
        {
            _hierarchyListView?.UnbindRuntime();
            _hierarchyListView = null;
        }

        public void SetItems(IReadOnlyList<TreeViewItemData<StructureNode>> items)
        {
            _hierarchyListView?.SetHierarchyItems(items);
        }

        public void SetEnabled(bool enabled)
        {
            _hierarchyListView?.SetEnabled(enabled);
        }

        public void ClearSelection()
        {
            _hierarchyListView?.ClearHierarchySelection();
        }

        public void SelectElementByKey(string elementKey, IReadOnlyList<TreeViewItemData<StructureNode>> items)
        {
            _hierarchyListView?.SelectElementByKey(elementKey);
        }
    }
}
