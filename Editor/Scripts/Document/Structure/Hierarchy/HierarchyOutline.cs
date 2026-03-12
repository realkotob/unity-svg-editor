using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SvgEditor.Document
{
    internal sealed class HierarchyOutline
    {
        public IReadOnlyList<HierarchyNode> Elements { get; set; } = Array.Empty<HierarchyNode>();
        public IReadOnlyList<LayerSummary> Layers { get; set; } = Array.Empty<LayerSummary>();
        public IReadOnlyList<TreeViewItemData<HierarchyNode>> HierarchyItems { get; set; } = Array.Empty<TreeViewItemData<HierarchyNode>>();
    }
}
