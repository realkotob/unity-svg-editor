using System;
using Core.UI.Extensions;

namespace SvgEditor.UI.Inspector
{
    internal sealed class PanelViewEventBinder
    {
        private readonly PanelViewImmediateApplyBinder _immediateApplyBinder;
        private readonly PanelViewRotationDragBinder _rotationDragBinder;
        private readonly PanelViewActionBinder _actionBinder;

        public PanelViewEventBinder(
            FormControls form,
            Action<PanelView.ImmediateApplyField> onImmediateApplyRequested,
            Action onFrameRectChanged,
            Action<PanelView.TransformHelperChange> onTransformHelperChanged,
            Action onRotationDragStarted,
            Action onRotationDragEnded,
            Action<PanelView.PositionAction> onPositionActionRequested,
            Action<PanelView.AttributeAction> onAttributeActionRequested)
        {
            _immediateApplyBinder = new PanelViewImmediateApplyBinder(
                form,
                onImmediateApplyRequested,
                onFrameRectChanged,
                onTransformHelperChanged);
            _rotationDragBinder = new PanelViewRotationDragBinder(
                form,
                onTransformHelperChanged,
                onRotationDragStarted,
                onRotationDragEnded);
            _actionBinder = new PanelViewActionBinder(
                form,
                onPositionActionRequested,
                onAttributeActionRequested);
        }

        public void Bind()
        {
            _immediateApplyBinder.Bind();
            _rotationDragBinder.Bind();
            _actionBinder.Bind();
        }

        public void Unbind()
        {
            _actionBinder.Unbind();
            _rotationDragBinder.Unbind();
            _immediateApplyBinder.Unbind();
        }
    }
}
