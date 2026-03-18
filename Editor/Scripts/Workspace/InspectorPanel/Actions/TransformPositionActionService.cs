using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Document;
using SvgEditor.DocumentModel;
using SvgEditor.Workspace.Coordination;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.Transforms;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class TransformPositionActionService
    {
        private delegate bool TryMutateSelectedElement(
            SvgDocumentModelMutationService mutationService,
            SvgDocumentModel workingDocumentModel,
            string elementKey,
            out MutationResult result,
            out bool applied,
            out string error);

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

            Host.TryApplyTargetFrameRect(new TargetFrameRectRequest(
                targetKey,
                desiredSceneRect,
                "Frame rect updated.",
                HistoryRecordingMode.Coalesced));
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
            IReadOnlyList<string> selectedElementKeys = ResolveMultiSelectedElementKeys();
            if (selectedElementKeys.Count > 1)
            {
                ApplyMultiAlignmentAction(action, selectedElementKeys);
                return;
            }

            string targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                Host?.UpdateSourceStatus("Alignment requires a non-root target.");
                return;
            }

            if (!Host.TryGetTargetSceneRect(targetKey, out Rect currentRect) ||
                !Host.TryGetViewportSceneRect(out Rect canvasRect))
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

            Host.TryApplyTargetFrameRect(new TargetFrameRectRequest(targetKey, desiredRect, successStatus));
        }

        private void ApplyRotateClockwiseAction(float deltaDegrees)
        {
            IReadOnlyList<string> selectedElementKeys = ResolveMultiSelectedElementKeys();
            if (selectedElementKeys.Count > 1)
            {
                ApplyMultiRotateAction(deltaDegrees, selectedElementKeys);
                return;
            }

            string targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                Host?.UpdateSourceStatus("Rotation requires a non-root target.");
                return;
            }

            var rotationSession = new ElementRotationSession();
            _view.CaptureState(_inspectorPanelState);
            if (!Host.TryGetRotationPivotParentSpace(targetKey, out Vector2 parentPivot) ||
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
            IReadOnlyList<string> selectedElementKeys = ResolveMultiSelectedElementKeys();
            if (selectedElementKeys.Count > 1)
            {
                ApplyMultiFlipAction(scale, successStatus, selectedElementKeys);
                return;
            }

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

            if (!TryBuildMultiScaleSource(new[] { targetKey }, scale, sceneRect.center, out string updatedSource, out string error))
            {
                Host?.UpdateSourceStatus(
                    string.IsNullOrWhiteSpace(error)
                        ? "Flip failed: transform update could not be prepared."
                        : $"Flip failed: {error}");
                return;
            }

            Host.ApplyUpdatedSource(updatedSource, successStatus, HistoryRecordingMode.Coalesced);
        }

        private void ApplyMultiAlignmentAction(PanelView.PositionAction action, IReadOnlyList<string> selectedElementKeys)
        {
            string error = string.Empty;
            if (Host == null ||
                !Host.TryGetCurrentSelectionSceneRect(out Rect currentSelectionRect))
            {
                Host?.UpdateSourceStatus("Alignment failed: preview bounds are unavailable.");
                return;
            }

            string successStatus = action switch
            {
                PanelView.PositionAction.AlignLeft => "Aligned left.",
                PanelView.PositionAction.AlignCenter => "Aligned center.",
                PanelView.PositionAction.AlignRight => "Aligned right.",
                PanelView.PositionAction.AlignTop => "Aligned top.",
                PanelView.PositionAction.AlignMiddle => "Aligned middle.",
                PanelView.PositionAction.AlignBottom => "Aligned bottom.",
                _ => "Position updated."
            };

            if (!TryBuildAlignedSelectionSource(selectedElementKeys, currentSelectionRect, action, out string updatedSource, out error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Host.UpdateSourceStatus($"Alignment failed: {error}");
                }

                return;
            }

            Host.ApplyUpdatedSource(updatedSource, successStatus, HistoryRecordingMode.Coalesced);
        }

        private void ApplyMultiRotateAction(float deltaDegrees, IReadOnlyList<string> selectedElementKeys)
        {
            string error = string.Empty;
            if (Host == null ||
                !Host.TryGetCurrentSelectionSceneRect(out Rect selectionRect) ||
                !TryBuildMultiRotationSource(selectedElementKeys, deltaDegrees, selectionRect.center, out string updatedSource, out error))
            {
                Host?.UpdateSourceStatus(
                    string.IsNullOrWhiteSpace(error)
                        ? "Rotation failed: transform update could not be prepared."
                        : $"Rotation failed: {error}");
                return;
            }

            Host.ApplyUpdatedSource(updatedSource, $"Rotated {deltaDegrees:+0;-0}°.", HistoryRecordingMode.Coalesced);
        }

        private void ApplyMultiFlipAction(Vector2 scale, string successStatus, IReadOnlyList<string> selectedElementKeys)
        {
            string error = string.Empty;
            if (Host == null ||
                !Host.TryGetCurrentSelectionSceneRect(out Rect selectionRect) ||
                !TryBuildMultiScaleSource(selectedElementKeys, scale, selectionRect.center, out string updatedSource, out error))
            {
                Host?.UpdateSourceStatus(
                    string.IsNullOrWhiteSpace(error)
                        ? "Flip failed: transform update could not be prepared."
                        : $"Flip failed: {error}");
                return;
            }

            Host.ApplyUpdatedSource(updatedSource, successStatus, HistoryRecordingMode.Coalesced);
        }

        private IReadOnlyList<string> ResolveMultiSelectedElementKeys()
        {
            return Host?.SelectedElementKeys != null && Host.SelectedElementKeys.Count > 1
                ? Host.SelectedElementKeys
                : Array.Empty<string>();
        }

        private bool TryBuildAlignedSelectionSource(
            IReadOnlyList<string> selectedElementKeys,
            Rect selectionRect,
            PanelView.PositionAction action,
            out string updatedSource,
            out string error)
        {
            return TryBuildSelectionSource(
                selectedElementKeys,
                (
                    SvgDocumentModelMutationService mutationService,
                    SvgDocumentModel workingDocumentModel,
                    string elementKey,
                    out MutationResult result,
                    out bool applied,
                    out string mutationError) =>
                {
                    result = default;
                    applied = false;
                    mutationError = string.Empty;

                    if (string.IsNullOrWhiteSpace(elementKey) ||
                        !Host.TryGetElementSceneRect(elementKey, out Rect elementRect) ||
                        !Host.TryGetElementParentWorldTransform(elementKey, out Matrix2D parentWorldTransform))
                    {
                        mutationError = $"Could not resolve preview geometry for '{elementKey}'.";
                        return false;
                    }

                    Vector2 sceneDelta = action switch
                    {
                        PanelView.PositionAction.AlignLeft => new Vector2(selectionRect.xMin - elementRect.xMin, 0f),
                        PanelView.PositionAction.AlignCenter => new Vector2(selectionRect.center.x - elementRect.center.x, 0f),
                        PanelView.PositionAction.AlignRight => new Vector2(selectionRect.xMax - elementRect.xMax, 0f),
                        PanelView.PositionAction.AlignTop => new Vector2(0f, selectionRect.yMin - elementRect.yMin),
                        PanelView.PositionAction.AlignMiddle => new Vector2(0f, selectionRect.center.y - elementRect.center.y),
                        PanelView.PositionAction.AlignBottom => new Vector2(0f, selectionRect.yMax - elementRect.yMax),
                        _ => Vector2.zero
                    };

                    if (sceneDelta.sqrMagnitude <= Mathf.Epsilon)
                        return true;

                    applied = mutationService.TryPrependElementTranslation(
                        workingDocumentModel,
                        new TranslateElementRequest(elementKey, parentWorldTransform.Inverse().MultiplyVector(sceneDelta)),
                        out result);
                    if (!applied)
                        mutationError = result.Error;

                    return applied;
                },
                out updatedSource,
                out error);
        }

        private bool TryBuildMultiRotationSource(
            IReadOnlyList<string> selectedElementKeys,
            float deltaDegrees,
            Vector2 pivotWorld,
            out string updatedSource,
            out string error)
        {
            return TryBuildSelectionSource(
                selectedElementKeys,
                (
                    SvgDocumentModelMutationService mutationService,
                    SvgDocumentModel workingDocumentModel,
                    string elementKey,
                    out MutationResult result,
                    out bool applied,
                    out string mutationError) =>
                {
                    result = default;
                    applied = false;
                    mutationError = string.Empty;

                    if (string.IsNullOrWhiteSpace(elementKey) ||
                        !Host.TryGetElementParentWorldTransform(elementKey, out Matrix2D parentWorldTransform))
                    {
                        mutationError = $"Could not resolve parent transform for '{elementKey}'.";
                        return false;
                    }

                    applied = mutationService.TryPrependElementRotation(
                        workingDocumentModel,
                        new RotateElementRequest(elementKey, deltaDegrees, ElementRotationUtility.ToParentSpacePoint(parentWorldTransform, pivotWorld)),
                        out result);
                    if (!applied)
                        mutationError = result.Error;

                    return applied;
                },
                out updatedSource,
                out error);
        }

        private bool TryBuildMultiScaleSource(
            IReadOnlyList<string> selectedElementKeys,
            Vector2 scale,
            Vector2 pivotWorld,
            out string updatedSource,
            out string error)
        {
            return TryBuildSelectionSource(
                selectedElementKeys,
                (
                    SvgDocumentModelMutationService mutationService,
                    SvgDocumentModel workingDocumentModel,
                    string elementKey,
                    out MutationResult result,
                    out bool applied,
                    out string mutationError) =>
                {
                    result = default;
                    applied = false;
                    mutationError = string.Empty;

                    if (string.IsNullOrWhiteSpace(elementKey) ||
                        !Host.TryGetElementParentWorldTransform(elementKey, out Matrix2D parentWorldTransform))
                    {
                        mutationError = $"Could not resolve parent transform for '{elementKey}'.";
                        return false;
                    }

                    applied = mutationService.TryPrependElementScale(
                        workingDocumentModel,
                        new ScaleElementRequest(elementKey, scale, ElementRotationUtility.ToParentSpacePoint(parentWorldTransform, pivotWorld)),
                        out result);
                    if (!applied)
                        mutationError = result.Error;

                    return applied;
                },
                out updatedSource,
                out error);
        }

        private bool TryBuildSelectionSource(
            IReadOnlyList<string> selectedElementKeys,
            TryMutateSelectedElement tryMutate,
            out string updatedSource,
            out string error)
        {
            updatedSource = string.Empty;
            error = string.Empty;

            if (!TryGetWorkingDocumentModel(out SvgDocumentModel workingDocumentModel, out error))
                return false;

            SvgDocumentModelMutationService mutationService = new();
            bool appliedAny = false;
            foreach (string elementKey in selectedElementKeys)
            {
                if (!tryMutate(
                        mutationService,
                        workingDocumentModel,
                        elementKey,
                        out MutationResult result,
                        out bool applied,
                        out error))
                {
                    return false;
                }

                if (!applied)
                    continue;

                workingDocumentModel = result.UpdatedDocumentModel;
                appliedAny = true;
            }

            if (!appliedAny)
                return false;

            updatedSource = workingDocumentModel?.SourceText ?? string.Empty;
            return !string.IsNullOrWhiteSpace(updatedSource);
        }

        private bool TryGetWorkingDocumentModel(out SvgDocumentModel workingDocumentModel, out string error)
        {
            workingDocumentModel = Host?.CurrentDocument?.DocumentModel;
            error = string.Empty;
            if (workingDocumentModel != null)
                return true;

            error = "Document model is unavailable.";
            return false;
        }

        private string ResolveSelectedTargetKey()
        {
            return _selectedTargetKeyAccessor?.Invoke() ?? string.Empty;
        }

    }
}
