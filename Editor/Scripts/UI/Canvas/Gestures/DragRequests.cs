using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Preview;
using SvgEditor.UI.Workspace.Transforms;
using Core.UI.Extensions;

namespace SvgEditor.UI.Canvas
{
    internal readonly struct DragBeginRequest
    {
        public DragBeginRequest(
            DocumentSession currentDocument,
            PreviewSnapshot previewSnapshot,
            string elementKey,
            Vector2 localPosition,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform,
            IReadOnlyList<ElementMoveTarget> moveTargets = null)
        {
            CurrentDocument = currentDocument;
            PreviewSnapshot = previewSnapshot;
            ElementKey = elementKey;
            LocalPosition = localPosition;
            ElementSceneRect = elementSceneRect;
            ParentWorldTransform = parentWorldTransform;
            MoveTargets = moveTargets;
        }

        public DocumentSession CurrentDocument { get; }
        public PreviewSnapshot PreviewSnapshot { get; }
        public string ElementKey { get; }
        public Vector2 LocalPosition { get; }
        public Rect ElementSceneRect { get; }
        public Matrix2D ParentWorldTransform { get; }
        public IReadOnlyList<ElementMoveTarget> MoveTargets { get; }
    }

    internal readonly struct ElementMoveTarget
    {
        public ElementMoveTarget(string elementKey, Matrix2D parentWorldTransform)
        {
            ElementKey = elementKey;
            ParentWorldTransform = parentWorldTransform;
        }

        public string ElementKey { get; }
        public Matrix2D ParentWorldTransform { get; }
    }

    internal readonly struct RotateBeginRequest
    {
        public RotateBeginRequest(
            DragBeginRequest dragBeginRequest,
            Vector2 rotationPivotWorld,
            Vector2 rotationPivotParentSpace)
        {
            DragBeginRequest = dragBeginRequest;
            RotationPivotWorld = rotationPivotWorld;
            RotationPivotParentSpace = rotationPivotParentSpace;
        }

        public DragBeginRequest DragBeginRequest { get; }
        public Vector2 RotationPivotWorld { get; }
        public Vector2 RotationPivotParentSpace { get; }
    }

    internal readonly struct ResizeBeginRequest
    {
        public ResizeBeginRequest(
            DocumentSession currentDocument,
            string elementKey,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Rect selectionViewportRect,
            Rect selectionSceneRect,
            Matrix2D parentWorldTransform,
            IReadOnlyList<ElementMoveTarget> moveTargets = null)
        {
            CurrentDocument = currentDocument;
            ElementKey = elementKey;
            ProjectionSceneRect = projectionSceneRect;
            PreserveAspectRatioMode = preserveAspectRatioMode;
            SelectionViewportRect = selectionViewportRect;
            SelectionSceneRect = selectionSceneRect;
            ParentWorldTransform = parentWorldTransform;
            MoveTargets = moveTargets;
        }

        public DocumentSession CurrentDocument { get; }
        public string ElementKey { get; }
        public Rect ProjectionSceneRect { get; }
        public SvgPreserveAspectRatioMode PreserveAspectRatioMode { get; }
        public Rect SelectionViewportRect { get; }
        public Rect SelectionSceneRect { get; }
        public Matrix2D ParentWorldTransform { get; }
        public IReadOnlyList<ElementMoveTarget> MoveTargets { get; }
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

    internal readonly struct ElementDeltaRequest
    {
        public ElementDeltaRequest(
            Vector2 localPosition,
            Vector2 viewportDelta,
            bool uniformScale,
            bool centerAnchor,
            bool axisLock,
            bool snapEnabled)
        {
            LocalPosition = localPosition;
            ViewportDelta = viewportDelta;
            UniformScale = uniformScale;
            CenterAnchor = centerAnchor;
            AxisLock = axisLock;
            SnapEnabled = snapEnabled;
        }

        public Vector2 LocalPosition { get; }
        public Vector2 ViewportDelta { get; }
        public bool UniformScale { get; }
        public bool CenterAnchor { get; }
        public bool AxisLock { get; }
        public bool SnapEnabled { get; }
    }

    internal readonly struct CanvasNudgeRequest
    {
        public CanvasNudgeRequest(
            ICanvasPointerDragHost host,
            SceneProjector sceneProjector,
            PointerDragController pointerDragController,
            Vector2 sceneDelta)
        {
            Host = host;
            SceneProjector = sceneProjector;
            PointerDragController = pointerDragController;
            SceneDelta = sceneDelta;
        }

        public ICanvasPointerDragHost Host { get; }
        public SceneProjector SceneProjector { get; }
        public PointerDragController PointerDragController { get; }
        public Vector2 SceneDelta { get; }
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
