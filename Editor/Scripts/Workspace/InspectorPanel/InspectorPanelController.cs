using UnityEngine.UIElements;
using UnityEditor;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorPanelController
    {
        private readonly InspectorPanelState _inspectorPanelState;
        private readonly InspectorPanelView _view;
        private readonly InspectorTargetSyncService _targetSyncService;
        private readonly System.Action<System.Action> _scheduleDeferredCall;
        private readonly System.Action<System.Action> _unscheduleDeferredCall;
        private IInspectorPanelHost _host;
        private bool _isRefreshScheduled;
        private bool _hasPendingRefresh;
        private SvgDocumentModel _pendingDocumentModel;
        private bool _isFrameRectApplyScheduled;
        private bool _hasPendingFrameRectApply;
        private bool _isTransformApplyScheduled;
        private bool _hasPendingTransformApply;
        private InspectorPanelView.TransformHelperChange _pendingTransformHelperChange;

        public InspectorPanelController(
            InspectorPanelState inspectorPanelState,
            System.Action<System.Action> scheduleDeferredCall = null,
            System.Action<System.Action> unscheduleDeferredCall = null)
        {
            _inspectorPanelState = inspectorPanelState;
            _scheduleDeferredCall = scheduleDeferredCall ?? ScheduleDeferredCall;
            _unscheduleDeferredCall = unscheduleDeferredCall ?? UnscheduleDeferredCall;
            _view = new InspectorPanelView();
            _targetSyncService = new InspectorTargetSyncService(
                inspectorPanelState,
                _view,
                () => _host,
                () => UpdateInteractivity(HasInspectableDocument()));

            _view.ImmediateApplyRequested += OnImmediateApplyRequested;
            _view.FrameRectChanged += OnFrameRectChanged;
            _view.TransformHelperChanged += OnTransformHelperChanged;
            _view.RotationDragStarted += OnRotationDragStarted;
            _view.RotationDragEnded += OnRotationDragEnded;
            _view.PositionActionRequested += OnPositionActionRequested;
        }

        private static void ScheduleDeferredCall(System.Action callback)
        {
            if (callback == null)
                return;

            EditorApplication.delayCall += callback.Invoke;
        }

        private static void UnscheduleDeferredCall(System.Action callback)
        {
            if (callback == null)
                return;

            EditorApplication.delayCall -= callback.Invoke;
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
            UpdateInteractivity(HasInspectableDocument());
        }

        public void Unbind()
        {
            ClearPendingRefresh();
            ClearPendingFrameRectApply();
            ClearPendingTransformApply();
            _view.Unbind();
            _host = null;
        }

        public void RefreshTargets()
        {
            ClearPendingRefresh();
            _targetSyncService.RefreshTargets();
        }

        public void RefreshTargets(SvgDocumentModel documentModel)
        {
            ClearPendingRefresh();
            _targetSyncService.RefreshTargets(documentModel);
        }

        public void QueueRefreshTargets()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _pendingDocumentModel = null;
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

        private bool HasInspectableDocument()
        {
            var currentDocument = _host?.CurrentDocument;
            return currentDocument?.DocumentModel != null &&
                   string.IsNullOrWhiteSpace(currentDocument.DocumentModelLoadError) &&
                   string.Equals(currentDocument.DocumentModel.SourceText, currentDocument.WorkingSourceText, System.StringComparison.Ordinal);
        }

        public void UpdateInteractivity(bool hasDocument)
        {
            SetEnabledIfNotNull(_view.FillColorControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeColorControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeWidthControl, hasDocument);
            SetEnabledIfNotNull(_view.OpacityControl, hasDocument);
            SetEnabledIfNotNull(_view.CornerRadiusControl, hasDocument && _inspectorPanelState.CornerRadiusEnabled);
            SetEnabledIfNotNull(_view.DashLengthControl, hasDocument);
            SetEnabledIfNotNull(_view.DashGapControl, hasDocument);
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
            SetEnabledIfNotNull(_view.PositionAlignLeftControl, hasDocument);
            SetEnabledIfNotNull(_view.PositionAlignCenterControl, hasDocument);
            SetEnabledIfNotNull(_view.PositionAlignRightControl, hasDocument);
            SetEnabledIfNotNull(_view.PositionAlignTopControl, hasDocument);
            SetEnabledIfNotNull(_view.PositionAlignMiddleControl, hasDocument);
            SetEnabledIfNotNull(_view.PositionAlignBottomControl, hasDocument);
            SetEnabledIfNotNull(_view.PositionRotateClockwise90Control, hasDocument);
            SetEnabledIfNotNull(_view.PositionFlipHorizontalControl, hasDocument);
            SetEnabledIfNotNull(_view.PositionFlipVerticalControl, hasDocument);
        }

        private void OnImmediateApplyRequested(InspectorPanelView.ImmediateApplyField field) => _targetSyncService.ApplyImmediatePatch(field);

        private void OnFrameRectChanged() => QueueFrameRectApply();

        private void OnTransformHelperChanged(InspectorPanelView.TransformHelperChange change)
        {
            _pendingTransformHelperChange = change;
            QueueTransformApply();
        }

        private void OnRotationDragStarted() => _targetSyncService.BeginRotationDrag();

        private void OnRotationDragEnded() => _targetSyncService.EndRotationDrag();

        private void OnPositionActionRequested(InspectorPanelView.PositionAction action) => _targetSyncService.ApplyPositionAction(action);

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

            SvgDocumentModel documentModel = _pendingDocumentModel;
            _pendingDocumentModel = null;
            _hasPendingRefresh = false;
            if (documentModel != null)
            {
                _targetSyncService.RefreshTargets(documentModel);
                return;
            }

            _targetSyncService.RefreshTargets();
        }

        private void ClearPendingRefresh()
        {
            _pendingDocumentModel = null;
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

        private void QueueTransformApply()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _hasPendingTransformApply = true;
            if (_isTransformApplyScheduled)
            {
                return;
            }

            _isTransformApplyScheduled = true;
            _scheduleDeferredCall(ProcessPendingTransformApply);
        }

        private void ProcessPendingTransformApply()
        {
            _isTransformApplyScheduled = false;
            _unscheduleDeferredCall(ProcessPendingTransformApply);

            if (!_hasPendingTransformApply)
            {
                return;
            }

            _hasPendingTransformApply = false;
            _targetSyncService.ApplyTransformFromHelper(_pendingTransformHelperChange);
        }

        private void ClearPendingTransformApply()
        {
            _hasPendingTransformApply = false;
            if (!_isTransformApplyScheduled)
            {
                return;
            }

            _isTransformApplyScheduled = false;
            _unscheduleDeferredCall(ProcessPendingTransformApply);
        }
    }
}
