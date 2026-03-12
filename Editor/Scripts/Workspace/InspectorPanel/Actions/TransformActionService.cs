using System;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Document;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.Transforms;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
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
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                return;
            }

            if (!Host.TryGetTargetRotationPivotParentSpace(targetKey, out _rotationDragParentPivot) ||
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
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            string targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                Host?.UpdateSourceStatus("Rotation requires a non-root target.");
                return;
            }

            if (Mathf.Approximately(deltaDegrees, 0f))
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            string transform = string.Empty;
            string error = string.Empty;

            bool useDragSession = _isRotationDragActive &&
                                  string.Equals(_rotationDragTargetKey, targetKey, StringComparison.Ordinal);
            if (!useDragSession)
            {
                if (!Host.TryGetTargetRotationPivotParentSpace(targetKey, out _rotationDragParentPivot))
                {
                    Host?.UpdateSourceStatus("Rotation failed: stable pivot is unavailable.");
                    return;
                }

                if (!_rotationSession.TryBegin(Host.CurrentDocument, targetKey, _rotationDragParentPivot))
                {
                    Host?.UpdateSourceStatus("Rotation failed: transform update could not be prepared.");
                    return;
                }
            }

            if (!_rotationSession.TryBuildTransform(deltaDegrees, out transform, out error))
            {
                Host?.UpdateSourceStatus(
                    string.IsNullOrWhiteSpace(error)
                        ? "Rotation failed: transform update could not be prepared."
                        : $"Rotation failed: {error}");
                if (!useDragSession)
                {
                    _rotationSession.End();
                }

                return;
            }

            _inspectorPanelState.Transform = transform;
            _inspectorPanelState.TransformEnabled = true;
            _inspectorPanelState.TrySyncTransformHelperFromText();
            _view.ApplyState(_inspectorPanelState);
            if (!useDragSession)
            {
                _rotationSession.End();
            }

            Host.TryApplyPatchRequest(
                new AttributePatchRequest
                {
                    TargetKey = targetKey,
                    Transform = transform
                },
                "Rotation updated.",
                HistoryRecordingMode.Coalesced);
        }
    }
}
