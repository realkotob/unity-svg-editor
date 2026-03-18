using System;
using UnityEngine;
using SvgEditor.DocumentModel;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class TargetSyncService
    {
        private readonly TargetCatalogService _targetCatalogService;
        private readonly TransformActionService _transformActionService;
        private readonly PatchApplyService _patchApplyService;

        public TargetSyncService(
            PanelState inspectorPanelState,
            PanelView view,
            Func<IPanelHost> hostAccessor,
            Action updateInteractivity)
        {
            _targetCatalogService = new TargetCatalogService(
                inspectorPanelState,
                view,
                hostAccessor,
                updateInteractivity);
            _transformActionService = new TransformActionService(
                inspectorPanelState,
                view,
                hostAccessor,
                _targetCatalogService.ResolveSelectedTargetKey,
                updateInteractivity);
            _patchApplyService = new PatchApplyService(
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

        public void ApplyTransformFromHelper(PanelView.TransformHelperChange change) => _transformActionService.ApplyTransformFromHelper(change);

        public void ApplyPatchToSource() => _patchApplyService.ApplyPatchToSource();

        public void ApplyPatchToSource(string successStatus) => _patchApplyService.ApplyPatchToSource(successStatus);

        public void ApplyImmediatePatch(PanelView.ImmediateApplyField field) => _patchApplyService.ApplyImmediatePatch(field);

        public void ApplyAttributeAction(PanelView.AttributeAction action) => _patchApplyService.ApplyAttributeAction(action);

        public void ApplyPositionAction(PanelView.PositionAction action) => _transformActionService.ApplyPositionAction(action);
    }
}
