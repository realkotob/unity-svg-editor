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
            string transform = BuildAndApplyTransformToView();

            Host?.UpdateSourceStatus(string.IsNullOrWhiteSpace(transform)
                ? "Transform helper produced an empty value."
                : "Transform string updated.");
            _updateInteractivity?.Invoke();
        }

        public string SyncTransformTextFromHelper()
        {
            if (!TryCaptureBoundState())
                return string.Empty;

            return BuildAndApplyTransformToView();
        }

        public bool SyncTransformHelperFromText()
        {
            if (!TryCaptureBoundState())
                return false;

            if (!_inspectorPanelState.TrySyncTransformHelperFromText())
                return false;

            BuildAndApplyTransformToView();
            return true;
        }

        public void ApplyStandardTransformFromHelper()
        {
            if (!TryGetBoundHostWithDocument(out IPanelHost host))
                return;

            string targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) || !TryCaptureBoundState())
                return;

            string transform = BuildAndApplyTransformToView(enableTransformEditing: true);
            host.TryApplyPatchRequest(
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

        private bool TryGetBoundHostWithDocument(out IPanelHost host)
        {
            host = Host;
            return host?.CurrentDocument != null && _view.IsBound;
        }

        private bool TryCaptureBoundState()
        {
            if (!_view.IsBound)
                return false;

            _view.CaptureState(_inspectorPanelState);
            return true;
        }

        private string BuildAndApplyTransformToView(bool enableTransformEditing = false)
        {
            string transform = _inspectorPanelState.BuildTransformFromHelper();
            _inspectorPanelState.Transform = transform;
            if (enableTransformEditing)
                _inspectorPanelState.TransformEnabled = true;

            _view.ApplyState(_inspectorPanelState);
            return transform;
        }
    }
}
