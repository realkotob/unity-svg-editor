using UnityEngine.UIElements;
using UnityEditor;
using SvgEditor.DocumentModel;

using SvgEditor;
using SvgEditor.Shared;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PanelController
    {
        private readonly PanelState _inspectorPanelState;
        private readonly PanelView _view;
        private readonly TargetSyncService _targetSyncService;
        private readonly EditorDeferredActionGate _refreshGate;
        private readonly EditorDeferredActionGate _frameRectApplyGate;
        private readonly EditorDeferredActionGate _transformApplyGate;
        private IPanelHost _host;
        private bool _hasPendingRefresh;
        private SvgDocumentModel _pendingDocumentModel;
        private bool _hasPendingFrameRectApply;
        private bool _hasPendingTransformApply;
        private PanelView.TransformHelperChange _pendingTransformHelperChange;

        public PanelController(
            PanelState inspectorPanelState,
            System.Action<System.Action> scheduleDeferredCall = null,
            System.Action<System.Action> unscheduleDeferredCall = null)
        {
            _inspectorPanelState = inspectorPanelState;
            System.Action<System.Action> deferredScheduler = scheduleDeferredCall ?? ScheduleDeferredCall;
            System.Action<System.Action> deferredUnscheduler = unscheduleDeferredCall ?? UnscheduleDeferredCall;
            _view = new PanelView();
            _targetSyncService = new TargetSyncService(
                inspectorPanelState,
                _view,
                () => _host,
                () => UpdateInteractivity(HasInspectableDocument()));
            _refreshGate = new EditorDeferredActionGate(ProcessPendingRefresh, deferredScheduler, deferredUnscheduler);
            _frameRectApplyGate = new EditorDeferredActionGate(ProcessPendingFrameRectApply, deferredScheduler, deferredUnscheduler);
            _transformApplyGate = new EditorDeferredActionGate(ProcessPendingTransformApply, deferredScheduler, deferredUnscheduler);

            _view.ImmediateApplyRequested += OnImmediateApplyRequested;
            _view.FrameRectChanged += OnFrameRectChanged;
            _view.TransformHelperChanged += OnTransformHelperChanged;
            _view.RotationDragStarted += OnRotationDragStarted;
            _view.RotationDragEnded += OnRotationDragEnded;
            _view.PositionActionRequested += OnPositionActionRequested;
            _view.AttributeActionRequested += OnAttributeActionRequested;
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

        public void Bind(VisualElement root, IPanelHost host)
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
            _refreshGate.Schedule();
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
            SetEnabledIfNotNull(_view.FillAddControl, hasDocument);
            SetEnabledIfNotNull(_view.FillRemoveControl, hasDocument);
            SetEnabledIfNotNull(_view.FillColorControl, hasDocument && _inspectorPanelState.FillEnabled);
            SetEnabledIfNotNull(_view.StrokeAddControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeRemoveControl, hasDocument);
            SetEnabledIfNotNull(_view.StrokeColorControl, hasDocument && _inspectorPanelState.StrokeEnabled);
            SetEnabledIfNotNull(_view.StrokeWidthControl, hasDocument && _inspectorPanelState.StrokeEnabled);
            SetEnabledIfNotNull(_view.OpacityControl, hasDocument);
            SetEnabledIfNotNull(_view.CornerRadiusControl, hasDocument && _inspectorPanelState.CornerRadiusEnabled);
            SetEnabledIfNotNull(_view.DashLengthControl, hasDocument && _inspectorPanelState.StrokeEnabled);
            SetEnabledIfNotNull(_view.DashGapControl, hasDocument && _inspectorPanelState.StrokeEnabled);
            SetEnabledIfNotNull(_view.FrameXControl, hasDocument);
            SetEnabledIfNotNull(_view.FrameYControl, hasDocument);
            SetEnabledIfNotNull(_view.FrameWidthControl, hasDocument);
            SetEnabledIfNotNull(_view.FrameHeightControl, hasDocument);
            SetEnabledIfNotNull(_view.LinecapControl, hasDocument && _inspectorPanelState.StrokeEnabled);
            SetEnabledIfNotNull(_view.LinejoinControl, hasDocument && _inspectorPanelState.StrokeEnabled);
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

        private void OnImmediateApplyRequested(PanelView.ImmediateApplyField field) => _targetSyncService.ApplyImmediatePatch(field);

        private void OnFrameRectChanged() => QueueFrameRectApply();

        private void OnTransformHelperChanged(PanelView.TransformHelperChange change)
        {
            _pendingTransformHelperChange = change;
            QueueTransformApply();
        }

        private void OnRotationDragStarted() => _targetSyncService.BeginRotationDrag();

        private void OnRotationDragEnded() => _targetSyncService.EndRotationDrag();

        private void OnPositionActionRequested(PanelView.PositionAction action) => _targetSyncService.ApplyPositionAction(action);

        private void OnAttributeActionRequested(PanelView.AttributeAction action) => _targetSyncService.ApplyAttributeAction(action);

        private static void SetEnabledIfNotNull(VisualElement element, bool enabled)
        {
            if (element != null)
                element.SetEnabled(enabled);
        }

        private void ProcessPendingRefresh()
        {
            _refreshGate.Cancel();

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
            _refreshGate.Cancel();
        }

        private void QueueFrameRectApply()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _hasPendingFrameRectApply = true;
            _frameRectApplyGate.Schedule();
        }

        private void ProcessPendingFrameRectApply()
        {
            _frameRectApplyGate.Cancel();

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
            _frameRectApplyGate.Cancel();
        }

        private void QueueTransformApply()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _hasPendingTransformApply = true;
            _transformApplyGate.Schedule();
        }

        private void ProcessPendingTransformApply()
        {
            _transformApplyGate.Cancel();

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
            _transformApplyGate.Cancel();
        }
    }
}
