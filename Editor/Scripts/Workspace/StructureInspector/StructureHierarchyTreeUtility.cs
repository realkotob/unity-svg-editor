using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Core.UI.Foundation;

namespace UnitySvgEditor.Editor
{
    internal static class StructureHierarchyTreeUtility
    {
        private static readonly string[] HierarchyIconVariantClasses =
        {
            AssetHierarchyTreeRow.UssClassName.ICON_SQUARE,
            AssetHierarchyTreeRow.UssClassName.ICON_CIRCLE,
            AssetHierarchyTreeRow.UssClassName.ICON_FILE_TEXT,
            AssetHierarchyTreeRow.UssClassName.ICON_MINUS,
            AssetHierarchyTreeRow.UssClassName.ICON_PEN,
            AssetHierarchyTreeRow.UssClassName.ICON_FOLDER,
            AssetHierarchyTreeRow.UssClassName.ICON_FILE
        };

        public static void SelectElementByKey(
            TreeView treeView,
            string elementKey,
            IReadOnlyList<TreeViewItemData<StructureNode>> items)
        {
            if (treeView == null)
                return;

            if (string.IsNullOrWhiteSpace(elementKey))
            {
                treeView.ClearSelection();
                return;
            }

            if (!TryFindHierarchyItemId(elementKey, items, out int treeItemId))
            {
                treeView.ClearSelection();
                return;
            }

            ExpandHierarchyPath(treeView, elementKey, items);
            treeView.SetSelectionById(treeItemId);
            treeView.ScrollToItemById(treeItemId);
        }

        public static bool TryFindHierarchyItemId(
            string elementKey,
            IEnumerable<TreeViewItemData<StructureNode>> items,
            out int itemId)
        {
            foreach (TreeViewItemData<StructureNode> item in items)
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
            IEnumerable<TreeViewItemData<StructureNode>> items,
            out TreeViewItemData<StructureNode> foundItem)
        {
            foreach (TreeViewItemData<StructureNode> item in items)
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
            IEnumerable<TreeViewItemData<StructureNode>> items)
        {
            foreach (TreeViewItemData<StructureNode> item in items)
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

        public static string ResolveHierarchyIconKind(string tagName)
        {
            return tagName switch
            {
                "svg" => "square",
                "g" => "folder",
                "text" => "file-text",
                "rect" => "square",
                "circle" => "circle",
                "ellipse" => "circle",
                "line" => "minus",
                "polyline" => "minus",
                "polygon" => "pen",
                "path" => "pen",
                _ => "file"
            };
        }

        public static void ApplyHierarchyIconVariant(VisualElement iconElement, string iconKind)
        {
            if (iconElement == null)
                return;

            foreach (string className in HierarchyIconVariantClasses)
                iconElement.RemoveFromClassList(className);

            iconElement.AddClass($"{AssetHierarchyTreeRow.UssClassName.ICON}--{iconKind}");
        }

        public static string BuildHierarchyLabel(StructureNode item)
        {
            string source = !string.IsNullOrWhiteSpace(item.TreeLabel) ? item.TreeLabel : item.DisplayName;
            if (string.IsNullOrWhiteSpace(source))
                return "<unnamed>";

            return source.Trim().Replace('_', ' ').Replace('-', ' ');
        }
    }
}
