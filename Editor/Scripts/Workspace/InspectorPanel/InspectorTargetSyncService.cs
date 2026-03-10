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
        private bool _suppressSelectionSync;

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

            _suppressSelectionSync = true;
            try
            {
                _view.SetSelectedTargetLabel(label, notify: true);
            }
            finally
            {
                _suppressSelectionSync = false;
            }

            ReadSelectedTargetAttributes();
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
            if (_suppressSelectionSync)
            {
                return;
            }

            Host?.SyncSelectionFromInspectorTarget(ResolveSelectedTargetKey());
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
            var transform = SyncTransformTextFromHelper();

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
            var transform = _inspectorPanelState.BuildTransformFromHelper();
            _view.SetTransformText(transform);
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

            _view.ApplyState(_inspectorPanelState);
            return true;
        }

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
            var request = _inspectorPanelState.BuildPatchRequest();
            Host.TryApplyPatchRequest(
                request,
                string.IsNullOrWhiteSpace(successStatus) ? "Patch applied to source." : successStatus);
        }

        public void ApplyImmediatePatch(InspectorPanelView.ImmediateApplyField field)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            var request = _inspectorPanelState.BuildPatchRequest(field);
            Host.TryApplyPatchRequest(request, "Inspector changes applied.");
        }
    }
}
