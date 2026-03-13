using System;
using UnityEngine.UIElements;
using SvgEditor.Shared;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PanelViewActionBinder
    {
        private readonly FormControls _form;
        private readonly Action<PanelView.PositionAction> _onPositionActionRequested;
        private readonly Action<PanelView.AttributeAction> _onAttributeActionRequested;

        public PanelViewActionBinder(
            FormControls form,
            Action<PanelView.PositionAction> onPositionActionRequested,
            Action<PanelView.AttributeAction> onAttributeActionRequested)
        {
            _form = form;
            _onPositionActionRequested = onPositionActionRequested;
            _onAttributeActionRequested = onAttributeActionRequested;
        }

        public void Bind()
        {
            ToggleAttributeActionCallbacks(register: true);
            TogglePositionActionCallbacks(register: true);
        }

        public void Unbind()
        {
            TogglePositionActionCallbacks(register: false);
            ToggleAttributeActionCallbacks(register: false);
        }

        private void OnFillAddRequested() => RequestAttributeAction(PanelView.AttributeAction.AddFill);
        private void OnFillRemoveRequested() => RequestAttributeAction(PanelView.AttributeAction.RemoveFill);
        private void OnStrokeAddRequested() => RequestAttributeAction(PanelView.AttributeAction.AddStroke);
        private void OnStrokeRemoveRequested() => RequestAttributeAction(PanelView.AttributeAction.RemoveStroke);

        private void OnPositionAlignLeftRequested() => RequestPositionAction(PanelView.PositionAction.AlignLeft);
        private void OnPositionAlignCenterRequested() => RequestPositionAction(PanelView.PositionAction.AlignCenter);
        private void OnPositionAlignRightRequested() => RequestPositionAction(PanelView.PositionAction.AlignRight);
        private void OnPositionAlignTopRequested() => RequestPositionAction(PanelView.PositionAction.AlignTop);
        private void OnPositionAlignMiddleRequested() => RequestPositionAction(PanelView.PositionAction.AlignMiddle);
        private void OnPositionAlignBottomRequested() => RequestPositionAction(PanelView.PositionAction.AlignBottom);
        private void OnPositionRotateClockwise90Requested() => RequestPositionAction(PanelView.PositionAction.RotateClockwise90);
        private void OnPositionFlipHorizontalRequested() => RequestPositionAction(PanelView.PositionAction.FlipHorizontal);
        private void OnPositionFlipVerticalRequested() => RequestPositionAction(PanelView.PositionAction.FlipVertical);

        private void RequestAttributeAction(PanelView.AttributeAction action)
        {
            _onAttributeActionRequested?.Invoke(action);
        }

        private void RequestPositionAction(PanelView.PositionAction action)
        {
            _onPositionActionRequested?.Invoke(action);
        }

        private void ToggleAttributeActionCallbacks(bool register)
        {
            ToggleButtonClicked(_form.FillAddButton, OnFillAddRequested, register);
            ToggleButtonClicked(_form.FillRemoveButton, OnFillRemoveRequested, register);
            ToggleButtonClicked(_form.StrokeAddButton, OnStrokeAddRequested, register);
            ToggleButtonClicked(_form.StrokeRemoveButton, OnStrokeRemoveRequested, register);
        }

        private void TogglePositionActionCallbacks(bool register)
        {
            ToggleButtonClicked(_form.PositionAlignLeftButton, OnPositionAlignLeftRequested, register);
            ToggleButtonClicked(_form.PositionAlignCenterButton, OnPositionAlignCenterRequested, register);
            ToggleButtonClicked(_form.PositionAlignRightButton, OnPositionAlignRightRequested, register);
            ToggleButtonClicked(_form.PositionAlignTopButton, OnPositionAlignTopRequested, register);
            ToggleButtonClicked(_form.PositionAlignMiddleButton, OnPositionAlignMiddleRequested, register);
            ToggleButtonClicked(_form.PositionAlignBottomButton, OnPositionAlignBottomRequested, register);
            ToggleButtonClicked(_form.PositionRotateClockwise90Button, OnPositionRotateClockwise90Requested, register);
            ToggleButtonClicked(_form.PositionFlipHorizontalButton, OnPositionFlipHorizontalRequested, register);
            ToggleButtonClicked(_form.PositionFlipVerticalButton, OnPositionFlipVerticalRequested, register);
        }

        private static void ToggleButtonClicked(Button button, Action callback, bool register)
        {
            CallbackBindingUtility.ToggleButtonClicked(button, callback, register);
        }
    }
}
