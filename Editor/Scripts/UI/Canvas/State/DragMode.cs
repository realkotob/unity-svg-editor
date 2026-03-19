using SvgEditor;
using SvgEditor.Core.Preview;
using Core.UI.Extensions;

namespace SvgEditor.UI.Canvas
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
