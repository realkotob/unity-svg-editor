using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Document.Structure.Hierarchy;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal readonly struct CanvasLineSegment
    {
        public CanvasLineSegment(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }

        public Vector2 Start { get; }
        public Vector2 End { get; }
    }

    internal sealed class CanvasDefinitionOverlayScene
    {
        public CanvasDefinitionOverlayKind Kind { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
        public string DefinitionElementKey { get; set; } = string.Empty;
        public SceneNode RootNode { get; set; }
        public Dictionary<SceneNode, float> NodeOpacities { get; } = new();
    }

    internal sealed class CanvasDefinitionOverlayVisual
    {
        public CanvasDefinitionOverlayKind Kind { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
        public string ProxyElementKey { get; set; } = string.Empty;
        public string DefinitionElementKey { get; set; } = string.Empty;
        public Rect SceneBounds { get; set; }
        public Matrix2D ParentWorldTransform { get; set; } = Matrix2D.identity;
        public Rect ViewportBounds { get; set; }
        public IReadOnlyList<CanvasLineSegment> OutlineSegments { get; set; } = Array.Empty<CanvasLineSegment>();
    }

    internal sealed class CanvasDefinitionProxySelection
    {
        public string SourceElementKey { get; set; } = string.Empty;
        public string ProxyElementKey { get; set; } = string.Empty;
        public string DefinitionElementKey { get; set; } = string.Empty;
        public CanvasDefinitionOverlayKind Kind { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
    }
}
