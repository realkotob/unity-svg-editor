using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorTargetSyncService
    {
        private readonly InspectorPanelState _inspectorPanelState;
        private readonly InspectorPanelView _view;
        private readonly Func<IInspectorPanelHost> _hostAccessor;
        private readonly Action _updateInteractivity;
        private readonly ElementRotationSession _rotationSession = new();
        private bool _isRotationDragActive;
        private string _rotationDragTargetKey = string.Empty;
        private Vector2 _rotationDragParentPivot;
        private bool _suppressSelectionSync;

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
        }

        private IInspectorPanelHost Host => _hostAccessor?.Invoke();

        public void ApplyCurrentStateToView()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _view.ApplyState(_inspectorPanelState);
        }

        public void RefreshTargets()
        {
            RefreshTargets(ResolveCurrentDocumentModel());
        }

        public void RefreshTargets(SvgDocumentModel documentModel)
        {
            if (!_view.IsBound)
            {
                return;
            }

            IReadOnlyList<PatchTarget> targets = documentModel != null
                ? InspectorDocumentModelReader.ExtractTargets(documentModel)
                : Array.Empty<PatchTarget>();

            _inspectorPanelState.SetTargets(targets);
            ApplyCurrentStateToView();
            ReadSelectedTargetAttributes(documentModel);
        }

        public bool TrySelectTargetByKey(string targetKey, out string label)
        {
            label = string.Empty;
            if (string.IsNullOrWhiteSpace(targetKey) || !_view.IsBound)
            {
                return false;
            }

            if (!_inspectorPanelState.TrySelectTargetByKey(targetKey, out label))
                return false;

            ReadSelectedTargetAttributes();
            return true;
        }

        public string ResolveSelectedTargetKey() => _inspectorPanelState.ResolveSelectedTargetKey();

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

        public void ReadSelectedTargetAttributes()
        {
            ReadSelectedTargetAttributes(ResolveCurrentDocumentModel());
        }

        private void ReadSelectedTargetAttributes(SvgDocumentModel documentModel)
        {
            if (Host?.CurrentDocument == null)
            {
                return;
            }

            string error = documentModel == null
                ? "Document model was not available."
                : string.Empty;

            if (documentModel == null ||
                !InspectorDocumentModelReader.TryReadAttributes(
                    documentModel,
                    ResolveSelectedTargetKey(),
                    out Dictionary<string, string> attributes,
                    out string tagName,
                    out error))
            {
                Host.UpdateSourceStatus($"Read target failed: {error}");
                return;
            }

            _inspectorPanelState.SyncFromAttributes(attributes, tagName);
            SyncFramePositionFromPreview();
            _view.ApplyState(_inspectorPanelState);
            _updateInteractivity?.Invoke();
        }

        public void BuildTransformFromHelper()
        {
            var transform = SyncTransformTextFromHelper();

            Host?.UpdateSourceStatus(string.IsNullOrWhiteSpace(transform)
                ? "Transform helper produced an empty value."
                : "Transform string updated.");
            _updateInteractivity?.Invoke();
        }

        public string SyncTransformTextFromHelper()
        {
            if (!_view.IsBound)
            {
                return string.Empty;
            }

            _view.CaptureState(_inspectorPanelState);
            var transform = _inspectorPanelState.BuildTransformFromHelper();
            _inspectorPanelState.Transform = transform;
            _view.ApplyState(_inspectorPanelState);
            return transform;
        }

        public bool SyncTransformHelperFromText()
        {
            if (!_view.IsBound)
            {
                return false;
            }

            _view.CaptureState(_inspectorPanelState);
            if (!_inspectorPanelState.TrySyncTransformHelperFromText())
            {
                return false;
            }

            _inspectorPanelState.Transform = _inspectorPanelState.BuildTransformFromHelper();
            _view.ApplyState(_inspectorPanelState);
            return true;
        }

        public void ApplyFrameRectFromView()
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            var targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal) ||
                !Host.TryGetTargetSceneRect(targetKey, out _))
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            var desiredSceneRect = new UnityEngine.Rect(
                _inspectorPanelState.FrameX,
                _inspectorPanelState.FrameY,
                Math.Max(0f, _inspectorPanelState.FrameWidth),
                Math.Max(0f, _inspectorPanelState.FrameHeight));

            Host.TryApplyTargetFrameRect(
                targetKey,
                desiredSceneRect,
                "Frame rect updated.",
                HistoryRecordingMode.Coalesced);
        }

        public void ApplyTransformFromHelper(InspectorPanelView.TransformHelperChange change)
        {
            if (change.Field == InspectorPanelView.TransformHelperField.Rotate)
            {
                ApplyRotationFromHelper(change.Delta);
                return;
            }

            ApplyStandardTransformFromHelper();
        }

        private void ApplyStandardTransformFromHelper()
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            string targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            string transform = _inspectorPanelState.BuildTransformFromHelper();
            _inspectorPanelState.Transform = transform;
            _inspectorPanelState.TransformEnabled = true;
            _view.ApplyState(_inspectorPanelState);
            Host.TryApplyPatchRequest(
                new AttributePatchRequest
                {
                    TargetKey = targetKey,
                    Transform = transform
                },
                "Transform updated.",
                HistoryRecordingMode.Coalesced);
        }

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
            var request = _inspectorPanelState.BuildPatchRequest();
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
            var request = _inspectorPanelState.BuildPatchRequest(field);
            Host.TryApplyPatchRequest(request, "Inspector changes applied.", HistoryRecordingMode.Coalesced);
        }

        public void ApplyPositionAction(InspectorPanelView.PositionAction action)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            switch (action)
            {
                case InspectorPanelView.PositionAction.AlignLeft:
                case InspectorPanelView.PositionAction.AlignCenter:
                case InspectorPanelView.PositionAction.AlignRight:
                case InspectorPanelView.PositionAction.AlignTop:
                case InspectorPanelView.PositionAction.AlignMiddle:
                case InspectorPanelView.PositionAction.AlignBottom:
                    ApplyAlignmentAction(action);
                    break;
                case InspectorPanelView.PositionAction.RotateClockwise90:
                    ApplyRotateClockwiseAction(90f);
                    break;
                case InspectorPanelView.PositionAction.FlipHorizontal:
                    ApplyFlipAction(new Vector2(1f, -1f), "Flipped horizontally.");
                    break;
                case InspectorPanelView.PositionAction.FlipVertical:
                    ApplyFlipAction(new Vector2(-1f, 1f), "Flipped vertically.");
                    break;
            }
        }

        private void SyncFramePositionFromPreview()
        {
            _inspectorPanelState.FramePositionEnabled = false;
            _inspectorPanelState.FrameX = 0f;
            _inspectorPanelState.FrameY = 0f;

            var targetKey = ResolveSelectedTargetKey();
            if (Host == null ||
                string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal) ||
                !Host.TryGetTargetSceneRect(targetKey, out var sceneRect))
            {
                return;
            }

            _inspectorPanelState.FramePositionEnabled = true;
            _inspectorPanelState.FrameX = sceneRect.xMin;
            _inspectorPanelState.FrameY = sceneRect.yMin;
            _inspectorPanelState.FrameWidth = sceneRect.width;
            _inspectorPanelState.FrameHeight = sceneRect.height;
        }

        private void ApplyAlignmentAction(InspectorPanelView.PositionAction action)
        {
            var targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                Host?.UpdateSourceStatus("Alignment requires a non-root target.");
                return;
            }

            if (!Host.TryGetTargetSceneRect(targetKey, out Rect currentRect) ||
                !Host.TryGetCanvasViewportSceneRect(out Rect canvasRect))
            {
                Host?.UpdateSourceStatus("Alignment failed: preview bounds are unavailable.");
                return;
            }

            Rect desiredRect = currentRect;
            string successStatus = "Position updated.";
            switch (action)
            {
                case InspectorPanelView.PositionAction.AlignLeft:
                    desiredRect.x = canvasRect.xMin;
                    successStatus = "Aligned left.";
                    break;
                case InspectorPanelView.PositionAction.AlignCenter:
                    desiredRect.x = canvasRect.center.x - (currentRect.width * 0.5f);
                    successStatus = "Aligned center.";
                    break;
                case InspectorPanelView.PositionAction.AlignRight:
                    desiredRect.x = canvasRect.xMax - currentRect.width;
                    successStatus = "Aligned right.";
                    break;
                case InspectorPanelView.PositionAction.AlignTop:
                    desiredRect.y = canvasRect.yMin;
                    successStatus = "Aligned top.";
                    break;
                case InspectorPanelView.PositionAction.AlignMiddle:
                    desiredRect.y = canvasRect.center.y - (currentRect.height * 0.5f);
                    successStatus = "Aligned middle.";
                    break;
                case InspectorPanelView.PositionAction.AlignBottom:
                    desiredRect.y = canvasRect.yMax - currentRect.height;
                    successStatus = "Aligned bottom.";
                    break;
            }

            Host.TryApplyTargetFrameRect(targetKey, desiredRect, successStatus);
        }

        private void ApplyRotateClockwiseAction(float deltaDegrees)
        {
            var targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                Host?.UpdateSourceStatus("Rotation requires a non-root target.");
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            if (!Host.TryGetTargetRotationPivotParentSpace(targetKey, out Vector2 parentPivot) ||
                !_rotationSession.TryBegin(Host.CurrentDocument, targetKey, parentPivot) ||
                !_rotationSession.TryBuildTransform(deltaDegrees, out string transform, out _))
            {
                Host?.UpdateSourceStatus("Rotation failed: transform update could not be prepared.");
                _rotationSession.End();
                return;
            }

            _inspectorPanelState.Transform = transform;
            _inspectorPanelState.TransformEnabled = true;
            _inspectorPanelState.TrySyncTransformHelperFromText();
            _view.ApplyState(_inspectorPanelState);
            _rotationSession.End();
            Host.TryApplyPatchRequest(
                new AttributePatchRequest
                {
                    TargetKey = targetKey,
                    Transform = transform
                },
                $"Rotated {deltaDegrees:+0;-0}°.");
        }

        private void ApplyFlipAction(Vector2 scale, string successStatus)
        {
            var targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                Host?.UpdateSourceStatus("Flip requires a non-root target.");
                return;
            }

            if (!Host.TryGetTargetSceneRect(targetKey, out Rect sceneRect))
            {
                Host?.UpdateSourceStatus("Flip failed: preview bounds are unavailable.");
                return;
            }

            if (!Host.TryGetTargetParentWorldTransform(targetKey, out var parentWorldTransform))
            {
                Host?.UpdateSourceStatus("Flip failed: parent transform is unavailable.");
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            Vector2 parentPivot = ElementRotationUtility.ToParentSpacePoint(parentWorldTransform, sceneRect.center);
            string existingTransform = _inspectorPanelState.Transform ?? string.Empty;
            string transform = PrependTransform(
                existingTransform,
                TransformStringBuilder.BuildScaleAround(scale, parentPivot));
            _inspectorPanelState.Transform = transform;
            _inspectorPanelState.TransformEnabled = true;
            _view.ApplyState(_inspectorPanelState);
            Host.TryApplyPatchRequest(
                new AttributePatchRequest
                {
                    TargetKey = targetKey,
                    Transform = transform
                },
                successStatus);
        }

        private static string PrependTransform(string existingTransform, string transformSegment)
        {
            if (string.IsNullOrWhiteSpace(transformSegment))
            {
                return existingTransform ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(existingTransform))
            {
                return transformSegment;
            }

            return $"{transformSegment} {existingTransform}";
        }

        private SvgDocumentModel ResolveCurrentDocumentModel()
        {
            if (Host?.CurrentDocument == null ||
                Host.CurrentDocument.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(Host.CurrentDocument.DocumentModelLoadError))
            {
                return null;
            }

            return Host.CurrentDocument.DocumentModel;
        }
    }
}
