using System;
using System.Collections.Generic;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Lookup;
using SvgEditor.Shared;

namespace SvgEditor.DocumentModel
{
    internal sealed class SvgDocumentModelMutationService
    {
        private readonly SvgDocumentModelSerializer _serializer = new();

        public bool CanApplyAttributePatch(AttributePatchRequest request)
        {
            if (request == null)
                return false;

            return request.Fill != null ||
                   request.Stroke != null ||
                   request.StrokeWidth != null ||
                   request.Opacity != null ||
                   request.FillOpacity != null ||
                   request.StrokeOpacity != null ||
                   request.StrokeLinecap != null ||
                   request.StrokeLinejoin != null ||
                   request.StrokeDasharray != null ||
                   request.CornerRadiusX != null ||
                   request.CornerRadiusY != null ||
                   request.Transform != null ||
                   request.Display != null;
        }

        public bool TryApplyAttributePatch(
            SvgDocumentModel documentModel,
            AttributePatchRequest request,
            out MutationResult result)
        {
            result = default;
            string error = string.Empty;

            if (documentModel?.Root == null)
            {
                result = new MutationResult(null, string.Empty, "Document model is unavailable.");
                return false;
            }

            if (!CanApplyAttributePatch(request))
            {
                result = new MutationResult(null, string.Empty, "Patch request is not supported by the document model mutation path.");
                return false;
            }

            SvgDocumentModel updatedDocumentModel = SvgDocumentModelCloneUtility.CloneDocumentModel(documentModel);
            if (!SvgDocumentModelMutationLookupUtility.TryResolveTargetNode(updatedDocumentModel, request.TargetKey, out SvgNodeModel targetNode))
            {
                result = new MutationResult(null, string.Empty, $"Could not find target '{request.TargetKey}'.");
                return false;
            }

            SvgMutationWriter.ApplyAttributePatch(targetNode, request);
            if (!SvgMutationWriter.TrySerialize(_serializer, updatedDocumentModel, out string updatedSourceText, out error))
            {
                result = new MutationResult(null, string.Empty, error);
                return false;
            }

            result = new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty);
            return true;
        }

        public bool TryReorderElementWithinSameParent(
            SvgDocumentModel documentModel,
            ReorderElementRequest request,
            out MutationResult result)
        {
            result = default;

            if (documentModel?.Root == null)
            {
                result = new MutationResult(null, string.Empty, "Document model is unavailable.");
                return false;
            }

            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(documentModel, request.ElementKey, out SvgNodeModel movedNode))
            {
                result = new MutationResult(null, string.Empty, $"Could not find element '{request.ElementKey}'.");
                return false;
            }

            if (!documentModel.TryGetNode(movedNode.ParentId, out SvgNodeModel parentNode) || parentNode == null)
            {
                result = new MutationResult(null, string.Empty, "Moved element does not have a parent node.");
                return false;
            }

