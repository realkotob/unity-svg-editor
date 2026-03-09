using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal static class TransformStringBuilder
    {
        public static string BuildTransform(
            float translateX,
            float translateY,
            float rotate,
            float scaleX,
            float scaleY,
            Func<float, string> numberFormatter = null)
        {
            numberFormatter ??= FormatNumber;

            List<string> commands = new();
            if (!Mathf.Approximately(translateX, 0f) || !Mathf.Approximately(translateY, 0f))
            {
                commands.Add($"translate({numberFormatter(translateX)} {numberFormatter(translateY)})");
            }

            if (!Mathf.Approximately(rotate, 0f))
            {
                commands.Add($"rotate({numberFormatter(rotate)})");
            }

            if (!Mathf.Approximately(scaleX, 1f) || !Mathf.Approximately(scaleY, 1f))
            {
                commands.Add($"scale({numberFormatter(scaleX)} {numberFormatter(scaleY)})");
            }

            return string.Join(" ", commands);
        }

        public static string BuildTranslate(Vector2 translation, Func<float, string> numberFormatter = null)
        {
            numberFormatter ??= FormatNumber;
            return $"translate({numberFormatter(translation.x)} {numberFormatter(translation.y)})";
        }

        public static string BuildScaleAround(Vector2 scale, Vector2 pivot, Func<float, string> numberFormatter = null)
        {
            numberFormatter ??= FormatNumber;
            return $"translate({numberFormatter(pivot.x)} {numberFormatter(pivot.y)}) scale({numberFormatter(scale.x)} {numberFormatter(scale.y)}) translate({numberFormatter(-pivot.x)} {numberFormatter(-pivot.y)})";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
