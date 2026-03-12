using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasDefinitionOverlayPresenter
    {
        private CanvasDefinitionOverlayVisual _maskOverlayVisual;
        private CanvasDefinitionOverlayVisual _clipOverlayVisual;
        private VisualElement _maskBoundsBox;
        private VisualElement _clipBoundsBox;
        private CanvasPolylineOverlayElement _maskOutline;
        private CanvasPolylineOverlayElement _clipOutline;

        public void Bind(
            VisualElement maskBoundsBox,
            VisualElement clipBoundsBox,
            CanvasPolylineOverlayElement maskOutline,
            CanvasPolylineOverlayElement clipOutline)
        {
            _maskBoundsBox = maskBoundsBox;
            _clipBoundsBox = clipBoundsBox;
            _maskOutline = maskOutline;
            _clipOutline = clipOutline;
        }

        public void Clear()
        {
            _maskOverlayVisual = null;
            _clipOverlayVisual = null;
            ClearOverlay(_maskBoundsBox, _maskOutline);
            ClearOverlay(_clipBoundsBox, _clipOutline);
        }

        public void SetOverlays(IReadOnlyList<CanvasDefinitionOverlayVisual> overlays)
        {
            Clear();
            if (overlays == null || overlays.Count == 0)
            {
                return;
            }

            for (int index = 0; index < overlays.Count; index++)
            {
                CanvasDefinitionOverlayVisual overlay = overlays[index];
                if (overlay == null)
                {
                    continue;
                }

                switch (overlay.Kind)
                {
                    case CanvasDefinitionOverlayKind.Mask:
                        _maskOverlayVisual = overlay;
                        ApplyOverlay(_maskBoundsBox, _maskOutline, overlay, new Color(0.86f, 0.72f, 0.45f, 0.95f));
                        break;
                    case CanvasDefinitionOverlayKind.ClipPath:
                        _clipOverlayVisual = overlay;
                        ApplyOverlay(_clipBoundsBox, _clipOutline, overlay, new Color(0.55f, 0.83f, 0.62f, 0.95f));
                        break;
                }
            }
        }

        public bool TryHitTest(Vector2 localPoint, out CanvasDefinitionOverlayVisual overlay)
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

        private static void ApplyOverlay(
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

        private static void ClearOverlay(VisualElement boundsBox, CanvasPolylineOverlayElement outlineElement)
        {
            if (boundsBox != null)
            {
                boundsBox.style.display = DisplayStyle.None;
            }

            outlineElement?.ClearSegments();
        }
    }
}
