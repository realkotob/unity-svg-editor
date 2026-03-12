using System;
using System.Collections.Generic;
using SvgEditor.DocumentModel;

namespace SvgEditor.RenderModel
{
    internal sealed class SvgRenderNode
    {
        public SvgNodeId NodeId { get; set; }
        public SvgNodeId ParentNodeId { get; set; }
        public string ElementKey { get; set; } = string.Empty;
        public string TargetKey { get; set; } = string.Empty;
        public string TagName { get; set; } = string.Empty;
        public int Depth { get; set; }
        public int DrawOrder { get; set; }
        public bool IsDefinitionNode { get; set; }
        public IReadOnlyList<SvgNodeId> Children { get; set; } = Array.Empty<SvgNodeId>();
    }
}
