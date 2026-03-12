using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor
{
    internal sealed class StructurePanelState
    {
        private readonly List<StructureNode> _elements = new();
        private readonly List<LayerSummary> _layers = new();
        private readonly List<TreeViewItemData<StructureNode>> _hierarchyItems = new();

        public IReadOnlyList<StructureNode> Elements => _elements;
        public IReadOnlyList<LayerSummary> Layers => _layers;
        public IReadOnlyList<TreeViewItemData<StructureNode>> HierarchyItems => _hierarchyItems;

        public string SelectedElementKey { get; private set; } = string.Empty;
        public bool SelectedElementCanUseTarget { get; private set; }
        public string SelectedLayerKey { get; private set; } = string.Empty;
        public bool SelectedLayerVisible { get; private set; } = true;

        public float QuickTranslateX { get; private set; }
        public float QuickTranslateY { get; private set; }
        public float QuickRotate { get; private set; }
        public float QuickScaleX { get; private set; } = 1f;
        public float QuickScaleY { get; private set; } = 1f;

        public void Clear()
        {
            _elements.Clear();
            _layers.Clear();
            _hierarchyItems.Clear();
            SelectedElementKey = string.Empty;
            SelectedElementCanUseTarget = false;
            SelectedLayerKey = string.Empty;
            SelectedLayerVisible = true;
        }

        public void SetStructure(StructureOutline snapshot, string activeTargetKey)
        {
            _elements.Clear();
            _layers.Clear();
            _hierarchyItems.Clear();

            if (snapshot?.Elements != null)
            {
                _elements.AddRange(snapshot.Elements);
            }

            if (snapshot?.Layers != null)
            {
                _layers.AddRange(snapshot.Layers);
            }

            if (snapshot?.HierarchyItems != null)
            {
                _hierarchyItems.AddRange(snapshot.HierarchyItems);
            }

            SelectElement(activeTargetKey);

            var activeLayer = _elements
                .FirstOrDefault(item => string.Equals(item.Key, activeTargetKey, StringComparison.Ordinal))
                ?.LayerKey;

            SelectLayer(activeLayer);
        }

        public void SelectElement(string elementKey)
        {
            var matched = _elements.FirstOrDefault(item => string.Equals(item.Key, elementKey, StringComparison.Ordinal));
            SelectedElementKey = matched?.Key ?? string.Empty;
            SelectedElementCanUseTarget = matched?.CanUseAsTarget ?? false;
        }

        public void SelectLayer(string layerKey)
        {
            var matched = _layers.FirstOrDefault(item => string.Equals(item.Key, layerKey, StringComparison.Ordinal));
            SelectedLayerKey = matched?.Key ?? string.Empty;
            SelectedLayerVisible = matched?.IsVisible ?? true;
        }

        public bool TryGetHierarchyId(string elementKey, out int treeId)
        {
            foreach (var item in _hierarchyItems)
            {
                if (TryGetHierarchyIdRecursive(item, elementKey, out treeId))
                {
                    return true;
                }
            }

            treeId = default;
            return false;
        }

        public string BuildQuickTransformString(Func<float, string> numberFormatter)
        {
            return TransformStringBuilder.BuildTransform(
                QuickTranslateX,
                QuickTranslateY,
                QuickRotate,
                QuickScaleX,
                QuickScaleY,
                numberFormatter);
        }

        public void SetQuickTranslateX(float value)
        {
            QuickTranslateX = value;
        }

        public void SetQuickTranslateY(float value)
        {
            QuickTranslateY = value;
        }

        public void SetQuickRotate(float value)
        {
            QuickRotate = value;
        }

        public void SetQuickScaleX(float value)
        {
            QuickScaleX = value;
        }

        public void SetQuickScaleY(float value)
        {
            QuickScaleY = value;
        }

        private static bool TryGetHierarchyIdRecursive(
            TreeViewItemData<StructureNode> item,
            string elementKey,
            out int treeId)
        {
            if (item.data != null && string.Equals(item.data.Key, elementKey, StringComparison.Ordinal))
            {
                treeId = item.id;
                return true;
            }

            foreach (var child in item.children)
            {
                if (TryGetHierarchyIdRecursive(child, elementKey, out treeId))
                {
                    return true;
                }
            }

            treeId = default;
            return false;
        }
    }
}
