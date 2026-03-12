using System;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Document;
using SvgEditor.Workspace.Transforms;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class TransformPositionActionService
    {
        private readonly PanelState _inspectorPanelState;
        private readonly PanelView _view;
        private readonly Func<IPanelHost> _hostAccessor;
        private readonly Func<string> _selectedTargetKeyAccessor;

        public TransformPositionActionService(
            PanelState inspectorPanelState,
            PanelView view,
            Func<IPanelHost> hostAccessor,
            Func<string> selectedTargetKeyAccessor)
        {
            _inspectorPanelState = inspectorPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _selectedTargetKeyAccessor = selectedTargetKeyAccessor;
        }

        private IPanelHost Host => _hostAccessor?.Invoke();

        public void ApplyFrameRectFromView()
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            string targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal) ||
                !Host.TryGetTargetSceneRect(targetKey, out _))
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            Rect desiredSceneRect = new(
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

        public void ApplyPositionAction(PanelView.PositionAction action)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            switch (action)
            {
                case PanelView.PositionAction.AlignLeft:
                case PanelView.PositionAction.AlignCenter:
                case PanelView.PositionAction.AlignRight:
                case PanelView.PositionAction.AlignTop:
                case PanelView.PositionAction.AlignMiddle:
                case PanelView.PositionAction.AlignBottom:
                    ApplyAlignmentAction(action);
                    break;
                case PanelView.PositionAction.RotateClockwise90:
                    ApplyRotateClockwiseAction(90f);
                    break;
                case PanelView.PositionAction.FlipHorizontal:
                    ApplyFlipAction(new Vector2(1f, -1f), "Flipped horizontally.");
                    break;
                case PanelView.PositionAction.FlipVertical:
                    ApplyFlipAction(new Vector2(-1f, 1f), "Flipped vertically.");
                    break;
            }
        }

        private void ApplyAlignmentAction(PanelView.PositionAction action)
        {
            string targetKey = ResolveSelectedTargetKey();
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
                case PanelView.PositionAction.AlignLeft:
                    desiredRect.x = canvasRect.xMin;
                    successStatus = "Aligned left.";
                    break;
                case PanelView.PositionAction.AlignCenter:
                    desiredRect.x = canvasRect.center.x - (currentRect.width * 0.5f);
                    successStatus = "Aligned center.";
                    break;
                case PanelView.PositionAction.AlignRight:
                    desiredRect.x = canvasRect.xMax - currentRect.width;
                    successStatus = "Aligned right.";
                    break;
                case PanelView.PositionAction.AlignTop:
                    desiredRect.y = canvasRect.yMin;
                    successStatus = "Aligned top.";
                    break;
                case PanelView.PositionAction.AlignMiddle:
                    desiredRect.y = canvasRect.center.y - (currentRect.height * 0.5f);
                    successStatus = "Aligned middle.";
                    break;
                case PanelView.PositionAction.AlignBottom:
                    desiredRect.y = canvasRect.yMax - currentRect.height;
                    successStatus = "Aligned bottom.";
                    break;
            }

            Host.TryApplyTargetFrameRect(targetKey, desiredRect, successStatus);
        }

        private void ApplyRotateClockwiseAction(float deltaDegrees)
        {
            string targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                Host?.UpdateSourceStatus("Rotation requires a non-root target.");
                return;
            }

            var rotationSession = new ElementRotationSession();
            _view.CaptureState(_inspectorPanelState);
            if (!Host.TryGetTargetRotationPivotParentSpace(targetKey, out Vector2 parentPivot) ||
                !rotationSession.TryBegin(Host.CurrentDocument, targetKey, parentPivot) ||
                !rotationSession.TryBuildTransform(deltaDegrees, out string transform, out _))
            {
                Host?.UpdateSourceStatus("Rotation failed: transform update could not be prepared.");
                rotationSession.End();
                return;
            }

            _inspectorPanelState.Transform = transform;
            _inspectorPanelState.TransformEnabled = true;
            _inspectorPanelState.TrySyncTransformHelperFromText();
            _view.ApplyState(_inspectorPanelState);
            rotationSession.End();
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
            string targetKey = ResolveSelectedTargetKey();
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

            if (!Host.TryGetTargetParentWorldTransform(targetKey, out Matrix2D parentWorldTransform))
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

        private string ResolveSelectedTargetKey()
        {
            return _selectedTargetKeyAccessor?.Invoke() ?? string.Empty;
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
    }
}
