using System;
using UnityEngine.UIElements;
using SvgEditor.Workspace.HierarchyPanel;

namespace SvgEditor.Workspace
{
    internal sealed class WorkspaceShellBinder
    {
        public HierarchyListView HierarchyListView { get; private set; }

        public bool IsBound => HierarchyListView != null;

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            HierarchyListView = root.Q<HierarchyListView>("asset-hierarchy-list");
        }

        public void Unbind()
        {
            HierarchyListView = null;
        }
    }
}
