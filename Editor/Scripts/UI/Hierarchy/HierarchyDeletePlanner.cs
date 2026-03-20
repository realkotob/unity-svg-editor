using System;
using System.Collections.Generic;
using System.Linq;
using SvgEditor.Core.Svg.Hierarchy;

namespace SvgEditor.UI.Hierarchy
{
    internal readonly struct HierarchyDeletePlan
    {
        public HierarchyDeletePlan(IReadOnlyList<string> deleteKeys, string fallbackElementKey)
        {
            DeleteKeys = deleteKeys ?? Array.Empty<string>();
            FallbackElementKey = fallbackElementKey ?? string.Empty;
        }

        public IReadOnlyList<string> DeleteKeys { get; }
        public string FallbackElementKey { get; }
    }

    internal static class HierarchyDeletePlanner
    {
        public static HierarchyDeletePlan Plan(
            IReadOnlyList<HierarchyNode> elements,
            IReadOnlyList<string> selectedElementKeys,
            string primaryElementKey)
        {
            if (elements == null || elements.Count == 0 || selectedElementKeys == null || selectedElementKeys.Count == 0)
            {
                return new HierarchyDeletePlan(Array.Empty<string>(), string.Empty);
            }

            Dictionary<string, HierarchyNode> elementsByKey = elements
                .Where(node => node != null && !string.IsNullOrWhiteSpace(node.Key))
                .GroupBy(node => node.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            List<string> normalizedSelection = selectedElementKeys
                .Select(key => NormalizeSelectedKey(elementsByKey, key))
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (normalizedSelection.Count == 0)
            {
                return new HierarchyDeletePlan(Array.Empty<string>(), string.Empty);
            }

            string normalizedPrimaryKey = NormalizeSelectedKey(elementsByKey, primaryElementKey);
            if (string.IsNullOrWhiteSpace(normalizedPrimaryKey) ||
                !normalizedSelection.Contains(normalizedPrimaryKey, StringComparer.Ordinal))
            {
                normalizedPrimaryKey = normalizedSelection[normalizedSelection.Count - 1];
            }

            HashSet<string> selectedSet = new(normalizedSelection, StringComparer.Ordinal);
            List<string> deleteKeys = normalizedSelection
                .Where(key => !HasSelectedAncestor(elementsByKey, selectedSet, key))
                .ToList();

            HashSet<string> deleteSet = new(deleteKeys, StringComparer.Ordinal);
            string fallbackElementKey = ResolveFallbackElementKey(elements, elementsByKey, deleteSet, normalizedPrimaryKey);
            return new HierarchyDeletePlan(deleteKeys, fallbackElementKey);
        }

        private static string NormalizeSelectedKey(
            IReadOnlyDictionary<string, HierarchyNode> elementsByKey,
            string selectedElementKey)
        {
            if (string.IsNullOrWhiteSpace(selectedElementKey))
            {
                return string.Empty;
            }

            if (!elementsByKey.TryGetValue(selectedElementKey, out HierarchyNode selectedNode))
            {
                return selectedElementKey;
            }

            if (selectedNode.IsDefinitionProxy && !string.IsNullOrWhiteSpace(selectedNode.SourceElementKey))
            {
                return selectedNode.SourceElementKey;
            }

            return selectedNode.Key;
        }

        private static bool HasSelectedAncestor(
            IReadOnlyDictionary<string, HierarchyNode> elementsByKey,
            ISet<string> selectedKeys,
            string elementKey)
        {
            if (!elementsByKey.TryGetValue(elementKey, out HierarchyNode currentNode))
            {
                return false;
            }

            while (!string.IsNullOrWhiteSpace(currentNode.ParentKey) &&
                   elementsByKey.TryGetValue(currentNode.ParentKey, out currentNode))
            {
                if (selectedKeys.Contains(currentNode.Key))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolveFallbackElementKey(
            IReadOnlyList<HierarchyNode> elements,
            IReadOnlyDictionary<string, HierarchyNode> elementsByKey,
            ISet<string> deleteKeys,
            string primaryElementKey)
        {
            if (string.IsNullOrWhiteSpace(primaryElementKey) ||
                !elementsByKey.TryGetValue(primaryElementKey, out HierarchyNode primaryNode))
            {
                return string.Empty;
            }

            List<HierarchyNode> siblings = elements
                .Where(node => node != null &&
                               !node.IsDefinitionProxy &&
                               string.Equals(node.ParentKey, primaryNode.ParentKey, StringComparison.Ordinal))
                .ToList();

            int siblingIndex = siblings.FindIndex(node => string.Equals(node.Key, primaryNode.Key, StringComparison.Ordinal));
            if (siblingIndex >= 0)
            {
                for (int index = siblingIndex + 1; index < siblings.Count; index++)
                {
                    string nextSiblingKey = siblings[index].Key;
                    if (!IsDeleted(elementsByKey, deleteKeys, nextSiblingKey))
                    {
                        return nextSiblingKey;
                    }
                }

                for (int index = siblingIndex - 1; index >= 0; index--)
                {
                    string previousSiblingKey = siblings[index].Key;
                    if (!IsDeleted(elementsByKey, deleteKeys, previousSiblingKey))
                    {
                        return previousSiblingKey;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(primaryNode.ParentKey) &&
                !IsDeleted(elementsByKey, deleteKeys, primaryNode.ParentKey))
            {
                return primaryNode.ParentKey;
            }

            return string.Empty;
        }

        private static bool IsDeleted(
            IReadOnlyDictionary<string, HierarchyNode> elementsByKey,
            ISet<string> deleteKeys,
            string elementKey)
        {
            if (string.IsNullOrWhiteSpace(elementKey))
            {
                return false;
            }

            if (deleteKeys.Contains(elementKey))
            {
                return true;
            }

            if (!elementsByKey.TryGetValue(elementKey, out HierarchyNode currentNode))
            {
                return false;
            }

            while (!string.IsNullOrWhiteSpace(currentNode.ParentKey) &&
                   elementsByKey.TryGetValue(currentNode.ParentKey, out currentNode))
            {
                if (deleteKeys.Contains(currentNode.Key))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
