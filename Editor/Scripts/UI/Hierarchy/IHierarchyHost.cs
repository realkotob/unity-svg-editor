using System.Collections.Generic;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Source;
using Core.UI.Extensions;

namespace SvgEditor.UI.Hierarchy
{
    internal interface IHierarchyHost
    {
        DocumentSession CurrentDocument { get; }
        IReadOnlyList<TreeViewItemData<HierarchyNode>> HierarchyItems { get; }

        HierarchyNode FindHierarchyNode(string elementKey);
        void ApplyUpdatedSource(string updatedSource, string successStatus);
        void UpdateSourceStatus(string status);
    }
}
