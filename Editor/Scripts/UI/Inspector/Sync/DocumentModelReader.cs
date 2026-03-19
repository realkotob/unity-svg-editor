using System;
using System.Collections.Generic;
using SvgEditor.Core.Svg.Structure.Lookup;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.UI.Inspector.State;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.UI.Inspector
{
    internal static class DocumentModelReader
    {
        public static IReadOnlyList<PatchTarget> ExtractTargets(SvgDocumentModel documentModel)
        {
            List<PatchTarget> targets = new();
            if (documentModel?.NodeOrder == null)
                return targets;

            HashSet<string> knownKeys = new(StringComparer.Ordinal);
            foreach (SvgNodeId nodeId in documentModel.NodeOrder)
            {
                if (!documentModel.TryGetNode(nodeId, out SvgNodeModel node) ||
                    node == null ||
                    node.Id.IsRoot ||
                    string.IsNullOrWhiteSpace(node.LegacyTargetKey) ||
                    !knownKeys.Add(node.LegacyTargetKey))
                {
                    continue;
                }

                targets.Add(new PatchTarget
                {
                    Key = node.LegacyTargetKey,
                    DisplayName = BuildDisplayName(node)
                });
            }

            return targets;
        }

        public static bool TryReadAttributes(
            SvgDocumentModel documentModel,
            string targetKey,
            out Dictionary<string, string> attributes,
            out string tagName,
            out string error)
        {
            Result<ReadAttributesResult> result = ReadAttributes(documentModel, targetKey);
            ReadAttributesResult payload = result.GetValueOrDefault();
            attributes = payload.Attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            tagName = payload.TagName ?? string.Empty;
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        private static Result<ReadAttributesResult> ReadAttributes(SvgDocumentModel documentModel, string targetKey)
        {
            return ValidateDocumentModel(documentModel)
                .Bind(model => ResolveNode(model, targetKey)
                    .Map(node => BuildAttributes(model, node)));
        }

        private static Result<SvgDocumentModel> ValidateDocumentModel(SvgDocumentModel documentModel)
        {
            return documentModel?.Root != null
                ? Result.Success(documentModel)
                : Result.Failure<SvgDocumentModel>("Document model is unavailable.");
        }

        private static Result<SvgNodeModel> ResolveNode(SvgDocumentModel documentModel, string targetKey)
        {
            string normalizedTargetKey = MutationLookup.NormalizeTargetKey(targetKey);

            return MutationLookup.TryResolveTargetNode(documentModel, normalizedTargetKey, out SvgNodeModel node)
                ? Result.Success(node)
                : Result.Failure<SvgNodeModel>($"Could not find target '{normalizedTargetKey}'.");
        }

        private static ReadAttributesResult BuildAttributes(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            Dictionary<string, string> attributes = new(StringComparer.OrdinalIgnoreCase);
            CopyAttributes(node.RawAttributes, attributes);
            ResolvePresentationAttributes(documentModel, node, attributes);

            return new ReadAttributesResult(node.TagName ?? string.Empty, attributes);
        }

        private static string BuildDisplayName(SvgNodeModel node)
        {
            return node.HasXmlId
                ? $"#{node.XmlId}  <{node.TagName}>"
                : $"{node.TagName}  [{node.SiblingIndex + 1}]";
        }

        private static void CopyAttributes(
            IReadOnlyDictionary<string, string> source,
            IDictionary<string, string> destination)
        {
            if (source == null || destination == null)
                return;

            foreach (var pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                destination[pair.Key] = pair.Value ?? string.Empty;
            }
        }

        private static void ResolvePresentationAttributes(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IDictionary<string, string> attributes)
        {
            if (documentModel == null || node == null || attributes == null)
                return;

            string[] presentationAttributes =
            {
                SvgAttributeName.FILL,
                SvgAttributeName.FILL_OPACITY,
                SvgAttributeName.STROKE,
                SvgAttributeName.STROKE_OPACITY,
                SvgAttributeName.STROKE_WIDTH,
                SvgAttributeName.STROKE_LINECAP,
                SvgAttributeName.STROKE_LINEJOIN,
                SvgAttributeName.STROKE_DASHARRAY
            };

            foreach (string attributeName in presentationAttributes)
            {
                if (attributes.ContainsKey(attributeName))
                    continue;

                if (TryGetInheritedAttribute(documentModel, node, attributeName, out string resolvedValue))
                    attributes[attributeName] = resolvedValue;
            }

            if (string.Equals(node.TagName, SvgTagName.PATH, StringComparison.OrdinalIgnoreCase) &&
                !attributes.ContainsKey(SvgAttributeName.FILL))
            {
                attributes[SvgAttributeName.FILL] = "#000000";
            }
        }

        private static bool TryGetInheritedAttribute(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            string attributeName,
            out string value)
        {
            value = string.Empty;
            SvgNodeModel current = node;
            while (current != null)
            {
                if (AttributeUtility.TryGetAttribute(current.RawAttributes, attributeName, out value) &&
                    !string.Equals(value, "inherit", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (documentModel == null || current.Id.IsRoot)
                    break;

                if (!documentModel.TryGetNode(current.ParentId, out current))
                    break;
            }

            value = string.Empty;
            return false;
        }

        private readonly struct ReadAttributesResult
        {
            public ReadAttributesResult(string tagName, Dictionary<string, string> attributes)
            {
                TagName = tagName;
                Attributes = attributes;
            }

            public string TagName { get; }
            public Dictionary<string, string> Attributes { get; }
        }
    }
}
