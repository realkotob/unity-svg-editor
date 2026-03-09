using System.Collections.Generic;
using Core.UI.Foundation;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class AssetHierarchyPreviewRenderer
    {
        private List<TreeViewItemData<StructureNode>> _previewHierarchyItems;

        public void ApplyPreview(
            VisualElement owner,
            TreeView hierarchyTreeView,
            List<TreeViewItemData<StructureNode>> hierarchyItems,
            string previewEnabledClassName)
        {
            if (owner == null || hierarchyTreeView == null || hierarchyItems == null)
            {
                return;
            }

            hierarchyItems.Clear();
            hierarchyItems.AddRange(GetPreviewHierarchyItems());
            hierarchyTreeView.SetRootItems<StructureNode>(hierarchyItems);
            owner.EnableClass(previewEnabledClassName, true);

            foreach (TreeViewItemData<StructureNode> hierarchyItem in hierarchyItems)
            {
                ExpandHierarchyItemRecursive(hierarchyTreeView, hierarchyItem);
            }

            hierarchyTreeView.Rebuild();
        }

        public void ClearPreview(
            VisualElement owner,
            TreeView hierarchyTreeView,
            List<TreeViewItemData<StructureNode>> hierarchyItems,
            string previewEnabledClassName)
        {
            if (owner == null || hierarchyTreeView == null || hierarchyItems == null)
            {
                return;
            }

            hierarchyItems.Clear();
            hierarchyTreeView.SetRootItems<StructureNode>(hierarchyItems);
            owner.EnableClass(previewEnabledClassName, false);
            hierarchyTreeView.Rebuild();
            _previewHierarchyItems = null;
        }

        private IReadOnlyList<TreeViewItemData<StructureNode>> GetPreviewHierarchyItems()
        {
            _previewHierarchyItems ??= CreatePreviewHierarchyItems();
            return _previewHierarchyItems;
        }

        private static void ExpandHierarchyItemRecursive(TreeView hierarchyTreeView, TreeViewItemData<StructureNode> hierarchyItem)
        {
            if (!hierarchyItem.hasChildren)
            {
                return;
            }

            hierarchyTreeView.ExpandItem(hierarchyItem.id, false);
            foreach (TreeViewItemData<StructureNode> childItem in hierarchyItem.children)
            {
                ExpandHierarchyItemRecursive(hierarchyTreeView, childItem);
            }
        }

        private static List<TreeViewItemData<StructureNode>> CreatePreviewHierarchyItems()
        {
            StructureNode rootNode = CreatePreviewHierarchyNode("preview-svg", "landing-hero", "svg", 0);
            StructureNode heroLayerNode = CreatePreviewHierarchyNode("preview-hero-layer", "hero-layer", "g", 1, rootNode.Key, "hero-layer");
            StructureNode heroBackgroundNode = CreatePreviewHierarchyNode("preview-hero-background", "hero-bg", "rect", 2, heroLayerNode.Key, "hero-layer");
            StructureNode heroAccentNode = CreatePreviewHierarchyNode("preview-hero-accent", "hero-accent", "path", 2, heroLayerNode.Key, "hero-layer");
            StructureNode badgeLayerNode = CreatePreviewHierarchyNode("preview-badge-layer", "badge-layer", "g", 1, rootNode.Key, "badge-layer");
            StructureNode badgeRingNode = CreatePreviewHierarchyNode("preview-badge-ring", "badge-ring", "circle", 2, badgeLayerNode.Key, "badge-layer");

            return new List<TreeViewItemData<StructureNode>>
            {
                new TreeViewItemData<StructureNode>(
                    1,
                    rootNode,
                    new List<TreeViewItemData<StructureNode>>
                    {
                        new(
                            2,
                            heroLayerNode,
                            new List<TreeViewItemData<StructureNode>>
                            {
                                new(3, heroBackgroundNode),
                                new(4, heroAccentNode)
                            }),
                        new(
                            5,
                            badgeLayerNode,
                            new List<TreeViewItemData<StructureNode>>
                            {
                                new(6, badgeRingNode)
                            })
                    })
            };
        }

        private static StructureNode CreatePreviewHierarchyNode(
            string key,
            string targetKey,
            string tagName,
            int depth,
            string parentKey = "",
            string layerKey = "")
        {
            string label = $"{tagName}#{targetKey}";
            return new StructureNode
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
