using System;
using System.Collections.Generic;
using Core.UI.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Core.Shared
{
    internal sealed class DragSelectionHandler
    {
        private readonly VisualElement _target;
        private readonly ScrollView _scrollView;
        private readonly Func<float, float, int> _hitTest;
        private readonly HashSet<int> _selectedIndices = new();

        private int _pointerDownDataIndex = -1;
        private int _pointerDownClickCount;
        private EventModifiers _pointerDownModifiers;
        private int _lastClickedIndex = -1;
        private int _activePointerId = -1;

        public DragSelectionHandler(
            VisualElement target,
            ScrollView scrollView,
            Func<float, float, int> hitTest)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _scrollView = scrollView ?? throw new ArgumentNullException(nameof(scrollView));
            _hitTest = hitTest ?? throw new ArgumentNullException(nameof(hitTest));

            _target.Callback(OnPointerDown)
                   .Callback(OnPointerUp);
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
                   .RemoveCallback(OnPointerUp);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            _activePointerId = evt.pointerId;

            Vector2 viewportPos = _target.WorldToLocal(evt.position);
            Vector2 contentPos = viewportPos + new Vector2(0f, _scrollView.scrollOffset.y);
            _pointerDownDataIndex = _hitTest(contentPos.x, contentPos.y);
            _pointerDownClickCount = evt.clickCount;
            _pointerDownModifiers = evt.modifiers;

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _activePointerId)
            {
                return;
            }

            _activePointerId = -1;

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

            OnSelectionChanged?.Invoke();
            evt.StopPropagation();
        }
    }
}
