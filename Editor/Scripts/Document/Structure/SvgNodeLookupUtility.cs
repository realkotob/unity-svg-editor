using System;
using System.Collections.Generic;

namespace SvgEditor
{
    internal static class SvgNodeLookupUtility
    {
        public static Dictionary<string, SvgNodeModel> BuildNodeLookupByXmlId(SvgDocumentModel documentModel)
        {
            var lookup = new Dictionary<string, SvgNodeModel>(StringComparer.Ordinal);
            if (documentModel?.NodeIdsByXmlId == null)
            {
                return lookup;
            }

            foreach (var pair in documentModel.NodeIdsByXmlId)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) ||
                    !documentModel.TryGetNode(pair.Value, out var node) ||
                    node == null)
                {
                    continue;
                }

                lookup[pair.Key] = node;
            }

            return lookup;
        }

        public static bool TryFindNodeByLegacyElementKey(SvgDocumentModel documentModel, string elementKey, out SvgNodeModel node)
        {
            node = null;
            if (documentModel?.NodeOrder == null || string.IsNullOrWhiteSpace(elementKey))
            {
                return false;
            }

            for (var index = 0; index < documentModel.NodeOrder.Count; index++)
            {
                var nodeId = documentModel.NodeOrder[index];
                if (!documentModel.TryGetNode(nodeId, out var candidate) || candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.LegacyElementKey, elementKey, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool TryResolveUseReference(
            SvgNodeModel useNode,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            out SvgNodeModel referencedNode)
        {
            referencedNode = null;
            if (useNode?.References == null || nodesByXmlId == null)
            {
                return false;
            }

            for (var index = 0; index < useNode.References.Count; index++)
            {
                var reference = useNode.References[index];
                if (string.IsNullOrWhiteSpace(reference?.FragmentId))
                {
                    continue;
                }

                if (nodesByXmlId.TryGetValue(reference.FragmentId, out referencedNode))
                {
                    return referencedNode != null;
                }
            }

            return false;
        }

        public static bool TryExtractFragmentId(string rawValue, out string fragmentId)
        {
            fragmentId = string.Empty;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            var hashIndex = rawValue.IndexOf('#');
            var closeIndex = rawValue.IndexOf(')', hashIndex + 1);
            if (hashIndex < 0 || closeIndex <= hashIndex)
            {
                return false;
            }

            fragmentId = rawValue.Substring(hashIndex + 1, closeIndex - hashIndex - 1).Trim();
            return !string.IsNullOrWhiteSpace(fragmentId);
        }
    }
}
