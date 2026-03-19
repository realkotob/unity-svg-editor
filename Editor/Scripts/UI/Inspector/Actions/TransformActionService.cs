using System;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Svg;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.UI.Workspace.Document;
using SvgEditor.UI.Workspace.Transforms;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.UI.Inspector
{
    internal sealed class TransformActionService
    {
        private readonly PanelState _inspectorPanelState;
        private readonly PanelView _view;
        private readonly Func<IPanelHost> _hostAccessor;
        private readonly Func<string> _selectedTargetKeyAccessor;
        private readonly TransformHelperSyncService _helperSyncService;
        private readonly TransformPositionActionService _positionActionService;
        private readonly ElementRotationSession _rotationSession = new();
        private bool _isRotationDragActive;
        private string _rotationDragTargetKey = string.Empty;
        private Vector2 _rotationDragParentPivot;

        public TransformActionService(
            PanelState inspectorPanelState,
            PanelView view,
            Func<IPanelHost> hostAccessor,
            Func<string> selectedTargetKeyAccessor,
            Action updateInteractivity)
        {
            _inspectorPanelState = inspectorPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _selectedTargetKeyAccessor = selectedTargetKeyAccessor;
            _helperSyncService = new TransformHelperSyncService(
                inspectorPanelState,
                view,
                hostAccessor,
                selectedTargetKeyAccessor,
                updateInteractivity);
            _positionActionService = new TransformPositionActionService(
                inspectorPanelState,
                view,
                hostAccessor,
                selectedTargetKeyAccessor);
        }

        private IPanelHost Host => _hostAccessor?.Invoke();

        public void BeginRotationDrag()
        {
            EndRotationDrag();

            string targetKey = ResolveSelectedTargetKey();
            if (Host?.CurrentDocument == null ||
                string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgTargets.RootTargetKey, StringComparison.Ordinal))
            {
                return;
            }

            if (!Host.TryGetRotationPivotParentSpace(targetKey, out _rotationDragParentPivot) ||
                !_rotationSession.TryBegin(Host.CurrentDocument, targetKey, _rotationDragParentPivot))
            {
                _rotationSession.End();
                return;
            }

            _rotationDragTargetKey = targetKey;
            _isRotationDragActive = true;
        }

        public void EndRotationDrag()
        {
            _isRotationDragActive = false;
            _rotationDragTargetKey = string.Empty;
            _rotationDragParentPivot = default;
            _rotationSession.End();
        }

        public void BuildTransformFromHelper()
        {
            _helperSyncService.BuildTransformFromHelper();
        }

        public string SyncTransformTextFromHelper() => _helperSyncService.SyncTransformTextFromHelper();

        public bool SyncTransformHelperFromText() => _helperSyncService.SyncTransformHelperFromText();

        public void ApplyFrameRectFromView() => _positionActionService.ApplyFrameRectFromView();

        public void ApplyTransformFromHelper(PanelView.TransformHelperChange change)
        {
            if (change.Field == PanelView.TransformHelperField.Rotate)
            {
                ApplyRotationFromHelper(change.Delta);
                return;
            }

            ApplyStandardTransformFromHelper();
        }

        public void ApplyPositionAction(PanelView.PositionAction action)
            => _positionActionService.ApplyPositionAction(action);

        private string ResolveSelectedTargetKey()
        {
            return _selectedTargetKeyAccessor?.Invoke() ?? string.Empty;
        }

        private void ApplyStandardTransformFromHelper()
            => _helperSyncService.ApplyStandardTransformFromHelper();

        private void ApplyRotationFromHelper(float deltaDegrees)
        {
            if (!TryGetBoundHostWithDocument(out IPanelHost host))
                return;

            string targetKey = ResolveSelectedTargetKey();
            if (!TryValidateRotationTargetKey(targetKey))
                return;

            if (Mathf.Approximately(deltaDegrees, 0f))
                return;

            _view.CaptureState(_inspectorPanelState);
            bool useDragSession = _isRotationDragActive &&
                                  string.Equals(_rotationDragTargetKey, targetKey, StringComparison.Ordinal);
            if (!TryPrepareRotationSession(host, targetKey, useDragSession))
                return;

            if (!TryBuildRotationTransform(deltaDegrees, useDragSession, out string transform))
                return;

            _inspectorPanelState.Transform = transform;
            _inspectorPanelState.TransformEnabled = true;
            _inspectorPanelState.TrySyncTransformHelperFromText();
            _view.ApplyState(_inspectorPanelState);
            EndStandaloneRotationSession(useDragSession);

            host.TryApplyPatchRequest(
                new AttributePatchRequest
                {
                    TargetKey = targetKey,
                    Transform = transform
                },
                "Rotation updated.",
                HistoryRecordingMode.Coalesced);
        }

        private bool TryGetBoundHostWithDocument(out IPanelHost host)
        {
            host = Host;
            return host?.CurrentDocument != null && _view.IsBound;
        }

        private bool TryValidateRotationTargetKey(string targetKey)
        {
            if (!string.IsNullOrWhiteSpace(targetKey) &&
                !string.Equals(targetKey, SvgTargets.RootTargetKey, StringComparison.Ordinal))
            {
                return true;
            }

            Host?.UpdateSourceStatus("Rotation requires a non-root target.");
            return false;
        }

        private bool TryPrepareRotationSession(IPanelHost host, string targetKey, bool useDragSession)
        {
            if (useDragSession)
                return true;

            if (!host.TryGetRotationPivotParentSpace(targetKey, out _rotationDragParentPivot))
            {
                host.UpdateSourceStatus("Rotation failed: stable pivot is unavailable.");
                return false;
            }

            if (_rotationSession.TryBegin(host.CurrentDocument, targetKey, _rotationDragParentPivot))
                return true;

            host.UpdateSourceStatus("Rotation failed: transform update could not be prepared.");
            return false;
        }

        private bool TryBuildRotationTransform(float deltaDegrees, bool useDragSession, out string transform)
        {
            transform = string.Empty;
            if (_rotationSession.TryBuildTransform(deltaDegrees, out transform, out string error))
                return true;

            Host?.UpdateSourceStatus(
                string.IsNullOrWhiteSpace(error)
                    ? "Rotation failed: transform update could not be prepared."
                    : $"Rotation failed: {error}");
            EndStandaloneRotationSession(useDragSession);
            return false;
        }

        private void EndStandaloneRotationSession(bool useDragSession)
        {
            if (!useDragSession)
                _rotationSession.End();
        }
    }
}
