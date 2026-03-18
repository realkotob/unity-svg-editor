using SvgEditor;
using SvgEditor.Preview;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.Canvas
{
    internal enum DragMode
    {
        None,
        PanCanvas,
        MoveFrame,
        ResizeFrame,
        SelectArea,
        MoveElement,
        ResizeElement,
        RotateElement
    }
}
