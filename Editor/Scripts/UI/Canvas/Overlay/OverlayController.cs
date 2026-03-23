using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class OverlayController
    {
        private VisualElement _overlay;
        private VisualElement _frameElement;
        private Label _frameLabel;
        private readonly CanvasTextOverlayPresenter _textOverlayPresenter = new();
        private readonly DefinitionOverlayPresenter _definitionOverlayPresenter = new();
        private readonly PathOverlayPresenter _pathOverlayPresenter = new();
        private readonly SelectionChromePresenter _selectionChromePresenter = new();
        private VisualElement _maskBoundsBox;
        private VisualElement _clipBoundsBox;
        private PolylineOverlayElement _maskOutline;
        private PolylineOverlayElement _clipOutline;
        private PolylineOverlayElement _pathOutline;
        private PolylineOverlayElement _pathHandleOutline;
        private PathEditSession _pathEditSession;

        public VisualElement Overlay => _overlay;
        public bool HasPathEditSession => _pathEditSession != null;
        public PathEditSession CurrentPathEditSession => _pathEditSession;

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
            _overlay.AddClass(OverlayClassName.OVERLAY);
            _overlay.pickingMode = PickingMode.Position;
            host.Add(_overlay);

            _frameLabel = new Label();
            _frameLabel.AddClass(OverlayClassName.FRAME_LABEL);
            _frameLabel.pickingMode = PickingMode.Position;
            _overlay.Add(_frameLabel);

            _selectionChromePresenter.BindBackdrop(_overlay);

            _maskBoundsBox = CreateDefinitionBoundsBox(OverlayClassName.DEFINITION_BOUNDS_MASK);
            _clipBoundsBox = CreateDefinitionBoundsBox(OverlayClassName.DEFINITION_BOUNDS_CLIP);
            _maskOutline = CreateDefinitionOutline();
            _clipOutline = CreateDefinitionOutline();
            _definitionOverlayPresenter.Bind(_maskBoundsBox, _clipBoundsBox, _maskOutline, _clipOutline);
            _pathOutline = CreateDefinitionOutline();
            _pathHandleOutline = CreateDefinitionOutline();
            _pathOutline.AddToClassList(OverlayClassName.PATH_LINE);
            _pathHandleOutline.AddToClassList(OverlayClassName.BEZIER_HANDLE_LINE);
            _pathOverlayPresenter.Bind(_overlay, _pathOutline, _pathHandleOutline);

            _selectionChromePresenter.BindSelectionChrome(_overlay);
            _textOverlayPresenter.Bind(_overlay);

            ClearFrame();
            ClearSelection();
            if (_pathEditSession != null)
            {
                _pathOverlayPresenter.SetSession(_pathEditSession);
            }
        }

        public void Detach()
        {
            _selectionChromePresenter.Detach();
            _pathOverlayPresenter.Clear();
            _pathOutline?.RemoveFromHierarchy();
            _pathHandleOutline?.RemoveFromHierarchy();
            _maskOutline?.RemoveFromHierarchy();
            _clipOutline?.RemoveFromHierarchy();
            _maskBoundsBox?.RemoveFromHierarchy();
            _clipBoundsBox?.RemoveFromHierarchy();
            _frameLabel?.RemoveFromHierarchy();
            _textOverlayPresenter.Clear();
            _overlay?.RemoveFromHierarchy();

            _pathOutline = null;
            _pathHandleOutline = null;
            _maskOutline = null;
            _clipOutline = null;
            _maskBoundsBox = null;
            _clipBoundsBox = null;
            _frameLabel = null;
            _overlay = null;
            if (_frameElement != null)
            {
                _frameElement.style.display = DisplayStyle.None;
            }

            _frameElement = null;
        }

        public void SetDefinitionOverlays(IReadOnlyList<CanvasDefinitionOverlayVisual> overlays)
        {
            _definitionOverlayPresenter.SetOverlays(overlays);
        }

        public void ClearDefinitionOverlays()
        {
            _definitionOverlayPresenter.Clear();
        }

        public void SetPathEditSession(PathEditSession session)
        {
            _pathEditSession = session;
            _pathOverlayPresenter.SetSession(session);
        }

        public void ClearPathEditSession()
        {
            _pathEditSession = null;
            _pathOverlayPresenter.Clear();
        }

        public bool TryHitTestDefinitionOverlay(Vector2 localPoint, out CanvasDefinitionOverlayVisual overlay)
        {
            return _definitionOverlayPresenter.TryHitTest(localPoint, out overlay);
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

        public void SetTextOverlays(PreviewSnapshot previewSnapshot, SceneProjector sceneProjector)
        {
            _textOverlayPresenter.SetTextOverlays(previewSnapshot, sceneProjector);
        }

        public bool IsFrameLabelTarget(object target)
        {
            return target != null && ReferenceEquals(target, _frameLabel);
        }

        public void ClearSelection()
        {
            _selectionChromePresenter.ClearSelection();
        }

        public void ClearHover()
        {
            _selectionChromePresenter.ClearHover();
        }

        public void ClearMarquee()
        {
            _selectionChromePresenter.ClearMarquee();
        }

        public void SetMarquee(Rect viewportRect)
        {
            _selectionChromePresenter.SetMarquee(viewportRect);
        }

        public void SetHover(Rect viewportRect)
        {
            _selectionChromePresenter.SetHover(viewportRect);
        }

        public void SetSelection(CanvasSelectionVisual selection)
        {
            _selectionChromePresenter.SetSelection(selection);
        }

        public bool TryHitTestSelectionHandle(Vector2 localPoint, out SelectionHandle handle)
        {
            return _selectionChromePresenter.TryHitTestSelectionHandle(localPoint, out handle);
        }

        public void UpdateInteractionCursor(bool showPointer)
        {
            _overlay?.EnableInClassList(OverlayClassName.OVERLAY_INTERACTIVE_HIT, showPointer);
        }

        public void ResetInteractionCursor()
        {
            _overlay?.EnableInClassList(OverlayClassName.OVERLAY_INTERACTIVE_HIT, false);
        }

        private VisualElement CreateDefinitionBoundsBox(string modifierClass)
        {
            var element = new VisualElement();
            element.AddClass(OverlayClassName.DEFINITION_BOUNDS);
            element.AddClass(modifierClass);
            element.pickingMode = PickingMode.Ignore;
            element.style.position = Position.Absolute;
            element.style.display = DisplayStyle.None;
            element.SetDashedBorder(true);
            _overlay.Add(element);
            return element;
        }

        private PolylineOverlayElement CreateDefinitionOutline()
        {
            var element = new PolylineOverlayElement();
            _overlay.Add(element);
            return element;
        }
    }
}
