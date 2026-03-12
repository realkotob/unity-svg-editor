using System;
using System.Linq;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Preview;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.Host;
using SvgEditor.DocumentModel;
using SvgEditor.Document;

namespace SvgEditor.Workspace.Coordination
{
    internal sealed class MutationCoordinator
    {
        private readonly IEditorWorkspaceHost _host;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly SvgDocumentModelMutationService _documentModelMutationService = new();

        public MutationCoordinator(IEditorWorkspaceHost host, Func<DocumentSession> currentDocumentAccessor)
        {
            _host = host;
            _currentDocumentAccessor = currentDocumentAccessor;
        }

        private DocumentSession CurrentDocument => _currentDocumentAccessor?.Invoke();

        public bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode)
        {
            if (CurrentDocument == null || request == null)
                return false;

            if (CurrentDocument.DocumentModel == null)
            {
                _host.UpdateSourceStatus("Patch failed: Document model is unavailable.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CurrentDocument.DocumentModelLoadError))
            {
                _host.UpdateSourceStatus($"Patch failed: {CurrentDocument.DocumentModelLoadError}");
                return false;
            }

            if (!string.Equals(CurrentDocument.DocumentModel.SourceText, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                _host.UpdateSourceStatus("Patch failed: Document model is out of sync with the working source.");
                return false;
            }

            if (!_documentModelMutationService.CanApplyAttributePatch(request))
            {
                _host.UpdateSourceStatus("Patch failed: Patch request is not supported by the document model mutation path.");
                return false;
            }

            if (!_documentModelMutationService.TryApplyAttributePatch(
                    CurrentDocument.DocumentModel,
                    request,
                    out SvgDocumentModel _,
                    out string patched,
                    out string error))
            {
                _host.UpdateSourceStatus($"Patch failed: {error}");
                return false;
            }

            if (string.Equals(patched, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                _host.UpdateSourceStatus("No patch changes were applied.");
                return false;
            }

            _host.ApplyUpdatedSource(patched, successStatus, recordingMode);
            return true;
        }

        public bool TryApplyTargetFrameRect(
            string targetKey,
            Rect targetSceneRect,
            string successStatus,
            HistoryRecordingMode recordingMode)
        {
            if (CurrentDocument == null || string.IsNullOrWhiteSpace(targetKey))
                return false;

            PreviewElementGeometry targetElement = _host.PreviewSnapshot?.Elements?
                .FirstOrDefault(item => string.Equals(item?.TargetKey, targetKey, StringComparison.Ordinal));
            if (targetElement == null)
                return false;

            Rect currentSceneRect = targetElement.VisualBounds;
            if (CurrentDocument.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(CurrentDocument.DocumentModelLoadError) ||
                !string.Equals(CurrentDocument.DocumentModel.SourceText, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                return false;
            }

            SvgDocumentModel workingDocumentModel = CurrentDocument.DocumentModel;
            string updatedSource = CurrentDocument.WorkingSourceText;
            bool hasChanged = false;

            if (currentSceneRect.width > Mathf.Epsilon &&
                currentSceneRect.height > Mathf.Epsilon &&
                (!Mathf.Approximately(currentSceneRect.width, targetSceneRect.width) ||
                 !Mathf.Approximately(currentSceneRect.height, targetSceneRect.height)))
            {
                Vector2 scale = new(
                    Mathf.Max(0f, targetSceneRect.width) / currentSceneRect.width,
                    Mathf.Max(0f, targetSceneRect.height) / currentSceneRect.height);
                Vector2 pivot = new(currentSceneRect.xMin, currentSceneRect.yMin);
                Vector2 parentPivot = ToParentSpacePoint(targetElement.ParentWorldTransform, pivot);
                bool scaleSucceeded = _documentModelMutationService.TryPrependElementScale(
                    workingDocumentModel,
                    targetElement.Key,
                    scale,
                    parentPivot,
                    out workingDocumentModel,
                    out updatedSource,
                    out _);

                if (!scaleSucceeded)
                {
                    return false;
                }

                hasChanged = true;
            }

            Vector2 sceneDelta = new(
                targetSceneRect.xMin - currentSceneRect.xMin,
                targetSceneRect.yMin - currentSceneRect.yMin);
            if (sceneDelta.sqrMagnitude > Mathf.Epsilon)
            {
                Vector2 parentDelta = ToParentSpaceDelta(targetElement.ParentWorldTransform, sceneDelta);
                bool translateSucceeded = _documentModelMutationService.TryPrependElementTranslation(
                    workingDocumentModel,
                    targetElement.Key,
                    parentDelta,
                    out workingDocumentModel,
                    out updatedSource,
                    out _);

                if (!translateSucceeded)
                {
                    return false;
                }

                hasChanged = true;
            }

            if (!hasChanged || string.Equals(updatedSource, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
                return false;

            _host.ApplyUpdatedSource(updatedSource, successStatus, recordingMode);
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
