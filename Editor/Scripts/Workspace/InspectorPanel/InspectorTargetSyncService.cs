using System;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorTargetSyncService
    {
        private readonly InspectorTargetCatalogService _targetCatalogService;
        private readonly InspectorTransformActionService _transformActionService;
        private readonly InspectorPatchApplyService _patchApplyService;

        public InspectorTargetSyncService(
            InspectorPanelState inspectorPanelState,
            InspectorPanelView view,
            Func<IInspectorPanelHost> hostAccessor,
            Action updateInteractivity)
        {
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
            _patchApplyService = new InspectorPatchApplyService(
                inspectorPanelState,
                view,
                hostAccessor,
                updateInteractivity);
        }

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

        public void ApplyPatchToSource() => _patchApplyService.ApplyPatchToSource();

        public void ApplyPatchToSource(string successStatus) => _patchApplyService.ApplyPatchToSource(successStatus);

        public void ApplyImmediatePatch(InspectorPanelView.ImmediateApplyField field) => _patchApplyService.ApplyImmediatePatch(field);

        public void ApplyAttributeAction(InspectorPanelView.AttributeAction action) => _patchApplyService.ApplyAttributeAction(action);

        public void ApplyPositionAction(InspectorPanelView.PositionAction action) => _transformActionService.ApplyPositionAction(action);
    }
}
