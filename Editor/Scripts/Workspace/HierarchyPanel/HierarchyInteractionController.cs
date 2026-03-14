using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Shared;
using SvgEditor.DocumentModel;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal sealed class HierarchyInteractionController
    {
        private const float DragThreshold = 5f;
        private TreeView _treeView;
        private IHierarchyHost _host;
        private readonly ReorderSession _reorderSession = new();
        private readonly DropIndicatorPresenter _dropIndicatorPresenter = new();
        private readonly ReorderMutationService _mutationService = new();
        private string _selectionAnchorElementKey = string.Empty;
        private string _preferredPrimaryElementKey = string.Empty;

        public string PreferredPrimaryElementKey => _preferredPrimaryElementKey;
        public string SelectionAnchorElementKey => _selectionAnchorElementKey;

        public void Bind(TreeView treeView, IHierarchyHost host)
        {
            Unbind();
            _treeView = treeView;
            _host = host;
            if (_treeView == null)
                return;

            CallbackBindingUtility.ToggleCallback<PointerMoveEvent>(_treeView, OnHierarchyPointerMove, register: true);
            CallbackBindingUtility.ToggleCallback<PointerUpEvent>(_treeView, OnHierarchyPointerUp, register: true);
            CallbackBindingUtility.ToggleCallback<PointerCancelEvent>(_treeView, OnHierarchyPointerCancel, register: true);
            _dropIndicatorPresenter.Bind(_treeView);
        }

        public void Unbind()
        {
            ResetDragState();

            if (_treeView != null)
            {
                CallbackBindingUtility.ToggleCallback<PointerMoveEvent>(_treeView, OnHierarchyPointerMove, register: false);
                CallbackBindingUtility.ToggleCallback<PointerUpEvent>(_treeView, OnHierarchyPointerUp, register: false);
                CallbackBindingUtility.ToggleCallback<PointerCancelEvent>(_treeView, OnHierarchyPointerCancel, register: false);
            }

            _dropIndicatorPresenter.Unbind(_treeView);
            _host = null;
            _treeView = null;
        }

        public void CancelPendingPress()
        {
            _reorderSession.CancelPendingPress();
        }

        public bool ConsumeSuppressedRowClick()
        {
            return _reorderSession.ConsumeSuppressedRowClick();
        }

        public void SetSelectionState(string primaryElementKey, string selectionAnchorElementKey)
        {
            _preferredPrimaryElementKey = primaryElementKey ?? string.Empty;
            _selectionAnchorElementKey = selectionAnchorElementKey ?? _preferredPrimaryElementKey;
        }

        public void OnHierarchyRowPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || evt.currentTarget is not VisualElement row)
                return;

            string pressedHierarchyElementKey = row.userData as string;

            if (_treeView == null ||
                _host == null ||
                !HierarchyTreeUtility.TryFindHierarchyItemId(pressedHierarchyElementKey, _host.HierarchyItems, out int treeItemId))
            {
                return;
            }

            bool rangeSelection = (evt.modifiers & EventModifiers.Shift) != 0;
            bool toggleSelection = (evt.modifiers & (EventModifiers.Command | EventModifiers.Control)) != 0;

            if (rangeSelection)
            {
                ApplyRangeSelection(pressedHierarchyElementKey);
                evt.StopPropagation();
                return;
            }

            if (toggleSelection)
            {
                ApplyToggleSelection(pressedHierarchyElementKey);
                evt.StopPropagation();
                return;
            }

            SetSelectionState(pressedHierarchyElementKey, pressedHierarchyElementKey);
            _treeView.SetSelectionById(treeItemId);
            _reorderSession.BeginPress(_treeView, pressedHierarchyElementKey, evt.pointerId, evt.position);
        }

        private void OnHierarchyPointerMove(PointerMoveEvent evt)
        {
            if (_treeView == null ||
                _host == null ||
                !_reorderSession.Matches(evt.pointerId) ||
                string.IsNullOrWhiteSpace(_reorderSession.PressedHierarchyElementKey))
            {
                return;
            }

            HierarchyNode draggedItem = _host.FindHierarchyNode(_reorderSession.PressedHierarchyElementKey);
            if (draggedItem == null || string.IsNullOrWhiteSpace(draggedItem.ParentKey))
            {
                ResetDragState();
                return;
            }

            if (!_reorderSession.TryBeginDrag(evt.position, DragThreshold))
            {
                return;
            }

            UpdateHierarchyInsertionIndicator(evt.position, ResolveHoveredElement(evt.position), draggedItem);
            evt.StopPropagation();
        }

        private void OnHierarchyPointerUp(PointerUpEvent evt)
        {
            if (_treeView == null || _host == null || !_reorderSession.Matches(evt.pointerId))
                return;

            if (_reorderSession.IsHierarchyDragging)
            {
                _mutationService.ApplyMove(
                    _host,
                    new MoveElementRequest(
                        _reorderSession.PressedHierarchyElementKey,
                        _reorderSession.PendingHierarchyDropParentKey,
                        _reorderSession.PendingHierarchyDropChildIndex));
            }

            ResetDragState();
        }

        private void OnHierarchyPointerCancel(PointerCancelEvent evt)
        {
            if (_reorderSession.Matches(evt.pointerId))
                ResetDragState();
        }

        private void UpdateHierarchyInsertionIndicator(Vector2 pointerPosition, VisualElement hoveredElement, HierarchyNode draggedItem)
        {
            if (_treeView == null || _host == null)
            {
                ClearPendingDropIndicator();
                return;
            }

            if (draggedItem == null || string.IsNullOrWhiteSpace(draggedItem.ParentKey))
            {
                ClearPendingDropIndicator();
                return;
            }

            VisualElement hoveredRow = FindHoveredHierarchyRow(hoveredElement);
            if (hoveredRow?.userData is not string hoveredKey)
            {
                ClearPendingDropIndicator();
                return;
            }

            HierarchyNode hoveredItem = _host.FindHierarchyNode(hoveredKey);
            if (hoveredItem == null ||
                string.Equals(hoveredItem.Key, draggedItem.Key, StringComparison.Ordinal) ||
                IsSameOrDescendantOf(hoveredItem.Key, draggedItem.Key))
            {
                ClearPendingDropIndicator();
                return;
            }

            Vector2 rowTopLeft = _treeView.WorldToLocal(new Vector2(hoveredRow.worldBound.xMin, hoveredRow.worldBound.yMin));
            float rowHeight = Mathf.Max(hoveredRow.resolvedStyle.height, 1f);
            float pointerOffsetY = pointerPosition.y - hoveredRow.worldBound.yMin;
            bool canDropIntoHovered = CanAcceptChildren(hoveredItem) &&
                                      pointerOffsetY >= rowHeight * 0.25f &&
                                      pointerOffsetY <= rowHeight * 0.75f;

            if (canDropIntoHovered &&
                HierarchyTreeUtility.TryFindHierarchyItem(hoveredKey, _host.HierarchyItems, out TreeViewItemData<HierarchyNode> hoveredTreeItem))
            {
                int hoveredChildCount = CountChildren(hoveredTreeItem.children);
                bool insertAsFirstChild = hoveredChildCount > 0 && pointerOffsetY < rowHeight * 0.5f;

                _reorderSession.SetPendingDrop(hoveredKey, insertAsFirstChild ? 0 : hoveredChildCount);
                _dropIndicatorPresenter.Show(
                    hoveredRow.resolvedStyle.paddingLeft + HierarchyListView.Layout.ROW_DEPTH_OFFSET + 22f,
                    rowTopLeft.y + rowHeight - 1f);
                return;
            }

            if (string.IsNullOrWhiteSpace(hoveredItem.ParentKey) ||
                !HierarchyTreeUtility.TryFindHierarchyItem(hoveredItem.ParentKey, _host.HierarchyItems, out TreeViewItemData<HierarchyNode> parentItem))
            {
                ClearPendingDropIndicator();
                return;
            }

            int hoveredIndex = FindChildIndex(parentItem.children, hoveredKey);
            if (hoveredIndex < 0)
            {
                ClearPendingDropIndicator();
                return;
            }

            bool insertBefore = pointerPosition.y < hoveredRow.worldBound.center.y;
            _reorderSession.SetPendingDrop(hoveredItem.ParentKey, insertBefore ? hoveredIndex : hoveredIndex + 1);

            float indicatorTop = insertBefore
                ? rowTopLeft.y
                : rowTopLeft.y + rowHeight - 1f;
            float indicatorLeft = hoveredRow.resolvedStyle.paddingLeft + 22f;

            _dropIndicatorPresenter.Show(indicatorLeft, indicatorTop);
        }

        private VisualElement FindHoveredHierarchyRow(VisualElement hoveredElement)
        {
            for (VisualElement current = hoveredElement; current != null && current != _treeView; current = current.parent)
            {
                if (current.ClassListContains(HierarchyTreeRow.UssClassName.ITEM))
                {
                    return current;
                }
            }

            return null;
        }

        private VisualElement ResolveHoveredElement(Vector2 pointerPosition)
        {
            if (_treeView?.panel == null)
            {
                return null;
            }

            return _treeView.panel.Pick(pointerPosition) as VisualElement;
        }

        private bool IsSameOrDescendantOf(string candidateKey, string ancestorKey)
        {
            for (string currentKey = candidateKey; !string.IsNullOrWhiteSpace(currentKey);)
            {
                if (string.Equals(currentKey, ancestorKey, StringComparison.Ordinal))
                {
                    return true;
                }

                currentKey = _host?.FindHierarchyNode(currentKey)?.ParentKey;
            }

            return false;
        }

        private static bool CanAcceptChildren(HierarchyNode item)
        {
            if (item == null)
            {
                return false;
            }

            return string.Equals(item.TagName, SvgTagName.SVG, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(item.TagName, SvgTagName.GROUP, StringComparison.OrdinalIgnoreCase);
        }

        private static int FindChildIndex(IEnumerable<TreeViewItemData<HierarchyNode>> items, string targetKey)
        {
            int index = 0;
            foreach (TreeViewItemData<HierarchyNode> item in items)
            {
                if (string.Equals(item.data?.Key, targetKey, StringComparison.Ordinal))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private static int CountChildren(IEnumerable<TreeViewItemData<HierarchyNode>> items)
        {
            if (items == null)
            {
                return 0;
            }

            int count = 0;
            foreach (TreeViewItemData<HierarchyNode> _ in items)
            {
                count++;
            }

            return count;
        }

        private void ClearPendingDropIndicator()
        {
            _reorderSession.ClearPendingDrop();
            _dropIndicatorPresenter.Hide();
        }

        private void ResetDragState()
        {
            ClearPendingDropIndicator();
            _reorderSession.Reset(_treeView);
        }

        private void ApplyRangeSelection(string pressedHierarchyElementKey)
        {
            string anchorElementKey = ResolveSelectionAnchor(pressedHierarchyElementKey);
            IReadOnlyList<string> rangeKeys = HierarchyTreeUtility.BuildElementKeyRange(
                _treeView,
                _host.HierarchyItems,
                anchorElementKey,
                pressedHierarchyElementKey);
            if (rangeKeys.Count == 0)
            {
                ApplySelectionKeys(new[] { pressedHierarchyElementKey }, pressedHierarchyElementKey, pressedHierarchyElementKey);
                return;
            }

            List<string> selectionKeys = GetCurrentSelectionKeys();
            AddMissingSelectionKeys(selectionKeys, rangeKeys);
            ApplySelectionKeys(selectionKeys, pressedHierarchyElementKey, anchorElementKey);
        }

        private void ApplyToggleSelection(string pressedHierarchyElementKey)
        {
            List<string> selectionKeys = GetCurrentSelectionKeys();
            if (!selectionKeys.Remove(pressedHierarchyElementKey))
            {
                selectionKeys.Add(pressedHierarchyElementKey);
                ApplySelectionKeys(selectionKeys, pressedHierarchyElementKey, pressedHierarchyElementKey);
                return;
            }

            string fallbackPrimaryElementKey = ResolveFallbackPrimaryElementKey(selectionKeys);
            ApplySelectionKeys(selectionKeys, fallbackPrimaryElementKey, fallbackPrimaryElementKey);
        }

        private void ApplySelectionKeys(
            IReadOnlyList<string> selectionKeys,
            string primaryElementKey,
            string selectionAnchorElementKey)
        {
            SetSelectionState(primaryElementKey, selectionAnchorElementKey);
            HierarchyTreeUtility.SelectElementsByKey(_treeView, selectionKeys, _host.HierarchyItems, primaryElementKey);
        }

        private List<string> GetCurrentSelectionKeys()
        {
            var selectedKeys = new List<string>();
            foreach (int selectedIndex in _treeView.selectedIndices)
            {
                if (selectedIndex < 0)
                {
                    continue;
                }

                HierarchyNode selectedNode = _treeView.GetItemDataForIndex<HierarchyNode>(selectedIndex);
                if (selectedNode != null &&
                    !string.IsNullOrWhiteSpace(selectedNode.Key) &&
                    !selectedKeys.Contains(selectedNode.Key))
                {
                    selectedKeys.Add(selectedNode.Key);
                }
            }

            return selectedKeys;
        }

        private string ResolveSelectionAnchor(string pressedHierarchyElementKey)
        {
            return !string.IsNullOrWhiteSpace(_selectionAnchorElementKey)
                ? _selectionAnchorElementKey
                : pressedHierarchyElementKey;
        }

        private static string ResolveFallbackPrimaryElementKey(IReadOnlyList<string> selectionKeys)
        {
            return selectionKeys.Count > 0
                ? selectionKeys[selectionKeys.Count - 1]
                : string.Empty;
        }

        private static void AddMissingSelectionKeys(List<string> selectionKeys, IReadOnlyList<string> rangeKeys)
        {
            foreach (string rangeKey in rangeKeys)
            {
                if (!selectionKeys.Contains(rangeKey))
                    selectionKeys.Add(rangeKey);
            }
        }
    }
}
