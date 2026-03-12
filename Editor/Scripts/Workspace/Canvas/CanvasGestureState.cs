using UnityEngine;

namespace UnitySvgEditor.Editor.Workspace.Canvas
{
    internal sealed class CanvasGestureState
    {
        public CanvasDragMode Mode { get; private set; } = CanvasDragMode.None;
        public CanvasHandle ActiveHandle { get; private set; } = CanvasHandle.None;
        public Vector2 DragStartCanvasPan { get; private set; }
        public Rect DragStartFrameRect { get; private set; }

        public bool IsElementGesture =>
            Mode is CanvasDragMode.MoveElement or CanvasDragMode.ResizeElement or CanvasDragMode.RotateElement;

        public bool IsViewportGesture =>
            Mode is CanvasDragMode.PanCanvas or CanvasDragMode.MoveFrame or CanvasDragMode.ResizeFrame;

        public void Begin(CanvasDragMode mode, CanvasHandle activeHandle, Vector2 dragStartCanvasPan, Rect dragStartFrameRect)
        {
            Mode = mode;
            ActiveHandle = activeHandle;
            DragStartCanvasPan = dragStartCanvasPan;
            DragStartFrameRect = dragStartFrameRect;
        }

        public void Reset()
        {
            Mode = CanvasDragMode.None;
            ActiveHandle = CanvasHandle.None;
            DragStartCanvasPan = default;
            DragStartFrameRect = default;
        }
    }
}
