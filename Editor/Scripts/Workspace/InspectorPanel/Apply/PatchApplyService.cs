using System;
using UnityEngine;
using SvgEditor.Document;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PatchApplyService
    {
        private readonly PanelState _inspectorPanelState;
        private readonly PanelView _view;
        private readonly Func<IPanelHost> _hostAccessor;
        private readonly Action _updateInteractivity;

        public PatchApplyService(
            PanelState inspectorPanelState,
            PanelView view,
            Func<IPanelHost> hostAccessor,
            Action updateInteractivity)
        {
            _inspectorPanelState = inspectorPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _updateInteractivity = updateInteractivity;
        }

        private IPanelHost Host => _hostAccessor?.Invoke();

        public void ApplyPatchToSource()
        {
            ApplyPatchToSource("Patch applied to source.");
        }

        public void ApplyPatchToSource(string successStatus)
        {
            if (Host?.CurrentDocument == null)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            AttributePatchRequest request = _inspectorPanelState.BuildPatchRequest();
            Host.TryApplyPatchRequest(
                request,
                string.IsNullOrWhiteSpace(successStatus) ? "Patch applied to source." : successStatus);
        }

        public void ApplyImmediatePatch(PanelView.ImmediateApplyField field)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            AttributePatchRequest request = _inspectorPanelState.BuildPatchRequest(field);
            Host.TryApplyPatchRequest(request, "Inspector changes applied.", HistoryRecordingMode.Coalesced);
        }

        public void ApplyAttributeAction(PanelView.AttributeAction action)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            string successStatus;
            switch (action)
            {
                case PanelView.AttributeAction.AddFill:
                    _inspectorPanelState.FillEnabled = true;
                    successStatus = "Fill added.";
                    break;
                case PanelView.AttributeAction.RemoveFill:
                    _inspectorPanelState.FillEnabled = false;
                    successStatus = "Fill removed.";
                    break;
                case PanelView.AttributeAction.AddStroke:
                    _inspectorPanelState.StrokeEnabled = true;
                    _inspectorPanelState.StrokeWidthEnabled = true;
                    _inspectorPanelState.StrokeWidth = Mathf.Max(1f, _inspectorPanelState.StrokeWidth);
                    successStatus = "Stroke added.";
                    break;
                case PanelView.AttributeAction.RemoveStroke:
                    _inspectorPanelState.StrokeEnabled = false;
                    _inspectorPanelState.StrokeWidthEnabled = false;
                    _inspectorPanelState.DasharrayEnabled = false;
                    _inspectorPanelState.StrokeLinecap = string.Empty;
                    _inspectorPanelState.StrokeLinejoin = string.Empty;
                    successStatus = "Stroke removed.";
                    break;
                default:
                    return;
            }

            _view.ApplyState(_inspectorPanelState);
            _updateInteractivity?.Invoke();
            AttributePatchRequest request = _inspectorPanelState.BuildPatchRequest(action);
            Host.TryApplyPatchRequest(request, successStatus, HistoryRecordingMode.Coalesced);
        }
    }
}
