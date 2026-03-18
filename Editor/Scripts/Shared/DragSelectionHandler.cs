using System;
using System.Collections.Generic;
using Core.UI.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal sealed class DragSelectionHandler
    {
        private readonly float _dragThreshold;
        private readonly VisualElement _target;
        private readonly ScrollView _scrollView;
        private readonly Func<float, float, int> _hitTest;
        private readonly VisualElement _selectionRect;
        private readonly HashSet<int> _selectedIndices = new();
        private readonly HashSet<int> _preDragSnapshot = new();
        private readonly PointerDragSession _dragSession = new();

        private bool _isDragThresholdMet;
        private Vector2 _dragCurrent;
        private int _pointerDownDataIndex = -1;
        private int _pointerDownClickCount;
        private EventModifiers _pointerDownModifiers;
        private int _lastClickedIndex = -1;

        public DragSelectionHandler(
            VisualElement target,
            ScrollView scrollView,
            Func<float, float, int> hitTest,
            VisualElement selectionRect,
            float dragThreshold = 4f)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _scrollView = scrollView ?? throw new ArgumentNullException(nameof(scrollView));
            _hitTest = hitTest ?? throw new ArgumentNullException(nameof(hitTest));
            _selectionRect = selectionRect ?? throw new ArgumentNullException(nameof(selectionRect));
            _dragThreshold = dragThreshold;

            _target.Callback(OnPointerDown)
                   .Callback(OnPointerMove)
                   .Callback(OnPointerUp)
                   .Callback(OnPointerCaptureOut);
        }

        public HashSet<int> SelectedIndices => _selectedIndices;

        public int LastClickedIndex
        {
            get => _lastClickedIndex;
            set => _lastClickedIndex = value;
        }

        public event Action<int> OnItemClicked = delegate { };
        public event Action<int> OnItemDoubleClicked = delegate { };
        public event Action OnSelectionChanged = delegate { };

        public Func<Rect, HashSet<int>> HitTestRect { get; set; }

        public void ClearSelection()
        {
            _selectedIndices.Clear();
            _lastClickedIndex = -1;
            OnSelectionChanged?.Invoke();
        }

        public void SelectSingle(int dataIndex)
        {
            _selectedIndices.Clear();
            if (dataIndex >= 0)
            {
                _selectedIndices.Add(dataIndex);
                _lastClickedIndex = dataIndex;
            }

            OnSelectionChanged?.Invoke();
        }

        public void Detach()
        {
            _target.RemoveCallback(OnPointerDown)
                   .RemoveCallback(OnPointerMove)
                   .RemoveCallback(OnPointerUp)
                   .RemoveCallback(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            _isDragThresholdMet = false;

            Vector2 viewportPos = _target.WorldToLocal(evt.position);
            _dragSession.Begin(_target, evt.pointerId, viewportPos);
            _dragCurrent = viewportPos;

            Vector2 contentPos = viewportPos + new Vector2(0f, _scrollView.scrollOffset.y);
            _pointerDownDataIndex = _hitTest(contentPos.x, contentPos.y);
            _pointerDownClickCount = evt.clickCount;
            _pointerDownModifiers = evt.modifiers;

            bool isCtrl = (evt.modifiers & EventModifiers.Control) != 0 ||
                          (evt.modifiers & EventModifiers.Command) != 0;

            _preDragSnapshot.Clear();
            if (isCtrl)
            {
                foreach (int idx in _selectedIndices)
                {
                    _preDragSnapshot.Add(idx);
                }
            }
            else if (_pointerDownDataIndex < 0)
            {
                _selectedIndices.Clear();
                OnSelectionChanged?.Invoke();
            }

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragSession.Matches(evt.pointerId))
            {
                return;
            }

            Vector2 viewportPos = _target.WorldToLocal(evt.position);
            _dragCurrent = viewportPos;

            Vector2 delta = _dragCurrent - _dragSession.StartPosition;
            if (!_isDragThresholdMet)
            {
                if (Mathf.Abs(delta.x) < _dragThreshold && Mathf.Abs(delta.y) < _dragThreshold)
                {
                    return;
                }

                _isDragThresholdMet = true;
            }

            float left = Mathf.Min(_dragSession.StartPosition.x, _dragCurrent.x);
            float top = Mathf.Min(_dragSession.StartPosition.y, _dragCurrent.y);
            float width = Mathf.Abs(_dragCurrent.x - _dragSession.StartPosition.x);
            float height = Mathf.Abs(_dragCurrent.y - _dragSession.StartPosition.y);

            _selectionRect.style.display = DisplayStyle.Flex;
            _selectionRect.style.left = left;
            _selectionRect.style.top = top;
            _selectionRect.style.width = width;
            _selectionRect.style.height = height;

            UpdateDragSelection();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_dragSession.Matches(evt.pointerId))
            {
                return;
            }

            _selectionRect.style.display = DisplayStyle.None;
            _dragSession.End(_target);

            if (!_isDragThresholdMet)
            {
                int dataIndex = _pointerDownDataIndex;
                if (dataIndex >= 0)
                {
                    if (_pointerDownClickCount >= 2)
                    {
                        OnItemDoubleClicked?.Invoke(dataIndex);
                        evt.StopPropagation();
                        return;
                    }

                    bool isCtrl = (_pointerDownModifiers & EventModifiers.Control) != 0 ||
                                  (_pointerDownModifiers & EventModifiers.Command) != 0;
                    bool isShift = (_pointerDownModifiers & EventModifiers.Shift) != 0;

                    if (isCtrl)
                    {
                        if (_selectedIndices.Contains(dataIndex))
                        {
                            _selectedIndices.Remove(dataIndex);
                        }
                        else
                        {
                            _selectedIndices.Add(dataIndex);
                        }

                        _lastClickedIndex = dataIndex;
                    }
                    else if (isShift && _lastClickedIndex >= 0)
                    {
                        int min = Mathf.Min(_lastClickedIndex, dataIndex);
                        int max = Mathf.Max(_lastClickedIndex, dataIndex);
                        _selectedIndices.Clear();
                        for (int index = min; index <= max; index++)
                        {
                            _selectedIndices.Add(index);
                        }
                    }
                    else
                    {
                        _selectedIndices.Clear();
                        _selectedIndices.Add(dataIndex);
                        _lastClickedIndex = dataIndex;
                    }

                    OnItemClicked?.Invoke(dataIndex);
                }
            }

            OnSelectionChanged?.Invoke();
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!_dragSession.IsActive)
            {
                return;
            }

            _selectionRect.style.display = DisplayStyle.None;
            _dragSession.Reset();
            OnSelectionChanged?.Invoke();
        }

        private void UpdateDragSelection()
        {
            Vector2 scrollOffset = _scrollView.scrollOffset;
            float left = Mathf.Min(_dragSession.StartPosition.x, _dragCurrent.x);
            float top = Mathf.Min(_dragSession.StartPosition.y, _dragCurrent.y) + scrollOffset.y;
            float right = Mathf.Max(_dragSession.StartPosition.x, _dragCurrent.x);
            float bottom = Mathf.Max(_dragSession.StartPosition.y, _dragCurrent.y) + scrollOffset.y;

            Rect dragRect = new(left, top, right - left, bottom - top);

            _selectedIndices.Clear();
            foreach (int idx in _preDragSnapshot)
            {
                _selectedIndices.Add(idx);
            }

            if (HitTestRect == null)
            {
                OnSelectionChanged?.Invoke();
                return;
            }

            HashSet<int> hits = HitTestRect(dragRect);
            foreach (int idx in hits)
            {
                _selectedIndices.Add(idx);
            }

            OnSelectionChanged?.Invoke();
        }
    }
}
