using System;
using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal static class InspectorDocumentModelReader
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
            attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            tagName = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            string normalizedTargetKey = string.IsNullOrWhiteSpace(targetKey)
                ? SvgDocumentTargets.RootTargetKey
                : targetKey;

            if (!TryFindNodeByTargetKey(documentModel, normalizedTargetKey, out SvgNodeModel node))
            {
                error = $"Could not find target '{normalizedTargetKey}'.";
                return false;
            }

            tagName = node.TagName ?? string.Empty;

            if (node.RawAttributes == null)
                return true;

            foreach (var pair in node.RawAttributes)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                attributes[pair.Key] = pair.Value ?? string.Empty;
            }

            return true;
        }

        private static bool TryFindNodeByTargetKey(
            SvgDocumentModel documentModel,
            string targetKey,
            out SvgNodeModel node)
        {
            node = null;
            if (documentModel?.NodeOrder == null)
                return false;

            foreach (SvgNodeId nodeId in documentModel.NodeOrder)
            {
                if (!documentModel.TryGetNode(nodeId, out SvgNodeModel candidate) || candidate == null)
                    continue;

                if (string.Equals(candidate.LegacyTargetKey, targetKey, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string BuildDisplayName(SvgNodeModel node)
        {
            return node.HasXmlId
                ? $"#{node.XmlId}  <{node.TagName}>"
                : $"{node.TagName}  [{node.SiblingIndex + 1}]";
        }
    }
}
