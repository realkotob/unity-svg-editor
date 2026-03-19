using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class CanvasTextOverlayPresenter
    {
        private readonly List<Label> _textOverlays = new();
        private VisualElement _overlay;

        public void Bind(VisualElement overlay)
        {
            _overlay = overlay;
        }

        public void Clear()
        {
            for (int index = 0; index < _textOverlays.Count; index++)
            {
                _textOverlays[index]?.RemoveFromHierarchy();
            }

            _textOverlays.Clear();
        }

        public void SetTextOverlays(PreviewSnapshot previewSnapshot, SceneProjector sceneProjector)
        {
            Clear();
            if (_overlay == null || previewSnapshot?.TextOverlays == null || sceneProjector == null)
            {
                return;
            }

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

        private static float ResolveAnchorOffset(PreviewTextOverlay textOverlay)
        {
            if (textOverlay == null)
            {
                return 0f;
            }

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
    }
}
