using System;
using UnityEngine.UIElements;
using SvgEditor.Workspace.StructureInspector;

namespace SvgEditor.Workspace
{
    internal sealed class EditorWorkspaceShellBinder
    {
        public AssetHierarchyListView HierarchyListView { get; private set; }

        public bool IsBound => HierarchyListView != null;

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            HierarchyListView = root.Q<AssetHierarchyListView>("asset-hierarchy-list");
        }

        public void Unbind()
        {
            HierarchyListView = null;
        }
    }
}
