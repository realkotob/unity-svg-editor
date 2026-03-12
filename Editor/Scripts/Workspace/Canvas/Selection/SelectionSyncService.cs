using SvgEditor;
using SvgEditor.Preview;
using SvgEditor.Document;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class SelectionSyncService
    {
        private readonly ICanvasPointerDragHost _host;
        private readonly OverlayController _overlayController;
        private readonly ElementDragController _elementDragController;

        public SelectionSyncService(
            ICanvasPointerDragHost host,
            OverlayController overlayController,
            ElementDragController elementDragController)
        {
            _host = host;
            _overlayController = overlayController;
            _elementDragController = elementDragController;
        }

        public void ClearCanvasSelection()
        {
            _host.ClearSelection();
            _host.UpdateStructureInteractivity(_host.CurrentDocument != null);
            ResetSelectionInternal();
        }

        public void SelectCanvasFrame()
        {
            _host.SelectionKind = SelectionKind.Frame;
            _host.SelectFrame();
            _host.UpdateStructureInteractivity(_host.CurrentDocument != null);
            _host.UpdateSelectionVisual();
        }

        public void SelectCanvasElement(string elementKey, bool syncPatchTarget)
        {
            _host.SelectionKind = SelectionKind.Element;
            _host.SelectElement(elementKey, syncPatchTarget);
            _host.UpdateStructureInteractivity(_host.CurrentDocument != null);
            _host.UpdateSelectionVisual();
        }

        private void ResetSelectionInternal()
        {
            _host.SelectionKind = SelectionKind.None;
            _overlayController.ClearSelection();
        }
    }
}
