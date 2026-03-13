using UnityEngine;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class CanvasSelectionVisual
    {
        public SelectionKind Kind { get; set; }
        public Rect Rect { get; set; }
        public bool ShowSelectionHandles { get; set; }
        public bool AllowResizeHandleInteraction { get; set; } = true;
        public bool AllowRotateHandleInteraction { get; set; } = true;
        public string SizeText { get; set; } = string.Empty;
        public bool ShowVerticalGuide { get; set; }
        public float VerticalGuideX { get; set; }
        public bool ShowHorizontalGuide { get; set; }
        public float HorizontalGuideY { get; set; }
        public bool HasRotationPivot { get; set; }
        public Vector2 RotationPivotViewport { get; set; }
        public float RotationDegrees { get; set; }
    }
}
