using System.Collections.Generic;
using UnityEngine;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Shared;
using SvgEditor.UI.Workspace.Transforms;

namespace SvgEditor.UI.Canvas
{
    internal sealed class DragRotateMutationPipeline
    {
        public bool TryBuildPreview(
            ICanvasPointerDragHost host,
            DragSelectionState selection,
            DragRotationState rotation,
            ElementRotationSession rotationSession,
            Vector2 localPosition,
            bool snapEnabled,
            out SvgDocumentModel previewDocumentModel)
        {
            PreviewMutation preview = new(
                host,
                selection,
                rotation,
                rotationSession,
                localPosition,
                snapEnabled);
            return preview.TryBuild(out previewDocumentModel);
        }

        public bool TryBuildCommittedSource(
            DocumentSession currentDocument,
            DragSelectionState selection,
            DragRotationState rotation,
            out string updatedSource,
            out string error)
        {
            return new GroupRotateMutation(currentDocument, selection, rotation)
                .TryBuildSource(out updatedSource, out error);
        }

        private static bool HasGroupTargets(DragSelectionState selection)
        {
            return selection.MoveTargets != null && selection.MoveTargets.Count > 1;
        }

        private sealed class PreviewMutation
        {
            private readonly ICanvasPointerDragHost _host;
            private readonly DragSelectionState _selection;
            private readonly DragRotationState _rotation;
            private readonly ElementRotationSession _rotationSession;
            private readonly Vector2 _localPosition;
            private readonly bool _snapEnabled;

            public PreviewMutation(
                ICanvasPointerDragHost host,
                DragSelectionState selection,
                DragRotationState rotation,
                ElementRotationSession rotationSession,
                Vector2 localPosition,
                bool snapEnabled)
            {
                _host = host;
                _selection = selection;
                _rotation = rotation;
                _rotationSession = rotationSession;
                _localPosition = localPosition;
                _snapEnabled = snapEnabled;
            }

            public bool TryBuild(out SvgDocumentModel previewDocumentModel)
            {
                previewDocumentModel = null;
                if (!CanBuild() || !TryUpdateCurrentAngle())
                {
                    return false;
                }

                return HasGroupTargets(_selection)
                    ? new GroupRotateMutation(_host.CurrentDocument, _selection, _rotation)
                        .TryBuildPreview(out previewDocumentModel)
                    : _rotationSession.TryBuildPreview(_rotation.CurrentAngle, out previewDocumentModel, out _);
            }

            private bool CanBuild()
            {
                return _host.CurrentDocument != null &&
                       !string.IsNullOrWhiteSpace(_selection.ElementKey) &&
                       _rotation.StartVector.sqrMagnitude > Mathf.Epsilon;
            }

            private bool TryUpdateCurrentAngle()
            {
                Vector2 currentRotateVector = _localPosition - _rotation.StartPivotViewport;
                if (currentRotateVector.sqrMagnitude <= Mathf.Epsilon)
                {
                    return false;
                }

                float rotationAngle = Vector2.SignedAngle(_rotation.StartVector, currentRotateVector);
                _rotation.CurrentAngle = _snapEnabled
                    ? SnapUtility.SnapAngle(rotationAngle)
                    : rotationAngle;
                return true;
            }
        }

        private sealed class GroupRotateMutation
        {
            private readonly DocumentSession _currentDocument;
            private readonly DragSelectionState _selection;
            private readonly DragRotationState _rotation;

            public GroupRotateMutation(
                DocumentSession currentDocument,
                DragSelectionState selection,
                DragRotationState rotation)
            {
                _currentDocument = currentDocument;
                _selection = selection;
                _rotation = rotation;
            }

            public bool TryBuildPreview(out SvgDocumentModel previewDocumentModel)
            {
                return TryRotateTargets(out previewDocumentModel, out _);
            }

            public bool TryBuildSource(out string updatedSource, out string error)
            {
                updatedSource = string.Empty;
                error = string.Empty;
                if (!TryRotateTargets(out SvgDocumentModel updatedDocumentModel, out error))
                {
                    return false;
                }

                updatedSource = updatedDocumentModel?.SourceText ?? string.Empty;
                return !string.IsNullOrWhiteSpace(updatedSource);
            }

            private bool TryRotateTargets(
                out SvgDocumentModel updatedDocumentModel,
                out string error)
            {
                updatedDocumentModel = null;
                error = string.Empty;

                IReadOnlyList<ElementMoveTarget> moveTargets = _selection.MoveTargets;
                if (_currentDocument?.DocumentModel == null || moveTargets == null || moveTargets.Count == 0)
                {
                    error = "Rotation session is unavailable.";
                    return false;
                }

                SvgMutator svgMutator = new();
                SvgDocumentModel workingDocumentModel = _currentDocument.DocumentModel;
                foreach (ElementMoveTarget moveTarget in moveTargets)
                {
                    if (string.IsNullOrWhiteSpace(moveTarget.ElementKey))
                    {
                        continue;
                    }

                    Vector2 parentPivot = ElementRotationUtility.ToParentSpacePoint(
                        moveTarget.ParentWorldTransform,
                        _rotation.StartPivotWorld);
                    if (!svgMutator.TryPrependElementRotation(
                            workingDocumentModel,
                            new RotateElementRequest(moveTarget.ElementKey, _rotation.CurrentAngle, parentPivot),
                            out MutationResult result))
                    {
                        error = result.Error;
                        return false;
                    }

                    workingDocumentModel = result.UpdatedDocumentModel;
                }

                updatedDocumentModel = workingDocumentModel;
                return updatedDocumentModel != null;
            }
        }
    }
}
