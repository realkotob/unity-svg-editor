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
        public string ElementKey { get; private set; } = string.Empty;
        public Matrix2D StartParentWorldTransform { get; private set; } = Matrix2D.identity;
        public Vector2 StartRotationPivotViewport { get; private set; }
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
            StartProjectionSceneRect = projectionSceneRect;
            StartPreserveAspectRatioMode = preserveAspectRatioMode;
            StartSelectionViewportRect = selectionViewportRect;
            CurrentSelectionViewportRect = selectionViewportRect;
            StartElementSceneRect = elementSceneRect;
            ResizeCenterAnchor = false;
            StartRotationPivotViewport = Vector2.zero;
            StartRotateVector = Vector2.zero;
            CurrentRotationAngle = 0f;
        }

        public void BeginRotation(Vector2 pivotViewport, Vector2 startRotateVector)
        {
            StartRotationPivotViewport = pivotViewport;
            StartRotateVector = startRotateVector;
            CurrentRotationAngle = 0f;
        }

        public void Reset()
        {
            ElementKey = string.Empty;
            StartSelectionViewportRect = default;
            CurrentSelectionViewportRect = default;
            StartElementSceneRect = default;
            StartProjectionSceneRect = default;
            StartPreserveAspectRatioMode = SvgPreserveAspectRatioMode.Meet;
            ResizeCenterAnchor = false;
            StartParentWorldTransform = Matrix2D.identity;
            StartRotationPivotViewport = Vector2.zero;
            StartRotateVector = Vector2.zero;
            CurrentRotationAngle = 0f;
        }
    }
}
