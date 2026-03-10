using System;
using System.Collections.Generic;
using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class StructureHierarchyInteractionController
    {
        private static class UssClassName
        {
            public const string INSERT_INDICATOR = "svg-editor__hierarchy-insert-indicator";
        }

        private readonly StructureEditor _structureEditor;
        private readonly SvgDocumentModelMutationService _documentModelMutationService = new();

        private TreeView _treeView;
        private IStructureHierarchyHost _host;
        private VisualElement _insertionIndicator;
        private readonly PointerDragSession _dragSession = new();
        private bool _isHierarchyDragging;
        private bool _shouldSuppressHierarchyRowClick;
        private int _pendingHierarchyDropChildIndex = -1;
        private string _pressedHierarchyElementKey;

        public StructureHierarchyInteractionController(StructureEditor structureEditor)
        {
            _structureEditor = structureEditor;
        }

        public void Bind(TreeView treeView, IStructureHierarchyHost host)
        {
            Unbind();
            _treeView = treeView;
            _host = host;
            if (_treeView == null)
                return;

            _treeView.RegisterCallback<PointerMoveEvent>(OnHierarchyPointerMove);
            _treeView.RegisterCallback<PointerUpEvent>(OnHierarchyPointerUp);
            _treeView.RegisterCallback<PointerCancelEvent>(OnHierarchyPointerCancel);

            _insertionIndicator = new VisualElement();
            _insertionIndicator.AddClass(UssClassName.INSERT_INDICATOR);
            _insertionIndicator.style.display = DisplayStyle.None;
            _insertionIndicator.pickingMode = PickingMode.Ignore;
            _treeView.hierarchy.Add(_insertionIndicator);
        }

        public void Unbind()
        {
            ResetDragState();

            if (_treeView != null)
            {
                _treeView.UnregisterCallback<PointerMoveEvent>(OnHierarchyPointerMove);
                _treeView.UnregisterCallback<PointerUpEvent>(OnHierarchyPointerUp);
                _treeView.UnregisterCallback<PointerCancelEvent>(OnHierarchyPointerCancel);
                if (_insertionIndicator != null && _insertionIndicator.parent == _treeView)
                    _treeView.hierarchy.Remove(_insertionIndicator);
            }

            _host = null;
            _treeView = null;
            _insertionIndicator = null;
        }

        public void CancelPendingPress()
        {
            _pressedHierarchyElementKey = null;
            _dragSession.Reset();
        }

        public bool TryConsumeSuppressedRowClick()
        {
            if (!_shouldSuppressHierarchyRowClick)
                return false;

            _shouldSuppressHierarchyRowClick = false;
            return true;
        }

        public void OnHierarchyRowPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || evt.currentTarget is not VisualElement row)
                return;

            _pressedHierarchyElementKey = row.userData as string;
            _dragSession.Reset();
            _isHierarchyDragging = false;
            _pendingHierarchyDropChildIndex = -1;
            _shouldSuppressHierarchyRowClick = false;

            if (_treeView == null ||
                _host == null ||
                !StructureHierarchyTreeUtility.TryFindHierarchyItemId(_pressedHierarchyElementKey, _host.HierarchyItems, out int treeItemId))
            {
                return;
            }

            _treeView.SetSelectionById(treeItemId);
            _dragSession.Begin(_treeView, evt.pointerId, evt.position);
        }

        private void OnHierarchyPointerMove(PointerMoveEvent evt)
        {
            if (_treeView == null ||
                _host == null ||
                !_dragSession.Matches(evt.pointerId) ||
                string.IsNullOrWhiteSpace(_pressedHierarchyElementKey))
            {
                return;
            }

            StructureNode draggedItem = _host.FindStructureNode(_pressedHierarchyElementKey);
            if (draggedItem == null || string.IsNullOrWhiteSpace(draggedItem.ParentKey))
            {
                ResetDragState();
                return;
            }

            if (!_isHierarchyDragging)
            {
                float delta = (((Vector2)evt.position) - _dragSession.StartPosition).magnitude;
                if (delta < 5f)
                    return;

                _isHierarchyDragging = true;
                _shouldSuppressHierarchyRowClick = true;
            }

            UpdateHierarchyInsertionIndicator((Vector2)evt.position, evt.target as VisualElement, draggedItem);
            evt.StopPropagation();
        }

        private void OnHierarchyPointerUp(PointerUpEvent evt)
        {
            if (_treeView == null || _host == null || !_dragSession.Matches(evt.pointerId))
                return;

            if (_isHierarchyDragging &&
                _host.CurrentDocument != null &&
                !string.IsNullOrWhiteSpace(_pressedHierarchyElementKey) &&
                _pendingHierarchyDropChildIndex >= 0)
            {
                if (_host.CurrentDocument.DocumentModel == null ||
                    !string.IsNullOrWhiteSpace(_host.CurrentDocument.DocumentModelLoadError) ||
                    !string.Equals(_host.CurrentDocument.DocumentModel.SourceText, _host.CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
                {
                    _host.UpdateSourceStatus("Reorder failed: document model is unavailable.");
                    ResetDragState();
                    return;
                }

                if (_documentModelMutationService.TryReorderElementWithinSameParent(
                        _host.CurrentDocument.DocumentModel,
                        _pressedHierarchyElementKey,
                        _pendingHierarchyDropChildIndex,
                        out SvgDocumentModel _,
                        out string reorderedSource,
                        out string error))
                {
                    if (!string.Equals(reorderedSource, _host.CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
                        _host.ApplyUpdatedSource(reorderedSource, $"Reordered #{_pressedHierarchyElementKey}.");
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    _host.UpdateSourceStatus($"Reorder failed: {error}");
                }
            }

            ResetDragState();
        }

        private void OnHierarchyPointerCancel(PointerCancelEvent evt)
        {
            if (_dragSession.Matches(evt.pointerId))
                ResetDragState();
        }

        private void UpdateHierarchyInsertionIndicator(Vector2 pointerPosition, VisualElement hoveredElement, StructureNode draggedItem)
        {
            if (_treeView == null || _host == null)
                return;

            if (draggedItem == null ||
                string.IsNullOrWhiteSpace(draggedItem.ParentKey) ||
                !StructureHierarchyTreeUtility.TryFindHierarchyItem(draggedItem.ParentKey, _host.HierarchyItems, out TreeViewItemData<StructureNode> parentItem))
            {
                HideInsertionIndicator();
                return;
            }

            if (!parentItem.hasChildren)
            {
                HideInsertionIndicator();
                return;
            }

            VisualElement hoveredRow = FindHoveredHierarchyRow(hoveredElement);
            if (hoveredRow?.userData is not string hoveredKey)
            {
                HideInsertionIndicator();
                return;
            }

            StructureNode hoveredItem = _host.FindStructureNode(hoveredKey);
            if (hoveredItem == null || !string.Equals(hoveredItem.ParentKey, draggedItem.ParentKey, StringComparison.Ordinal))
            {
                HideInsertionIndicator();
                return;
            }

            int hoveredIndex = FindChildIndex(parentItem.children, hoveredKey);
            if (hoveredIndex < 0)
            {
                HideInsertionIndicator();
                return;
            }

            bool insertBefore = pointerPosition.y < hoveredRow.worldBound.center.y;
            _pendingHierarchyDropChildIndex = insertBefore ? hoveredIndex : hoveredIndex + 1;

            Vector2 rowTopLeft = _treeView.WorldToLocal(new Vector2(hoveredRow.worldBound.xMin, hoveredRow.worldBound.yMin));
            float indicatorTop = insertBefore
                ? rowTopLeft.y
                : rowTopLeft.y + hoveredRow.resolvedStyle.height - 1f;
            float indicatorLeft = hoveredRow.resolvedStyle.paddingLeft + 22f;

            _insertionIndicator.style.display = DisplayStyle.Flex;
            _insertionIndicator.style.left = indicatorLeft;
            _insertionIndicator.style.right = 8f;
            _insertionIndicator.style.top = indicatorTop;
        }

        private VisualElement FindHoveredHierarchyRow(VisualElement hoveredElement)
        {
            for (VisualElement current = hoveredElement; current != null && current != _treeView; current = current.parent)
            {
                if (current.ClassListContains(AssetHierarchyTreeRow.UssClassName.ITEM))
                {
                    return current;
                }
            }

            return null;
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

        private void HideInsertionIndicator()
        {
            _pendingHierarchyDropChildIndex = -1;
            if (_insertionIndicator != null)
                _insertionIndicator.style.display = DisplayStyle.None;
        }

        private void ResetDragState()
        {
            HideInsertionIndicator();
            _dragSession.End(_treeView);

            _isHierarchyDragging = false;
            _pressedHierarchyElementKey = null;
        }
    }
}
