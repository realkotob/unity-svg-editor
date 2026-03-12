using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine.UIElements;
using SvgEditor.Shared;

namespace SvgEditor.Document
{
    internal static class StructureOutlineBuilder
    {
        public static bool TryBuildSnapshot(
            string sourceText,
            out StructureOutline snapshot,
            out string error,
            bool showDefinitions = false)
        {
            snapshot = new StructureOutline();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            if (!SvgDocumentXmlUtility.TryGetRootElement(sourceText, out XmlDocument document, out var root, out error))
                return false;

            var context = new BuildContext(root, showDefinitions);
            context.Elements.Add(CreateElementItem(root, root, 0, string.Empty, string.Empty, 0));
            context.HierarchyItems.Add(new TreeViewItemData<StructureNode>(
                CreateTreeId(context.Elements[0].Key, context.UsedTreeIds),
                context.Elements[0],
                BuildVisibleChildren(context, root, 1, string.Empty, context.Elements[0].Key)));

            snapshot = new StructureOutline
            {
                Elements = context.Elements,
                Layers = context.Layers,
                HierarchyItems = context.HierarchyItems
            };
            return true;
        }

        private static List<TreeViewItemData<StructureNode>> BuildVisibleChildren(
            BuildContext context,
            XmlElement parent,
            int depth,
            string activeLayerKey,
            string parentKey)
        {
            var items = new List<TreeViewItemData<StructureNode>>();
            var children = SvgDocumentXmlUtility.GetElementChildren(parent);
            for (var childIndex = 0; childIndex < children.Count; childIndex++)
            {
                var child = children[childIndex];
                if (!context.ShowDefinitions &&
                    string.Equals(child.LocalName, SvgTagName.DEFS, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var childActiveLayerKey = RegisterLayer(context, child, activeLayerKey);
                var elementItem = CreateElementItem(
                    child,
                    context.Root,
                    depth,
                    parentKey,
                    childActiveLayerKey,
                    childIndex);

                context.Elements.Add(elementItem);
                IncrementLayerElementCount(context, childActiveLayerKey, elementItem);

                items.Add(new TreeViewItemData<StructureNode>(
                    CreateTreeId(elementItem.Key, context.UsedTreeIds),
                    elementItem,
                    BuildVisibleChildren(context, child, depth + 1, childActiveLayerKey, elementItem.Key)));
            }

            return items;
        }

        private static string RegisterLayer(BuildContext context, XmlElement child, string activeLayerKey)
        {
            var childActiveLayerKey = activeLayerKey;
                if (!SvgDocumentXmlUtility.IsLayerCandidate(child, context.Root) ||
                    !SvgDocumentXmlUtility.TryGetId(child, out var layerId))
            {
                return childActiveLayerKey;
            }

            if (!context.LayersByKey.ContainsKey(layerId))
            {
                var layerItem = new LayerSummary
                {
                    Key = layerId,
                    DisplayName = $"#{layerId}",
                    IsVisible = SvgDocumentXmlUtility.IsVisible(child),
                    ElementCount = 0
                };

                context.LayersByKey.Add(layerId, layerItem);
                context.Layers.Add(layerItem);
            }

            childActiveLayerKey = layerId;
            return childActiveLayerKey;
        }

        private static void IncrementLayerElementCount(BuildContext context, string activeLayerKey, StructureNode elementItem)
        {
            if (string.IsNullOrWhiteSpace(activeLayerKey))
                return;
            if (!context.LayersByKey.TryGetValue(activeLayerKey, out var ownerLayer))
                return;
            if (!elementItem.CanUseAsTarget)
                return;
            if (string.Equals(ownerLayer.Key, elementItem.TargetKey, StringComparison.Ordinal))
                return;

            ownerLayer.ElementCount++;
        }

        private static StructureNode CreateElementItem(
            XmlElement element,
            XmlElement root,
            int depth,
            string parentKey,
            string activeLayerKey,
            int siblingIndex)
        {
            var hasStableId = SvgDocumentXmlUtility.TryGetId(element, out string stableId);
            var elementKey = SvgDocumentXmlUtility.BuildElementKey(element, root);
            var isRoot = ReferenceEquals(element, root);
            return new StructureNode
            {
                Key = elementKey,
                TargetKey = isRoot ? string.Empty : elementKey,
                TagName = element.LocalName,
                Depth = depth,
                ParentKey = parentKey,
                LayerKey = activeLayerKey,
                DisplayName = BuildElementDisplayName(hasStableId ? stableId : string.Empty, element.LocalName, depth, siblingIndex),
                TreeLabel = BuildTreeLabel(hasStableId ? stableId : string.Empty, element.LocalName, siblingIndex),
                MaskReferenceId = ResolveReferencedFragmentId(element, SvgAttributeName.MASK),
                ClipPathReferenceId = ResolveReferencedFragmentId(element, SvgAttributeName.CLIP_PATH)
            };
        }

        private static string ResolveReferencedFragmentId(XmlElement element, string attributeName)
        {
            if (element == null)
                return string.Empty;

            string rawValue = element.GetAttribute(attributeName);
            if (string.IsNullOrWhiteSpace(rawValue))
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
                var hash = 17;
                foreach (var ch in key)
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
            var indent = new string(' ', Math.Max(0, depth) * 2);
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
            public BuildContext(XmlElement root, bool showDefinitions)
            {
                Root = root;
                ShowDefinitions = showDefinitions;
            }

            public XmlElement Root { get; }
            public bool ShowDefinitions { get; }
            public List<StructureNode> Elements { get; } = new();
            public List<LayerSummary> Layers { get; } = new();
            public List<TreeViewItemData<StructureNode>> HierarchyItems { get; } = new();
            public Dictionary<string, LayerSummary> LayersByKey { get; } = new(StringComparer.Ordinal);
            public HashSet<int> UsedTreeIds { get; } = new();
        }
    }
}
