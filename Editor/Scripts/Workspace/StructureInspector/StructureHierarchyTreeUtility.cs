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

        private static readonly string[] FoundationHierarchyIconClasses =
        {
            SvgEditorIconClass.HIERARCHY_SQUARE,
            SvgEditorIconClass.HIERARCHY_CIRCLE,
            SvgEditorIconClass.HIERARCHY_FILE_TEXT,
            SvgEditorIconClass.HIERARCHY_MINUS,
            SvgEditorIconClass.HIERARCHY_PEN,
            SvgEditorIconClass.HIERARCHY_MASK,
            SvgEditorIconClass.HIERARCHY_FOLDER,
            SvgEditorIconClass.HIERARCHY_FILE
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

        public static IconKind ResolveHierarchyIconKind(string tagName)
        {
            return tagName switch
            {
                SvgTagName.Svg => IconKind.Square,
                SvgTagName.Group => IconKind.Folder,
                SvgTagName.Text => IconKind.FileText,
                SvgTagName.Rect => IconKind.Square,
                SvgTagName.Circle => IconKind.Circle,
                SvgTagName.Ellipse => IconKind.Circle,
                SvgTagName.Line => IconKind.Minus,
                SvgTagName.ClipPath => IconKind.Mask,
                SvgTagName.Mask => IconKind.Mask,
                SvgTagName.Polyline => IconKind.Minus,
                SvgTagName.Polygon => IconKind.Pen,
                SvgTagName.Path => IconKind.Pen,
                _ => IconKind.File
            };
        }

        public static void ApplyHierarchyIconVariant(VisualElement iconElement, IconKind iconKind)
        {
            if (iconElement == null)
                return;

            foreach (string className in HierarchyIconVariantClasses)
                iconElement.RemoveFromClassList(className);

            foreach (string className in FoundationHierarchyIconClasses)
                iconElement.RemoveFromClassList(className);

            iconElement.AddClass(ResolveHierarchyVariantClass(iconKind));
            iconElement.AddClass(SvgEditorIconClass.ResolveHierarchyIcon(iconKind));
        }

        private static string ResolveHierarchyVariantClass(IconKind iconKind)
        {
            return iconKind switch
            {
                IconKind.Square => AssetHierarchyTreeRow.UssClassName.ICON_SQUARE,
                IconKind.Circle => AssetHierarchyTreeRow.UssClassName.ICON_CIRCLE,
                IconKind.FileText => AssetHierarchyTreeRow.UssClassName.ICON_FILE_TEXT,
                IconKind.Minus => AssetHierarchyTreeRow.UssClassName.ICON_MINUS,
                IconKind.Pen => AssetHierarchyTreeRow.UssClassName.ICON_PEN,
                IconKind.Mask => AssetHierarchyTreeRow.UssClassName.ICON_FILE,
                IconKind.Folder => AssetHierarchyTreeRow.UssClassName.ICON_FOLDER,
                _ => AssetHierarchyTreeRow.UssClassName.ICON_FILE
            };
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
