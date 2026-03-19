using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.Core.Preview
{
    internal sealed class PreviewSnapshot : IDisposable
    {
        public VectorImage PreviewVectorImage { get; set; }
        public Rect DocumentViewportRect { get; set; }
        public Rect ProjectionRect { get; set; }
        public Rect VisualContentBounds { get; set; }
        public SvgPreserveAspectRatioMode PreserveAspectRatioMode { get; set; } = SvgPreserveAspectRatioMode.Meet;
        public IReadOnlyList<PreviewElementGeometry> Elements { get; set; } = Array.Empty<PreviewElementGeometry>();
        public IReadOnlyList<PreviewTextOverlay> TextOverlays { get; set; } = Array.Empty<PreviewTextOverlay>();

        public bool HasDocumentViewport =>
            DocumentViewportRect.width > 0f && DocumentViewportRect.height > 0f;

        public bool HasProjectionRect =>
            ProjectionRect.width > 0f && ProjectionRect.height > 0f;

        public bool HasVisualContentBounds =>
            VisualContentBounds.width > 0f || VisualContentBounds.height > 0f;

        // Projection uses a resolved viewport rect that stays stable for live/transient
        // preview refreshes. The actual SVG document viewport, when present, is stored
        // separately in DocumentViewportRect.
        public Rect CanvasViewportRect =>
            HasProjectionRect
                ? ProjectionRect
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
