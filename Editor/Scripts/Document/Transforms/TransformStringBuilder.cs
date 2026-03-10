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

        public static bool TryParseSimpleTransform(
            string transform,
            out float translateX,
            out float translateY,
            out float rotate,
            out float scaleX,
            out float scaleY)
        {
            translateX = 0f;
            translateY = 0f;
            rotate = 0f;
            scaleX = 1f;
            scaleY = 1f;

            if (string.IsNullOrWhiteSpace(transform))
            {
                return true;
            }

            var stage = 0;
            var index = 0;
            while (index < transform.Length)
            {
                SkipWhitespace(transform, ref index);
                if (index >= transform.Length)
                {
                    return true;
                }

                var nameStart = index;
                while (index < transform.Length && char.IsLetter(transform[index]))
                {
                    index++;
                }

                if (nameStart == index)
                {
                    return false;
                }

                var command = transform.Substring(nameStart, index - nameStart).ToLowerInvariant();
                SkipWhitespace(transform, ref index);
                if (index >= transform.Length || transform[index] != '(')
                {
                    return false;
                }

                index++;
                var closeIndex = transform.IndexOf(')', index);
                if (closeIndex < 0)
                {
                    return false;
                }

                var argsText = transform.Substring(index, closeIndex - index);
                index = closeIndex + 1;
                if (!TryParseArguments(argsText, out var args))
                {
                    return false;
                }

                switch (command)
                {
                    case "translate":
                        if (stage > 0 || args.Count is < 1 or > 2)
                        {
                            return false;
                        }

                        translateX = args[0];
                        translateY = args.Count > 1 ? args[1] : 0f;
                        stage = 1;
                        break;
                    case "rotate":
                        if (stage > 1 || args.Count != 1)
                        {
                            return false;
                        }

                        rotate = args[0];
                        stage = 2;
                        break;
                    case "scale":
                        if (stage > 2 || args.Count is < 1 or > 2)
                        {
                            return false;
                        }

                        scaleX = args[0];
                        scaleY = args.Count > 1 ? args[1] : args[0];
                        stage = 3;
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        private static bool TryParseArguments(string argsText, out List<float> values)
        {
            values = new List<float>();
            var tokens = argsText.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    values.Clear();
                    return false;
                }

                values.Add(value);
            }

            return values.Count > 0;
        }

        private static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
