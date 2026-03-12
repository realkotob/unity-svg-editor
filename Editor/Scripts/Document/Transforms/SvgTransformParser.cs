using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.VectorGraphics;
using UnityEngine;

namespace SvgEditor
{
    internal static class SvgTransformParser
    {
        public static Matrix2D Parse(IReadOnlyDictionary<string, string> attributes)
        {
            if (!TryGetAttribute(attributes, "transform", out var transformText) ||
                string.IsNullOrWhiteSpace(transformText))
            {
                return Matrix2D.identity;
            }

            return TryParseMatrix(transformText, out var matrix)
                ? matrix
                : Matrix2D.identity;
        }

        private static bool TryParseMatrix(string transformText, out Matrix2D matrix)
        {
            matrix = Matrix2D.identity;
            var index = 0;
            while (index < transformText.Length)
            {
                while (index < transformText.Length && char.IsWhiteSpace(transformText[index]))
                {
                    index++;
                }

                if (index >= transformText.Length)
                {
                    break;
                }

                var nameStart = index;
                while (index < transformText.Length && char.IsLetter(transformText[index]))
                {
                    index++;
                }

                if (nameStart == index)
                {
                    return false;
                }

                var command = transformText.Substring(nameStart, index - nameStart).ToLowerInvariant();
                while (index < transformText.Length && char.IsWhiteSpace(transformText[index]))
                {
                    index++;
                }

                if (index >= transformText.Length || transformText[index] != '(')
                {
                    return false;
                }

                index++;
                var closeIndex = transformText.IndexOf(')', index);
                if (closeIndex < 0)
                {
                    return false;
                }

                if (!TryParseArguments(transformText.Substring(index, closeIndex - index), out var args))
                {
                    return false;
                }

                index = closeIndex + 1;
                if (!TryBuildCommandMatrix(command, args, out var commandMatrix))
                {
                    return false;
                }

                matrix = matrix * commandMatrix;
            }

            return true;
        }

        private static bool TryParseArguments(string argsText, out List<float> args)
        {
            args = new List<float>();
            var tokens = argsText.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < tokens.Length; index++)
            {
                if (!float.TryParse(tokens[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    return false;
                }

                args.Add(value);
            }

            return args.Count > 0;
        }

        private static bool TryBuildCommandMatrix(string command, IReadOnlyList<float> args, out Matrix2D matrix)
        {
            matrix = Matrix2D.identity;
            switch (command)
            {
                case "translate":
                    if (args.Count is < 1 or > 2)
                    {
                        return false;
                    }

                    matrix = Matrix2D.Translate(new Vector2(args[0], args.Count > 1 ? args[1] : 0f));
                    return true;
                case "scale":
                    if (args.Count is < 1 or > 2)
                    {
                        return false;
                    }

                    matrix = Matrix2D.Scale(new Vector2(args[0], args.Count > 1 ? args[1] : args[0]));
                    return true;
                case "rotate":
                    if (args.Count != 1 && args.Count != 3)
                    {
                        return false;
                    }

                    matrix = BuildRotationMatrix(args[0], args.Count == 3 ? new Vector2(args[1], args[2]) : Vector2.zero, args.Count == 3);
                    return true;
                case "matrix":
                    if (args.Count != 6)
                    {
                        return false;
                    }

                    matrix = new Matrix2D(
                        new Vector2(args[0], args[1]),
                        new Vector2(args[2], args[3]),
                        new Vector2(args[4], args[5]));
                    return true;
                default:
                    return false;
            }
        }

        private static Matrix2D BuildRotationMatrix(float degrees, Vector2 pivot, bool aroundPivot)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            var rotation = new Matrix2D(
                new Vector2(cos, sin),
                new Vector2(-sin, cos),
                Vector2.zero);

            return aroundPivot
                ? Matrix2D.Translate(pivot) * rotation * Matrix2D.Translate(-pivot)
                : rotation;
        }

        private static bool TryGetAttribute(IReadOnlyDictionary<string, string> attributes, string name, out string value)
        {
            value = string.Empty;
            return attributes != null &&
                   attributes.TryGetValue(name, out value) &&
                   !string.IsNullOrWhiteSpace(value);
        }
    }
}
