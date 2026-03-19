using System;
using UnityEngine;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.UI.Inspector.State;
using SvgEditor.UI.Workspace.Document;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.UI.Inspector
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

        public void ApplyImmediatePatch(ImmediateApplyField field)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            AttributePatchRequest request = _inspectorPanelState.BuildPatchRequest(field);
            Host.TryApplyPatchRequest(request, "Inspector changes applied.", HistoryRecordingMode.Coalesced);
        }

        public void ApplyAttributeAction(AttributeAction action)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            if (!AttributeActionStateService.TryApply(_inspectorPanelState, action, out string successStatus))
            {
                return;
            }

            _view.ApplyState(_inspectorPanelState);
            _updateInteractivity?.Invoke();
            AttributePatchRequest request = _inspectorPanelState.BuildPatchRequest(action);
            Host.TryApplyPatchRequest(request, successStatus, HistoryRecordingMode.Coalesced);
        }
    }
}
