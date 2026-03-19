using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.UI.Canvas;
using SvgEditor.Core.Svg.Structure.Lookup;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.UI.Hierarchy
{
    internal static class HierarchyModelReader
    {
        public static bool TryBuildSnapshot(
            SvgDocumentModel documentModel,
            out HierarchyOutline snapshot,
            out string error,
            bool showDefinitions = false)
        {
            snapshot = new HierarchyOutline();
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            OutlineBuildSession buildSession = new(showDefinitions);
            HierarchyNode rootNode = CreateHierarchyNode(documentModel.Root, parentKey: string.Empty, activeLayerKey: string.Empty);
            buildSession.Elements.Add(rootNode);
            OutlineTraversal rootTraversal = new(documentModel, buildSession, activeLayerKey: string.Empty);
            buildSession.HierarchyItems.Add(new TreeViewItemData<HierarchyNode>(
                CreateTreeId(rootNode.Key, buildSession.UsedTreeIds),
                rootNode,
                BuildVisibleChildren(rootTraversal, documentModel.Root)));

            snapshot = new HierarchyOutline
            {
                Elements = buildSession.Elements,
                Layers = buildSession.Layers,
                HierarchyItems = buildSession.HierarchyItems
            };
            return true;
        }

        private static List<TreeViewItemData<HierarchyNode>> BuildVisibleChildren(
            OutlineTraversal traversal,
            SvgNodeModel parentNode)
        {
            List<TreeViewItemData<HierarchyNode>> items = new();
            if (parentNode?.Children == null)
                return items;

            foreach (SvgNodeId childId in parentNode.Children)
            {
                if (!traversal.DocumentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                if (!traversal.BuildSession.ShowDefinitions && childNode.Kind == SvgNodeCategory.Definitions)
                    continue;

                string childActiveLayerKey = RegisterLayer(traversal, childNode);
                HierarchyNode structureNode = CreateHierarchyNode(childNode, parentNode.LegacyElementKey, childActiveLayerKey);

                traversal.BuildSession.Elements.Add(structureNode);
                IncrementLayerElementCount(traversal.BuildSession, childActiveLayerKey, structureNode);
                List<TreeViewItemData<HierarchyNode>> childItems = BuildVisibleChildren(traversal.WithActiveLayerKey(childActiveLayerKey), childNode);
                AppendDefinitionProxyItems(traversal, structureNode, childItems);

                items.Add(new TreeViewItemData<HierarchyNode>(
                    CreateTreeId(structureNode.Key, traversal.BuildSession.UsedTreeIds),
                    structureNode,
                    childItems));
            }

            return items;
        }

        private static string RegisterLayer(
            OutlineTraversal traversal,
            SvgNodeModel childNode)
        {
            string childActiveLayerKey = traversal.ActiveLayerKey;
            if (!IsLayerCandidate(childNode, traversal.DocumentModel.Root) || !childNode.HasXmlId)
                return childActiveLayerKey;

            string layerId = childNode.XmlId;
            if (!traversal.BuildSession.LayersByKey.ContainsKey(layerId))
            {
                LayerSummary layerSummary = new()
                {
                    Key = layerId,
                    DisplayName = $"#{layerId}",
                    IsVisible = IsVisible(childNode),
                    ElementCount = 0
                };

                traversal.BuildSession.LayersByKey.Add(layerId, layerSummary);
                traversal.BuildSession.Layers.Add(layerSummary);
            }

            childActiveLayerKey = layerId;
            return childActiveLayerKey;
        }

        private static void IncrementLayerElementCount(OutlineBuildSession buildSession, string activeLayerKey, HierarchyNode elementItem)
        {
            if (string.IsNullOrWhiteSpace(activeLayerKey))
                return;
            if (!buildSession.LayersByKey.TryGetValue(activeLayerKey, out LayerSummary ownerLayer))
                return;
            if (!elementItem.CanUseAsTarget)
                return;
            if (string.Equals(ownerLayer.Key, elementItem.TargetKey, StringComparison.Ordinal))
                return;

            ownerLayer.ElementCount++;
        }

        private static HierarchyNode CreateHierarchyNode(SvgNodeModel node, string parentKey, string activeLayerKey)
        {
            bool isRoot = node.Id.IsRoot;
            return new HierarchyNode
            {
                Key = node.LegacyElementKey,
                TargetKey = isRoot ? string.Empty : node.LegacyTargetKey,
                TagName = node.TagName,
                Depth = node.Depth,
                ParentKey = parentKey,
                LayerKey = activeLayerKey,
                DisplayName = BuildElementDisplayName(node),
                TreeLabel = BuildTreeLabel(node.XmlId, node.TagName, node.SiblingIndex),
                MaskReferenceId = ResolveReferencedFragmentId(node, SvgAttributeName.MASK),
                ClipPathReferenceId = ResolveReferencedFragmentId(node, SvgAttributeName.CLIP_PATH)
            };
        }

        private static void AppendDefinitionProxyItems(
            OutlineTraversal traversal,
            HierarchyNode sourceNode,
            List<TreeViewItemData<HierarchyNode>> childItems)
        {
            if (traversal.DocumentModel == null || sourceNode == null || childItems == null)
                return;

            AppendDefinitionProxyItem(new DefinitionProxyDescriptor(
                traversal,
                sourceNode,
                childItems,
                CanvasDefinitionOverlayKind.Mask,
                sourceNode.MaskReferenceId));
            AppendDefinitionProxyItem(new DefinitionProxyDescriptor(
                traversal,
                sourceNode,
                childItems,
                CanvasDefinitionOverlayKind.ClipPath,
                sourceNode.ClipPathReferenceId));
        }

        private static void AppendDefinitionProxyItem(DefinitionProxyDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(descriptor.ReferenceId))
                return;

            string definitionElementKey = string.Empty;
            if (descriptor.Traversal.DocumentModel.TryGetNodeByXmlId(descriptor.ReferenceId, out SvgNodeModel definitionNode) && definitionNode != null)
                definitionElementKey = definitionNode.LegacyElementKey;

            string label = DefinitionProxyUtility.BuildProxyLabel(descriptor.Kind, descriptor.ReferenceId);
            HierarchyNode proxyNode = new()
            {
                Key = DefinitionProxyUtility.BuildProxyKey(descriptor.SourceNode.Key, descriptor.Kind, descriptor.ReferenceId),
                TargetKey = string.Empty,
                TagName = descriptor.Kind == CanvasDefinitionOverlayKind.Mask ? SvgTagName.MASK : SvgTagName.CLIP_PATH,
                Depth = descriptor.SourceNode.Depth + 1,
                ParentKey = descriptor.SourceNode.Key,
                LayerKey = descriptor.SourceNode.LayerKey,
                DisplayName = label,
                TreeLabel = label,
                IsDefinitionProxy = true,
                SourceElementKey = descriptor.SourceNode.Key,
                DefinitionElementKey = definitionElementKey,
                DefinitionReferenceId = descriptor.ReferenceId,
                DefinitionProxyKind = descriptor.Kind
            };

            descriptor.Traversal.BuildSession.Elements.Add(proxyNode);
            descriptor.ChildItems.Add(new TreeViewItemData<HierarchyNode>(
                CreateTreeId(proxyNode.Key, descriptor.Traversal.BuildSession.UsedTreeIds),
                proxyNode,
                new List<TreeViewItemData<HierarchyNode>>()));
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
                AttributeUtility.IsDisabledPaintValue(display))
            {
                return false;
            }

            return !TryGetRawAttribute(node, SvgAttributeName.VISIBILITY, out string visibility) ||
                   (!string.Equals(visibility, SvgText.VISIBILITY_HIDDEN, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(visibility, SvgText.VISIBILITY_COLLAPSE, StringComparison.OrdinalIgnoreCase));
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

            if (AttributeUtility.IsDisabledPaintValue(rawValue))
                return string.Empty;

            return SvgFragmentReferenceUtility.TryExtractFragmentId(rawValue, out string fragmentId)
                ? fragmentId
                : string.Empty;
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

        private static string BuildElementDisplayName(SvgNodeModel node)
        {
            string indent = new(' ', Math.Max(0, node?.Depth ?? 0) * 2);
            return string.IsNullOrWhiteSpace(node?.XmlId)
                ? $"{indent}<{node?.TagName}> [{(node?.SiblingIndex ?? 0) + 1}]"
                : $"{indent}#{node.XmlId}  <{node.TagName}>";
        }

        private static string BuildTreeLabel(string id, string localName, int siblingIndex)
        {
            if (!string.IsNullOrWhiteSpace(id))
                return SvgElementLabelFormatter.Normalize(id, localName);

            return siblingIndex <= 0
                ? localName
                : $"{localName} [{siblingIndex + 1}]";
        }

        private sealed class OutlineBuildSession
        {
            public OutlineBuildSession(bool showDefinitions)
            {
                ShowDefinitions = showDefinitions;
            }

            public bool ShowDefinitions { get; }
            public List<HierarchyNode> Elements { get; } = new();
            public List<LayerSummary> Layers { get; } = new();
            public List<TreeViewItemData<HierarchyNode>> HierarchyItems { get; } = new();
            public Dictionary<string, LayerSummary> LayersByKey { get; } = new(StringComparer.Ordinal);
            public HashSet<int> UsedTreeIds { get; } = new();
        }

        private readonly struct OutlineTraversal
        {
            public OutlineTraversal(SvgDocumentModel documentModel, OutlineBuildSession buildSession, string activeLayerKey)
            {
                DocumentModel = documentModel;
                BuildSession = buildSession;
                ActiveLayerKey = activeLayerKey;
            }

            public SvgDocumentModel DocumentModel { get; }
            public OutlineBuildSession BuildSession { get; }
            public string ActiveLayerKey { get; }

            public OutlineTraversal WithActiveLayerKey(string activeLayerKey)
            {
                return new OutlineTraversal(DocumentModel, BuildSession, activeLayerKey);
            }
        }

        private readonly struct DefinitionProxyDescriptor
        {
            public DefinitionProxyDescriptor(
                OutlineTraversal traversal,
                HierarchyNode sourceNode,
                List<TreeViewItemData<HierarchyNode>> childItems,
                CanvasDefinitionOverlayKind kind,
                string referenceId)
            {
                Traversal = traversal;
                SourceNode = sourceNode;
                ChildItems = childItems;
                Kind = kind;
                ReferenceId = referenceId;
            }

            public OutlineTraversal Traversal { get; }
            public HierarchyNode SourceNode { get; }
            public List<TreeViewItemData<HierarchyNode>> ChildItems { get; }
            public CanvasDefinitionOverlayKind Kind { get; }
            public string ReferenceId { get; }
        }
    }
}
