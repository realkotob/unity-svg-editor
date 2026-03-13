using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Core.UI.Foundation;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal static class HierarchyTreeUtility
    {
        private static readonly string[] HierarchyIconVariantClasses =
        {
            HierarchyTreeRow.UssClassName.ICON_SQUARE,
            HierarchyTreeRow.UssClassName.ICON_CIRCLE,
            HierarchyTreeRow.UssClassName.ICON_FILE_TEXT,
            HierarchyTreeRow.UssClassName.ICON_MINUS,
            HierarchyTreeRow.UssClassName.ICON_PEN,
            HierarchyTreeRow.UssClassName.ICON_FOLDER,
            HierarchyTreeRow.UssClassName.ICON_FILE
        };

        private static readonly string[] FoundationHierarchyIconClasses =
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
                IconKind.Square => HierarchyTreeRow.UssClassName.ICON_SQUARE,
                IconKind.Circle => HierarchyTreeRow.UssClassName.ICON_CIRCLE,
                IconKind.FileText => HierarchyTreeRow.UssClassName.ICON_FILE_TEXT,
                IconKind.Minus => HierarchyTreeRow.UssClassName.ICON_MINUS,
                IconKind.Pen => HierarchyTreeRow.UssClassName.ICON_PEN,
                IconKind.Mask => HierarchyTreeRow.UssClassName.ICON_FILE,
                IconKind.Folder => HierarchyTreeRow.UssClassName.ICON_FOLDER,
                _ => HierarchyTreeRow.UssClassName.ICON_FILE
            };
        }

        public static string BuildHierarchyLabel(HierarchyNode item)
        {
            string source = !string.IsNullOrWhiteSpace(item.TreeLabel) ? item.TreeLabel : item.DisplayName;
            if (string.IsNullOrWhiteSpace(source))
                return "<unnamed>";

            return source.Trim().Replace('_', ' ').Replace('-', ' ');
        }
    }
}
