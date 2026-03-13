using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Document;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal readonly struct ResizeBeginRequest
    {
        public ResizeBeginRequest(
            DocumentSession currentDocument,
            string elementKey,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Rect selectionViewportRect,
            Rect selectionSceneRect,
            Matrix2D parentWorldTransform)
        {
            CurrentDocument = currentDocument;
            ElementKey = elementKey;
            ProjectionSceneRect = projectionSceneRect;
            PreserveAspectRatioMode = preserveAspectRatioMode;
            SelectionViewportRect = selectionViewportRect;
            SelectionSceneRect = selectionSceneRect;
            ParentWorldTransform = parentWorldTransform;
        }

        public DocumentSession CurrentDocument { get; }
        public string ElementKey { get; }
        public Rect ProjectionSceneRect { get; }
        public SvgPreserveAspectRatioMode PreserveAspectRatioMode { get; }
        public Rect SelectionViewportRect { get; }
        public Rect SelectionSceneRect { get; }
        public Matrix2D ParentWorldTransform { get; }
    }

    internal readonly struct CommitDragRequest
    {
        public CommitDragRequest(
            ICanvasPointerDragHost host,
            DragMode dragMode,
            Vector2 canvasDelta)
        {
            Host = host;
            DragMode = dragMode;
            CanvasDelta = canvasDelta;
        }

        public ICanvasPointerDragHost Host { get; }
        public DragMode DragMode { get; }
        public Vector2 CanvasDelta { get; }
    }

    internal readonly struct NudgeSourceRequest
    {
        public NudgeSourceRequest(
            DocumentSession currentDocument,
            string elementKey,
            Vector2 sceneDelta,
            Matrix2D parentWorldTransform)
        {
            CurrentDocument = currentDocument;
            ElementKey = elementKey;
            SceneDelta = sceneDelta;
            ParentWorldTransform = parentWorldTransform;
        }

        public DocumentSession CurrentDocument { get; }
        public string ElementKey { get; }
        public Vector2 SceneDelta { get; }
        public Matrix2D ParentWorldTransform { get; }
    }
}
