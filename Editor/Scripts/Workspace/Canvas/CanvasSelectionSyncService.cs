namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasSelectionSyncService
    {
        private readonly ICanvasPointerDragHost _host;
        private readonly CanvasOverlayController _overlayController;
        private readonly CanvasElementDragController _elementDragController;

        public CanvasSelectionSyncService(
            ICanvasPointerDragHost host,
            CanvasOverlayController overlayController,
            CanvasElementDragController elementDragController)
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
            _host.SelectionKind = CanvasSelectionKind.Frame;
            _host.SelectFrame();
            _host.UpdateStructureInteractivity(_host.CurrentDocument != null);
            _host.UpdateSelectionVisual();
        }

        public void SelectCanvasElement(string elementKey, bool syncPatchTarget)
        {
            _host.SelectionKind = CanvasSelectionKind.Element;
            _host.SelectElement(elementKey, syncPatchTarget);
            _host.UpdateStructureInteractivity(_host.CurrentDocument != null);
            _host.UpdateSelectionVisual();
        }

        private void ResetSelectionInternal()
        {
            _host.SelectionKind = CanvasSelectionKind.None;
            _overlayController.ClearSelection();
        }
    }
}
