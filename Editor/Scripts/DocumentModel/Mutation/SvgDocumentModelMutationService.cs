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
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            if (!CanApplyAttributePatch(request))
            {
                error = "Patch request is not supported by the document model mutation path.";
                return false;
            }

            updatedDocumentModel = SvgDocumentModelCloneUtility.CloneDocumentModel(documentModel);
            if (!SvgDocumentModelMutationLookupUtility.TryResolveTargetNode(updatedDocumentModel, request.TargetKey, out SvgNodeModel targetNode))
            {
                error = $"Could not find target '{request.TargetKey}'.";
                return false;
            }

            Dictionary<string, string> attributes = SvgDocumentModelCloneUtility.CloneAttributes(targetNode.RawAttributes);
            ApplyAttribute(attributes, SvgAttributeName.FILL, request.Fill);
            ApplyAttribute(attributes, SvgAttributeName.STROKE, request.Stroke);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_WIDTH, request.StrokeWidth);
            ApplyAttribute(attributes, SvgAttributeName.OPACITY, request.Opacity);
            ApplyAttribute(attributes, SvgAttributeName.FILL_OPACITY, request.FillOpacity);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_OPACITY, request.StrokeOpacity);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_LINECAP, request.StrokeLinecap);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_LINEJOIN, request.StrokeLinejoin);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_DASHARRAY, request.StrokeDasharray);
            ApplyAttribute(attributes, SvgAttributeName.RX, request.CornerRadiusX);
            ApplyAttribute(attributes, SvgAttributeName.RY, request.CornerRadiusY);
            ApplyAttribute(attributes, SvgAttributeName.TRANSFORM, request.Transform);
            ApplyAttribute(attributes, SvgAttributeName.DISPLAY, request.Display);

            targetNode.RawAttributes = attributes;
            targetNode.References = SvgDocumentModelReferenceUtility.RebuildReferences(attributes);
            return TrySerializeUpdatedDocumentModel(updatedDocumentModel, out updatedSourceText, out error);
        }

        public bool TryReorderElementWithinSameParent(
            SvgDocumentModel documentModel,
            string elementKey,
            int targetChildIndex,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(documentModel, elementKey, out SvgNodeModel movedNode))
            {
                error = $"Could not find element '{elementKey}'.";
                return false;
            }

            if (!documentModel.TryGetNode(movedNode.ParentId, out SvgNodeModel parentNode) || parentNode == null)
            {
                error = "Moved element does not have a parent node.";
                return false;
            }

            return TryMoveElement(
                documentModel,
                elementKey,
                parentNode.LegacyElementKey,
                targetChildIndex,
                out updatedDocumentModel,
                out updatedSourceText,
                out error);
        }

        public bool TryMoveElement(
            SvgDocumentModel documentModel,
            string elementKey,
            string targetParentKey,
            int targetChildIndex,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            updatedDocumentModel = SvgDocumentModelCloneUtility.CloneDocumentModel(documentModel);
            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(updatedDocumentModel, elementKey, out SvgNodeModel movedNode))
            {
                error = $"Could not find element '{elementKey}'.";
                return false;
            }

            if (!updatedDocumentModel.TryGetNode(movedNode.ParentId, out SvgNodeModel sourceParentNode) || sourceParentNode == null)
            {
                error = "Moved element does not have a parent node.";
                return false;
            }

            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(updatedDocumentModel, targetParentKey, out SvgNodeModel targetParentNode))
            {
                error = $"Could not find target parent '{targetParentKey}'.";
                return false;
            }

            if (targetParentNode.Id == movedNode.Id ||
                SvgDocumentModelMutationLookupUtility.IsSameOrDescendantOf(updatedDocumentModel, targetParentNode, movedNode.Id))
            {
                error = "Cannot move an element into itself or its descendant.";
                return false;
            }

            bool movingWithinSameParent = sourceParentNode.Id == targetParentNode.Id;
            List<SvgNodeId> sourceChildren = new(sourceParentNode.Children ?? Array.Empty<SvgNodeId>());
            int sourceIndex = sourceChildren.IndexOf(movedNode.Id);
            if (sourceIndex < 0)
            {
                error = "Could not resolve the moved element index.";
                return false;
            }

            if (movingWithinSameParent)
            {
                int clampedTargetIndex = Math.Clamp(targetChildIndex, 0, sourceChildren.Count);
                if (sourceIndex < clampedTargetIndex)
                    clampedTargetIndex--;

                if (clampedTargetIndex == sourceIndex)
                {
                    updatedSourceText = documentModel.SourceText;
                    updatedDocumentModel.SourceText = updatedSourceText;
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
                int clampedTargetIndex = Math.Clamp(targetChildIndex, 0, targetChildren.Count);

                sourceChildren.RemoveAt(sourceIndex);
                targetChildren.Insert(clampedTargetIndex, movedNode.Id);

                sourceParentNode.Children = sourceChildren;
                targetParentNode.Children = targetChildren;
                movedNode.ParentId = targetParentNode.Id;

                SvgDocumentModelMutationLookupUtility.RefreshSiblingOrder(updatedDocumentModel, sourceParentNode);
                SvgDocumentModelMutationLookupUtility.RefreshSiblingOrder(updatedDocumentModel, targetParentNode);
            }

            return TrySerializeUpdatedDocumentModel(updatedDocumentModel, out updatedSourceText, out error);
        }

        public bool TryPrependElementTranslation(
            SvgDocumentModel documentModel,
            string elementKey,
            UnityEngine.Vector2 translation,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            if (UnityEngine.Mathf.Approximately(translation.x, 0f) &&
                UnityEngine.Mathf.Approximately(translation.y, 0f))
            {
                updatedDocumentModel = documentModel;
                updatedSourceText = documentModel?.SourceText ?? string.Empty;
                error = string.Empty;
                return true;
            }

            return TryApplyPrependTransformMutation(
                documentModel,
                elementKey,
                existingTransform => PrependTransform(existingTransform, TransformStringBuilder.BuildTranslate(translation)),
                out updatedDocumentModel,
                out updatedSourceText,
                out error);
        }

        public bool TryPrependElementScale(
            SvgDocumentModel documentModel,
            string elementKey,
            UnityEngine.Vector2 scale,
            UnityEngine.Vector2 pivot,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            if ((UnityEngine.Mathf.Approximately(scale.x, 1f) && UnityEngine.Mathf.Approximately(scale.y, 1f)) ||
                scale.x <= UnityEngine.Mathf.Epsilon ||
                scale.y <= UnityEngine.Mathf.Epsilon)
            {
                updatedDocumentModel = documentModel;
                updatedSourceText = documentModel?.SourceText ?? string.Empty;
                error = string.Empty;
                return true;
            }

            return TryApplyPrependTransformMutation(
                documentModel,
                elementKey,
                existingTransform => PrependTransform(existingTransform, TransformStringBuilder.BuildScaleAround(scale, pivot)),
                out updatedDocumentModel,
                out updatedSourceText,
                out error);
        }

        private bool TryApplyPrependTransformMutation(
            SvgDocumentModel documentModel,
            string elementKey,
            Func<string, string> buildUpdatedTransform,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            updatedDocumentModel = SvgDocumentModelCloneUtility.CloneDocumentModel(documentModel);
            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(updatedDocumentModel, elementKey, out SvgNodeModel node))
            {
                error = $"Could not find element '{elementKey}'.";
                return false;
            }

            Dictionary<string, string> attributes = SvgDocumentModelCloneUtility.CloneAttributes(node.RawAttributes);
            string existingTransform = attributes.TryGetValue(SvgAttributeName.TRANSFORM, out string transform)
                ? transform ?? string.Empty
                : string.Empty;
            attributes[SvgAttributeName.TRANSFORM] = buildUpdatedTransform(existingTransform);
            node.RawAttributes = attributes;
            node.References = SvgDocumentModelReferenceUtility.RebuildReferences(attributes);
            return TrySerializeUpdatedDocumentModel(updatedDocumentModel, out updatedSourceText, out error);
        }

        private bool TrySerializeUpdatedDocumentModel(
            SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (!_serializer.TrySerialize(updatedDocumentModel, out updatedSourceText, out error))
                return false;

            updatedDocumentModel.SourceText = updatedSourceText;
            return true;
        }

        private static string PrependTransform(string existingTransform, string transformSegment)
        {
            return string.IsNullOrWhiteSpace(existingTransform)
                ? transformSegment
                : $"{transformSegment} {existingTransform}";
        }

        private static void ApplyAttribute(Dictionary<string, string> attributes, string attributeName, string value)
        {
            if (value == null)
                return;

            if (string.IsNullOrWhiteSpace(value))
            {
                attributes.Remove(attributeName);
                return;
            }

            attributes[attributeName] = value.Trim();
        }
    }
}
