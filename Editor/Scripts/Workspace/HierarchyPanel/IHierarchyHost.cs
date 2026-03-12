using System.Collections.Generic;
using UnityEngine.UIElements;
using SvgEditor.Document;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal interface IHierarchyHost
    {
        DocumentSession CurrentDocument { get; }
        IReadOnlyList<TreeViewItemData<StructureNode>> HierarchyItems { get; }

        StructureNode FindStructureNode(string elementKey);
        void ApplyUpdatedSource(string updatedSource, string successStatus);
        void UpdateSourceStatus(string status);
    }
}
