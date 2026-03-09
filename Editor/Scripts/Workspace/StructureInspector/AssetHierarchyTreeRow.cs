using System;
using UnityEngine.UIElements;
using Core.UI.Foundation;

namespace UnitySvgEditor.Editor
{
    internal sealed class AssetHierarchyTreeRow : VisualElement
    {
        internal VisualElement Expander { get; }

        private VisualElement Icon { get; }

        private Label TextLabel { get; }

        internal string ElementKey => userData as string;

        internal bool HasExpandableChildren => Expander.userData is string;

        public AssetHierarchyTreeRow()
        {
            this.AddClass(AssetHierarchyListView.UssClassName.ITEM);

            Expander = new VisualElement()
                .SetName(AssetHierarchyListView.ElementName.HIERARCHY_EXPANDER)
                .AddClass(AssetHierarchyListView.UssClassName.EXPANDER);
            Add(Expander);

            Icon = new VisualElement()
                .SetName(AssetHierarchyListView.ElementName.HIERARCHY_ICON)
                .AddClass(AssetHierarchyListView.UssClassName.ICON);
            Add(Icon);

            TextLabel = new Label()
                .SetName(AssetHierarchyListView.ElementName.HIERARCHY_TEXT)
                .AddClass(AssetHierarchyListView.UssClassName.TEXT);
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
                .EnableClass(AssetHierarchyListView.UssClassName.EXPANDER_PLACEHOLDER, !hasChildren)
                .EnableClass(AssetHierarchyListView.UssClassName.EXPANDER_EXPANDED, hasChildren && isExpanded);

            bool isGroup = string.Equals(tagName, "g", StringComparison.Ordinal);
            bool isText = string.Equals(tagName, "text", StringComparison.Ordinal);
            this.EnableClass(AssetHierarchyListView.UssClassName.ITEM_GROUP, isGroup)
                .EnableClass(AssetHierarchyListView.UssClassName.ITEM_TEXT, isText);
        }

        internal void Unbind()
        {
            tooltip = string.Empty;
            userData = null;
            style.paddingLeft = StyleKeyword.Null;
            this.EnableClass(AssetHierarchyListView.UssClassName.ITEM_GROUP, false)
                .EnableClass(AssetHierarchyListView.UssClassName.ITEM_TEXT, false);

            Expander.style.display = DisplayStyle.Flex;
            Expander.userData = null;
            Expander
                .EnableClass(AssetHierarchyListView.UssClassName.EXPANDER_PLACEHOLDER, true)
                .EnableClass(AssetHierarchyListView.UssClassName.EXPANDER_EXPANDED, false);

            StructureHierarchyTreeUtility.ApplyHierarchyIconVariant(Icon, "file");
            TextLabel.SetText(string.Empty);
        }
    }
}
