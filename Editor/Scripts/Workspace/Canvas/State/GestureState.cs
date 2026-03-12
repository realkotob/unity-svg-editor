using UnityEngine;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class GestureState
    {
        public DragMode Mode { get; private set; } = DragMode.None;
        public SelectionHandle ActiveHandle { get; private set; } = SelectionHandle.None;
        public Vector2 DragStartCanvasPan { get; private set; }
        public Rect DragStartFrameRect { get; private set; }

        public bool IsElementGesture =>
            Mode is DragMode.MoveElement or DragMode.ResizeElement or DragMode.RotateElement;

        public bool IsViewportGesture =>
            Mode is DragMode.PanCanvas or DragMode.MoveFrame or DragMode.ResizeFrame;

        public void Begin(DragMode mode, SelectionHandle activeHandle, Vector2 dragStartCanvasPan, Rect dragStartFrameRect)
        {
            Mode = mode;
            ActiveHandle = activeHandle;
            DragStartCanvasPan = dragStartCanvasPan;
            DragStartFrameRect = dragStartFrameRect;
        }

        public void Reset()
        {
            Mode = DragMode.None;
            ActiveHandle = SelectionHandle.None;
            DragStartCanvasPan = default;
            DragStartFrameRect = default;
        }
    }
}
