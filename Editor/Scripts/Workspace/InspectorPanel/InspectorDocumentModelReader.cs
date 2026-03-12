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

            CopyAttributes(node.RawAttributes, attributes);
            ResolvePresentationAttributes(documentModel, node, attributes);

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
                if (TryGetAttribute(current.RawAttributes, attributeName, out value) &&
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

        private static bool TryGetAttribute(
            IReadOnlyDictionary<string, string> attributes,
            string attributeName,
            out string value)
        {
            value = string.Empty;
            return attributes != null &&
                   attributes.TryGetValue(attributeName, out value) &&
                   !string.IsNullOrWhiteSpace(value);
        }
    }
}
