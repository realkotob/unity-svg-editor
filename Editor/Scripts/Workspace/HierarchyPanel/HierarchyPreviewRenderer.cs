using System.Collections.Generic;
using UnityEngine.UIElements;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;
using SvgEditor.Shared;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal sealed class HierarchyPreviewRenderer : PreviewCollectionRenderer<TreeView, TreeViewItemData<HierarchyNode>>
    {
        protected override void ApplyPreviewItems(TreeView hierarchyTreeView, List<TreeViewItemData<HierarchyNode>> hierarchyItems)
        {
            hierarchyTreeView.SetRootItems<HierarchyNode>(hierarchyItems);
        }

        protected override void ClearPreviewItems(TreeView hierarchyTreeView, List<TreeViewItemData<HierarchyNode>> hierarchyItems)
        {
            hierarchyTreeView.SetRootItems<HierarchyNode>(hierarchyItems);
        }

        protected override void AfterApplyPreview(TreeView hierarchyTreeView, List<TreeViewItemData<HierarchyNode>> hierarchyItems)
        {
            foreach (TreeViewItemData<HierarchyNode> hierarchyItem in hierarchyItems)
            {
                ExpandHierarchyItemRecursive(hierarchyTreeView, hierarchyItem);
            }

            hierarchyTreeView.Rebuild();
        }

        protected override void AfterClearPreview(TreeView hierarchyTreeView, List<TreeViewItemData<HierarchyNode>> hierarchyItems)
        {
            hierarchyTreeView.Rebuild();
        }

        protected override List<TreeViewItemData<HierarchyNode>> CreatePreviewItems()
        {
            HierarchyNode rootNode = CreatePreviewHierarchyNode("preview-svg", "landing-hero", "svg", 0);
            HierarchyNode heroLayerNode = CreatePreviewHierarchyNode("preview-hero-layer", "hero-layer", "g", 1, rootNode.Key, "hero-layer");
            HierarchyNode heroBackgroundNode = CreatePreviewHierarchyNode("preview-hero-background", "hero-bg", "rect", 2, heroLayerNode.Key, "hero-layer");
            HierarchyNode heroAccentNode = CreatePreviewHierarchyNode("preview-hero-accent", "hero-accent", "path", 2, heroLayerNode.Key, "hero-layer");
            HierarchyNode badgeLayerNode = CreatePreviewHierarchyNode("preview-badge-layer", "badge-layer", "g", 1, rootNode.Key, "badge-layer");
            HierarchyNode badgeRingNode = CreatePreviewHierarchyNode("preview-badge-ring", "badge-ring", "circle", 2, badgeLayerNode.Key, "badge-layer");

            return new List<TreeViewItemData<HierarchyNode>>
            {
                new TreeViewItemData<HierarchyNode>(
                    1,
                    rootNode,
                    new List<TreeViewItemData<HierarchyNode>>
                    {
                        new(
                            2,
                            heroLayerNode,
                            new List<TreeViewItemData<HierarchyNode>>
                            {
                                new(3, heroBackgroundNode),
                                new(4, heroAccentNode)
                            }),
                        new(
                            5,
                            badgeLayerNode,
                            new List<TreeViewItemData<HierarchyNode>>
                            {
                                new(6, badgeRingNode)
                            })
                    })
            };
        }

        private static void ExpandHierarchyItemRecursive(TreeView hierarchyTreeView, TreeViewItemData<HierarchyNode> hierarchyItem)
        {
            if (!hierarchyItem.hasChildren)
            {
                return;
            }

            hierarchyTreeView.ExpandItem(hierarchyItem.id, false);
            foreach (TreeViewItemData<HierarchyNode> childItem in hierarchyItem.children)
            {
                ExpandHierarchyItemRecursive(hierarchyTreeView, childItem);
            }
        }

        private static HierarchyNode CreatePreviewHierarchyNode(
            string key,
            string targetKey,
            string tagName,
            int depth,
            string parentKey = "",
            string layerKey = "")
        {
            string label = $"{tagName}#{targetKey}";
            return new HierarchyNode
            {
                Key = key,
                TargetKey = targetKey,
                TagName = tagName,
                Depth = depth,
                ParentKey = parentKey,
                LayerKey = layerKey,
                DisplayName = label,
                TreeLabel = label
            };
        }
    }
}
