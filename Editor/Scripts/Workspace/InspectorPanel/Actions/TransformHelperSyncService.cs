using System;
using SvgEditor.Document;
using SvgEditor.Workspace.Document;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class TransformHelperSyncService
    {
        private readonly PanelState _inspectorPanelState;
        private readonly PanelView _view;
        private readonly Func<IPanelHost> _hostAccessor;
        private readonly Func<string> _selectedTargetKeyAccessor;
        private readonly Action _updateInteractivity;

        public TransformHelperSyncService(
            PanelState inspectorPanelState,
            PanelView view,
            Func<IPanelHost> hostAccessor,
            Func<string> selectedTargetKeyAccessor,
            Action updateInteractivity)
        {
            _inspectorPanelState = inspectorPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _selectedTargetKeyAccessor = selectedTargetKeyAccessor;
            _updateInteractivity = updateInteractivity;
        }

        private IPanelHost Host => _hostAccessor?.Invoke();

        public void BuildTransformFromHelper()
        {
            string transform = SyncTransformTextFromHelper();

            Host?.UpdateSourceStatus(string.IsNullOrWhiteSpace(transform)
                ? "Transform helper produced an empty value."
                : "Transform string updated.");
            _updateInteractivity?.Invoke();
        }

        public string SyncTransformTextFromHelper()
        {
            if (!_view.IsBound)
            {
                return string.Empty;
            }

            _view.CaptureState(_inspectorPanelState);
            string transform = _inspectorPanelState.BuildTransformFromHelper();
            _inspectorPanelState.Transform = transform;
            _view.ApplyState(_inspectorPanelState);
            return transform;
        }

        public bool SyncTransformHelperFromText()
        {
            if (!_view.IsBound)
            {
                return false;
            }

            _view.CaptureState(_inspectorPanelState);
            if (!_inspectorPanelState.TrySyncTransformHelperFromText())
            {
                return false;
            }

            _inspectorPanelState.Transform = _inspectorPanelState.BuildTransformFromHelper();
            _view.ApplyState(_inspectorPanelState);
            return true;
        }

        public void ApplyStandardTransformFromHelper()
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            string targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            string transform = _inspectorPanelState.BuildTransformFromHelper();
            _inspectorPanelState.Transform = transform;
            _inspectorPanelState.TransformEnabled = true;
            _view.ApplyState(_inspectorPanelState);
            Host.TryApplyPatchRequest(
                new AttributePatchRequest
                {
                    TargetKey = targetKey,
                    Transform = transform
                },
                "Transform updated.",
                HistoryRecordingMode.Coalesced);
        }

        private string ResolveSelectedTargetKey()
        {
            return _selectedTargetKeyAccessor?.Invoke() ?? string.Empty;
        }
    }
}
