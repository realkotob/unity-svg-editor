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
        private const float SelectionHandleHalfSize = 4f;
        private const float EdgeZoneThickness = 12f;
        private const float RotateZoneHalfSize = 18f;

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
            PositionEdgeZones(selection.Rect, selection.ShowSelectionHandles);
            PositionRotationZones(selection.Rect, selection.ShowSelectionHandles);
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
                var handleWidth = pair.Value.resolvedStyle.width > 0f
                    ? pair.Value.resolvedStyle.width
                    : SelectionHandleHalfSize * 2f;
                var handleHeight = pair.Value.resolvedStyle.height > 0f
                    ? pair.Value.resolvedStyle.height
                    : SelectionHandleHalfSize * 2f;
                var handleRect = new Rect(
                    pair.Value.resolvedStyle.left,
                    pair.Value.resolvedStyle.top,
                    handleWidth,
                    handleHeight);

                if (handleRect.Contains(localPoint))
                {
                    handle = pair.Key;
                    return true;
                }
            }

            foreach (var pair in _edgeZones)
            {
                var zoneRect = new Rect(
                    pair.Value.resolvedStyle.left,
                    pair.Value.resolvedStyle.top,
                    pair.Value.resolvedStyle.width,
                    pair.Value.resolvedStyle.height);
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
            element.AddClass(ResolveSelectionHandleCursorClass(handle));
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

            element.style.left = x - SelectionHandleHalfSize;
            element.style.top = y - SelectionHandleHalfSize;
        }

        private void PositionEdgeZones(Rect selectionRect, bool visible)
        {
            PositionEdgeZone(SelectionHandle.Top, selectionRect.xMin + SelectionHandleHalfSize, selectionRect.yMin - (EdgeZoneThickness * 0.5f), Mathf.Max(0f, selectionRect.width - (SelectionHandleHalfSize * 2f)), EdgeZoneThickness, visible);
            PositionEdgeZone(SelectionHandle.Right, selectionRect.xMax - (EdgeZoneThickness * 0.5f), selectionRect.yMin + SelectionHandleHalfSize, EdgeZoneThickness, Mathf.Max(0f, selectionRect.height - (SelectionHandleHalfSize * 2f)), visible);
            PositionEdgeZone(SelectionHandle.Bottom, selectionRect.xMin + SelectionHandleHalfSize, selectionRect.yMax - (EdgeZoneThickness * 0.5f), Mathf.Max(0f, selectionRect.width - (SelectionHandleHalfSize * 2f)), EdgeZoneThickness, visible);
            PositionEdgeZone(SelectionHandle.Left, selectionRect.xMin - (EdgeZoneThickness * 0.5f), selectionRect.yMin + SelectionHandleHalfSize, EdgeZoneThickness, Mathf.Max(0f, selectionRect.height - (SelectionHandleHalfSize * 2f)), visible);
        }

        private void PositionEdgeZone(SelectionHandle handle, float left, float top, float width, float height, bool visible)
        {
            if (!_edgeZones.TryGetValue(handle, out var element))
            {
                return;
            }

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.left = left;
            element.style.top = top;
            element.style.width = width;
            element.style.height = height;
        }

        private void PositionRotationZones(Rect selectionRect, bool showSelectionHandles)
        {
            if (_rotationZones.Count < 4)
            {
                return;
            }

            PositionRotationZone(_rotationZones[0], selectionRect.min, new Vector2(-RotateZoneHalfSize, -RotateZoneHalfSize), showSelectionHandles);
            PositionRotationZone(_rotationZones[1], new Vector2(selectionRect.xMax, selectionRect.yMin), new Vector2(0f, -RotateZoneHalfSize), showSelectionHandles);
            PositionRotationZone(_rotationZones[2], selectionRect.max, Vector2.zero, showSelectionHandles);
            PositionRotationZone(_rotationZones[3], new Vector2(selectionRect.xMin, selectionRect.yMax), new Vector2(-RotateZoneHalfSize, 0f), showSelectionHandles);
        }

        private static void PositionRotationZone(VisualElement element, Vector2 corner, Vector2 offset, bool visible)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.left = corner.x + offset.x;
            element.style.top = corner.y + offset.y;
            element.style.width = RotateZoneHalfSize;
            element.style.height = RotateZoneHalfSize;
        }

        private static string ResolveSelectionHandleCursorClass(SelectionHandle handle)
        {
            return handle switch
            {
                SelectionHandle.TopLeft => OverlayClassName.SELECTION_HANDLE_TOP_LEFT,
                SelectionHandle.Top => OverlayClassName.SELECTION_HANDLE_TOP,
                SelectionHandle.TopRight => OverlayClassName.SELECTION_HANDLE_TOP_RIGHT,
                SelectionHandle.Right => OverlayClassName.SELECTION_HANDLE_RIGHT,
                SelectionHandle.BottomRight => OverlayClassName.SELECTION_HANDLE_BOTTOM_RIGHT,
                SelectionHandle.Bottom => OverlayClassName.SELECTION_HANDLE_BOTTOM,
                SelectionHandle.BottomLeft => OverlayClassName.SELECTION_HANDLE_BOTTOM_LEFT,
                SelectionHandle.Left => OverlayClassName.SELECTION_HANDLE_LEFT,
                _ => string.Empty
            };
        }

        private bool TryHitTestRotationZone(Vector2 localPoint, out SelectionHandle handle)
        {
            handle = SelectionHandle.None;
            if (_currentSelection == null || !_currentSelection.ShowSelectionHandles)
            {
                return false;
            }

            var selectionRect = _currentSelection.Rect;
            if (TryHitCornerRotationZone(localPoint, selectionRect.min, point => point.x <= selectionRect.xMin && point.y <= selectionRect.yMin) ||
                TryHitCornerRotationZone(localPoint, new Vector2(selectionRect.xMax, selectionRect.yMin), point => point.x >= selectionRect.xMax && point.y <= selectionRect.yMin) ||
                TryHitCornerRotationZone(localPoint, selectionRect.max, point => point.x >= selectionRect.xMax && point.y >= selectionRect.yMax) ||
                TryHitCornerRotationZone(localPoint, new Vector2(selectionRect.xMin, selectionRect.yMax), point => point.x <= selectionRect.xMin && point.y >= selectionRect.yMax))
            {
                handle = SelectionHandle.Rotate;
                return true;
            }

            return false;
        }

        private static bool TryHitCornerRotationZone(Vector2 localPoint, Vector2 corner, Func<Vector2, bool> diagonalPredicate)
        {
            var outerZone = new Rect(
                corner.x - RotateZoneHalfSize,
                corner.y - RotateZoneHalfSize,
                RotateZoneHalfSize * 2f,
                RotateZoneHalfSize * 2f);
            var resizeZone = new Rect(
                corner.x - SelectionHandleHalfSize,
                corner.y - SelectionHandleHalfSize,
                SelectionHandleHalfSize * 2f,
                SelectionHandleHalfSize * 2f);

            return outerZone.Contains(localPoint) &&
                   !resizeZone.Contains(localPoint) &&
                   diagonalPredicate(localPoint);
        }
    }
}