            return TryMoveElement(
                documentModel,
                new MoveElementRequest(request.ElementKey, parentNode.LegacyElementKey, request.TargetChildIndex),
                out result);
        }

        public bool TryMoveElement(
            SvgDocumentModel documentModel,
            MoveElementRequest request,
            out MutationResult result)
        {
            result = default;
            string error = string.Empty;

            if (documentModel?.Root == null)
            {
                result = new MutationResult(null, string.Empty, "Document model is unavailable.");
                return false;
            }

            SvgDocumentModel updatedDocumentModel = SvgDocumentModelCloneUtility.CloneDocumentModel(documentModel);
            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(updatedDocumentModel, request.ElementKey, out SvgNodeModel movedNode))
            {
                result = new MutationResult(null, string.Empty, $"Could not find element '{request.ElementKey}'.");
                return false;
            }

            if (!updatedDocumentModel.TryGetNode(movedNode.ParentId, out SvgNodeModel sourceParentNode) || sourceParentNode == null)
            {
                result = new MutationResult(null, string.Empty, "Moved element does not have a parent node.");
                return false;
            }

            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(updatedDocumentModel, request.TargetParentKey, out SvgNodeModel targetParentNode))
            {
                result = new MutationResult(null, string.Empty, $"Could not find target parent '{request.TargetParentKey}'.");
                return false;
            }

            if (targetParentNode.Id == movedNode.Id ||
                SvgDocumentModelMutationLookupUtility.IsSameOrDescendantOf(updatedDocumentModel, targetParentNode, movedNode.Id))
            {
                result = new MutationResult(null, string.Empty, "Cannot move an element into itself or its descendant.");
                return false;
            }

            bool movingWithinSameParent = sourceParentNode.Id == targetParentNode.Id;
            List<SvgNodeId> sourceChildren = new(sourceParentNode.Children ?? Array.Empty<SvgNodeId>());
            int sourceIndex = sourceChildren.IndexOf(movedNode.Id);
            if (sourceIndex < 0)
            {
                result = new MutationResult(null, string.Empty, "Could not resolve the moved element index.");
                return false;
            }

            if (movingWithinSameParent)
            {
                int clampedTargetIndex = Math.Clamp(request.TargetChildIndex, 0, sourceChildren.Count);
                if (sourceIndex < clampedTargetIndex)
                    clampedTargetIndex--;

                if (clampedTargetIndex == sourceIndex)
                {
                    updatedDocumentModel.SourceText = documentModel.SourceText;
                    result = new MutationResult(updatedDocumentModel, documentModel.SourceText, string.Empty);
                    return true;
                }

                sourceChildren.RemoveAt(sourceIndex);
                sourceChildren.Insert(clampedTargetIndex, movedNode.Id);
                sourceParentNode.Children = sourceChildren;
                SvgDocumentModelMutationLookupUtility.RefreshSiblingOrder(updatedDocumentModel, sourceParentNode);
            }
            else
            {
                List<SvgNodeId> targetChildren = new(targetParentNode.Children ?? Array.Empty<SvgNodeId>());
                int clampedTargetIndex = Math.Clamp(request.TargetChildIndex, 0, targetChildren.Count);

                sourceChildren.RemoveAt(sourceIndex);
                targetChildren.Insert(clampedTargetIndex, movedNode.Id);

                sourceParentNode.Children = sourceChildren;
                targetParentNode.Children = targetChildren;
                movedNode.ParentId = targetParentNode.Id;

                SvgDocumentModelMutationLookupUtility.RefreshSiblingOrder(updatedDocumentModel, sourceParentNode);
                SvgDocumentModelMutationLookupUtility.RefreshSiblingOrder(updatedDocumentModel, targetParentNode);
            }

            if (!SvgMutationWriter.TrySerialize(_serializer, updatedDocumentModel, out string updatedSourceText, out error))
            {
                result = new MutationResult(null, string.Empty, error);
                return false;
            }

            result = new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty);
            return true;
        }

        public bool TryPrependElementTranslation(
            SvgDocumentModel documentModel,
            TranslateElementRequest request,
            out MutationResult result)
        {
            if (UnityEngine.Mathf.Approximately(request.Translation.x, 0f) &&
                UnityEngine.Mathf.Approximately(request.Translation.y, 0f))
            {
                result = new MutationResult(documentModel, documentModel?.SourceText ?? string.Empty, string.Empty);
                return true;
            }

            return TryApplyPrependTransformMutation(
                documentModel,
                request.ElementKey,
                existingTransform => SvgMutationWriter.PrependTransform(existingTransform, TransformStringBuilder.BuildTranslate(request.Translation)),
                out result);
        }

        public bool TryPrependElementScale(
            SvgDocumentModel documentModel,
            ScaleElementRequest request,
            out MutationResult result)
        {
            if ((UnityEngine.Mathf.Approximately(request.Scale.x, 1f) && UnityEngine.Mathf.Approximately(request.Scale.y, 1f)) ||
                request.Scale.x <= UnityEngine.Mathf.Epsilon ||
                request.Scale.y <= UnityEngine.Mathf.Epsilon)
            {
                result = new MutationResult(documentModel, documentModel?.SourceText ?? string.Empty, string.Empty);
                return true;
            }

            return TryApplyPrependTransformMutation(
                documentModel,
                request.ElementKey,
                existingTransform => SvgMutationWriter.PrependTransform(existingTransform, TransformStringBuilder.BuildScaleAround(request.Scale, request.Pivot)),
                out result);
        }

        private bool TryApplyPrependTransformMutation(
            SvgDocumentModel documentModel,
            string elementKey,
            Func<string, string> buildUpdatedTransform,
            out MutationResult result)
        {
            result = default;
            string error = string.Empty;

            if (documentModel?.Root == null)
            {
                result = new MutationResult(null, string.Empty, "Document model is unavailable.");
                return false;
            }

            SvgDocumentModel updatedDocumentModel = SvgDocumentModelCloneUtility.CloneDocumentModel(documentModel);
            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(updatedDocumentModel, elementKey, out SvgNodeModel node))
            {
                result = new MutationResult(null, string.Empty, $"Could not find element '{elementKey}'.");
                return false;
            }

            SvgMutationWriter.ApplyPrependTransform(node, buildUpdatedTransform);
            if (!SvgMutationWriter.TrySerialize(_serializer, updatedDocumentModel, out string updatedSourceText, out error))
            {
                result = new MutationResult(null, string.Empty, error);
                return false;
            }

            result = new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty);
            return true;
        }
    }
}
