using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal static class StructureDocumentModelReader
    {
        public static bool TryBuildSnapshot(SvgDocumentModel documentModel, out StructureOutline snapshot, out string error)
        {
            snapshot = new StructureOutline();
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            BuildContext context = new();
            StructureNode rootNode = CreateStructureNode(documentModel.Root, parentKey: string.Empty, activeLayerKey: string.Empty);
            context.Elements.Add(rootNode);
            context.HierarchyItems.Add(new TreeViewItemData<StructureNode>(
                CreateTreeId(rootNode.Key, context.UsedTreeIds),
                rootNode,
                BuildVisibleChildren(documentModel, context, documentModel.Root, activeLayerKey: string.Empty)));

            snapshot = new StructureOutline
            {
                Elements = context.Elements,
                Layers = context.Layers,
                HierarchyItems = context.HierarchyItems
            };
            return true;
        }

        private static List<TreeViewItemData<StructureNode>> BuildVisibleChildren(
            SvgDocumentModel documentModel,
            BuildContext context,
            SvgNodeModel parentNode,
            string activeLayerKey)
        {
            List<TreeViewItemData<StructureNode>> items = new();
            if (parentNode?.Children == null)
                return items;

            foreach (SvgNodeId childId in parentNode.Children)
            {
                if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                string childActiveLayerKey = RegisterLayer(context, documentModel.Root, childNode, activeLayerKey);
                StructureNode structureNode = CreateStructureNode(childNode, parentNode.LegacyElementKey, childActiveLayerKey);

                context.Elements.Add(structureNode);
                IncrementLayerElementCount(context, childActiveLayerKey, structureNode);

                items.Add(new TreeViewItemData<StructureNode>(
                    CreateTreeId(structureNode.Key, context.UsedTreeIds),
                    structureNode,
                    BuildVisibleChildren(documentModel, context, childNode, childActiveLayerKey)));
            }

            return items;
        }

        private static string RegisterLayer(
            BuildContext context,
            SvgNodeModel rootNode,
            SvgNodeModel childNode,
            string activeLayerKey)
        {
            string childActiveLayerKey = activeLayerKey;
            if (!IsLayerCandidate(childNode, rootNode) || !childNode.HasXmlId)
                return childActiveLayerKey;

            string layerId = childNode.XmlId;
            if (!context.LayersByKey.ContainsKey(layerId))
            {
                LayerSummary layerSummary = new()
                {
                    Key = layerId,
                    DisplayName = $"#{layerId}",
                    IsVisible = IsVisible(childNode),
                    ElementCount = 0
                };

                context.LayersByKey.Add(layerId, layerSummary);
                context.Layers.Add(layerSummary);
            }

            childActiveLayerKey = layerId;
            return childActiveLayerKey;
        }

        private static void IncrementLayerElementCount(BuildContext context, string activeLayerKey, StructureNode elementItem)
        {
            if (string.IsNullOrWhiteSpace(activeLayerKey))
                return;
            if (!context.LayersByKey.TryGetValue(activeLayerKey, out LayerSummary ownerLayer))
                return;
            if (!elementItem.CanUseAsTarget)
                return;
            if (string.Equals(ownerLayer.Key, elementItem.TargetKey, StringComparison.Ordinal))
                return;

            ownerLayer.ElementCount++;
        }

        private static StructureNode CreateStructureNode(SvgNodeModel node, string parentKey, string activeLayerKey)
        {
            bool isRoot = node.Id.IsRoot;
            return new StructureNode
            {
                Key = node.LegacyElementKey,
                TargetKey = isRoot ? string.Empty : node.LegacyTargetKey,
                TagName = node.TagName,
                Depth = node.Depth,
                ParentKey = parentKey,
                LayerKey = activeLayerKey,
                DisplayName = BuildElementDisplayName(node.XmlId, node.TagName, node.Depth, node.SiblingIndex),
                TreeLabel = BuildTreeLabel(node.XmlId, node.TagName, node.SiblingIndex)
            };
        }

        private static bool IsLayerCandidate(SvgNodeModel node, SvgNodeModel rootNode)
        {
            if (node == null || rootNode == null)
                return false;
            if (!string.Equals(node.TagName, "g", StringComparison.OrdinalIgnoreCase))
                return false;
            if (node.ParentId == rootNode.Id)
                return true;

            if (TryGetRawAttribute(node, "inkscape:groupmode", out string prefixedMode) &&
                string.Equals(prefixedMode, "layer", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return TryGetRawAttribute(node, "groupmode", out string rawMode) &&
                   string.Equals(rawMode, "layer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVisible(SvgNodeModel node)
        {
            if (node == null)
                return true;

            if (TryGetRawAttribute(node, "display", out string display) &&
                string.Equals(display, "none", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !TryGetRawAttribute(node, "visibility", out string visibility) ||
                   (!string.Equals(visibility, "hidden", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(visibility, "collapse", StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryGetRawAttribute(SvgNodeModel node, string attributeName, out string value)
        {
            value = string.Empty;
            return node?.RawAttributes != null &&
                   node.RawAttributes.TryGetValue(attributeName, out value) &&
                   !string.IsNullOrWhiteSpace(value);
        }

        private static int CreateTreeId(string key, ISet<int> usedTreeIds)
        {
            unchecked
            {
                int hash = 17;
                foreach (char ch in key)
                    hash = (hash * 31) + ch;

                if (hash == 0)
                    hash = 1;

                while (usedTreeIds.Contains(hash))
                    hash++;

                usedTreeIds.Add(hash);
                return hash;
            }
        }

        private static string BuildElementDisplayName(string id, string localName, int depth, int siblingIndex)
        {
            string indent = new(' ', Math.Max(0, depth) * 2);
            return string.IsNullOrWhiteSpace(id)
                ? $"{indent}<{localName}> [{siblingIndex + 1}]"
                : $"{indent}#{id}  <{localName}>";
        }

        private static string BuildTreeLabel(string id, string localName, int siblingIndex)
        {
            if (!string.IsNullOrWhiteSpace(id))
                return id.Trim().Replace('_', ' ').Replace('-', ' ');

            return siblingIndex <= 0
                ? localName
                : $"{localName} [{siblingIndex + 1}]";
        }

        private sealed class BuildContext
        {
            public List<StructureNode> Elements { get; } = new();
            public List<LayerSummary> Layers { get; } = new();
            public List<TreeViewItemData<StructureNode>> HierarchyItems { get; } = new();
            public Dictionary<string, LayerSummary> LayersByKey { get; } = new(StringComparer.Ordinal);
            public HashSet<int> UsedTreeIds { get; } = new();
        }
    }
}
