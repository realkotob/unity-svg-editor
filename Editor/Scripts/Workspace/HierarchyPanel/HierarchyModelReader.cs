using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using SvgEditor.Workspace.Canvas;
using SvgEditor.DocumentModel;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.HierarchyPanel
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

            BuildContext context = new(showDefinitions);
            HierarchyNode rootNode = CreateHierarchyNode(documentModel.Root, parentKey: string.Empty, activeLayerKey: string.Empty);
            context.Elements.Add(rootNode);
            BuildScope rootScope = new(documentModel, context, activeLayerKey: string.Empty);
            context.HierarchyItems.Add(new TreeViewItemData<HierarchyNode>(
                CreateTreeId(rootNode.Key, context.UsedTreeIds),
                rootNode,
                BuildVisibleChildren(rootScope, documentModel.Root)));

            snapshot = new HierarchyOutline
            {
                Elements = context.Elements,
                Layers = context.Layers,
                HierarchyItems = context.HierarchyItems
            };
            return true;
        }

        private static List<TreeViewItemData<HierarchyNode>> BuildVisibleChildren(
            BuildScope scope,
            SvgNodeModel parentNode)
        {
            List<TreeViewItemData<HierarchyNode>> items = new();
            if (parentNode?.Children == null)
                return items;

            foreach (SvgNodeId childId in parentNode.Children)
            {
                if (!scope.DocumentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                if (!scope.Context.ShowDefinitions && childNode.Kind == SvgNodeKind.Definitions)
                    continue;

                string childActiveLayerKey = RegisterLayer(scope, childNode);
                HierarchyNode structureNode = CreateHierarchyNode(childNode, parentNode.LegacyElementKey, childActiveLayerKey);

                scope.Context.Elements.Add(structureNode);
                IncrementLayerElementCount(scope.Context, childActiveLayerKey, structureNode);
                List<TreeViewItemData<HierarchyNode>> childItems = BuildVisibleChildren(scope.WithActiveLayerKey(childActiveLayerKey), childNode);
                AppendDefinitionProxyItems(scope, structureNode, childItems);

                items.Add(new TreeViewItemData<HierarchyNode>(
                    CreateTreeId(structureNode.Key, scope.Context.UsedTreeIds),
                    structureNode,
                    childItems));
            }

            return items;
        }

        private static string RegisterLayer(
            BuildScope scope,
            SvgNodeModel childNode)
        {
            string childActiveLayerKey = scope.ActiveLayerKey;
            if (!IsLayerCandidate(childNode, scope.DocumentModel.Root) || !childNode.HasXmlId)
                return childActiveLayerKey;

            string layerId = childNode.XmlId;
            if (!scope.Context.LayersByKey.ContainsKey(layerId))
            {
                LayerSummary layerSummary = new()
                {
                    Key = layerId,
                    DisplayName = $"#{layerId}",
                    IsVisible = IsVisible(childNode),
                    ElementCount = 0
                };

                scope.Context.LayersByKey.Add(layerId, layerSummary);
                scope.Context.Layers.Add(layerSummary);
            }

            childActiveLayerKey = layerId;
            return childActiveLayerKey;
        }

        private static void IncrementLayerElementCount(BuildContext context, string activeLayerKey, HierarchyNode elementItem)
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
            BuildScope scope,
            HierarchyNode sourceNode,
            List<TreeViewItemData<HierarchyNode>> childItems)
        {
            if (scope.DocumentModel == null || scope.Context == null || sourceNode == null || childItems == null)
                return;

            AppendDefinitionProxyItem(new ProxyAppendRequest(
                scope.DocumentModel,
                scope.Context,
                sourceNode,
                childItems,
                CanvasDefinitionOverlayKind.Mask,
                sourceNode.MaskReferenceId));
            AppendDefinitionProxyItem(new ProxyAppendRequest(
                scope.DocumentModel,
                scope.Context,
                sourceNode,
                childItems,
                CanvasDefinitionOverlayKind.ClipPath,
                sourceNode.ClipPathReferenceId));
        }

        private static void AppendDefinitionProxyItem(ProxyAppendRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ReferenceId))
                return;

            string definitionElementKey = string.Empty;
            if (request.DocumentModel.TryGetNodeByXmlId(request.ReferenceId, out SvgNodeModel definitionNode) && definitionNode != null)
                definitionElementKey = definitionNode.LegacyElementKey;

            string label = DefinitionProxyUtility.BuildProxyLabel(request.Kind, request.ReferenceId);
            HierarchyNode proxyNode = new()
            {
                Key = DefinitionProxyUtility.BuildProxyKey(request.SourceNode.Key, request.Kind, request.ReferenceId),
                TargetKey = string.Empty,
                TagName = request.Kind == CanvasDefinitionOverlayKind.Mask ? SvgTagName.MASK : SvgTagName.CLIP_PATH,
                Depth = request.SourceNode.Depth + 1,
                ParentKey = request.SourceNode.Key,
                LayerKey = request.SourceNode.LayerKey,
                DisplayName = label,
                TreeLabel = label,
                IsDefinitionProxy = true,
                SourceElementKey = request.SourceNode.Key,
                DefinitionElementKey = definitionElementKey,
                DefinitionReferenceId = request.ReferenceId,
                DefinitionProxyKind = request.Kind
            };

            request.Context.Elements.Add(proxyNode);
            request.ChildItems.Add(new TreeViewItemData<HierarchyNode>(
                CreateTreeId(proxyNode.Key, request.Context.UsedTreeIds),
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
            public List<HierarchyNode> Elements { get; } = new();
            public List<LayerSummary> Layers { get; } = new();
            public List<TreeViewItemData<HierarchyNode>> HierarchyItems { get; } = new();
            public Dictionary<string, LayerSummary> LayersByKey { get; } = new(StringComparer.Ordinal);
            public HashSet<int> UsedTreeIds { get; } = new();
        }

        private readonly struct BuildScope
        {
            public BuildScope(SvgDocumentModel documentModel, BuildContext context, string activeLayerKey)
            {
                DocumentModel = documentModel;
                Context = context;
                ActiveLayerKey = activeLayerKey;
            }

            public SvgDocumentModel DocumentModel { get; }
            public BuildContext Context { get; }
            public string ActiveLayerKey { get; }

            public BuildScope WithActiveLayerKey(string activeLayerKey)
            {
                return new BuildScope(DocumentModel, Context, activeLayerKey);
            }
        }

        private readonly struct ProxyAppendRequest
        {
            public ProxyAppendRequest(
                SvgDocumentModel documentModel,
                BuildContext context,
                HierarchyNode sourceNode,
                List<TreeViewItemData<HierarchyNode>> childItems,
                CanvasDefinitionOverlayKind kind,
                string referenceId)
            {
                DocumentModel = documentModel;
                Context = context;
                SourceNode = sourceNode;
                ChildItems = childItems;
                Kind = kind;
                ReferenceId = referenceId;
            }

            public SvgDocumentModel DocumentModel { get; }
            public BuildContext Context { get; }
            public HierarchyNode SourceNode { get; }
            public List<TreeViewItemData<HierarchyNode>> ChildItems { get; }
            public CanvasDefinitionOverlayKind Kind { get; }
            public string ReferenceId { get; }
        }
    }
}
