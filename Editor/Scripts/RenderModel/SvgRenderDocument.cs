using System;
using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgRenderDocument
    {
        public SvgNodeId RootNodeId { get; set; } = SvgNodeId.Root;
        public IReadOnlyDictionary<SvgNodeId, SvgRenderNode> Nodes { get; set; } =
            new Dictionary<SvgNodeId, SvgRenderNode>();
        public IReadOnlyList<SvgNodeId> DrawOrder { get; set; } = Array.Empty<SvgNodeId>();

        public bool TryGetNode(SvgNodeId nodeId, out SvgRenderNode node)
        {
            node = null;
            return Nodes != null && Nodes.TryGetValue(nodeId, out node);
        }
    }
}
