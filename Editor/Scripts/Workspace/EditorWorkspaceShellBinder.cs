using System;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class EditorWorkspaceShellBinder
    {
        public AssetHierarchyListView HierarchyListView { get; private set; }

        public bool IsBound => HierarchyListView != null;

        public void Bind(
            VisualElement root,
            StructureHierarchyController hierarchyController,
            IStructureHierarchyHost hierarchyHost,
            Action<StructureNode> onSelectionChanged)
        {
            Unbind(hierarchyController);
            if (root == null)
                return;

            HierarchyListView = root.Q<AssetHierarchyListView>("asset-hierarchy-list");

            hierarchyController.Bind(HierarchyListView, hierarchyHost, onSelectionChanged);
        }

        public void Unbind(StructureHierarchyController hierarchyController)
        {
            hierarchyController?.Unbind();
            HierarchyListView = null;
        }
    }
}
