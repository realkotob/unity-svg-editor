using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class PolylineOverlayElement : VisualElement
    {
        private IReadOnlyList<CanvasLineSegment> _segments = Array.Empty<CanvasLineSegment>();
        private Color _strokeColor = Color.white;
        private float _lineWidth = 1.25f;

        public PolylineOverlayElement()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.right = 0f;
            style.bottom = 0f;
            style.display = DisplayStyle.None;
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetSegments(IReadOnlyList<CanvasLineSegment> segments, Color strokeColor, float lineWidth = 1.25f)
        {
            _segments = segments ?? Array.Empty<CanvasLineSegment>();
            _strokeColor = strokeColor;
            _lineWidth = Mathf.Max(1f, lineWidth);
            style.display = _segments.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            MarkDirtyRepaint();
        }

        public void ClearSegments()
        {
            _segments = Array.Empty<CanvasLineSegment>();
            style.display = DisplayStyle.None;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_segments == null || _segments.Count == 0)
                return;

            Painter2D painter = context.painter2D;
            painter.strokeColor = _strokeColor;
            painter.lineWidth = _lineWidth;
            painter.BeginPath();

            for (int index = 0; index < _segments.Count; index++)
            {
                CanvasLineSegment segment = _segments[index];
                if ((segment.End - segment.Start).sqrMagnitude <= Mathf.Epsilon)
                    continue;

                painter.MoveTo(segment.Start);
                painter.LineTo(segment.End);
            }

            painter.Stroke();
        }
    }
}
