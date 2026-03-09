using System;
using UnityEngine.UIElements;
using Core.UI.Foundation;

namespace UnitySvgEditor.Editor
{
    internal sealed class AssetHierarchyTreeRow : VisualElement
    {
        internal static class ElementName
        {
            public const string HIERARCHY_EXPANDER = "hierarchy-expander";
            public const string HIERARCHY_ICON = "hierarchy-icon";
            public const string HIERARCHY_TEXT = "hierarchy-text";
        }

        internal static class UssClassName
        {
            public const string BASE = "svg-editor__asset-hierarchy";
            private const string ELEMENT_PREFIX = BASE + "__";

            public const string ITEM = ELEMENT_PREFIX + "item";
            public const string EXPANDER = ELEMENT_PREFIX + "expander";
            public const string ICON = ELEMENT_PREFIX + "icon";
            public const string TEXT = ELEMENT_PREFIX + "text";
            public const string ITEM_GROUP = ITEM + "--group";
            public const string ITEM_TEXT = ITEM + "--text";
            public const string EXPANDER_PLACEHOLDER = EXPANDER + "--placeholder";
            public const string EXPANDER_EXPANDED = EXPANDER + "--expanded";
            public const string ICON_SQUARE = ICON + "--square";
            public const string ICON_CIRCLE = ICON + "--circle";
            public const string ICON_FILE_TEXT = ICON + "--file-text";
            public const string ICON_MINUS = ICON + "--minus";
            public const string ICON_PEN = ICON + "--pen";
            public const string ICON_FOLDER = ICON + "--folder";
            public const string ICON_FILE = ICON + "--file";
        }

        internal VisualElement Expander { get; }

        private VisualElement Icon { get; }

        private Label TextLabel { get; }

        internal string ElementKey => userData as string;

        internal bool HasExpandableChildren => Expander.userData is string;

        public AssetHierarchyTreeRow()
        {
            this.AddClass(UssClassName.ITEM);

            Expander = new VisualElement()
                .SetName(ElementName.HIERARCHY_EXPANDER)
                .AddClass(UssClassName.EXPANDER);
            Add(Expander);

            Icon = new VisualElement()
                .SetName(ElementName.HIERARCHY_ICON)
                .AddClass(UssClassName.ICON);
            Add(Icon);

            TextLabel = new Label()
                .SetName(ElementName.HIERARCHY_TEXT)
                .AddClass(UssClassName.TEXT);
            Add(TextLabel);
        }

        internal void Bind(StructureNode hierarchyNode, bool hasChildren, bool isExpanded)
        {
            string tagName = (hierarchyNode.TagName ?? string.Empty).ToLowerInvariant();
            StructureHierarchyTreeUtility.ApplyHierarchyIconVariant(
                Icon,
                StructureHierarchyTreeUtility.ResolveHierarchyIconKind(tagName));
            TextLabel.SetText(StructureHierarchyTreeUtility.BuildHierarchyLabel(hierarchyNode));
            style.paddingLeft = AssetHierarchyListView.Layout.ROW_PADDING_LEFT +
                (hierarchyNode.Depth * AssetHierarchyListView.Layout.ROW_DEPTH_OFFSET);
            userData = hierarchyNode.Key;
            tooltip = hierarchyNode.CanUseAsTarget
                ? $"#{hierarchyNode.TargetKey} <{hierarchyNode.TagName}>"
                : $"<{hierarchyNode.TagName}>";

            Expander.style.display = DisplayStyle.Flex;
            Expander.userData = hasChildren ? hierarchyNode.Key : null;
            Expander
                .EnableClass(UssClassName.EXPANDER_PLACEHOLDER, !hasChildren)
                .EnableClass(UssClassName.EXPANDER_EXPANDED, hasChildren && isExpanded);

            bool isGroup = string.Equals(tagName, "g", StringComparison.Ordinal);
            bool isText = string.Equals(tagName, "text", StringComparison.Ordinal);
            this.EnableClass(UssClassName.ITEM_GROUP, isGroup)
                .EnableClass(UssClassName.ITEM_TEXT, isText);
        }

        internal void Unbind()
        {
            tooltip = string.Empty;
            userData = null;
            style.paddingLeft = StyleKeyword.Null;
            this.EnableClass(UssClassName.ITEM_GROUP, false)
                .EnableClass(UssClassName.ITEM_TEXT, false);

            Expander.style.display = DisplayStyle.Flex;
            Expander.userData = null;
            Expander
                .EnableClass(UssClassName.EXPANDER_PLACEHOLDER, true)
                .EnableClass(UssClassName.EXPANDER_EXPANDED, false);

            StructureHierarchyTreeUtility.ApplyHierarchyIconVariant(Icon, "file");
            TextLabel.SetText(string.Empty);
        }
    }
}
