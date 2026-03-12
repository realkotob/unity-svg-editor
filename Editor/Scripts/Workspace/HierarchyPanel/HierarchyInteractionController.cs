using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Shared;
using SvgEditor.Document;

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

        public void Bind(TreeView treeView, IHierarchyHost host)
        {
            Unbind();
            _treeView = treeView;
            _host = host;
            if (_treeView == null)
                return;

            _treeView.RegisterCallback<PointerMoveEvent>(OnHierarchyPointerMove);
            _treeView.RegisterCallback<PointerUpEvent>(OnHierarchyPointerUp);
            _treeView.RegisterCallback<PointerCancelEvent>(OnHierarchyPointerCancel);
            _dropIndicatorPresenter.Bind(_treeView);
        }

        public void Unbind()
        {
            ResetDragState();

            if (_treeView != null)
            {
                _treeView.UnregisterCallback<PointerMoveEvent>(OnHierarchyPointerMove);
                _treeView.UnregisterCallback<PointerUpEvent>(OnHierarchyPointerUp);
                _treeView.UnregisterCallback<PointerCancelEvent>(OnHierarchyPointerCancel);
            }

            _dropIndicatorPresenter.Unbind(_treeView);
            _host = null;
            _treeView = null;
        }

        public void CancelPendingPress()
        {
            _reorderSession.CancelPendingPress();
        }

        public bool TryConsumeSuppressedRowClick()
        {
            return _reorderSession.TryConsumeSuppressedRowClick();
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

            StructureNode draggedItem = _host.FindStructureNode(_reorderSession.PressedHierarchyElementKey);
            if (draggedItem == null || string.IsNullOrWhiteSpace(draggedItem.ParentKey))
            {
                ResetDragState();
                return;
            }

            if (!_reorderSession.TryBeginDrag(evt.position, DragThreshold))
            {
                return;
            }

            UpdateHierarchyInsertionIndicator(evt.position, ResolveHoveredHierarchyElement(evt.position), draggedItem);
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
                    _reorderSession.PressedHierarchyElementKey,
                    _reorderSession.PendingHierarchyDropParentKey,
                    _reorderSession.PendingHierarchyDropChildIndex);
            }

            ResetDragState();
        }

        private void OnHierarchyPointerCancel(PointerCancelEvent evt)
        {
            if (_reorderSession.Matches(evt.pointerId))
                ResetDragState();
        }

        private void UpdateHierarchyInsertionIndicator(Vector2 pointerPosition, VisualElement hoveredElement, StructureNode draggedItem)
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

            StructureNode hoveredItem = _host.FindStructureNode(hoveredKey);
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
                HierarchyTreeUtility.TryFindHierarchyItem(hoveredKey, _host.HierarchyItems, out TreeViewItemData<StructureNode> hoveredTreeItem))
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
                !HierarchyTreeUtility.TryFindHierarchyItem(hoveredItem.ParentKey, _host.HierarchyItems, out TreeViewItemData<StructureNode> parentItem))
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

        private VisualElement ResolveHoveredHierarchyElement(Vector2 pointerPosition)
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

                currentKey = _host?.FindStructureNode(currentKey)?.ParentKey;
            }

            return false;
        }

        private static bool CanAcceptChildren(StructureNode item)
        {
            if (item == null)
            {
                return false;
            }

            return string.Equals(item.TagName, SvgTagName.SVG, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(item.TagName, SvgTagName.GROUP, StringComparison.OrdinalIgnoreCase);
        }

        private static int FindChildIndex(IEnumerable<TreeViewItemData<StructureNode>> items, string targetKey)
        {
            int index = 0;
            foreach (TreeViewItemData<StructureNode> item in items)
            {
                if (string.Equals(item.data?.Key, targetKey, StringComparison.Ordinal))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private static int CountChildren(IEnumerable<TreeViewItemData<StructureNode>> items)
        {
            if (items == null)
            {
                return 0;
            }

            int count = 0;
            foreach (TreeViewItemData<StructureNode> _ in items)
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
    }
}
