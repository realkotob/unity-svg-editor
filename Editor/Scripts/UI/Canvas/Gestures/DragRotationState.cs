using UnityEngine;

namespace SvgEditor.UI.Canvas
{
    internal sealed class DragRotationState
    {
        public Vector2 StartPivotViewport { get; private set; }
        public Vector2 StartPivotWorld { get; private set; }
        public Vector2 StartVector { get; private set; }
        public float CurrentAngle { get; set; }

        public void Begin(Vector2 pivotViewport, Vector2 pivotWorld, Vector2 startVector)
        {
            StartPivotViewport = pivotViewport;
            StartPivotWorld = pivotWorld;
            StartVector = startVector;
            CurrentAngle = 0f;
        }

        public void Reset()
        {
            StartPivotViewport = Vector2.zero;
            StartPivotWorld = Vector2.zero;
            StartVector = Vector2.zero;
            CurrentAngle = 0f;
        }
    }
}
