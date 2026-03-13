using UnityEngine;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal readonly struct SelectionVisualRequest
    {
        public SelectionVisualRequest(
            SelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showSelectionHandles)
        {
            Kind = kind;
            ViewportRect = viewportRect;
            SourceSize = sourceSize;
            ShowSelectionHandles = showSelectionHandles;
            ProjectionSceneRect = default;
            PreserveAspectRatioMode = SvgPreserveAspectRatioMode.Meet;
            HasProjectionOverride = false;
        }

        public SelectionKind Kind { get; }
        public Rect ViewportRect { get; }
        public Vector2 SourceSize { get; }
        public bool ShowSelectionHandles { get; }
        public Rect ProjectionSceneRect { get; }
        public SvgPreserveAspectRatioMode PreserveAspectRatioMode { get; }
        public bool HasProjectionOverride { get; }

        public SelectionVisualRequest WithProjection(Rect projectionSceneRect)
        {
            return WithProjection(projectionSceneRect, SvgPreserveAspectRatioMode.Meet);
        }

        public SelectionVisualRequest WithProjection(
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode)
        {
            return new SelectionVisualRequest(
                Kind,
                ViewportRect,
                SourceSize,
                ShowSelectionHandles,
                projectionSceneRect,
                preserveAspectRatioMode,
                hasProjectionOverride: true);
        }

        private SelectionVisualRequest(
            SelectionKind kind,
            Rect viewportRect,
            Vector2 sourceSize,
            bool showSelectionHandles,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            bool hasProjectionOverride)
        {
            Kind = kind;
            ViewportRect = viewportRect;
            SourceSize = sourceSize;
            ShowSelectionHandles = showSelectionHandles;
            ProjectionSceneRect = projectionSceneRect;
            PreserveAspectRatioMode = preserveAspectRatioMode;
            HasProjectionOverride = hasProjectionOverride;
        }
    }
}
