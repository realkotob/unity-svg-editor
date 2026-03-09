using System;
using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal sealed class PatchInspectorTargetSyncService
    {
        private readonly AttributePatcher _attributePatcher;
        private readonly PatchPanelState _patchPanelState;
        private readonly PatchInspectorView _view;
        private readonly Func<IPatchInspectorHost> _hostAccessor;
        private readonly Action _updateInteractivity;

        public PatchInspectorTargetSyncService(
            AttributePatcher attributePatcher,
            PatchPanelState patchPanelState,
            PatchInspectorView view,
            Func<IPatchInspectorHost> hostAccessor,
            Action updateInteractivity)
        {
            _attributePatcher = attributePatcher;
            _patchPanelState = patchPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _updateInteractivity = updateInteractivity;
        }

        private IPatchInspectorHost Host => _hostAccessor?.Invoke();

        public void ApplyCurrentStateToView()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _view.SetTargetChoices(_patchPanelState.TargetChoices);
            _view.ApplyState(_patchPanelState);
        }

        public void RefreshTargets(string sourceText)
        {
            if (!_view.IsBound)
            {
                return;
            }

            IReadOnlyList<PatchTarget> targets = _attributePatcher.ExtractTargets(sourceText);
            _patchPanelState.SetTargets(targets);
            ApplyCurrentStateToView();
            ReadSelectedTargetAttributes();
        }

        public bool TrySelectTargetByKey(string targetKey, out string label)
        {
            label = string.Empty;
            if (string.IsNullOrWhiteSpace(targetKey) || !_view.IsBound)
            {
                return false;
            }

            if (!_patchPanelState.TrySelectTargetByKey(targetKey, out label))
            {
                return false;
            }

            _view.SetSelectedTargetLabel(label, notify: true);
            return true;
        }

        public string ResolveSelectedTargetKey()
        {
            if (_view.IsBound)
            {
                _patchPanelState.SelectTargetLabel(_view.SelectedTargetLabel);
            }

            return _patchPanelState.ResolveSelectedTargetKey();
        }

        public void HandleTargetSelectionChanged(string label)
        {
            _patchPanelState.SelectTargetLabel(label);
            ReadSelectedTargetAttributes();
        }

        public void ReadSelectedTargetAttributes()
        {
            if (Host?.CurrentDocument == null)
            {
                return;
            }

            if (!_attributePatcher.TryReadAttributes(
                    Host.CurrentDocument.WorkingSourceText,
                    ResolveSelectedTargetKey(),
                    out Dictionary<string, string> attributes,
                    out string error))
            {
                Host.UpdateSourceStatus($"Read target failed: {error}");
                return;
            }

            _patchPanelState.SyncFromAttributes(attributes);
            _view.ApplyState(_patchPanelState);
            _updateInteractivity?.Invoke();
        }

        public void BuildTransformFromHelper()
        {
            _view.CaptureState(_patchPanelState);
            var transform = _patchPanelState.BuildTransformFromHelper();
            _view.SetTransformText(transform);

            Host?.UpdateSourceStatus(string.IsNullOrWhiteSpace(transform)
                ? "Transform helper produced an empty value."
                : "Transform string updated.");
            _updateInteractivity?.Invoke();
        }

        public void ApplyPatchToSource()
        {
            if (Host?.CurrentDocument == null)
            {
                return;
            }

            _view.CaptureState(_patchPanelState);
            var request = _patchPanelState.BuildPatchRequest();
            Host.TryApplyPatchRequest(request, "Patch applied to source.");
        }
    }
}
