using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Core.UI.Foundation;
using UnitySvgEditor.Editor.Workspace.Canvas;

namespace UnitySvgEditor.Editor
{
    internal sealed class AssetHierarchyTreeRow : VisualElement
    {
        internal static class ElementName
        {
            public const string HIERARCHY_EXPANDER = "hierarchy-expander";
            public const string HIERARCHY_ICON = "hierarchy-icon";
            public const string HIERARCHY_TEXT = "hierarchy-text";
            public const string HIERARCHY_BADGE_CONTAINER = "hierarchy-badge-container";
            public const string HIERARCHY_MASK_BADGE = "hierarchy-mask-badge";
            public const string HIERARCHY_CLIP_BADGE = "hierarchy-clip-badge";
        }

        internal static class UssClassName
        {
            public const string BASE = "svg-editor__asset-hierarchy";
            private const string ELEMENT_PREFIX = BASE + "__";

            public const string ITEM = ELEMENT_PREFIX + "item";
            public const string EXPANDER = ELEMENT_PREFIX + "expander";
            public const string ICON = ELEMENT_PREFIX + "icon";
            public const string TEXT = ELEMENT_PREFIX + "text";
            public const string BADGE_CONTAINER = ELEMENT_PREFIX + "badge-container";
            public const string BADGE = ELEMENT_PREFIX + "badge";
            public const string BADGE_MASK = BADGE + "--mask";
            public const string BADGE_CLIP = BADGE + "--clip";
            public const string ITEM_PROXY_MASK = ITEM + "--proxy-mask";
            public const string ITEM_PROXY_CLIP = ITEM + "--proxy-clip";
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

        private VisualElement BadgeContainer { get; }

        private Label MaskBadge { get; }

        private Label ClipBadge { get; }

        internal string ElementKey => userData as string;

        internal bool HasExpandableChildren => Expander.userData is string;

        public AssetHierarchyTreeRow()
        {
            this.AddClass(UssClassName.ITEM);

            Expander = new VisualElement()
                .SetName(ElementName.HIERARCHY_EXPANDER)
                .AddClass(UssClassName.EXPANDER)
                .AddClass(IconClass.CHEVRON_RIGHT);
            Expander.pickingMode = PickingMode.Position;
            Add(Expander);

            Icon = new VisualElement()
                .SetName(ElementName.HIERARCHY_ICON)
                .AddClass(UssClassName.ICON);
            Icon.pickingMode = PickingMode.Ignore;
            Add(Icon);

            TextLabel = new Label()
                .SetName(ElementName.HIERARCHY_TEXT)
                .AddClass(UssClassName.TEXT);
            TextLabel.pickingMode = PickingMode.Ignore;
            Add(TextLabel);

            BadgeContainer = new VisualElement()
                .SetName(ElementName.HIERARCHY_BADGE_CONTAINER)
                .AddClass(UssClassName.BADGE_CONTAINER);
            BadgeContainer.pickingMode = PickingMode.Ignore;

            MaskBadge = new Label("M")
                .SetName(ElementName.HIERARCHY_MASK_BADGE)
                .AddClass(UssClassName.BADGE)
                .AddClass(UssClassName.BADGE_MASK);
            MaskBadge.pickingMode = PickingMode.Ignore;
            BadgeContainer.Add(MaskBadge);

            ClipBadge = new Label("C")
                .SetName(ElementName.HIERARCHY_CLIP_BADGE)
                .AddClass(UssClassName.BADGE)
                .AddClass(UssClassName.BADGE_CLIP);
            ClipBadge.pickingMode = PickingMode.Ignore;
            BadgeContainer.Add(ClipBadge);

            Add(BadgeContainer);
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
            tooltip = BuildReferenceTooltip(hierarchyNode);

            Expander.style.display = DisplayStyle.Flex;
            Expander.userData = hasChildren ? hierarchyNode.Key : null;
            Expander.pickingMode = hasChildren ? PickingMode.Position : PickingMode.Ignore;
            Expander
                .EnableClass(UssClassName.EXPANDER_PLACEHOLDER, !hasChildren)
                .EnableClass(UssClassName.EXPANDER_EXPANDED, hasChildren && isExpanded);

            bool isGroup = string.Equals(tagName, SvgTagName.GROUP, StringComparison.Ordinal);
            bool isText = string.Equals(tagName, SvgTagName.TEXT, StringComparison.Ordinal);
            this.EnableClass(UssClassName.ITEM_GROUP, isGroup)
                .EnableClass(UssClassName.ITEM_TEXT, isText);

            bool isMaskProxy = hierarchyNode.IsDefinitionProxy &&
                               hierarchyNode.DefinitionProxyKind == CanvasDefinitionOverlayKind.Mask;
            bool isClipProxy = hierarchyNode.IsDefinitionProxy &&
                               hierarchyNode.DefinitionProxyKind == CanvasDefinitionOverlayKind.ClipPath;
            MaskBadge.style.display = isMaskProxy ? DisplayStyle.Flex : DisplayStyle.None;
            ClipBadge.style.display = isClipProxy ? DisplayStyle.Flex : DisplayStyle.None;
            BadgeContainer.style.display = hierarchyNode.IsDefinitionProxy ? DisplayStyle.Flex : DisplayStyle.None;
            this.EnableClass(UssClassName.ITEM_PROXY_MASK, isMaskProxy)
                .EnableClass(UssClassName.ITEM_PROXY_CLIP, isClipProxy);
            tooltip = BuildReferenceTooltip(hierarchyNode);
        }

        internal void Unbind()
        {
            tooltip = string.Empty;
            userData = null;
            style.paddingLeft = StyleKeyword.Null;
            this.EnableClass(UssClassName.ITEM_GROUP, false)
                .EnableClass(UssClassName.ITEM_TEXT, false)
                .EnableClass(UssClassName.ITEM_PROXY_MASK, false)
                .EnableClass(UssClassName.ITEM_PROXY_CLIP, false);

            Expander.style.display = DisplayStyle.Flex;
            Expander.userData = null;
            Expander.pickingMode = PickingMode.Ignore;
            Expander
                .EnableClass(UssClassName.EXPANDER_PLACEHOLDER, true)
                .EnableClass(UssClassName.EXPANDER_EXPANDED, false);

            StructureHierarchyTreeUtility.ApplyHierarchyIconVariant(Icon, IconKind.File);
            TextLabel.SetText(string.Empty);
            MaskBadge.style.display = DisplayStyle.None;
            ClipBadge.style.display = DisplayStyle.None;
            BadgeContainer.style.display = DisplayStyle.None;
        }

        private static string BuildReferenceTooltip(StructureNode hierarchyNode)
        {
            if (hierarchyNode == null || !hierarchyNode.IsDefinitionProxy || string.IsNullOrWhiteSpace(hierarchyNode.DefinitionReferenceId))
                return null;

            List<string> lines = new();
            if (hierarchyNode.DefinitionProxyKind == CanvasDefinitionOverlayKind.Mask)
                lines.Add($"Mask: #{hierarchyNode.DefinitionReferenceId}");
            if (hierarchyNode.DefinitionProxyKind == CanvasDefinitionOverlayKind.ClipPath)
                lines.Add($"Clip: #{hierarchyNode.DefinitionReferenceId}");

            return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
        }
    }
}
