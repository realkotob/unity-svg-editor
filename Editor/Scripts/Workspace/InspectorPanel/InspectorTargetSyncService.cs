using System;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorTargetSyncService
    {
        private readonly InspectorTargetCatalogService _targetCatalogService;
        private readonly InspectorTransformActionService _transformActionService;
        private readonly InspectorPanelState _inspectorPanelState;
        private readonly InspectorPanelView _view;
        private readonly Func<IInspectorPanelHost> _hostAccessor;
        private readonly Action _updateInteractivity;

        public InspectorTargetSyncService(
            InspectorPanelState inspectorPanelState,
            InspectorPanelView view,
            Func<IInspectorPanelHost> hostAccessor,
            Action updateInteractivity)
        {
            _inspectorPanelState = inspectorPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _updateInteractivity = updateInteractivity;
            _targetCatalogService = new InspectorTargetCatalogService(
                inspectorPanelState,
                view,
                hostAccessor,
                updateInteractivity);
            _transformActionService = new InspectorTransformActionService(
                inspectorPanelState,
                view,
                hostAccessor,
                _targetCatalogService.ResolveSelectedTargetKey,
                updateInteractivity);
        }

        private IInspectorPanelHost Host => _hostAccessor?.Invoke();

        public void ApplyCurrentStateToView()
        {
            _targetCatalogService.ApplyCurrentStateToView();
        }

        public void RefreshTargets()
        {
            _targetCatalogService.RefreshTargets();
        }

        public void RefreshTargets(SvgDocumentModel documentModel)
        {
            _targetCatalogService.RefreshTargets(documentModel);
        }

        public bool TrySelectTargetByKey(string targetKey, out string label)
        {
            return _targetCatalogService.TrySelectTargetByKey(targetKey, out label);
        }

        public string ResolveSelectedTargetKey()
        {
            return _targetCatalogService.ResolveSelectedTargetKey();
        }

        public void BeginRotationDrag() => _transformActionService.BeginRotationDrag();

        public void EndRotationDrag() => _transformActionService.EndRotationDrag();

        public void BuildTransformFromHelper() => _transformActionService.BuildTransformFromHelper();

        public string SyncTransformTextFromHelper() => _transformActionService.SyncTransformTextFromHelper();

        public bool SyncTransformHelperFromText() => _transformActionService.SyncTransformHelperFromText();

        public void ApplyFrameRectFromView() => _transformActionService.ApplyFrameRectFromView();

        public void ApplyTransformFromHelper(InspectorPanelView.TransformHelperChange change) => _transformActionService.ApplyTransformFromHelper(change);

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
            AttributePatchRequest request = _inspectorPanelState.BuildPatchRequest();
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
            AttributePatchRequest request = _inspectorPanelState.BuildPatchRequest(field);
            Host.TryApplyPatchRequest(request, "Inspector changes applied.", HistoryRecordingMode.Coalesced);
        }

        public void ApplyAttributeAction(InspectorPanelView.AttributeAction action)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            string successStatus;
            switch (action)
            {
                case InspectorPanelView.AttributeAction.AddFill:
                    _inspectorPanelState.FillEnabled = true;
                    successStatus = "Fill added.";
                    break;
                case InspectorPanelView.AttributeAction.RemoveFill:
                    _inspectorPanelState.FillEnabled = false;
                    successStatus = "Fill removed.";
                    break;
                case InspectorPanelView.AttributeAction.AddStroke:
                    _inspectorPanelState.StrokeEnabled = true;
                    _inspectorPanelState.StrokeWidthEnabled = true;
                    _inspectorPanelState.StrokeWidth = Mathf.Max(1f, _inspectorPanelState.StrokeWidth);
                    successStatus = "Stroke added.";
                    break;
                case InspectorPanelView.AttributeAction.RemoveStroke:
                    _inspectorPanelState.StrokeEnabled = false;
                    _inspectorPanelState.StrokeWidthEnabled = false;
                    _inspectorPanelState.DasharrayEnabled = false;
                    _inspectorPanelState.StrokeLinecap = string.Empty;
                    _inspectorPanelState.StrokeLinejoin = string.Empty;
                    successStatus = "Stroke removed.";
                    break;
                default:
                    return;
            }

            _view.ApplyState(_inspectorPanelState);
            _updateInteractivity?.Invoke();
            AttributePatchRequest request = _inspectorPanelState.BuildPatchRequest(action);
            Host.TryApplyPatchRequest(request, successStatus, HistoryRecordingMode.Coalesced);
        }

        public void ApplyPositionAction(InspectorPanelView.PositionAction action) => _transformActionService.ApplyPositionAction(action);
    }
}
