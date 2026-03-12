using UnityEngine;

namespace UnitySvgEditor.Editor.Workspace.Canvas
{
    internal sealed class CanvasSelectionVisual
    {
        public CanvasSelectionKind Kind { get; set; }
        public Rect Rect { get; set; }
        public bool ShowHandles { get; set; }
        public string SizeText { get; set; } = string.Empty;
        public bool ShowVerticalGuide { get; set; }
        public float VerticalGuideX { get; set; }
        public bool ShowHorizontalGuide { get; set; }
        public float HorizontalGuideY { get; set; }
        public bool HasRotationPivot { get; set; }
        public Vector2 RotationPivotViewport { get; set; }
    }
}
