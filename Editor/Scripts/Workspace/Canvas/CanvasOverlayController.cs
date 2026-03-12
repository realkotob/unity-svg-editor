using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Foundation;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasOverlayController
    {
        private static class UssClassName
        {
            private const string Prefix = "svg-editor__";

            public const string OVERLAY = Prefix + "canvas-overlay";
            public const string FRAME_LABEL = Prefix + "canvas-frame-label";
            public const string ALIGNMENT_GUIDE = Prefix + "alignment-guide";
            public const string ALIGNMENT_GUIDE_VERTICAL = ALIGNMENT_GUIDE + "--vertical";
            public const string ALIGNMENT_GUIDE_HORIZONTAL = ALIGNMENT_GUIDE + "--horizontal";
            public const string HOVER_BOX = Prefix + "hover-box";
            public const string SELECTION_BOX = Prefix + "selection-box";
            public const string SELECTION_BOX_FRAME = SELECTION_BOX + "--frame";
            public const string SELECTION_SIZE_BADGE = Prefix + "selection-size-badge";
            public const string SELECTION_HANDLE = Prefix + "selection-handle";
            public const string SELECTION_HANDLE_TOP_LEFT = SELECTION_HANDLE + "--top-left";
            public const string SELECTION_HANDLE_TOP = SELECTION_HANDLE + "--top";
            public const string SELECTION_HANDLE_TOP_RIGHT = SELECTION_HANDLE + "--top-right";
            public const string SELECTION_HANDLE_RIGHT = SELECTION_HANDLE + "--right";
            public const string SELECTION_HANDLE_BOTTOM_RIGHT = SELECTION_HANDLE + "--bottom-right";
            public const string SELECTION_HANDLE_BOTTOM = SELECTION_HANDLE + "--bottom";
            public const string SELECTION_HANDLE_BOTTOM_LEFT = SELECTION_HANDLE + "--bottom-left";
            public const string SELECTION_HANDLE_LEFT = SELECTION_HANDLE + "--left";
            public const string EDGE_ZONE = Prefix + "edge-zone";
            public const string EDGE_ZONE_VERTICAL = EDGE_ZONE + "--vertical";
            public const string EDGE_ZONE_HORIZONTAL = EDGE_ZONE + "--horizontal";
            public const string ROTATION_ZONE = Prefix + "rotation-zone";
            public const string DEFINITION_BOUNDS = Prefix + "definition-overlay-bounds";
            public const string DEFINITION_BOUNDS_MASK = DEFINITION_BOUNDS + "--mask";
            public const string DEFINITION_BOUNDS_CLIP = DEFINITION_BOUNDS + "--clip";
        }

        private const float HANDLE_HALF_SIZE = 4f;
        private const float EDGE_ZONE_THICKNESS = 12f;
        private const float ROTATE_ZONE_HALF_SIZE = 18f;

        private readonly Dictionary<CanvasHandle, VisualElement> _handles = new();
        private readonly Dictionary<CanvasHandle, VisualElement> _edgeZones = new();
        private readonly List<VisualElement> _rotationZones = new();
        private VisualElement _overlay;
        private VisualElement _frameElement;
        private Label _frameLabel;
        private VisualElement _hoverBox;
        private VisualElement _selectionBox;
        private Label _sizeBadge;
        private VisualElement _verticalGuide;
        private VisualElement _horizontalGuide;
        private readonly List<Label> _textOverlays = new();
        private CanvasSelectionVisual _currentSelection;
        private VisualElement _maskBoundsBox;
        private VisualElement _clipBoundsBox;
        private CanvasPolylineOverlayElement _maskOutline;
        private CanvasPolylineOverlayElement _clipOutline;
        private CanvasDefinitionOverlayVisual _maskOverlayVisual;
        private CanvasDefinitionOverlayVisual _clipOverlayVisual;

        public VisualElement Overlay => _overlay;

        public void Attach(VisualElement host, VisualElement frameElement)
        {
            Detach();
            if (host == null || frameElement == null)
            {
                return;
            }

            _frameElement = frameElement;
            _frameElement.pickingMode = PickingMode.Ignore;
            _frameElement.style.position = Position.Absolute;
            _frameElement.style.display = DisplayStyle.None;

            _overlay = new VisualElement
            {
                focusable = true,
                tabIndex = 0
            };
            _overlay.AddClass(UssClassName.OVERLAY);
            _overlay.pickingMode = PickingMode.Position;
            host.Add(_overlay);

            _frameLabel = new Label();
            _frameLabel.AddClass(UssClassName.FRAME_LABEL);
            _frameLabel.pickingMode = PickingMode.Position;
            _overlay.Add(_frameLabel);

            _verticalGuide = new VisualElement();
            _verticalGuide.AddClass(UssClassName.ALIGNMENT_GUIDE);
            _verticalGuide.AddClass(UssClassName.ALIGNMENT_GUIDE_VERTICAL);
            _verticalGuide.pickingMode = PickingMode.Ignore;
            _overlay.Add(_verticalGuide);

            _horizontalGuide = new VisualElement();
            _horizontalGuide.AddClass(UssClassName.ALIGNMENT_GUIDE);
            _horizontalGuide.AddClass(UssClassName.ALIGNMENT_GUIDE_HORIZONTAL);
            _horizontalGuide.pickingMode = PickingMode.Ignore;
            _overlay.Add(_horizontalGuide);

            _hoverBox = new VisualElement();
            _hoverBox.AddClass(UssClassName.HOVER_BOX);
            _hoverBox.pickingMode = PickingMode.Ignore;
            _overlay.Add(_hoverBox);

            _maskBoundsBox = CreateDefinitionBoundsBox(UssClassName.DEFINITION_BOUNDS_MASK);
            _clipBoundsBox = CreateDefinitionBoundsBox(UssClassName.DEFINITION_BOUNDS_CLIP);
            _maskOutline = CreateDefinitionOutline();
            _clipOutline = CreateDefinitionOutline();

            _selectionBox = new VisualElement();
            _selectionBox.AddClass(UssClassName.SELECTION_BOX);
            _selectionBox.pickingMode = PickingMode.Ignore;
            _overlay.Add(_selectionBox);

            _sizeBadge = new Label();
            _sizeBadge.AddClass(UssClassName.SELECTION_SIZE_BADGE);
            _sizeBadge.pickingMode = PickingMode.Ignore;
            _overlay.Add(_sizeBadge);

            CreateHandle(CanvasHandle.TopLeft);
            CreateHandle(CanvasHandle.TopRight);
            CreateHandle(CanvasHandle.BottomRight);
            CreateHandle(CanvasHandle.BottomLeft);
            CreateEdgeZone(CanvasHandle.Top);
            CreateEdgeZone(CanvasHandle.Right);
            CreateEdgeZone(CanvasHandle.Bottom);
            CreateEdgeZone(CanvasHandle.Left);
            for (int index = 0; index < 4; index++)
            {
                CreateRotationZone();
            }
            ClearFrame();
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
            foreach (VisualElement rotationZone in _rotationZones)
            {
                rotationZone?.RemoveFromHierarchy();
            }

            _rotationZones.Clear();
            _sizeBadge?.RemoveFromHierarchy();
            _selectionBox?.RemoveFromHierarchy();
            _hoverBox?.RemoveFromHierarchy();
            _maskOutline?.RemoveFromHierarchy();
            _clipOutline?.RemoveFromHierarchy();
            _maskBoundsBox?.RemoveFromHierarchy();
            _clipBoundsBox?.RemoveFromHierarchy();
            _verticalGuide?.RemoveFromHierarchy();
            _horizontalGuide?.RemoveFromHierarchy();
            _frameLabel?.RemoveFromHierarchy();
            ClearTextOverlays();
            _overlay?.RemoveFromHierarchy();

            _sizeBadge = null;
            _selectionBox = null;
            _hoverBox = null;
            _maskOutline = null;
            _clipOutline = null;
            _maskBoundsBox = null;
            _clipBoundsBox = null;
            _verticalGuide = null;
            _horizontalGuide = null;
            _frameLabel = null;
            _overlay = null;
            if (_frameElement != null)
            {
                _frameElement.style.display = DisplayStyle.None;
            }

            _frameElement = null;
            _currentSelection = null;
        }

        public void SetDefinitionOverlays(IReadOnlyList<CanvasDefinitionOverlayVisual> overlays)
        {
            ClearDefinitionOverlays();
            if (overlays == null || overlays.Count == 0)
                return;

            for (int index = 0; index < overlays.Count; index++)
            {
                CanvasDefinitionOverlayVisual overlay = overlays[index];
                if (overlay == null)
                    continue;

                switch (overlay.Kind)
                {
                    case CanvasDefinitionOverlayKind.Mask:
                        _maskOverlayVisual = overlay;
                        ApplyDefinitionOverlay(_maskBoundsBox, _maskOutline, overlay, new Color(0.86f, 0.72f, 0.45f, 0.95f));
                        break;
                    case CanvasDefinitionOverlayKind.ClipPath:
                        _clipOverlayVisual = overlay;
                        ApplyDefinitionOverlay(_clipBoundsBox, _clipOutline, overlay, new Color(0.55f, 0.83f, 0.62f, 0.95f));
                        break;
                }
            }
        }

        public void ClearDefinitionOverlays()
        {
            _maskOverlayVisual = null;
            _clipOverlayVisual = null;
            ClearDefinitionOverlay(_maskBoundsBox, _maskOutline);
            ClearDefinitionOverlay(_clipBoundsBox, _clipOutline);
        }

        public bool TryHitTestDefinitionOverlay(Vector2 localPoint, out CanvasDefinitionOverlayVisual overlay)
        {
            overlay = null;
            if (_clipOverlayVisual != null && _clipOverlayVisual.ViewportBounds.Contains(localPoint))
            {
                overlay = _clipOverlayVisual;
                return true;
            }

            if (_maskOverlayVisual != null && _maskOverlayVisual.ViewportBounds.Contains(localPoint))
            {
                overlay = _maskOverlayVisual;
                return true;
            }

            return false;
        }

        public void ClearFrame()
        {
            if (_frameElement != null)
            {
                _frameElement.style.display = DisplayStyle.None;
            }

            if (_frameLabel != null)
            {
                _frameLabel.text = string.Empty;
                _frameLabel.style.display = DisplayStyle.None;
            }
        }

        public void SetFrame(Rect viewportRect, string label)
        {
            if (_frameElement == null)
            {
                return;
            }

            _frameElement.style.display = DisplayStyle.Flex;
            _frameElement.style.left = viewportRect.xMin;
            _frameElement.style.top = viewportRect.yMin;
            _frameElement.style.width = viewportRect.width;
            _frameElement.style.height = viewportRect.height;

            if (_frameLabel != null)
            {
                _frameLabel.style.display = DisplayStyle.Flex;
                _frameLabel.text = string.IsNullOrWhiteSpace(label) ? "Frame 1" : label;
                _frameLabel.style.left = viewportRect.xMin;
                _frameLabel.style.top = viewportRect.yMin - 24f;
            }
        }

        public void SetTextOverlays(PreviewSnapshot previewSnapshot, CanvasSceneProjector sceneProjector)
        {
            ClearTextOverlays();
            if (_overlay == null || previewSnapshot?.TextOverlays == null || sceneProjector == null)
                return;

            for (int index = 0; index < previewSnapshot.TextOverlays.Count; index++)
            {
                PreviewTextOverlay textOverlay = previewSnapshot.TextOverlays[index];
                if (textOverlay == null ||
                    string.IsNullOrWhiteSpace(textOverlay.Text) ||
                    !sceneProjector.TryScenePointToViewportPoint(previewSnapshot, textOverlay.ScenePosition, out Vector2 viewportPoint))
                {
                    continue;
                }

                Label label = new(textOverlay.Text);
                label.pickingMode = PickingMode.Ignore;
                label.style.position = Position.Absolute;
                label.style.left = viewportPoint.x + ResolveAnchorOffset(textOverlay);
                label.style.top = viewportPoint.y - (textOverlay.FontSize * 1.05f);
                label.style.fontSize = textOverlay.FontSize;
                label.style.color = textOverlay.Color;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                _overlay.Add(label);
                _textOverlays.Add(label);
            }
        }

        private void ClearTextOverlays()
        {
            for (int index = 0; index < _textOverlays.Count; index++)
            {
                _textOverlays[index]?.RemoveFromHierarchy();
            }

            _textOverlays.Clear();
        }

        public bool IsFrameLabelTarget(object target)
        {
            return target != null && ReferenceEquals(target, _frameLabel);
        }

        private static float ResolveAnchorOffset(PreviewTextOverlay textOverlay)
        {
            if (textOverlay == null)
                return 0f;

            float estimatedWidth = (textOverlay.SceneBounds.width > 0f)
                ? textOverlay.SceneBounds.width
                : textOverlay.Text.Length * Mathf.Max(1f, textOverlay.FontSize) * 0.68f;
            return textOverlay.TextAnchor switch
            {
                "middle" => -estimatedWidth * 0.5f,
                "end" => -estimatedWidth,
                _ => 0f
            };
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

            foreach (VisualElement rotationZone in _rotationZones)
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
                return;

            _hoverBox.style.display = DisplayStyle.Flex;
            _hoverBox.style.left = viewportRect.xMin;
            _hoverBox.style.top = viewportRect.yMin;
            _hoverBox.style.width = viewportRect.width;
            _hoverBox.style.height = viewportRect.height;
        }

        public void SetSelection(CanvasSelectionVisual selection)
        {
            _currentSelection = selection;

            if (_selectionBox == null || selection == null || selection.Kind == CanvasSelectionKind.None)
            {
                ClearSelection();
                return;
            }

            _selectionBox.style.display = DisplayStyle.Flex;
            _selectionBox.EnableClass(UssClassName.SELECTION_BOX_FRAME, selection.Kind == CanvasSelectionKind.Frame);
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
                pair.Value.style.display = selection.ShowHandles ? DisplayStyle.Flex : DisplayStyle.None;
            }

            foreach (var pair in _edgeZones)
            {
                pair.Value.style.display = selection.ShowHandles ? DisplayStyle.Flex : DisplayStyle.None;
            }

            PositionHandle(CanvasHandle.TopLeft, selection.Rect.xMin, selection.Rect.yMin);
            PositionHandle(CanvasHandle.TopRight, selection.Rect.xMax, selection.Rect.yMin);
            PositionHandle(CanvasHandle.BottomRight, selection.Rect.xMax, selection.Rect.yMax);
            PositionHandle(CanvasHandle.BottomLeft, selection.Rect.xMin, selection.Rect.yMax);
            PositionEdgeZones(selection.Rect, selection.ShowHandles);
            PositionRotationZones(selection.Rect, selection.ShowHandles);
        }

        public bool TryHitTestHandle(Vector2 localPoint, out CanvasHandle handle)
        {
            handle = CanvasHandle.None;
            if (_currentSelection == null || !_currentSelection.ShowHandles)
            {
                return false;
            }

            foreach (var pair in _handles)
            {
                var handleWidth = pair.Value.resolvedStyle.width > 0f
                    ? pair.Value.resolvedStyle.width
                    : HANDLE_HALF_SIZE * 2f;
                var handleHeight = pair.Value.resolvedStyle.height > 0f
                    ? pair.Value.resolvedStyle.height
                    : HANDLE_HALF_SIZE * 2f;
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
                Rect zoneRect = new(
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

        public void UpdateInteractionCursor(Vector2 localPoint)
        {
        }

        public void ResetInteractionCursor()
        {
        }

        private VisualElement CreateDefinitionBoundsBox(string modifierClass)
        {
            VisualElement element = new();
            element.AddClass(UssClassName.DEFINITION_BOUNDS);
            element.AddClass(modifierClass);
            element.pickingMode = PickingMode.Ignore;
            element.style.position = Position.Absolute;
            element.style.display = DisplayStyle.None;
            element.SetDashedBorder(true);
            _overlay.Add(element);
            return element;
        }

        private CanvasPolylineOverlayElement CreateDefinitionOutline()
        {
            CanvasPolylineOverlayElement element = new();
            _overlay.Add(element);
            return element;
        }

        private static void ApplyDefinitionOverlay(
            VisualElement boundsBox,
            CanvasPolylineOverlayElement outlineElement,
            CanvasDefinitionOverlayVisual overlay,
            Color outlineColor)
        {
            if (boundsBox != null)
            {
                boundsBox.style.display = DisplayStyle.Flex;
                boundsBox.style.left = overlay.ViewportBounds.xMin;
                boundsBox.style.top = overlay.ViewportBounds.yMin;
                boundsBox.style.width = overlay.ViewportBounds.width;
                boundsBox.style.height = overlay.ViewportBounds.height;
            }

            outlineElement?.SetSegments(overlay.OutlineSegments, outlineColor);
        }

        private static void ClearDefinitionOverlay(VisualElement boundsBox, CanvasPolylineOverlayElement outlineElement)
        {
            if (boundsBox != null)
                boundsBox.style.display = DisplayStyle.None;

            outlineElement?.ClearSegments();
        }

        private void CreateHandle(CanvasHandle handle)
        {
            var element = new VisualElement();
            element.AddClass(UssClassName.SELECTION_HANDLE);
            element.AddClass(ResolveHandleCursorClass(handle));
            element.pickingMode = PickingMode.Position;
            _overlay.Add(element);
            _handles[handle] = element;
        }

        private void CreateEdgeZone(CanvasHandle handle)
        {
            VisualElement element = new();
            element.AddClass(UssClassName.EDGE_ZONE);
            element.AddClass(handle is CanvasHandle.Top or CanvasHandle.Bottom
                ? UssClassName.EDGE_ZONE_VERTICAL
                : UssClassName.EDGE_ZONE_HORIZONTAL);
            element.pickingMode = PickingMode.Position;
            element.style.display = DisplayStyle.None;
            _overlay.Add(element);
            _edgeZones[handle] = element;
        }

        private void CreateRotationZone()
        {
            VisualElement element = new();
            element.AddClass(UssClassName.ROTATION_ZONE);
            element.pickingMode = PickingMode.Position;
            element.style.display = DisplayStyle.None;
            _overlay.Add(element);
            _rotationZones.Add(element);
        }

        private void PositionHandle(CanvasHandle handle, float x, float y)
        {
            if (!_handles.TryGetValue(handle, out var element))
            {
                return;
            }

            element.style.left = x - HANDLE_HALF_SIZE;
            element.style.top = y - HANDLE_HALF_SIZE;
        }

        private void PositionEdgeZones(Rect selectionRect, bool visible)
        {
            PositionEdgeZone(CanvasHandle.Top, selectionRect.xMin + HANDLE_HALF_SIZE, selectionRect.yMin - (EDGE_ZONE_THICKNESS * 0.5f), Mathf.Max(0f, selectionRect.width - (HANDLE_HALF_SIZE * 2f)), EDGE_ZONE_THICKNESS, visible);
            PositionEdgeZone(CanvasHandle.Right, selectionRect.xMax - (EDGE_ZONE_THICKNESS * 0.5f), selectionRect.yMin + HANDLE_HALF_SIZE, EDGE_ZONE_THICKNESS, Mathf.Max(0f, selectionRect.height - (HANDLE_HALF_SIZE * 2f)), visible);
            PositionEdgeZone(CanvasHandle.Bottom, selectionRect.xMin + HANDLE_HALF_SIZE, selectionRect.yMax - (EDGE_ZONE_THICKNESS * 0.5f), Mathf.Max(0f, selectionRect.width - (HANDLE_HALF_SIZE * 2f)), EDGE_ZONE_THICKNESS, visible);
            PositionEdgeZone(CanvasHandle.Left, selectionRect.xMin - (EDGE_ZONE_THICKNESS * 0.5f), selectionRect.yMin + HANDLE_HALF_SIZE, EDGE_ZONE_THICKNESS, Mathf.Max(0f, selectionRect.height - (HANDLE_HALF_SIZE * 2f)), visible);
        }

        private void PositionEdgeZone(CanvasHandle handle, float left, float top, float width, float height, bool visible)
        {
            if (!_edgeZones.TryGetValue(handle, out VisualElement element))
            {
                return;
            }

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.left = left;
            element.style.top = top;
            element.style.width = width;
            element.style.height = height;
        }

        private void PositionRotationZones(Rect selectionRect, bool showHandles)
        {
            if (_rotationZones.Count < 4)
            {
                return;
            }

            PositionRotationZone(_rotationZones[0], selectionRect.min, new Vector2(-ROTATE_ZONE_HALF_SIZE, -ROTATE_ZONE_HALF_SIZE), showHandles);
            PositionRotationZone(_rotationZones[1], new Vector2(selectionRect.xMax, selectionRect.yMin), new Vector2(0f, -ROTATE_ZONE_HALF_SIZE), showHandles);
            PositionRotationZone(_rotationZones[2], selectionRect.max, Vector2.zero, showHandles);
            PositionRotationZone(_rotationZones[3], new Vector2(selectionRect.xMin, selectionRect.yMax), new Vector2(-ROTATE_ZONE_HALF_SIZE, 0f), showHandles);
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
            element.style.width = ROTATE_ZONE_HALF_SIZE;
            element.style.height = ROTATE_ZONE_HALF_SIZE;
        }

        private static string ResolveHandleCursorClass(CanvasHandle handle)
        {
            return handle switch
            {
                CanvasHandle.TopLeft => UssClassName.SELECTION_HANDLE_TOP_LEFT,
                CanvasHandle.Top => UssClassName.SELECTION_HANDLE_TOP,
                CanvasHandle.TopRight => UssClassName.SELECTION_HANDLE_TOP_RIGHT,
                CanvasHandle.Right => UssClassName.SELECTION_HANDLE_RIGHT,
                CanvasHandle.BottomRight => UssClassName.SELECTION_HANDLE_BOTTOM_RIGHT,
                CanvasHandle.Bottom => UssClassName.SELECTION_HANDLE_BOTTOM,
                CanvasHandle.BottomLeft => UssClassName.SELECTION_HANDLE_BOTTOM_LEFT,
                CanvasHandle.Left => UssClassName.SELECTION_HANDLE_LEFT,
                _ => string.Empty
            };
        }

        private bool TryHitTestRotationZone(Vector2 localPoint, out CanvasHandle handle)
        {
            handle = CanvasHandle.None;
            if (_currentSelection == null || !_currentSelection.ShowHandles)
            {
                return false;
            }

            Rect selectionRect = _currentSelection.Rect;
            if (TryHitCornerRotationZone(localPoint, selectionRect.min, point => point.x <= selectionRect.xMin && point.y <= selectionRect.yMin) ||
                TryHitCornerRotationZone(localPoint, new Vector2(selectionRect.xMax, selectionRect.yMin), point => point.x >= selectionRect.xMax && point.y <= selectionRect.yMin) ||
                TryHitCornerRotationZone(localPoint, selectionRect.max, point => point.x >= selectionRect.xMax && point.y >= selectionRect.yMax) ||
                TryHitCornerRotationZone(localPoint, new Vector2(selectionRect.xMin, selectionRect.yMax), point => point.x <= selectionRect.xMin && point.y >= selectionRect.yMax))
            {
                handle = CanvasHandle.Rotate;
                return true;
            }

            return false;
        }

        private static bool TryHitCornerRotationZone(Vector2 localPoint, Vector2 corner, System.Func<Vector2, bool> diagonalPredicate)
        {
            Rect outerZone = new(
                corner.x - ROTATE_ZONE_HALF_SIZE,
                corner.y - ROTATE_ZONE_HALF_SIZE,
                ROTATE_ZONE_HALF_SIZE * 2f,
                ROTATE_ZONE_HALF_SIZE * 2f);
            Rect resizeZone = new(
                corner.x - HANDLE_HALF_SIZE,
                corner.y - HANDLE_HALF_SIZE,
                HANDLE_HALF_SIZE * 2f,
                HANDLE_HALF_SIZE * 2f);

            return outerZone.Contains(localPoint) &&
                   !resizeZone.Contains(localPoint) &&
                   diagonalPredicate(localPoint);
        }
    }
}
