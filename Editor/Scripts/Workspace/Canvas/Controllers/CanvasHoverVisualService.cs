using UnityEngine;
using SvgEditor.Preview;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class CanvasHoverVisualService
    {
        private readonly ICanvasWorkspaceHost _host;
        private readonly SceneProjector _sceneProjector;
        private readonly OverlayController _overlayController;

        public CanvasHoverVisualService(
            ICanvasWorkspaceHost host,
            SceneProjector sceneProjector,
            OverlayController overlayController)
        {
            _host = host;
            _sceneProjector = sceneProjector;
            _overlayController = overlayController;
        }

        public void UpdateHoverVisual(string hoveredElementKey, SelectionKind selectionKind)
        {
            PreviewSnapshot previewSnapshot = _host.PreviewSnapshot;
            if (previewSnapshot == null ||
                string.IsNullOrWhiteSpace(hoveredElementKey) ||
                (selectionKind == SelectionKind.Element &&
                 string.Equals(hoveredElementKey, _host.SelectedElementKey, System.StringComparison.Ordinal)))
            {
                _overlayController.ClearHover();
                return;
            }

            if (string.Equals(hoveredElementKey, InteractionController.FrameHoverSentinel, System.StringComparison.Ordinal))
            {
                if (selectionKind != SelectionKind.Frame &&
                    _sceneProjector.TryGetFrameViewportRect(out Rect frameViewportRect))
                {
                    _overlayController.SetHover(frameViewportRect);
                    return;
                }

                _overlayController.ClearHover();
                return;
            }

            if (_sceneProjector.TryResolveSelectedElementSceneRect(previewSnapshot, hoveredElementKey, out Rect hoveredElementSceneRect) &&
                _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, hoveredElementSceneRect, out Rect hoveredElementViewportRect))
            {
                _overlayController.SetHover(hoveredElementViewportRect);
                return;
            }

            _overlayController.ClearHover();
        }
    }
}
