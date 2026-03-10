using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorPanelController
    {
        private readonly InspectorPanelView _view;
        private readonly InspectorTargetSyncService _targetSyncService;
        private IInspectorPanelHost _host;

        public InspectorPanelController(AttributePatcher attributePatcher, InspectorPanelState inspectorPanelState)
        {
            _view = new InspectorPanelView();
            _targetSyncService = new InspectorTargetSyncService(
                attributePatcher,
                inspectorPanelState,
                _view,
                () => _host,
                () => UpdateInteractivity(_host?.CurrentDocument != null));

            _view.TargetChanged += OnTargetChanged;
            _view.ImmediateApplyRequested += OnImmediateApplyRequested;
            _view.TransformHelperChanged += OnTransformHelperChanged;
            _view.TransformTextChanged += OnTransformTextChanged;
            _view.ReadRequested += OnReadTargetClicked;
            _view.BuildTransformRequested += OnBuildTransformClicked;
            _view.ApplyRequested += OnApplyPatchClicked;
        }

        public void Bind(VisualElement root, IInspectorPanelHost host)
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
            SetEnabledIfNotNull(_view.FillColorControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeColorControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeWidthControl, hasDocument);
            SetEnabledIfNotNull(_view.OpacityControl, hasDocument);
            SetEnabledIfNotNull(_view.DashLengthControl, hasDocument);
            SetEnabledIfNotNull(_view.DashGapControl, hasDocument);
            SetEnabledIfNotNull(_view.TransformControl, hasDocument);
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

        private void OnTargetChanged(string label) => _targetSyncService.HandleTargetSelectionChanged(label);

        private void OnImmediateApplyRequested(InspectorPanelView.ImmediateApplyField field) => _targetSyncService.ApplyImmediatePatch(field);

        private void OnTransformHelperChanged() => _targetSyncService.SyncTransformTextFromHelper();

        private void OnTransformTextChanged() => _targetSyncService.SyncTransformHelperFromText();

        private void OnApplyPatchClicked() => _targetSyncService.ApplyPatchToSource();

        private void OnBuildTransformClicked() => _targetSyncService.BuildTransformFromHelper();

        private void OnReadTargetClicked() => _targetSyncService.ReadSelectedTargetAttributes();

        private static void SetEnabledIfNotNull(VisualElement element, bool enabled)
        {
            if (element != null)
                element.SetEnabled(enabled);
        }
    }
}
