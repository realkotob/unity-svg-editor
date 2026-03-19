using UnityEngine.UIElements;
using SvgEditor.UI.Hierarchy;
using Core.UI.Extensions;

namespace SvgEditor.UI.Workspace.Coordination
{
    internal sealed class WorkspaceViewBinder
    {
        private static class ElementName
        {
            public const string HIERARCHY_LIST = "asset-hierarchy-list";
        }

        public HierarchyListView HierarchyListView { get; private set; }

        public bool IsBound => HierarchyListView != null;

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            HierarchyListView = root.Q<HierarchyListView>(ElementName.HIERARCHY_LIST);
        }

        public void Unbind()
        {
            HierarchyListView = null;
        }
    }
}
