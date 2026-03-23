using System;
using System.Collections.Generic;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.Core.Svg.Serialization;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Mutation
{
    internal sealed class PathMutationService
    {
        private readonly SvgSerializer _serializer = new();

        public bool TryApplyPathData(
            SvgDocumentModel documentModel,
            string targetKey,
            PathData pathData,
            out MutationResult result)
        {
            Result<MutationResult> mutation = ApplyPathData(documentModel, targetKey, pathData);
            result = mutation.IsSuccess
                ? mutation.Value
                : new MutationResult(null, string.Empty, mutation.Error);
            return mutation.IsSuccess;
        }

        private Result<MutationResult> ApplyPathData(
            SvgDocumentModel documentModel,
            string targetKey,
            PathData pathData)
        {
            if (documentModel?.Root == null)
            {
                return Result.Failure<MutationResult>("Document model is unavailable.");
            }

            if (pathData == null)
            {
                return Result.Failure<MutationResult>("Path data is unavailable.");
            }

            Result<string> serializedPathData = PathDataSerializer.SerializeResult(pathData);
            if (serializedPathData.IsFailure)
            {
                return Result.Failure<MutationResult>(serializedPathData.Error);
            }

            SvgDocumentModel updatedDocumentModel = ModelClone.Create(documentModel);
            if (!MutationLookup.TryResolveTargetNode(updatedDocumentModel, targetKey, out SvgNodeModel targetNode))
            {
                return Result.Failure<MutationResult>($"Could not find target '{targetKey}'.");
            }

            if (!string.Equals(targetNode.TagName, SvgTagName.PATH, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<MutationResult>($"Target '{targetKey}' is not a path element.");
            }

            Dictionary<string, string> attributes = ModelClone.CloneAttributes(targetNode.RawAttributes);
            attributes[SvgAttributeName.D] = serializedPathData.Value;
            MutationWriter.ApplyAttributes(targetNode, attributes);
            targetNode.DisplayTagName = SvgTagName.PATH;

            return MutationWriter.Serialize(_serializer, updatedDocumentModel)
                .Map(updatedSourceText => new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty));
        }
    }
}
