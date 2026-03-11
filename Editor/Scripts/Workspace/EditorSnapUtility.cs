using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal static class EditorSnapUtility
    {
        public const float PositionIncrement = 8f;
        public const float SizeIncrement = 8f;
        public const float RotationIncrement = 15f;

        public static float SnapPosition(float value)
        {
            return Snap(value, PositionIncrement);
        }

        public static Vector2 SnapPosition(Vector2 value)
        {
            return new Vector2(SnapPosition(value.x), SnapPosition(value.y));
        }

        public static float SnapSize(float value)
        {
            return Mathf.Max(0f, Snap(value, SizeIncrement));
        }

        public static float SnapAngle(float value)
        {
            return NormalizeAngle(Snap(value, RotationIncrement));
        }

        public static Rect SnapRect(Rect rect, bool snapPosition = true, bool snapSize = true)
        {
            return new Rect(
                snapPosition ? SnapPosition(rect.x) : rect.x,
                snapPosition ? SnapPosition(rect.y) : rect.y,
                snapSize ? SnapSize(rect.width) : Mathf.Max(0f, rect.width),
                snapSize ? SnapSize(rect.height) : Mathf.Max(0f, rect.height));
        }

        private static float Snap(float value, float increment)
        {
            if (increment <= Mathf.Epsilon)
            {
                return value;
            }

            return Mathf.Round(value / increment) * increment;
        }

        private static float NormalizeAngle(float value)
        {
            value = Mathf.Repeat(value + 180f, 360f) - 180f;
            return Mathf.Approximately(value, -180f) ? 180f : value;
        }
    }
}
