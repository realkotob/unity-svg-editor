using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasWorkspaceController
    {
        private readonly CanvasViewportState _viewportState = new();
        private readonly CanvasOverlayController _overlayController = new();
        private readonly CanvasSceneProjector _sceneProjector;
        private readonly CanvasInteractionController _interactionController;

        public CanvasWorkspaceController(ICanvasWorkspaceHost host)
        {
            _sceneProjector = new CanvasSceneProjector(
                _viewportState,
                new PreviewElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);

            _interactionController = new CanvasInteractionController(
                host,
                _viewportState,
                _overlayController,
                _sceneProjector);
        }

        public CanvasSelectionKind SelectionKind => _interactionController.SelectionKind;

        public void Bind(CanvasStageView canvasStageView, Toggle moveToolToggle)
        {
            _interactionController.Bind(canvasStageView, moveToolToggle);
        }

        public void Dispose()
        {
            _interactionController.Dispose();
        }

        public bool TryCancelActiveDrag()
        {
            return _interactionController.TryCancelActiveDrag();
        }

        public void SetSelectionKind(CanvasSelectionKind selectionKind)
        {
            _interactionController.SetSelectionKind(selectionKind);
        }

        public void ResetCanvasView(bool clearSelection = false)
        {
            _interactionController.ResetCanvasView(clearSelection);
        }

        public void FitCanvasView(bool clearSelection = false)
        {
            _interactionController.FitCanvasView(clearSelection);
        }

        public void SyncCanvasFrameToPreview()
        {
            _interactionController.SyncCanvasFrameToPreview();
        }

        public void ResetSelection()
        {
            _interactionController.ResetSelection();
        }

        public void UpdateCanvasVisualState()
        {
            _interactionController.UpdateCanvasVisualState();
        }

        public void UpdateSelectionVisual()
        {
            _interactionController.UpdateSelectionVisual();
        }
    }
}
