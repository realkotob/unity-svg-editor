using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SvgEditor
{
    internal sealed class StructureOutline
    {
        public IReadOnlyList<StructureNode> Elements { get; set; } = Array.Empty<StructureNode>();
        public IReadOnlyList<LayerSummary> Layers { get; set; } = Array.Empty<LayerSummary>();
        public IReadOnlyList<TreeViewItemData<StructureNode>> HierarchyItems { get; set; } = Array.Empty<TreeViewItemData<StructureNode>>();
    }
}
