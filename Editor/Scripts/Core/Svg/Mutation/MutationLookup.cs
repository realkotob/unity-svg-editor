using System;
using SvgEditor.Core.Svg;
using SvgEditor.Core.Svg.Model;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Mutation
{
    internal static class MutationLookup
    {
        public static string NormalizeTargetKey(string targetKey)
        {
            return string.IsNullOrWhiteSpace(targetKey)
                ? SvgTargets.RootTargetKey
                : targetKey;
        }

        public static bool TryResolveTargetNode(SvgDocumentModel documentModel, string targetKey, out SvgNodeModel node)
        {
            node = null;
            if (documentModel?.Root == null)
                return false;

            string normalizedTargetKey = NormalizeTargetKey(targetKey);
            if (string.Equals(normalizedTargetKey, SvgTargets.RootTargetKey, StringComparison.Ordinal))
            {
                node = documentModel.Root;
                return true;
            }

            foreach (SvgNodeId nodeId in documentModel.NodeOrder)
            {
                if (!documentModel.TryGetNode(nodeId, out SvgNodeModel candidate) || candidate == null)
                    continue;

                if (string.Equals(candidate.LegacyTargetKey, normalizedTargetKey, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            return false;
        }

        public static void RefreshSiblingOrder(SvgDocumentModel documentModel, SvgNodeModel parentNode)
        {
            if (documentModel?.Nodes == null || parentNode?.Children == null)
                return;

            for (var index = 0; index < parentNode.Children.Count; index++)
            {
                SvgNodeId childNodeId = parentNode.Children[index];
                if (!documentModel.TryGetNode(childNodeId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                childNode.SiblingIndex = index;
            }
        }

        public static bool IsSameOrDescendantOf(SvgDocumentModel documentModel, SvgNodeModel node, SvgNodeId ancestorId)
        {
            for (SvgNodeModel current = node; current != null;)
            {
                if (current.Id == ancestorId)
                    return true;

                if (current.ParentId == default || !documentModel.TryGetNode(current.ParentId, out current))
                    break;
            }

            return false;
        }
    }
}
