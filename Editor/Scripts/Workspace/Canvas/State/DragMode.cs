using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal enum DragMode
    {
        None,
        PanCanvas,
        MoveFrame,
        ResizeFrame,
        MoveElement,
        ResizeElement,
        RotateElement
    }
}
