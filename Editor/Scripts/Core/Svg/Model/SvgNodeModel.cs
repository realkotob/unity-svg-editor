using System;
using System.Collections.Generic;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Model
{
    internal sealed class SvgNodeModel
    {
        public SvgNodeId Id { get; set; } = SvgNodeId.Root;
        public SvgNodeId ParentId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string DisplayTagName { get; set; } = string.Empty;
        public string ElementPrefix { get; set; } = string.Empty;
        public string ElementNamespaceUri { get; set; } = string.Empty;
        public SvgNodeCategory Kind { get; set; } = SvgNodeCategory.Other;
        public string XmlId { get; set; } = string.Empty;
        public string LegacyElementKey { get; set; } = string.Empty;
        public string LegacyTargetKey { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public int Depth { get; set; }
        public int SiblingIndex { get; set; }
        public bool IsDefinitionNode { get; set; }
        public IReadOnlyDictionary<string, string> RawAttributes { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public IReadOnlyList<SvgNodeId> Children { get; set; } = Array.Empty<SvgNodeId>();
        public IReadOnlyList<NodeReference> References { get; set; } = Array.Empty<NodeReference>();

        public bool HasXmlId => !string.IsNullOrWhiteSpace(XmlId);

        public bool HasChildren => Children.Count > 0;
    }
}
