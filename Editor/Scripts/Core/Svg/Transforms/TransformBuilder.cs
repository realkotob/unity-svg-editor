using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SvgEditor.Core.Svg.Transforms
{
    internal static class TransformBuilder
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

        public static string BuildRotateAround(float angle, Vector2 pivot, Func<float, string> numberFormatter = null)
        {
            numberFormatter ??= FormatNumber;
            return $"translate({numberFormatter(pivot.x)} {numberFormatter(pivot.y)}) rotate({numberFormatter(angle)}) translate({numberFormatter(-pivot.x)} {numberFormatter(-pivot.y)})";
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

            var matrix = AffineTransform2D.Identity;
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

                AffineTransform2D commandTransform;
                switch (command)
                {
                    case "translate":
                        if (args.Count is < 1 or > 2)
                        {
                            return false;
                        }

                        commandTransform = AffineTransform2D.Translate(args[0], args.Count > 1 ? args[1] : 0f);
                        break;
                    case "rotate":
                        if (args.Count != 1)
                        {
                            return false;
                        }

                        commandTransform = AffineTransform2D.Rotate(args[0]);
                        break;
                    case "scale":
                        if (args.Count is < 1 or > 2)
                        {
                            return false;
                        }

                        commandTransform = AffineTransform2D.Scale(args[0], args.Count > 1 ? args[1] : args[0]);
                        break;
                    default:
                        return false;
                }

                matrix = matrix * commandTransform;
            }

            return TryDecompose(matrix, out translateX, out translateY, out rotate, out scaleX, out scaleY);
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

        private static bool TryDecompose(
            AffineTransform2D matrix,
            out float translateX,
            out float translateY,
            out float rotate,
            out float scaleX,
            out float scaleY)
        {
            translateX = matrix.M02;
            translateY = matrix.M12;
            rotate = 0f;
            scaleX = 1f;
            scaleY = 1f;

            var firstColumn = new Vector2(matrix.M00, matrix.M10);
            if (firstColumn.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            scaleX = firstColumn.magnitude;
            var rotationAxis = firstColumn / scaleX;
            var shear = Vector2.Dot(new Vector2(matrix.M01, matrix.M11), rotationAxis);
            if (Mathf.Abs(shear) > 0.0001f)
            {
                return false;
            }

            var secondAxis = new Vector2(-rotationAxis.y, rotationAxis.x);
            scaleY = Vector2.Dot(new Vector2(matrix.M01, matrix.M11), secondAxis);
            rotate = Mathf.Atan2(rotationAxis.y, rotationAxis.x) * Mathf.Rad2Deg;
            return true;
        }

        private readonly struct AffineTransform2D
        {
            public static readonly AffineTransform2D Identity = new(1f, 0f, 0f, 0f, 1f, 0f);

            public AffineTransform2D(float m00, float m01, float m02, float m10, float m11, float m12)
            {
                M00 = m00;
                M01 = m01;
                M02 = m02;
                M10 = m10;
                M11 = m11;
                M12 = m12;
            }

            public float M00 { get; }
            public float M01 { get; }
            public float M02 { get; }
            public float M10 { get; }
            public float M11 { get; }
            public float M12 { get; }

            public static AffineTransform2D Translate(float x, float y) => new(1f, 0f, x, 0f, 1f, y);

            public static AffineTransform2D Rotate(float degrees)
            {
                var radians = degrees * Mathf.Deg2Rad;
                var cos = Mathf.Cos(radians);
                var sin = Mathf.Sin(radians);
                return new(cos, -sin, 0f, sin, cos, 0f);
            }

            public static AffineTransform2D Scale(float x, float y) => new(x, 0f, 0f, 0f, y, 0f);

            public static AffineTransform2D operator *(AffineTransform2D lhs, AffineTransform2D rhs)
            {
                return new AffineTransform2D(
                    lhs.M00 * rhs.M00 + lhs.M01 * rhs.M10,
                    lhs.M00 * rhs.M01 + lhs.M01 * rhs.M11,
                    lhs.M00 * rhs.M02 + lhs.M01 * rhs.M12 + lhs.M02,
                    lhs.M10 * rhs.M00 + lhs.M11 * rhs.M10,
                    lhs.M10 * rhs.M01 + lhs.M11 * rhs.M11,
                    lhs.M10 * rhs.M02 + lhs.M11 * rhs.M12 + lhs.M12);
            }
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
