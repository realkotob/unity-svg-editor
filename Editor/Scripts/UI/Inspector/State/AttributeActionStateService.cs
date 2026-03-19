using UnityEngine;

namespace SvgEditor.UI.Inspector
{
    internal static class AttributeActionStateService
    {
        public static bool TryApply(PanelState panelState, PanelView.AttributeAction action, out string successStatus)
        {
            successStatus = string.Empty;
            if (panelState == null)
            {
                return false;
            }

            switch (action)
            {
                case PanelView.AttributeAction.AddFill:
                    panelState.FillEnabled = true;
                    successStatus = "Fill added.";
                    return true;
                case PanelView.AttributeAction.RemoveFill:
                    panelState.FillEnabled = false;
                    successStatus = "Fill removed.";
                    return true;
                case PanelView.AttributeAction.AddStroke:
                    panelState.StrokeEnabled = true;
                    panelState.StrokeWidthEnabled = true;
                    panelState.StrokeWidth = Mathf.Max(1f, panelState.StrokeWidth);
                    successStatus = "Stroke added.";
                    return true;
                case PanelView.AttributeAction.RemoveStroke:
                    panelState.StrokeEnabled = false;
                    panelState.StrokeWidthEnabled = false;
                    panelState.DasharrayEnabled = false;
                    panelState.StrokeLinecap = string.Empty;
                    panelState.StrokeLinejoin = string.Empty;
                    successStatus = "Stroke removed.";
                    return true;
                default:
                    return false;
            }
        }
    }
}
