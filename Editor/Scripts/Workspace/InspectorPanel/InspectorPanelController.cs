using UnityEngine.UIElements;
using UnityEditor;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorPanelController
    {
        private readonly InspectorPanelView _view;
        private readonly InspectorTargetSyncService _targetSyncService;
        private readonly System.Action<System.Action> _scheduleDeferredCall;
        private readonly System.Action<System.Action> _unscheduleDeferredCall;
        private IInspectorPanelHost _host;
        private bool _isRefreshScheduled;
        private bool _hasPendingRefresh;
        private string _pendingSourceText = string.Empty;
        private bool _isFrameRectApplyScheduled;
        private bool _hasPendingFrameRectApply;

        public InspectorPanelController(
            AttributePatcher attributePatcher,
            InspectorPanelState inspectorPanelState,
            System.Action<System.Action> scheduleDeferredCall = null,
            System.Action<System.Action> unscheduleDeferredCall = null)
        {
            _scheduleDeferredCall = scheduleDeferredCall ?? (callback => EditorApplication.delayCall += callback);
            _unscheduleDeferredCall = unscheduleDeferredCall ?? (callback => EditorApplication.delayCall -= callback);
            _view = new InspectorPanelView();
            _targetSyncService = new InspectorTargetSyncService(
                attributePatcher,
                inspectorPanelState,
                _view,
                () => _host,
                () => UpdateInteractivity(_host?.CurrentDocument != null));

            _view.ImmediateApplyRequested += OnImmediateApplyRequested;
            _view.FrameRectChanged += OnFrameRectChanged;
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
            ClearPendingRefresh();
            ClearPendingFrameRectApply();
            _view.Unbind();
            _host = null;
        }

        public void RefreshTargets(string sourceText)
        {
            ClearPendingRefresh();
            _targetSyncService.RefreshTargets(sourceText);
        }

        public void QueueRefreshTargets(string sourceText)
        {
            if (!_view.IsBound)
            {
                return;
            }

            _pendingSourceText = sourceText ?? string.Empty;
            _hasPendingRefresh = true;
            if (_isRefreshScheduled)
            {
                return;
            }

            _isRefreshScheduled = true;
            _scheduleDeferredCall(ProcessPendingRefresh);
        }

        public bool TrySelectTargetByKey(string targetKey, out string label) => _targetSyncService.TrySelectTargetByKey(targetKey, out label);

        public string ResolveSelectedTargetKey() => _targetSyncService.ResolveSelectedTargetKey();

        public void UpdateInteractivity(bool hasDocument)
        {
            SetEnabledIfNotNull(_view.FillColorControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeColorControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeWidthControl, hasDocument);
            SetEnabledIfNotNull(_view.OpacityControl, hasDocument);
            SetEnabledIfNotNull(_view.DashLengthControl, hasDocument);
            SetEnabledIfNotNull(_view.DashGapControl, hasDocument);
            SetEnabledIfNotNull(_view.TransformControl, hasDocument);
            SetEnabledIfNotNull(_view.FrameXControl, hasDocument);
            SetEnabledIfNotNull(_view.FrameYControl, hasDocument);
            SetEnabledIfNotNull(_view.FrameWidthControl, hasDocument);
            SetEnabledIfNotNull(_view.FrameHeightControl, hasDocument);
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

        private void OnImmediateApplyRequested(InspectorPanelView.ImmediateApplyField field) => _targetSyncService.ApplyImmediatePatch(field);

        private void OnFrameRectChanged() => QueueFrameRectApply();

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

        private void ProcessPendingRefresh()
        {
            _isRefreshScheduled = false;
            _unscheduleDeferredCall(ProcessPendingRefresh);

            if (!_hasPendingRefresh)
            {
                return;
            }

            string sourceText = _pendingSourceText;
            _pendingSourceText = string.Empty;
            _hasPendingRefresh = false;
            _targetSyncService.RefreshTargets(sourceText);
        }

        private void ClearPendingRefresh()
        {
            _pendingSourceText = string.Empty;
            _hasPendingRefresh = false;
            if (!_isRefreshScheduled)
            {
                return;
            }

            _isRefreshScheduled = false;
            _unscheduleDeferredCall(ProcessPendingRefresh);
        }

        private void QueueFrameRectApply()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _hasPendingFrameRectApply = true;
            if (_isFrameRectApplyScheduled)
            {
                return;
            }

            _isFrameRectApplyScheduled = true;
            _scheduleDeferredCall(ProcessPendingFrameRectApply);
        }

        private void ProcessPendingFrameRectApply()
        {
            _isFrameRectApplyScheduled = false;
            _unscheduleDeferredCall(ProcessPendingFrameRectApply);

            if (!_hasPendingFrameRectApply)
            {
                return;
            }

            _hasPendingFrameRectApply = false;
            _targetSyncService.ApplyFrameRectFromView();
        }

        private void ClearPendingFrameRectApply()
        {
            _hasPendingFrameRectApply = false;
            if (!_isFrameRectApplyScheduled)
            {
                return;
            }

            _isFrameRectApplyScheduled = false;
            _unscheduleDeferredCall(ProcessPendingFrameRectApply);
        }
    }
}
