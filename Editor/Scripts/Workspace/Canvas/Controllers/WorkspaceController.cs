using UnityEngine;
using UnityEngine.UIElements;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class WorkspaceController
    {
        private readonly ViewportState _viewportState = new();
        private readonly OverlayController _overlayController = new();
        private readonly SceneProjector _sceneProjector;
        private readonly InteractionController _interactionController;

        public WorkspaceController(ICanvasWorkspaceHost host)
        {
            _sceneProjector = new SceneProjector(
                _viewportState,
                new PreviewElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);

            _interactionController = new InteractionController(
                host,
                _viewportState,
                _overlayController,
                _sceneProjector);
        }

        public SelectionKind SelectionKind => _interactionController.SelectionKind;

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

        public void SetSelectionKind(SelectionKind selectionKind)
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
