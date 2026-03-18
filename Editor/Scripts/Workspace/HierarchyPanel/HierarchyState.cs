using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal sealed class HierarchyState
    {
        private readonly List<HierarchyNode> _elements = new();
        private readonly List<LayerSummary> _layers = new();
        private readonly List<TreeViewItemData<HierarchyNode>> _hierarchyItems = new();
        private readonly List<string> _selectedElementKeys = new();

        public IReadOnlyList<HierarchyNode> Elements => _elements;
        public IReadOnlyList<LayerSummary> Layers => _layers;
        public IReadOnlyList<TreeViewItemData<HierarchyNode>> HierarchyItems => _hierarchyItems;
        public IReadOnlyList<string> SelectedElementKeys => _selectedElementKeys;

        public string SelectedElementKey { get; private set; } = string.Empty;
        public string SelectionRangeAnchorKey { get; private set; } = string.Empty;
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
            _selectedElementKeys.Clear();
            SelectedElementKey = string.Empty;
            SelectionRangeAnchorKey = string.Empty;
            SelectedElementCanUseTarget = false;
            SelectedLayerKey = string.Empty;
            SelectedLayerVisible = true;
        }

        public void SetStructure(HierarchyOutline snapshot, string activeTargetKey)
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

            RestoreElementSelection(activeTargetKey);

            var activeLayer = _elements
                .FirstOrDefault(item => string.Equals(item.Key, SelectedElementKey, StringComparison.Ordinal))
                ?.LayerKey;

            SelectLayer(activeLayer);
        }

        public void SelectElement(string elementKey)
        {
            var matched = _elements.FirstOrDefault(item => string.Equals(item.Key, elementKey, StringComparison.Ordinal));
            if (matched == null)
            {
                ClearElementSelection();
                return;
            }

            ApplyElementSelection(new[] { matched.Key }, matched.Key, matched.Key);
        }

        public void SetElementSelection(
            IEnumerable<string> selectedElementKeys,
            string primaryElementKey,
            string anchorElementKey)
        {
            ApplyElementSelection(selectedElementKeys, primaryElementKey, anchorElementKey);
        }

        public void ClearElementSelection()
        {
            _selectedElementKeys.Clear();
            SelectedElementKey = string.Empty;
            SelectionRangeAnchorKey = string.Empty;
            SelectedElementCanUseTarget = false;
        }

        public void ToggleElementSelection(string elementKey)
        {
            var matched = _elements.FirstOrDefault(item => string.Equals(item.Key, elementKey, StringComparison.Ordinal));
            if (matched == null)
            {
                return;
            }

            if (_selectedElementKeys.Remove(matched.Key))
            {
                if (_selectedElementKeys.Count == 0)
                {
                    ClearElementSelection();
                    return;
                }

                string fallbackPrimaryKey = _selectedElementKeys[^1];
                string anchorKey = _selectedElementKeys.Contains(SelectionRangeAnchorKey)
                    ? SelectionRangeAnchorKey
                    : fallbackPrimaryKey;
                ApplyElementSelection(_selectedElementKeys, fallbackPrimaryKey, anchorKey);
                return;
            }

            var updatedKeys = new List<string>(_selectedElementKeys)
            {
                matched.Key
            };

            ApplyElementSelection(updatedKeys, matched.Key, matched.Key);
        }

        public void AddElementSelectionRange(string elementKey)
        {
            var matched = _elements.FirstOrDefault(item => string.Equals(item.Key, elementKey, StringComparison.Ordinal));
            if (matched == null)
            {
                return;
            }

            string anchorKey = ResolveSelectionAnchorKey();
            if (string.IsNullOrWhiteSpace(anchorKey))
            {
                SelectElement(matched.Key);
                return;
            }

            List<string> rangeKeys = BuildRangeKeys(anchorKey, matched.Key);
            if (rangeKeys.Count == 0)
            {
                SelectElement(matched.Key);
                return;
            }

            var updatedKeys = new List<string>(_selectedElementKeys);
            foreach (string rangeKey in rangeKeys)
            {
                if (!updatedKeys.Contains(rangeKey))
                {
                    updatedKeys.Add(rangeKey);
                }
            }

            ApplyElementSelection(updatedKeys, matched.Key, anchorKey);
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
            TreeViewItemData<HierarchyNode> item,
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

        private void RestoreElementSelection(string fallbackElementKey)
        {
            List<string> survivingKeys = _selectedElementKeys
                .Where(ContainsElementKey)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            string primaryKey = ContainsElementKey(SelectedElementKey)
                ? SelectedElementKey
                : survivingKeys.LastOrDefault();
            string anchorKey = ContainsElementKey(SelectionRangeAnchorKey)
                ? SelectionRangeAnchorKey
                : primaryKey;

            if (survivingKeys.Count > 0 && !string.IsNullOrWhiteSpace(primaryKey))
            {
                ApplyElementSelection(survivingKeys, primaryKey, anchorKey);
                return;
            }

            if (ContainsElementKey(fallbackElementKey))
            {
                ApplyElementSelection(new[] { fallbackElementKey }, fallbackElementKey, fallbackElementKey);
                return;
            }

            ClearElementSelection();
        }

        private void ApplyElementSelection(
            IEnumerable<string> selectedElementKeys,
            string primaryElementKey,
            string anchorElementKey)
        {
            List<string> normalizedKeys = selectedElementKeys?.ToList() ?? new List<string>();
            _selectedElementKeys.Clear();

            foreach (string selectedElementKey in normalizedKeys)
            {
                if (string.IsNullOrWhiteSpace(selectedElementKey) ||
                    !ContainsElementKey(selectedElementKey) ||
                    _selectedElementKeys.Contains(selectedElementKey))
                {
                    continue;
                }

                _selectedElementKeys.Add(selectedElementKey);
            }

            if (_selectedElementKeys.Count == 0)
            {
                ClearElementSelection();
                return;
            }

            if (string.IsNullOrWhiteSpace(primaryElementKey) || !_selectedElementKeys.Contains(primaryElementKey))
            {
                primaryElementKey = _selectedElementKeys[^1];
            }

            SelectedElementKey = primaryElementKey;
            SelectionRangeAnchorKey = !string.IsNullOrWhiteSpace(anchorElementKey) &&
                                      _selectedElementKeys.Contains(anchorElementKey)
                ? anchorElementKey
                : primaryElementKey;
            SelectedElementCanUseTarget = _elements
                .FirstOrDefault(item => string.Equals(item.Key, SelectedElementKey, StringComparison.Ordinal))
                ?.CanUseAsTarget ?? false;
        }

        private string ResolveSelectionAnchorKey()
        {
            if (ContainsElementKey(SelectionRangeAnchorKey))
            {
                return SelectionRangeAnchorKey;
            }

            if (ContainsElementKey(SelectedElementKey))
            {
                return SelectedElementKey;
            }

            return string.Empty;
        }

        private List<string> BuildRangeKeys(string startElementKey, string endElementKey)
        {
            int startIndex = _elements.FindIndex(item => string.Equals(item.Key, startElementKey, StringComparison.Ordinal));
            int endIndex = _elements.FindIndex(item => string.Equals(item.Key, endElementKey, StringComparison.Ordinal));

            if (startIndex < 0 || endIndex < 0)
            {
                return new List<string>();
            }

            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            var rangeKeys = new List<string>();
            for (int index = startIndex; index <= endIndex; index++)
            {
                rangeKeys.Add(_elements[index].Key);
            }

            return rangeKeys;
        }

        private bool ContainsElementKey(string elementKey)
        {
            if (string.IsNullOrWhiteSpace(elementKey))
            {
                return false;
            }

            return _elements.Any(item => string.Equals(item.Key, elementKey, StringComparison.Ordinal));
        }
    }
}
