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
        }

        private const float HANDLE_HALF_SIZE = 4f;

        private readonly Dictionary<CanvasHandle, VisualElement> _handles = new();
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
            _frameLabel.pickingMode = PickingMode.Ignore;
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

            _selectionBox = new VisualElement();
            _selectionBox.AddClass(UssClassName.SELECTION_BOX);
            _selectionBox.pickingMode = PickingMode.Ignore;
            _overlay.Add(_selectionBox);

            _sizeBadge = new Label();
            _sizeBadge.AddClass(UssClassName.SELECTION_SIZE_BADGE);
            _sizeBadge.pickingMode = PickingMode.Ignore;
            _overlay.Add(_sizeBadge);

            CreateHandle(CanvasHandle.TopLeft);
            CreateHandle(CanvasHandle.Top);
            CreateHandle(CanvasHandle.TopRight);
            CreateHandle(CanvasHandle.Right);
            CreateHandle(CanvasHandle.BottomRight);
            CreateHandle(CanvasHandle.Bottom);
            CreateHandle(CanvasHandle.BottomLeft);
            CreateHandle(CanvasHandle.Left);

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
            _sizeBadge?.RemoveFromHierarchy();
            _selectionBox?.RemoveFromHierarchy();
            _hoverBox?.RemoveFromHierarchy();
            _verticalGuide?.RemoveFromHierarchy();
            _horizontalGuide?.RemoveFromHierarchy();
            _frameLabel?.RemoveFromHierarchy();
            ClearTextOverlays();
            _overlay?.RemoveFromHierarchy();

            _sizeBadge = null;
            _selectionBox = null;
            _hoverBox = null;
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

            PositionHandle(CanvasHandle.TopLeft, selection.Rect.xMin, selection.Rect.yMin);
            PositionHandle(CanvasHandle.Top, selection.Rect.center.x, selection.Rect.yMin);
            PositionHandle(CanvasHandle.TopRight, selection.Rect.xMax, selection.Rect.yMin);
            PositionHandle(CanvasHandle.Right, selection.Rect.xMax, selection.Rect.center.y);
            PositionHandle(CanvasHandle.BottomRight, selection.Rect.xMax, selection.Rect.yMax);
            PositionHandle(CanvasHandle.Bottom, selection.Rect.center.x, selection.Rect.yMax);
            PositionHandle(CanvasHandle.BottomLeft, selection.Rect.xMin, selection.Rect.yMax);
            PositionHandle(CanvasHandle.Left, selection.Rect.xMin, selection.Rect.center.y);
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

            return false;
        }

        private void CreateHandle(CanvasHandle handle)
        {
            var element = new VisualElement();
            element.AddClass(UssClassName.SELECTION_HANDLE);
            element.pickingMode = PickingMode.Ignore;
            _overlay.Add(element);
            _handles[handle] = element;
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
    }
}
