using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class PatchInspectorController
    {
        private readonly PatchInspectorView _view;
        private readonly PatchInspectorTargetSyncService _targetSyncService;
        private IPatchInspectorHost _host;

        public PatchInspectorController(AttributePatcher attributePatcher, PatchPanelState patchPanelState)
        {
            _view = new PatchInspectorView();
            _targetSyncService = new PatchInspectorTargetSyncService(
                attributePatcher,
                patchPanelState,
                _view,
                () => _host,
                () => UpdateInteractivity(_host?.CurrentDocument != null));

            _view.TargetChanged += OnPatchTargetChanged;
            _view.InteractivityToggleChanged += OnInteractivityToggleChanged;
            _view.ReadRequested += OnReadPatchTargetClicked;
            _view.BuildTransformRequested += OnBuildTransformClicked;
            _view.ApplyRequested += OnApplyPatchClicked;
        }

        public void Bind(VisualElement root, IPatchInspectorHost host)
        {
            _host = null;
            _view.Bind(root);
            if (root == null)
            {
                return;
            }

            _host = host;
            _targetSyncService.ApplyCurrentStateToView();
            UpdateInteractivity(_host?.CurrentDocument != null);
        }

        public void Unbind()
        {
            _view.Unbind();
            _host = null;
        }

        public void RefreshTargets(string sourceText) => _targetSyncService.RefreshTargets(sourceText);

        public bool TrySelectTargetByKey(string targetKey, out string label) => _targetSyncService.TrySelectTargetByKey(targetKey, out label);

        public string ResolveSelectedTargetKey() => _targetSyncService.ResolveSelectedTargetKey();

        public void UpdateInteractivity(bool hasDocument)
        {
            SetEnabledIfNotNull(_view.TargetControl, hasDocument);
            SetEnabledIfNotNull(_view.FillToggleControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeToggleControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeWidthToggleControl, hasDocument);
            SetEnabledIfNotNull(_view.OpacityToggleControl, hasDocument);
            SetEnabledIfNotNull(_view.FillOpacityToggleControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeOpacityToggleControl, hasDocument);
            SetEnabledIfNotNull(_view.DasharrayToggleControl, hasDocument);
            SetEnabledIfNotNull(_view.TransformToggleControl, hasDocument);
            SetEnabledIfNotNull(_view.FillColorControl, hasDocument && _view.FillEnabled);
            SetEnabledIfNotNull(_view.StrokeColorControl, hasDocument && _view.StrokeEnabled);
            SetEnabledIfNotNull(_view.StrokeWidthControl, hasDocument && _view.StrokeWidthEnabled);
            SetEnabledIfNotNull(_view.OpacityControl, hasDocument && _view.OpacityEnabled);
            SetEnabledIfNotNull(_view.FillOpacityControl, hasDocument && _view.FillOpacityEnabled);
            SetEnabledIfNotNull(_view.StrokeOpacityControl, hasDocument && _view.StrokeOpacityEnabled);
            SetEnabledIfNotNull(_view.DashLengthControl, hasDocument && _view.DasharrayEnabled);
            SetEnabledIfNotNull(_view.DashGapControl, hasDocument && _view.DasharrayEnabled);
            SetEnabledIfNotNull(_view.TransformControl, hasDocument && _view.TransformEnabled);
            SetEnabledIfNotNull(_view.LinecapControl, hasDocument);
            SetEnabledIfNotNull(_view.LinejoinControl, hasDocument);
            SetEnabledIfNotNull(_view.TranslateXControl, hasDocument);
            SetEnabledIfNotNull(_view.TranslateYControl, hasDocument);
            SetEnabledIfNotNull(_view.RotateControl, hasDocument);
            SetEnabledIfNotNull(_view.ScaleXControl, hasDocument);
            SetEnabledIfNotNull(_view.ScaleYControl, hasDocument);
            SetEnabledIfNotNull(_view.ReadButtonControl, hasDocument);
            SetEnabledIfNotNull(_view.BuildTransformButtonControl, hasDocument);
            SetEnabledIfNotNull(_view.ApplyButtonControl, hasDocument);
        }

        private void OnInteractivityToggleChanged() => UpdateInteractivity(_host?.CurrentDocument != null);

        private void OnPatchTargetChanged(string label) => _targetSyncService.HandleTargetSelectionChanged(label);

        private void OnApplyPatchClicked() => _targetSyncService.ApplyPatchToSource();

        private void OnBuildTransformClicked() => _targetSyncService.BuildTransformFromHelper();

        private void OnReadPatchTargetClicked() => _targetSyncService.ReadSelectedTargetAttributes();

        private static void SetEnabledIfNotNull(VisualElement element, bool enabled)
        {
            if (element != null)
                element.SetEnabled(enabled);
        }
    }
}
