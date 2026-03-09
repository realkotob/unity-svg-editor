using System;
using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorTargetSyncService
    {
        private readonly AttributePatcher _attributePatcher;
        private readonly InspectorPanelState _inspectorPanelState;
        private readonly InspectorPanelView _view;
        private readonly Func<IInspectorPanelHost> _hostAccessor;
        private readonly Action _updateInteractivity;

        public InspectorTargetSyncService(
            AttributePatcher attributePatcher,
            InspectorPanelState inspectorPanelState,
            InspectorPanelView view,
            Func<IInspectorPanelHost> hostAccessor,
            Action updateInteractivity)
        {
            _attributePatcher = attributePatcher;
            _inspectorPanelState = inspectorPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _updateInteractivity = updateInteractivity;
        }

        private IInspectorPanelHost Host => _hostAccessor?.Invoke();

        public void ApplyCurrentStateToView()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _view.SetTargetChoices(_inspectorPanelState.TargetChoices);
            _view.ApplyState(_inspectorPanelState);
        }

        public void RefreshTargets(string sourceText)
        {
            if (!_view.IsBound)
            {
                return;
            }

            IReadOnlyList<PatchTarget> targets = _attributePatcher.ExtractTargets(sourceText);
            _inspectorPanelState.SetTargets(targets);
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

            if (!_inspectorPanelState.TrySelectTargetByKey(targetKey, out label))
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
                _inspectorPanelState.SelectTargetLabel(_view.SelectedTargetLabel);
            }

            return _inspectorPanelState.ResolveSelectedTargetKey();
        }

        public void HandleTargetSelectionChanged(string label)
        {
            _inspectorPanelState.SelectTargetLabel(label);
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

            _inspectorPanelState.SyncFromAttributes(attributes);
            _view.ApplyState(_inspectorPanelState);
            _updateInteractivity?.Invoke();
        }

        public void BuildTransformFromHelper()
        {
            _view.CaptureState(_inspectorPanelState);
            var transform = _inspectorPanelState.BuildTransformFromHelper();
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

            _view.CaptureState(_inspectorPanelState);
            var request = _inspectorPanelState.BuildPatchRequest();
            Host.TryApplyPatchRequest(request, "Patch applied to source.");
        }
    }
}
