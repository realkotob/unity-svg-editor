using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using SvgEditor.Workspace.Canvas;
using SvgEditor.DocumentModel;
using SvgEditor.Shared;
using SvgEditor.Document;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal static class HierarchyDocumentModelReader
    {
        public static bool TryBuildSnapshot(
            SvgDocumentModel documentModel,
            out StructureOutline snapshot,
            out string error,
            bool showDefinitions = false)
        {
            snapshot = new StructureOutline();
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            BuildContext context = new(showDefinitions);
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

                if (!context.ShowDefinitions && childNode.Kind == SvgNodeKind.Definitions)
                    continue;

                string childActiveLayerKey = RegisterLayer(context, documentModel.Root, childNode, activeLayerKey);
                StructureNode structureNode = CreateStructureNode(childNode, parentNode.LegacyElementKey, childActiveLayerKey);

                context.Elements.Add(structureNode);
                IncrementLayerElementCount(context, childActiveLayerKey, structureNode);
                List<TreeViewItemData<StructureNode>> childItems = BuildVisibleChildren(documentModel, context, childNode, childActiveLayerKey);
                AppendDefinitionProxyItems(documentModel, context, structureNode, childItems);

                items.Add(new TreeViewItemData<StructureNode>(
                    CreateTreeId(structureNode.Key, context.UsedTreeIds),
                    structureNode,
                    childItems));
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
                TreeLabel = BuildTreeLabel(node.XmlId, node.TagName, node.SiblingIndex),
                MaskReferenceId = ResolveReferencedFragmentId(node, SvgAttributeName.MASK),
                ClipPathReferenceId = ResolveReferencedFragmentId(node, SvgAttributeName.CLIP_PATH)
            };
        }

        private static void AppendDefinitionProxyItems(
            SvgDocumentModel documentModel,
            BuildContext context,
            StructureNode sourceNode,
            List<TreeViewItemData<StructureNode>> childItems)
        {
            if (documentModel == null || context == null || sourceNode == null || childItems == null)
                return;

            AppendDefinitionProxyItem(documentModel, context, sourceNode, childItems, CanvasDefinitionOverlayKind.Mask, sourceNode.MaskReferenceId);
            AppendDefinitionProxyItem(documentModel, context, sourceNode, childItems, CanvasDefinitionOverlayKind.ClipPath, sourceNode.ClipPathReferenceId);
        }

        private static void AppendDefinitionProxyItem(
            SvgDocumentModel documentModel,
            BuildContext context,
            StructureNode sourceNode,
            List<TreeViewItemData<StructureNode>> childItems,
            CanvasDefinitionOverlayKind kind,
            string referenceId)
        {
            if (string.IsNullOrWhiteSpace(referenceId))
                return;

            string definitionElementKey = string.Empty;
            if (documentModel.TryGetNodeByXmlId(referenceId, out SvgNodeModel definitionNode) && definitionNode != null)
                definitionElementKey = definitionNode.LegacyElementKey;

            string label = DefinitionProxyUtility.BuildProxyLabel(kind, referenceId);
            StructureNode proxyNode = new()
            {
                Key = DefinitionProxyUtility.BuildProxyKey(sourceNode.Key, kind, referenceId),
                TargetKey = string.Empty,
                TagName = kind == CanvasDefinitionOverlayKind.Mask ? SvgTagName.MASK : SvgTagName.CLIP_PATH,
                Depth = sourceNode.Depth + 1,
                ParentKey = sourceNode.Key,
                LayerKey = sourceNode.LayerKey,
                DisplayName = label,
                TreeLabel = label,
                IsDefinitionProxy = true,
                SourceElementKey = sourceNode.Key,
                DefinitionElementKey = definitionElementKey,
                DefinitionReferenceId = referenceId,
                DefinitionProxyKind = kind
            };

            context.Elements.Add(proxyNode);
            childItems.Add(new TreeViewItemData<StructureNode>(
                CreateTreeId(proxyNode.Key, context.UsedTreeIds),
                proxyNode,
                new List<TreeViewItemData<StructureNode>>()));
        }

        private static bool IsLayerCandidate(SvgNodeModel node, SvgNodeModel rootNode)
        {
            if (node == null || rootNode == null)
                return false;
            if (!string.Equals(node.TagName, SvgTagName.GROUP, StringComparison.OrdinalIgnoreCase))
                return false;
            if (node.ParentId == rootNode.Id)
                return true;

            if (TryGetRawAttribute(node, SvgAttributeName.INKSCAPE_GROUPMODE, out string prefixedMode) &&
                string.Equals(prefixedMode, "layer", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return TryGetRawAttribute(node, SvgAttributeName.GROUPMODE, out string rawMode) &&
                   string.Equals(rawMode, "layer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVisible(SvgNodeModel node)
        {
            if (node == null)
                return true;

            if (TryGetRawAttribute(node, SvgAttributeName.DISPLAY, out string display) &&
                string.Equals(display, "none", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !TryGetRawAttribute(node, SvgAttributeName.VISIBILITY, out string visibility) ||
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

        private static string ResolveReferencedFragmentId(SvgNodeModel node, string attributeName)
        {
            if (!TryGetRawAttribute(node, attributeName, out string rawValue))
                return string.Empty;

            string value = rawValue.Trim();
            if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            const string urlPrefix = "url(";
            if (value.StartsWith(urlPrefix, StringComparison.OrdinalIgnoreCase) && value.EndsWith(")", StringComparison.Ordinal))
                value = value.Substring(urlPrefix.Length, value.Length - urlPrefix.Length - 1).Trim();

            value = value.Trim(' ', '\t', '"', '\'');
            if (value.StartsWith("#", StringComparison.Ordinal))
                value = value.Substring(1);

            return value;
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
            public BuildContext(bool showDefinitions)
            {
                ShowDefinitions = showDefinitions;
            }

            public bool ShowDefinitions { get; }
            public List<StructureNode> Elements { get; } = new();
            public List<LayerSummary> Layers { get; } = new();
            public List<TreeViewItemData<StructureNode>> HierarchyItems { get; } = new();
            public Dictionary<string, LayerSummary> LayersByKey { get; } = new(StringComparer.Ordinal);
            public HashSet<int> UsedTreeIds { get; } = new();
        }
    }
}
