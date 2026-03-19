using UnityEngine;
using SvgEditor.UI.Inspector.State;

namespace SvgEditor.UI.Inspector
{
    internal static class AttributeActionStateService
    {
        public static bool TryApply(PanelState panelState, AttributeAction action, out string successStatus)
        {
            successStatus = string.Empty;
            if (panelState == null)
            {
                return false;
            }

            switch (action)
            {
                case AttributeAction.AddFill:
                    panelState.FillEnabled = true;
                    successStatus = "Fill added.";
                    return true;
                case AttributeAction.RemoveFill:
                    panelState.FillEnabled = false;
                    successStatus = "Fill removed.";
                    return true;
                case AttributeAction.AddStroke:
                    panelState.StrokeEnabled = true;
                    panelState.StrokeWidthEnabled = true;
                    panelState.StrokeWidth = Mathf.Max(1f, panelState.StrokeWidth);
                    successStatus = "Stroke added.";
                    return true;
                case AttributeAction.RemoveStroke:
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
