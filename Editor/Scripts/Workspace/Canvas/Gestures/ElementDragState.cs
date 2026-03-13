using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class ElementDragState
    {
        public Rect StartSelectionViewportRect { get; private set; }
        public Rect CurrentSelectionViewportRect { get; set; }
        public Rect StartElementSceneRect { get; private set; }
        public Rect StartProjectionSceneRect { get; private set; }
        public SvgPreserveAspectRatioMode StartPreserveAspectRatioMode { get; private set; } = SvgPreserveAspectRatioMode.Meet;
        public bool ResizeCenterAnchor { get; set; }
        public SelectionHandle ActiveResizeHandle { get; set; } = SelectionHandle.None;
        public string ElementKey { get; private set; } = string.Empty;
        public IReadOnlyList<ElementMoveTarget> MoveTargets { get; private set; } = new[] { new ElementMoveTarget(string.Empty, Matrix2D.identity) };
        public Matrix2D StartParentWorldTransform { get; private set; } = Matrix2D.identity;
        public Vector2 StartRotationPivotViewport { get; private set; }
        public Vector2 StartRotationPivotWorld { get; private set; }
        public Vector2 StartRotateVector { get; private set; }
        public float CurrentRotationAngle { get; set; }

        public void BeginSelection(
            string elementKey,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Rect selectionViewportRect,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform)
        {
            ElementKey = elementKey ?? string.Empty;
            StartParentWorldTransform = parentWorldTransform;
            MoveTargets = new[] { new ElementMoveTarget(ElementKey, parentWorldTransform) };
            StartProjectionSceneRect = projectionSceneRect;
            StartPreserveAspectRatioMode = preserveAspectRatioMode;
            StartSelectionViewportRect = selectionViewportRect;
            CurrentSelectionViewportRect = selectionViewportRect;
            StartElementSceneRect = elementSceneRect;
            ResizeCenterAnchor = false;
            ActiveResizeHandle = SelectionHandle.None;
            StartRotationPivotViewport = Vector2.zero;
            StartRotationPivotWorld = Vector2.zero;
            StartRotateVector = Vector2.zero;
            CurrentRotationAngle = 0f;
        }

        public void BeginRotation(Vector2 pivotViewport, Vector2 pivotWorld, Vector2 startRotateVector)
        {
            StartRotationPivotViewport = pivotViewport;
            StartRotationPivotWorld = pivotWorld;
            StartRotateVector = startRotateVector;
            CurrentRotationAngle = 0f;
        }

        public void SetMoveTargets(IReadOnlyList<ElementMoveTarget> moveTargets)
        {
            MoveTargets = moveTargets != null && moveTargets.Count > 0
                ? moveTargets
                : new[] { new ElementMoveTarget(ElementKey, StartParentWorldTransform) };
        }

        public void Reset()
        {
            ElementKey = string.Empty;
            MoveTargets = new[] { new ElementMoveTarget(string.Empty, Matrix2D.identity) };
            StartSelectionViewportRect = default;
            CurrentSelectionViewportRect = default;
            StartElementSceneRect = default;
            StartProjectionSceneRect = default;
            StartPreserveAspectRatioMode = SvgPreserveAspectRatioMode.Meet;
            ResizeCenterAnchor = false;
            ActiveResizeHandle = SelectionHandle.None;
            StartParentWorldTransform = Matrix2D.identity;
            StartRotationPivotViewport = Vector2.zero;
            StartRotationPivotWorld = Vector2.zero;
            StartRotateVector = Vector2.zero;
            CurrentRotationAngle = 0f;
        }
    }
}
