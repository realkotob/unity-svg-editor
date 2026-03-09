using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewSnapshot : IDisposable
    {
        public VectorImage PreviewVectorImage { get; set; }
        public Rect DocumentViewportRect { get; set; }
        public Rect VisualContentBounds { get; set; }
        public IReadOnlyList<PreviewElementGeometry> Elements { get; set; } = Array.Empty<PreviewElementGeometry>();

        public bool HasDocumentViewport =>
            DocumentViewportRect.width > 0f && DocumentViewportRect.height > 0f;

        public bool HasVisualContentBounds =>
            VisualContentBounds.width > 0f || VisualContentBounds.height > 0f;

        // Projection uses the SVG document viewport when available and falls back to
        // world-space visual content bounds for documents without an explicit viewport.
        public Rect CanvasViewportRect =>
            HasDocumentViewport
                ? DocumentViewportRect
                : VisualContentBounds;

        public void Dispose()
        {
            if (PreviewVectorImage != null)
            {
                UnityEngine.Object.DestroyImmediate(PreviewVectorImage);
                PreviewVectorImage = null;
            }
        }
    }
}
