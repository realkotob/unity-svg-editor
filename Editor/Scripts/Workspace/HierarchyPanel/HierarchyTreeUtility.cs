using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal static class HierarchyTreeUtility
    {
        private static readonly string[] HierarchyIconClasses =
        {
            IconClass.SQUARE,
            IconClass.CIRCLE,
            IconClass.FILE_TEXT,
            IconClass.MINUS,
            IconClass.PEN,
            IconClass.MASK,
            IconClass.FOLDER,
            IconClass.FILE
        };

        public static void SelectElementByKey(
            TreeView treeView,
            string elementKey,
            IReadOnlyList<TreeViewItemData<HierarchyNode>> items)
        {
            SelectElementsByKey(
                treeView,
                string.IsNullOrWhiteSpace(elementKey) ? Array.Empty<string>() : new[] { elementKey },
                items,
                elementKey);
        }

        public static void SelectElementsByKey(
            TreeView treeView,
            IReadOnlyList<string> elementKeys,
            IReadOnlyList<TreeViewItemData<HierarchyNode>> items,
            string primaryElementKey)
        {
            if (treeView == null)
                return;

            if (elementKeys == null || elementKeys.Count == 0)
            {
                treeView.ClearSelection();
                return;
            }

            foreach (string elementKey in elementKeys)
            {
                if (!string.IsNullOrWhiteSpace(elementKey))
                {
                    ExpandHierarchyPath(treeView, elementKey, items);
                }
            }

            List<int> selectedIndices = new();
            foreach (string elementKey in elementKeys)
            {
                if (string.IsNullOrWhiteSpace(elementKey) ||
                    !TryFindHierarchyItemIndex(treeView, elementKey, items, out int selectedIndex))
                {
                    continue;
                }

                selectedIndices.Add(selectedIndex);
            }

            if (selectedIndices.Count == 0)
            {
                treeView.ClearSelection();
                return;
            }

            treeView.SetSelection(selectedIndices);

            string scrollTargetKey = !string.IsNullOrWhiteSpace(primaryElementKey)
                ? primaryElementKey
                : elementKeys[elementKeys.Count - 1];
            if (TryFindHierarchyItemId(scrollTargetKey, items, out int primaryTreeItemId))
            {
                treeView.ScrollToItemById(primaryTreeItemId);
            }
        }

        public static bool TryFindHierarchyItemId(
            string elementKey,
            IEnumerable<TreeViewItemData<HierarchyNode>> items,
            out int itemId)
        {
            foreach (TreeViewItemData<HierarchyNode> item in items)
            {
                if (string.Equals(item.data?.Key, elementKey, StringComparison.Ordinal))
                {
                    itemId = item.id;
                    return true;
                }

                if (!item.hasChildren)
                    continue;
                if (TryFindHierarchyItemId(elementKey, item.children, out itemId))
                    return true;
            }

            itemId = -1;
            return false;
        }

        public static bool TryFindHierarchyItem(
            string elementKey,
            IEnumerable<TreeViewItemData<HierarchyNode>> items,
            out TreeViewItemData<HierarchyNode> foundItem)
        {
            foreach (TreeViewItemData<HierarchyNode> item in items)
            {
                if (string.Equals(item.data?.Key, elementKey, StringComparison.Ordinal))
                {
                    foundItem = item;
                    return true;
                }

                if (!item.hasChildren)
                    continue;
                if (TryFindHierarchyItem(elementKey, item.children, out foundItem))
                    return true;
            }

            foundItem = default;
            return false;
        }

        public static bool ExpandHierarchyPath(
            TreeView treeView,
            string elementKey,
            IEnumerable<TreeViewItemData<HierarchyNode>> items)
        {
            foreach (TreeViewItemData<HierarchyNode> item in items)
            {
                if (string.Equals(item.data?.Key, elementKey, StringComparison.Ordinal))
                    return true;

                if (!item.hasChildren)
                    continue;
                if (!ExpandHierarchyPath(treeView, elementKey, item.children))
                    continue;

                treeView?.ExpandItem(item.id, false);
                return true;
            }

            return false;
        }

        public static IReadOnlyList<string> BuildElementKeyRange(
            TreeView treeView,
            IReadOnlyList<TreeViewItemData<HierarchyNode>> items,
            string startElementKey,
            string endElementKey)
        {
            List<string> orderedKeys = new();
            AppendHierarchyKeysInOrder(treeView, items, orderedKeys);

            int startIndex = orderedKeys.FindIndex(key => string.Equals(key, startElementKey, StringComparison.Ordinal));
            int endIndex = orderedKeys.FindIndex(key => string.Equals(key, endElementKey, StringComparison.Ordinal));
            if (startIndex < 0 || endIndex < 0)
            {
                return Array.Empty<string>();
            }

            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            var rangeKeys = new List<string>();
            for (int index = startIndex; index <= endIndex; index++)
            {
                rangeKeys.Add(orderedKeys[index]);
            }

            return rangeKeys;
        }

        public static IconKind ResolveHierarchyIconKind(string tagName)
        {
            return tagName switch
            {
                SvgTagName.SVG => IconKind.Square,
                SvgTagName.GROUP => IconKind.Folder,
                SvgTagName.TEXT => IconKind.FileText,
                SvgTagName.RECT => IconKind.Square,
                SvgTagName.CIRCLE => IconKind.Circle,
                SvgTagName.ELLIPSE => IconKind.Circle,
                SvgTagName.LINE => IconKind.Minus,
                SvgTagName.CLIP_PATH => IconKind.Mask,
                SvgTagName.MASK => IconKind.Mask,
                SvgTagName.POLYLINE => IconKind.Minus,
                SvgTagName.POLYGON => IconKind.Pen,
                SvgTagName.PATH => IconKind.Pen,
                _ => IconKind.File
            };
        }

        public static void ApplyHierarchyIconVariant(VisualElement iconElement, IconKind iconKind)
        {
            if (iconElement == null)
                return;

            foreach (string className in HierarchyIconClasses)
                iconElement.RemoveFromClassList(className);

            iconElement.AddClass(ResolveHierarchyIconClass(iconKind));
        }

        private static string ResolveHierarchyIconClass(IconKind iconKind)
        {
            return iconKind switch
            {
                IconKind.Square   => IconClass.SQUARE,
                IconKind.Circle   => IconClass.CIRCLE,
                IconKind.FileText => IconClass.FILE_TEXT,
                IconKind.Minus    => IconClass.MINUS,
                IconKind.Pen      => IconClass.PEN,
                IconKind.Mask     => IconClass.MASK,
                IconKind.Folder   => IconClass.FOLDER,
                _                 => IconClass.FILE
            };
        }

        public static string BuildHierarchyLabel(HierarchyNode item)
        {
            string source = !string.IsNullOrWhiteSpace(item.TreeLabel) ? item.TreeLabel : item.DisplayName;
            if (string.IsNullOrWhiteSpace(source))
                return "<unnamed>";

            return source.Trim().Replace('_', ' ').Replace('-', ' ');
        }

        private static bool TryFindHierarchyItemIndex(
            TreeView treeView,
            string elementKey,
            IEnumerable<TreeViewItemData<HierarchyNode>> items,
            out int selectedIndex)
        {
            int currentIndex = 0;
            return TryFindHierarchyItemIndexRecursive(treeView, elementKey, items, ref currentIndex, out selectedIndex);
        }

        private static bool TryFindHierarchyItemIndexRecursive(
            TreeView treeView,
            string elementKey,
            IEnumerable<TreeViewItemData<HierarchyNode>> items,
            ref int currentIndex,
            out int selectedIndex)
        {
            foreach (TreeViewItemData<HierarchyNode> item in items)
            {
                if (string.Equals(item.data?.Key, elementKey, StringComparison.Ordinal))
                {
                    selectedIndex = currentIndex;
                    return true;
                }

                currentIndex++;

                if (item.hasChildren &&
                    treeView != null &&
                    treeView.IsExpanded(item.id) &&
                    TryFindHierarchyItemIndexRecursive(treeView, elementKey, item.children, ref currentIndex, out selectedIndex))
                {
                    return true;
                }
            }

            selectedIndex = -1;
            return false;
        }

        private static void AppendHierarchyKeysInOrder(
            TreeView treeView,
            IEnumerable<TreeViewItemData<HierarchyNode>> items,
            List<string> orderedKeys)
        {
            foreach (TreeViewItemData<HierarchyNode> item in items)
            {
                if (item.data != null && !string.IsNullOrWhiteSpace(item.data.Key))
                {
                    orderedKeys.Add(item.data.Key);
                }

                if (item.hasChildren &&
                    treeView != null &&
                    treeView.IsExpanded(item.id))
                {
                    AppendHierarchyKeysInOrder(treeView, item.children, orderedKeys);
                }
            }
        }
    }
}
