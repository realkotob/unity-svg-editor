using System;
using System.Linq;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Preview;
using SvgEditor.UI.Workspace.Document;
using SvgEditor.UI.Workspace.Host;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Svg.Source;
using Core.UI.Extensions;

namespace SvgEditor.UI.Workspace.Coordination
{
    internal sealed class MutationCoordinator
    {
        private readonly IEditorWorkspaceHost _host;
        private readonly SvgMutator _svgMutator = new();

        public MutationCoordinator(IEditorWorkspaceHost host)
        {
            _host = host;
        }

        private DocumentSession CurrentDocument => _host.CurrentDocument;

        public bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode)
        {
            if (CurrentDocument == null || request == null)
                return false;

            string editFailureReason = CurrentDocument.ResolveModelEditingFailureReason();
            if (!string.IsNullOrWhiteSpace(editFailureReason))
            {
                _host.UpdateSourceStatus($"Patch failed: {editFailureReason}");
                return false;
            }

            if (!_svgMutator.CanApplyAttributePatch(request))
            {
                _host.UpdateSourceStatus("Patch failed: Patch request is not supported by the document model mutation path.");
                return false;
            }

            if (!_svgMutator.TryApplyAttributePatch(
                    CurrentDocument.DocumentModel,
                    request,
                    out MutationResult result))
            {
                _host.UpdateSourceStatus($"Patch failed: {result.Error}");
                return false;
            }

            if (string.Equals(result.UpdatedSourceText, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                _host.UpdateSourceStatus("No patch changes were applied.");
                return false;
            }

            _host.ApplyUpdatedSource(result.UpdatedSourceText, successStatus, recordingMode);
            return true;
        }

        public bool TryApplyTargetFrameRect(TargetFrameRectRequest request)
        {
            if (CurrentDocument == null || string.IsNullOrWhiteSpace(request.TargetKey))
                return false;

            PreviewElementGeometry targetElement = _host.PreviewSnapshot?.Elements?
                .FirstOrDefault(item => string.Equals(item?.TargetKey, request.TargetKey, StringComparison.Ordinal));
            if (targetElement == null)
                return false;

            Rect currentSceneRect = targetElement.VisualBounds;
            if (!CurrentDocument.CanUseDocumentModelForEditing)
            {
                _host.UpdateSourceStatus($"Frame rect update failed: {CurrentDocument.ResolveModelEditingFailureReason()}");
                return false;
            }

            SvgDocumentModel workingDocumentModel = CurrentDocument.DocumentModel;
            string updatedSource = CurrentDocument.WorkingSourceText;
            bool hasChanged = false;

            if (currentSceneRect.width > Mathf.Epsilon &&
                currentSceneRect.height > Mathf.Epsilon &&
                (!Mathf.Approximately(currentSceneRect.width, request.TargetSceneRect.width) ||
                 !Mathf.Approximately(currentSceneRect.height, request.TargetSceneRect.height)))
            {
                Vector2 scale = new(
                    Mathf.Max(0f, request.TargetSceneRect.width) / currentSceneRect.width,
                    Mathf.Max(0f, request.TargetSceneRect.height) / currentSceneRect.height);
                Vector2 pivot = new(currentSceneRect.xMin, currentSceneRect.yMin);
                Vector2 parentPivot = ToParentSpacePoint(targetElement.ParentWorldTransform, pivot);
                bool scaleSucceeded = _svgMutator.TryPrependElementScale(
                    workingDocumentModel,
                    new ScaleElementRequest(targetElement.Key, scale, parentPivot),
                    out MutationResult scaleResult);

                if (!scaleSucceeded)
                {
                    return false;
                }

                workingDocumentModel = scaleResult.UpdatedDocumentModel;
                updatedSource = scaleResult.UpdatedSourceText;
                hasChanged = true;
            }

            Vector2 sceneDelta = new(
                request.TargetSceneRect.xMin - currentSceneRect.xMin,
                request.TargetSceneRect.yMin - currentSceneRect.yMin);
            if (sceneDelta.sqrMagnitude > Mathf.Epsilon)
            {
                Vector2 parentDelta = ToParentSpaceDelta(targetElement.ParentWorldTransform, sceneDelta);
                bool translateSucceeded = _svgMutator.TryPrependElementTranslation(
                    workingDocumentModel,
                    new TranslateElementRequest(targetElement.Key, parentDelta),
                    out MutationResult translateResult);

                if (!translateSucceeded)
                {
                    return false;
                }

                workingDocumentModel = translateResult.UpdatedDocumentModel;
                updatedSource = translateResult.UpdatedSourceText;
                hasChanged = true;
            }

            if (!hasChanged || string.Equals(updatedSource, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
                return false;

            _host.ApplyUpdatedSource(updatedSource, request.SuccessStatus, request.RecordingMode);
            return true;
        }

        private static Vector2 ToParentSpaceDelta(Matrix2D parentWorldTransform, Vector2 worldDelta)
        {
            return parentWorldTransform.Inverse().MultiplyVector(worldDelta);
        }

        private static Vector2 ToParentSpacePoint(Matrix2D parentWorldTransform, Vector2 worldPoint)
        {
            return parentWorldTransform.Inverse().MultiplyPoint(worldPoint);
        }
    }
}
