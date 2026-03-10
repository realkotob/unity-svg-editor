using System;
using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgNodeModel
    {
        public SvgNodeId Id { get; set; } = SvgNodeId.Root;
        public SvgNodeId ParentId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public SvgNodeKind Kind { get; set; } = SvgNodeKind.Other;
        public string XmlId { get; set; } = string.Empty;
        public string LegacyElementKey { get; set; } = string.Empty;
        public string LegacyTargetKey { get; set; } = string.Empty;
        public int Depth { get; set; }
        public int SiblingIndex { get; set; }
        public bool IsDefinitionNode { get; set; }
        public IReadOnlyDictionary<string, string> RawAttributes { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public IReadOnlyList<SvgNodeId> Children { get; set; } = Array.Empty<SvgNodeId>();
        public IReadOnlyList<SvgNodeReference> References { get; set; } = Array.Empty<SvgNodeReference>();

        public bool HasXmlId => !string.IsNullOrWhiteSpace(XmlId);

        public bool HasChildren => Children.Count > 0;
    }
}
