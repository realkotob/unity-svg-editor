using UnityEngine.UIElements;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PanelInteractivityApplier
    {
        private readonly PanelState _state;
        private readonly PanelView _view;

        public PanelInteractivityApplier(PanelState state, PanelView view)
        {
            _state = state;
            _view = view;
        }

        public void Apply(bool hasDocument)
        {
            SetEnabled(_view.FillAddControl, hasDocument);
            SetEnabled(_view.FillRemoveControl, hasDocument);
            SetEnabled(_view.FillColorControl, hasDocument && _state.FillEnabled);
            SetEnabled(_view.StrokeAddControl, hasDocument);
            SetEnabled(_view.StrokeRemoveControl, hasDocument);
            SetEnabled(_view.StrokeColorControl, hasDocument && _state.StrokeEnabled);
            SetEnabled(_view.StrokeWidthControl, hasDocument && _state.StrokeEnabled);
            SetEnabled(_view.OpacityControl, hasDocument);
            SetEnabled(_view.CornerRadiusControl, hasDocument && _state.CornerRadiusEnabled);
            SetEnabled(_view.DashLengthControl, hasDocument && _state.StrokeEnabled);
            SetEnabled(_view.DashGapControl, hasDocument && _state.StrokeEnabled);
            SetEnabled(_view.FrameXControl, hasDocument);
            SetEnabled(_view.FrameYControl, hasDocument);
            SetEnabled(_view.FrameWidthControl, hasDocument);
            SetEnabled(_view.FrameHeightControl, hasDocument);
            SetEnabled(_view.LinecapControl, hasDocument && _state.StrokeEnabled);
            SetEnabled(_view.LinejoinControl, hasDocument && _state.StrokeEnabled);
            SetEnabled(_view.TranslateXControl, hasDocument);
            SetEnabled(_view.TranslateYControl, hasDocument);
            SetEnabled(_view.RotateControl, hasDocument);
            SetEnabled(_view.ScaleXControl, hasDocument);
            SetEnabled(_view.ScaleYControl, hasDocument);
            SetEnabled(_view.PositionAlignLeftControl, hasDocument);
            SetEnabled(_view.PositionAlignCenterControl, hasDocument);
            SetEnabled(_view.PositionAlignRightControl, hasDocument);
            SetEnabled(_view.PositionAlignTopControl, hasDocument);
            SetEnabled(_view.PositionAlignMiddleControl, hasDocument);
            SetEnabled(_view.PositionAlignBottomControl, hasDocument);
            SetEnabled(_view.PositionRotateClockwise90Control, hasDocument);
            SetEnabled(_view.PositionFlipHorizontalControl, hasDocument);
            SetEnabled(_view.PositionFlipVerticalControl, hasDocument);
        }

        private static void SetEnabled(VisualElement element, bool enabled)
        {
            if (element != null)
            {
                element.SetEnabled(enabled);
            }
        }
    }
}
