using System;
using System.Collections.Generic;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Structure.Lookup;
using SvgEditor.Core.Svg.Serialization;
using SvgEditor.Core.Svg.Transforms;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Mutation
{
    internal sealed class SvgMutator
    {
        private readonly SvgSerializer _serializer = new();

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
            Result<MutationResult> mutation = ApplyAttributePatch(documentModel, request);
            result = ToPublicResult(mutation);
            return mutation.IsSuccess;
        }

        public bool TryReorderElementWithinSameParent(
            SvgDocumentModel documentModel,
            ReorderElementRequest request,
            out MutationResult result)
        {
            Result<MutationResult> mutation = ReorderElementWithinSameParent(documentModel, request);
            result = ToPublicResult(mutation);
            return mutation.IsSuccess;
        }

        public bool TryMoveElement(
            SvgDocumentModel documentModel,
            MoveElementRequest request,
            out MutationResult result)
        {
            Result<MutationResult> mutation = MoveElement(documentModel, request);
            result = ToPublicResult(mutation);
            return mutation.IsSuccess;
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
                existingTransform => MutationWriter.PrependTransform(existingTransform, TransformBuilder.BuildTranslate(request.Translation)),
                out result);
        }

        public bool TryPrependElementScale(
            SvgDocumentModel documentModel,
            ScaleElementRequest request,
            out MutationResult result)
        {
            if ((UnityEngine.Mathf.Approximately(request.Scale.x, 1f) && UnityEngine.Mathf.Approximately(request.Scale.y, 1f)) ||
                UnityEngine.Mathf.Abs(request.Scale.x) <= UnityEngine.Mathf.Epsilon ||
                UnityEngine.Mathf.Abs(request.Scale.y) <= UnityEngine.Mathf.Epsilon)
            {
                result = new MutationResult(documentModel, documentModel?.SourceText ?? string.Empty, string.Empty);
                return true;
            }

            return TryApplyPrependTransformMutation(
                documentModel,
                request.ElementKey,
                existingTransform => MutationWriter.PrependTransform(existingTransform, TransformBuilder.BuildScaleAround(request.Scale, request.Pivot)),
                out result);
        }

        public bool TryPrependElementRotation(
            SvgDocumentModel documentModel,
            RotateElementRequest request,
            out MutationResult result)
        {
            if (UnityEngine.Mathf.Approximately(request.Angle, 0f))
            {
                result = new MutationResult(documentModel, documentModel?.SourceText ?? string.Empty, string.Empty);
                return true;
            }

            return TryApplyPrependTransformMutation(
                documentModel,
                request.ElementKey,
                existingTransform => MutationWriter.PrependTransform(existingTransform, TransformBuilder.BuildRotateAround(request.Angle, request.Pivot)),
                out result);
        }

        private Result<MutationResult> ApplyAttributePatch(SvgDocumentModel documentModel, AttributePatchRequest request)
        {
            Result<SvgDocumentModel> validation = ValidateDocumentModel(documentModel);
            if (validation.IsFailure)
                return Result.Failure<MutationResult>(validation.Error);

            if (!CanApplyAttributePatch(request))
            {
                return Result.Failure<MutationResult>("Patch request is not supported by the document model mutation path.");
            }

            SvgDocumentModel updatedDocumentModel = ModelClone.Create(documentModel);
            if (!MutationLookup.TryResolveTargetNode(updatedDocumentModel, request.TargetKey, out SvgNodeModel targetNode))
            {
                return Result.Failure<MutationResult>($"Could not find target '{request.TargetKey}'.");
            }

            MutationWriter.ApplyAttributePatch(targetNode, request);
            return SerializeResult(updatedDocumentModel);
        }

        private Result<MutationResult> ReorderElementWithinSameParent(SvgDocumentModel documentModel, ReorderElementRequest request)
        {
            Result<SvgDocumentModel> validation = ValidateDocumentModel(documentModel);
            if (validation.IsFailure)
                return Result.Failure<MutationResult>(validation.Error);

            if (!NodeLookup.TryFindNodeByLegacyElementKey(documentModel, request.ElementKey, out SvgNodeModel movedNode))
            {
                return Result.Failure<MutationResult>($"Could not find element '{request.ElementKey}'.");
            }

            if (!documentModel.TryGetNode(movedNode.ParentId, out SvgNodeModel parentNode) || parentNode == null)
            {
                return Result.Failure<MutationResult>("Moved element does not have a parent node.");
            }

            return MoveElement(
                documentModel,
                new MoveElementRequest(request.ElementKey, parentNode.LegacyElementKey, request.TargetChildIndex));
        }

        private Result<MutationResult> MoveElement(SvgDocumentModel documentModel, MoveElementRequest request)
        {
            Result<SvgDocumentModel> validation = ValidateDocumentModel(documentModel);
            if (validation.IsFailure)
                return Result.Failure<MutationResult>(validation.Error);

            SvgDocumentModel updatedDocumentModel = ModelClone.Create(documentModel);
            if (!NodeLookup.TryFindNodeByLegacyElementKey(updatedDocumentModel, request.ElementKey, out SvgNodeModel movedNode))
            {
                return Result.Failure<MutationResult>($"Could not find element '{request.ElementKey}'.");
            }

            if (!updatedDocumentModel.TryGetNode(movedNode.ParentId, out SvgNodeModel sourceParentNode) || sourceParentNode == null)
            {
                return Result.Failure<MutationResult>("Moved element does not have a parent node.");
            }

            if (!NodeLookup.TryFindNodeByLegacyElementKey(updatedDocumentModel, request.TargetParentKey, out SvgNodeModel targetParentNode))
            {
                return Result.Failure<MutationResult>($"Could not find target parent '{request.TargetParentKey}'.");
            }

            if (targetParentNode.Id == movedNode.Id ||
                MutationLookup.IsSameOrDescendantOf(updatedDocumentModel, targetParentNode, movedNode.Id))
            {
                return Result.Failure<MutationResult>("Cannot move an element into itself or its descendant.");
            }

            if (sourceParentNode.Id == targetParentNode.Id)
            {
                return ReorderWithinParent(
                    documentModel.SourceText,
                    updatedDocumentModel,
                    request.TargetChildIndex,
                    movedNode,
                    sourceParentNode);
            }

            return MoveAcrossParents(
                updatedDocumentModel,
                request.TargetChildIndex,
                movedNode,
                sourceParentNode,
                targetParentNode);
        }

        private bool TryApplyPrependTransformMutation(
            SvgDocumentModel documentModel,
            string elementKey,
            Func<string, string> buildUpdatedTransform,
            out MutationResult result)
        {
            Result<MutationResult> mutation = ApplyTransformMutation(documentModel, elementKey, buildUpdatedTransform);
            result = ToPublicResult(mutation);
            return mutation.IsSuccess;
        }

        private static MutationResult ToPublicResult(Result<MutationResult> mutation)
        {
            return mutation.IsSuccess
                ? mutation.Value
                : new MutationResult(null, string.Empty, mutation.Error);
        }

        private static Result<SvgDocumentModel> ValidateDocumentModel(SvgDocumentModel documentModel)
        {
            if (documentModel?.Root != null)
                return Result.Success(documentModel);

            return Result.Failure<SvgDocumentModel>("Document model is unavailable.");
        }

        private Result<MutationResult> ReorderWithinParent(
            string sourceText,
            SvgDocumentModel updatedDocumentModel,
            int targetChildIndex,
            SvgNodeModel movedNode,
            SvgNodeModel parentNode)
        {
            List<SvgNodeId> children = new(parentNode.Children ?? Array.Empty<SvgNodeId>());
            int sourceIndex = children.IndexOf(movedNode.Id);
            if (sourceIndex < 0)
            {
                return Result.Failure<MutationResult>("Could not resolve the moved element index.");
            }

            int clampedTargetIndex = Math.Clamp(targetChildIndex, 0, children.Count);
            if (sourceIndex < clampedTargetIndex)
                clampedTargetIndex--;

            if (clampedTargetIndex == sourceIndex)
            {
                updatedDocumentModel.SourceText = sourceText;
                return Result.Success(new MutationResult(updatedDocumentModel, sourceText, string.Empty));
            }

            children.RemoveAt(sourceIndex);
            children.Insert(clampedTargetIndex, movedNode.Id);
            parentNode.Children = children;
            MutationLookup.RefreshSiblingOrder(updatedDocumentModel, parentNode);

            return SerializeResult(updatedDocumentModel);
        }

        private Result<MutationResult> MoveAcrossParents(
            SvgDocumentModel updatedDocumentModel,
            int targetChildIndex,
            SvgNodeModel movedNode,
            SvgNodeModel sourceParentNode,
            SvgNodeModel targetParentNode)
        {
            List<SvgNodeId> sourceChildren = new(sourceParentNode.Children ?? Array.Empty<SvgNodeId>());
            int sourceIndex = sourceChildren.IndexOf(movedNode.Id);
            if (sourceIndex < 0)
            {
                return Result.Failure<MutationResult>("Could not resolve the moved element index.");
            }

            List<SvgNodeId> targetChildren = new(targetParentNode.Children ?? Array.Empty<SvgNodeId>());
            int clampedTargetIndex = Math.Clamp(targetChildIndex, 0, targetChildren.Count);

            sourceChildren.RemoveAt(sourceIndex);
            targetChildren.Insert(clampedTargetIndex, movedNode.Id);

            sourceParentNode.Children = sourceChildren;
            targetParentNode.Children = targetChildren;
            movedNode.ParentId = targetParentNode.Id;

            MutationLookup.RefreshSiblingOrder(updatedDocumentModel, sourceParentNode);
            MutationLookup.RefreshSiblingOrder(updatedDocumentModel, targetParentNode);

            return SerializeResult(updatedDocumentModel);
        }

        private Result<MutationResult> ApplyTransformMutation(
            SvgDocumentModel documentModel,
            string elementKey,
            Func<string, string> buildUpdatedTransform)
        {
            Result<SvgDocumentModel> validation = ValidateDocumentModel(documentModel);
            if (validation.IsFailure)
                return Result.Failure<MutationResult>(validation.Error);

            SvgDocumentModel updatedDocumentModel = ModelClone.Create(documentModel);
            if (!NodeLookup.TryFindNodeByLegacyElementKey(updatedDocumentModel, elementKey, out SvgNodeModel node))
            {
                return Result.Failure<MutationResult>($"Could not find element '{elementKey}'.");
            }

            MutationWriter.ApplyPrependTransform(node, buildUpdatedTransform);
            return SerializeResult(updatedDocumentModel);
        }

        private Result<MutationResult> SerializeResult(SvgDocumentModel updatedDocumentModel)
        {
            return MutationWriter.Serialize(_serializer, updatedDocumentModel)
                .Map(updatedSourceText => new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty));
        }

    }
}
