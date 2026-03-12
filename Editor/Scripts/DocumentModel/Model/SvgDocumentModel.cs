using System;
using System.Collections.Generic;

namespace SvgEditor
{
    internal sealed class SvgDocumentModel
    {
        public string SourceText { get; set; } = string.Empty;
        public SvgNodeId RootId { get; set; } = SvgNodeId.Root;
        public IReadOnlyDictionary<SvgNodeId, SvgNodeModel> Nodes { get; set; } =
            new Dictionary<SvgNodeId, SvgNodeModel>();
        public IReadOnlyList<SvgNodeId> NodeOrder { get; set; } = Array.Empty<SvgNodeId>();
        public IReadOnlyDictionary<string, SvgNodeId> NodeIdsByXmlId { get; set; } =
            new Dictionary<string, SvgNodeId>(StringComparer.Ordinal);
        public IReadOnlyDictionary<string, string> Namespaces { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public IReadOnlyList<SvgNodeId> DefinitionNodeIds { get; set; } = Array.Empty<SvgNodeId>();

        public SvgNodeModel Root => TryGetNode(RootId, out var root) ? root : null;

        public string Width => GetRootAttribute(SvgAttributeName.WIDTH);

        public string Height => GetRootAttribute(SvgAttributeName.HEIGHT);

        public string ViewBox => GetRootAttribute(SvgAttributeName.VIEW_BOX);

        public string PreserveAspectRatio => GetRootAttribute(SvgAttributeName.PRESERVE_ASPECT_RATIO);

        public bool TryGetNode(SvgNodeId nodeId, out SvgNodeModel node)
        {
            node = null;
            return Nodes != null && Nodes.TryGetValue(nodeId, out node);
        }

        public bool TryGetNodeByXmlId(string xmlId, out SvgNodeModel node)
        {
            node = null;
            if (string.IsNullOrWhiteSpace(xmlId) ||
                NodeIdsByXmlId == null ||
                !NodeIdsByXmlId.TryGetValue(xmlId.Trim(), out var nodeId))
            {
                return false;
            }

            return TryGetNode(nodeId, out node);
        }

        private string GetRootAttribute(string attributeName)
        {
            if (Root?.RawAttributes == null ||
                !Root.RawAttributes.TryGetValue(attributeName, out var value))
            {
                return string.Empty;
            }

            return value ?? string.Empty;
        }
    }
}
