using System;
using System.Collections.Generic;
using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class SelectionChromePresenter
    {
        private readonly Dictionary<SelectionHandle, VisualElement> _handles = new();
        private readonly Dictionary<SelectionHandle, VisualElement> _edgeZones = new();
        private readonly List<VisualElement> _rotationZones = new();

        private VisualElement _overlay;
        private VisualElement _hoverBox;
        private VisualElement _selectionBox;
        private Label _sizeBadge;
        private VisualElement _verticalGuide;
        private VisualElement _horizontalGuide;
        private CanvasSelectionVisual _currentSelection;

        public void BindBackdrop(VisualElement overlay)
        {
            _overlay = overlay;

            _verticalGuide = new VisualElement();
            _verticalGuide.AddClass(OverlayClassName.ALIGNMENT_GUIDE);
            _verticalGuide.AddClass(OverlayClassName.ALIGNMENT_GUIDE_VERTICAL);
            _verticalGuide.pickingMode = PickingMode.Ignore;
            _overlay.Add(_verticalGuide);

            _horizontalGuide = new VisualElement();
            _horizontalGuide.AddClass(OverlayClassName.ALIGNMENT_GUIDE);
            _horizontalGuide.AddClass(OverlayClassName.ALIGNMENT_GUIDE_HORIZONTAL);
            _horizontalGuide.pickingMode = PickingMode.Ignore;
            _overlay.Add(_horizontalGuide);

            _hoverBox = new VisualElement();
            _hoverBox.AddClass(OverlayClassName.HOVER_BOX);
            _hoverBox.pickingMode = PickingMode.Ignore;
            _overlay.Add(_hoverBox);
        }

        public void BindSelectionChrome(VisualElement overlay)
        {
            _overlay = overlay;

            _selectionBox = new VisualElement();
            _selectionBox.AddClass(OverlayClassName.SELECTION_BOX);
            _selectionBox.pickingMode = PickingMode.Ignore;
            _overlay.Add(_selectionBox);

            _sizeBadge = new Label();
            _sizeBadge.AddClass(OverlayClassName.SELECTION_SIZE_BADGE);
            _sizeBadge.pickingMode = PickingMode.Ignore;
            _overlay.Add(_sizeBadge);

            CreateSelectionHandle(SelectionHandle.TopLeft);
            CreateSelectionHandle(SelectionHandle.TopRight);
            CreateSelectionHandle(SelectionHandle.BottomRight);
            CreateSelectionHandle(SelectionHandle.BottomLeft);
            CreateEdgeZone(SelectionHandle.Top);
            CreateEdgeZone(SelectionHandle.Right);
            CreateEdgeZone(SelectionHandle.Bottom);
            CreateEdgeZone(SelectionHandle.Left);
            for (var index = 0; index < 4; index++)
            {
                CreateRotationZone();
            }

            ClearSelection();
        }

        public void Detach()
        {
            foreach (var handle in _handles.Values)
            {
                handle?.RemoveFromHierarchy();
            }

            _handles.Clear();
            foreach (var edgeZone in _edgeZones.Values)
            {
                edgeZone?.RemoveFromHierarchy();
            }

            _edgeZones.Clear();
            foreach (var rotationZone in _rotationZones)
            {
                rotationZone?.RemoveFromHierarchy();
            }

            _rotationZones.Clear();
            _sizeBadge?.RemoveFromHierarchy();
            _selectionBox?.RemoveFromHierarchy();
            _hoverBox?.RemoveFromHierarchy();
            _verticalGuide?.RemoveFromHierarchy();
            _horizontalGuide?.RemoveFromHierarchy();

            _sizeBadge = null;
            _selectionBox = null;
            _hoverBox = null;
            _verticalGuide = null;
            _horizontalGuide = null;
            _overlay = null;
            _currentSelection = null;
        }

        public void ClearSelection()
        {
            _currentSelection = null;

            if (_selectionBox != null)
            {
                _selectionBox.style.display = DisplayStyle.None;
            }

            if (_sizeBadge != null)
            {
                _sizeBadge.style.display = DisplayStyle.None;
            }

            if (_verticalGuide != null)
            {
                _verticalGuide.style.display = DisplayStyle.None;
            }

            if (_horizontalGuide != null)
            {
                _horizontalGuide.style.display = DisplayStyle.None;
            }

            foreach (var handle in _handles.Values)
            {
                handle.style.display = DisplayStyle.None;
            }

            foreach (var edgeZone in _edgeZones.Values)
            {
                edgeZone.style.display = DisplayStyle.None;
            }

            foreach (var rotationZone in _rotationZones)
            {
                rotationZone.style.display = DisplayStyle.None;
            }
        }

        public void ClearHover()
        {
            if (_hoverBox != null)
            {
                _hoverBox.style.display = DisplayStyle.None;
            }
        }

        public void SetHover(Rect viewportRect)
        {
            if (_hoverBox == null)
            {
                return;
            }

            _hoverBox.style.display = DisplayStyle.Flex;
            _hoverBox.style.left = viewportRect.xMin;
            _hoverBox.style.top = viewportRect.yMin;
            _hoverBox.style.width = viewportRect.width;
            _hoverBox.style.height = viewportRect.height;
        }

        public void SetSelection(CanvasSelectionVisual selection)
        {
            _currentSelection = selection;

            if (_selectionBox == null || selection == null || selection.Kind == SelectionKind.None)
            {
                ClearSelection();
                return;
            }

            _selectionBox.style.display = DisplayStyle.Flex;
            _selectionBox.EnableClass(OverlayClassName.SELECTION_BOX_FRAME, selection.Kind == SelectionKind.Frame);
            _selectionBox.style.left = selection.Rect.xMin;
            _selectionBox.style.top = selection.Rect.yMin;
            _selectionBox.style.width = selection.Rect.width;
            _selectionBox.style.height = selection.Rect.height;

            if (_sizeBadge != null && !string.IsNullOrWhiteSpace(selection.SizeText))
            {
                _sizeBadge.style.display = DisplayStyle.Flex;
                _sizeBadge.text = selection.SizeText;
                _sizeBadge.style.left = selection.Rect.center.x - 24f;
                _sizeBadge.style.top = selection.Rect.yMax + 8f;
            }

            if (_verticalGuide != null)
            {
                _verticalGuide.style.display = selection.ShowVerticalGuide ? DisplayStyle.Flex : DisplayStyle.None;
                if (selection.ShowVerticalGuide)
                {
                    _verticalGuide.style.left = selection.VerticalGuideX;
                }
            }

            if (_horizontalGuide != null)
            {
                _horizontalGuide.style.display = selection.ShowHorizontalGuide ? DisplayStyle.Flex : DisplayStyle.None;
                if (selection.ShowHorizontalGuide)
                {
                    _horizontalGuide.style.top = selection.HorizontalGuideY;
                }
            }

            foreach (var pair in _handles)
            {
                pair.Value.style.display = selection.ShowSelectionHandles ? DisplayStyle.Flex : DisplayStyle.None;
            }

            foreach (var pair in _edgeZones)
            {
                pair.Value.style.display = selection.ShowSelectionHandles ? DisplayStyle.Flex : DisplayStyle.None;
            }

            PositionSelectionHandle(SelectionHandle.TopLeft, selection.Rect.xMin, selection.Rect.yMin);
            PositionSelectionHandle(SelectionHandle.TopRight, selection.Rect.xMax, selection.Rect.yMin);
            PositionSelectionHandle(SelectionHandle.BottomRight, selection.Rect.xMax, selection.Rect.yMax);
            PositionSelectionHandle(SelectionHandle.BottomLeft, selection.Rect.xMin, selection.Rect.yMax);
            SelectionChromeGeometry.PositionEdgeZones(_edgeZones, selection.Rect, selection.ShowSelectionHandles);
            SelectionChromeGeometry.PositionRotationZones(_rotationZones, selection.Rect, selection.ShowSelectionHandles);
        }

        public bool TryHitTestSelectionHandle(Vector2 localPoint, out SelectionHandle handle)
        {
            handle = SelectionHandle.None;
            if (_currentSelection == null || !_currentSelection.ShowSelectionHandles)
            {
                return false;
            }

            foreach (var pair in _handles)
            {
                Rect handleRect = SelectionChromeGeometry.BuildHandleRect(pair.Value);

                if (handleRect.Contains(localPoint))
                {
                    handle = pair.Key;
                    return true;
                }
            }

            foreach (var pair in _edgeZones)
            {
                Rect zoneRect = SelectionChromeGeometry.BuildRect(pair.Value);
                if (zoneRect.Contains(localPoint))
                {
                    handle = pair.Key;
                    return true;
                }
            }

            return TryHitTestRotationZone(localPoint, out handle);
        }

        private void CreateSelectionHandle(SelectionHandle handle)
        {
            var element = new VisualElement();
            element.AddClass(OverlayClassName.SELECTION_HANDLE);
            element.AddClass(SelectionChromeGeometry.ResolveSelectionHandleCursorClass(handle));
            element.pickingMode = PickingMode.Position;
            _overlay.Add(element);
            _handles[handle] = element;
        }

        private void CreateEdgeZone(SelectionHandle handle)
        {
            var element = new VisualElement();
            element.AddClass(OverlayClassName.EDGE_ZONE);
            element.AddClass(handle is SelectionHandle.Top or SelectionHandle.Bottom
                ? OverlayClassName.EDGE_ZONE_VERTICAL
                : OverlayClassName.EDGE_ZONE_HORIZONTAL);
            element.pickingMode = PickingMode.Position;
            element.style.display = DisplayStyle.None;
            _overlay.Add(element);
            _edgeZones[handle] = element;
        }

        private void CreateRotationZone()
        {
            var element = new VisualElement();
            element.AddClass(OverlayClassName.ROTATION_ZONE);
            element.pickingMode = PickingMode.Position;
            element.style.display = DisplayStyle.None;
            _overlay.Add(element);
            _rotationZones.Add(element);
        }

        private void PositionSelectionHandle(SelectionHandle handle, float x, float y)
        {
            if (!_handles.TryGetValue(handle, out var element))
            {
                return;
            }

            SelectionChromeGeometry.PositionHandle(element, x, y);
        }

        private bool TryHitTestRotationZone(Vector2 localPoint, out SelectionHandle handle)
        {
            return SelectionChromeGeometry.TryHitRotationZone(_currentSelection, localPoint, out handle);
        }
    }
}
