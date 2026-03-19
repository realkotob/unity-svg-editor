using System.Collections.Generic;
using UnityEngine;
using Unity.VectorGraphics;

using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class DragState
    {
        public DragSelectionState Selection { get; } = new();
        public DragRotationState Rotation { get; } = new();

        public void BeginSelection(
            string elementKey,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Rect selectionViewportRect,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform)
        {
            Selection.Begin(
                elementKey,
                projectionSceneRect,
                preserveAspectRatioMode,
                selectionViewportRect,
                elementSceneRect,
                parentWorldTransform);
            Rotation.Reset();
        }

        public void BeginRotation(Vector2 pivotViewport, Vector2 pivotWorld, Vector2 startRotateVector)
        {
            Rotation.Begin(pivotViewport, pivotWorld, startRotateVector);
        }

        public void SetMoveTargets(IReadOnlyList<ElementMoveTarget> moveTargets)
        {
            Selection.SetMoveTargets(moveTargets);
        }

        public void Reset()
        {
            Selection.Reset();
            Rotation.Reset();
        }
    }
}
