using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal static class PreviewSnapshotTextBuilder
    {
        public static IReadOnlyList<PreviewTextOverlay> BuildTextOverlays(SvgDocumentModel documentModel)
        {
            List<PreviewTextOverlay> overlays = new();
            if (documentModel?.Root == null)
                return overlays;

            BuildNodeOverlays(documentModel, documentModel.Root, Matrix2D.identity, overlays);
            return overlays;
        }

        public static IReadOnlyList<PreviewElementGeometry> BuildTextElements(
            IReadOnlyList<PreviewTextOverlay> textOverlays,
            int startingDrawOrder)
        {
            List<PreviewElementGeometry> elements = new();
            if (textOverlays == null)
                return elements;

            for (int index = 0; index < textOverlays.Count; index++)
            {
                PreviewTextOverlay overlay = textOverlays[index];
                if (overlay == null || string.IsNullOrWhiteSpace(overlay.Text))
                    continue;

                Rect bounds = overlay.SceneBounds.width > 0f || overlay.SceneBounds.height > 0f
                    ? overlay.SceneBounds
                    : EstimateTextBounds(overlay);
                elements.Add(new PreviewElementGeometry
                {
                    Key = overlay.Key,
                    TargetKey = overlay.TargetKey,
                    VisualBounds = bounds,
                    DrawOrder = startingDrawOrder + index,
                    HitGeometry = BuildRectHitGeometry(bounds),
                    BoundsQuality = BoundsQuality.Fallback,
                    WorldTransform = Matrix2D.identity,
                    ParentWorldTransform = Matrix2D.identity,
                    IsTextOverlay = true
                });
            }

            return elements;
        }

        private static void BuildNodeOverlays(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            Matrix2D parentWorldTransform,
            List<PreviewTextOverlay> overlays)
        {
            if (node == null)
                return;

            Matrix2D localTransform = ParseTransform(node.RawAttributes);
            Matrix2D worldTransform = parentWorldTransform * localTransform;

            if (node.Kind == SvgNodeKind.Text &&
                !string.IsNullOrWhiteSpace(node.TextContent) &&
                TryGetFloat(node.RawAttributes, "x", out float x) &&
                TryGetFloat(node.RawAttributes, "y", out float y))
            {
                PreviewTextOverlay overlay = new PreviewTextOverlay
                {
                    Key = node.LegacyElementKey,
                    TargetKey = node.LegacyTargetKey,
                    Text = node.TextContent,
                    ScenePosition = worldTransform.MultiplyPoint(new Vector2(x, y)),
                    FontSize = TryGetFloat(node.RawAttributes, "font-size", out float fontSize) ? Mathf.Max(1f, fontSize) : 16f,
                    Color = TryGetColor(node.RawAttributes, "fill", out Color color) ? color : Color.white,
                    TextAnchor = TryGetAttribute(node.RawAttributes, "text-anchor", out string textAnchor)
                        ? textAnchor
                        : "start"
                };
                overlay.SceneBounds = EstimateTextBounds(overlay);
                overlays.Add(overlay);
            }

            for (int index = 0; index < node.Children.Count; index++)
            {
                SvgNodeId childId = node.Children[index];
                if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                BuildNodeOverlays(documentModel, childNode, worldTransform, overlays);
            }
        }

        private static Matrix2D ParseTransform(IReadOnlyDictionary<string, string> attributes)
        {
            if (!TryGetAttribute(attributes, "transform", out string transformText) || string.IsNullOrWhiteSpace(transformText))
                return Matrix2D.identity;

            Matrix2D matrix = Matrix2D.identity;
            int index = 0;
            while (index < transformText.Length)
            {
                while (index < transformText.Length && char.IsWhiteSpace(transformText[index]))
                    index++;
                if (index >= transformText.Length)
                    break;

                int nameStart = index;
                while (index < transformText.Length && char.IsLetter(transformText[index]))
                    index++;
                if (nameStart == index)
                    return Matrix2D.identity;

                string command = transformText.Substring(nameStart, index - nameStart).ToLowerInvariant();
                while (index < transformText.Length && char.IsWhiteSpace(transformText[index]))
                    index++;
                if (index >= transformText.Length || transformText[index] != '(')
                    return Matrix2D.identity;

                index++;
                int closeIndex = transformText.IndexOf(')', index);
                if (closeIndex < 0)
                    return Matrix2D.identity;

                string argsText = transformText.Substring(index, closeIndex - index);
                index = closeIndex + 1;
                if (!TryParseArguments(argsText, out List<float> args))
                    return Matrix2D.identity;

                if (!TryBuildCommandMatrix(command, args, out Matrix2D commandMatrix))
                    return Matrix2D.identity;

                matrix = matrix * commandMatrix;
            }

            return matrix;
        }

        private static bool TryParseArguments(string argsText, out List<float> args)
        {
            args = new List<float>();
            string[] tokens = argsText.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < tokens.Length; index++)
            {
                if (!float.TryParse(tokens[index], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    return false;

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
                    matrix = Matrix2D.Translate(new Vector2(args[0], args.Count > 1 ? args[1] : 0f));
                    return true;
                case "scale":
                    matrix = Matrix2D.Scale(new Vector2(args[0], args.Count > 1 ? args[1] : args[0]));
                    return true;
                case "rotate":
                    if (args.Count == 1)
                    {
                        matrix = BuildRotation(args[0], Vector2.zero, false);
                        return true;
                    }

                    if (args.Count == 3)
                    {
                        matrix = BuildRotation(args[0], new Vector2(args[1], args[2]), true);
                        return true;
                    }

                    return false;
                case "matrix":
                    if (args.Count != 6)
                        return false;

                    matrix = new Matrix2D(
                        new Vector2(args[0], args[1]),
                        new Vector2(args[2], args[3]),
                        new Vector2(args[4], args[5]));
                    return true;
                default:
                    return false;
            }
        }

        private static Matrix2D BuildRotation(float degrees, Vector2 pivot, bool aroundPivot)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            Matrix2D rotation = new(
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

        private static bool TryGetFloat(IReadOnlyDictionary<string, string> attributes, string name, out float value)
        {
            value = 0f;
            return TryGetAttribute(attributes, name, out string text) &&
                   float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetColor(IReadOnlyDictionary<string, string> attributes, string name, out Color color)
        {
            color = Color.white;
            return TryGetAttribute(attributes, name, out string text) &&
                   ColorUtility.TryParseHtmlString(text.StartsWith("#", StringComparison.Ordinal) ? text : $"#{text}", out color);
        }

        private static Rect EstimateTextBounds(PreviewTextOverlay overlay)
        {
            float fontSize = Mathf.Max(1f, overlay.FontSize);
            float width = Mathf.Max(fontSize * 0.5f, overlay.Text.Length * fontSize * 0.55f);
            float height = fontSize * 1.2f;
            float x = overlay.ScenePosition.x;
            float y = overlay.ScenePosition.y - fontSize;

            if (string.Equals(overlay.TextAnchor, "middle", StringComparison.OrdinalIgnoreCase))
                x -= width * 0.5f;
            else if (string.Equals(overlay.TextAnchor, "end", StringComparison.OrdinalIgnoreCase))
                x -= width;

            return new Rect(x, y, width, height);
        }

        private static IReadOnlyList<Vector2[]> BuildRectHitGeometry(Rect bounds)
        {
            Vector2 topLeft = new(bounds.xMin, bounds.yMin);
            Vector2 topRight = new(bounds.xMax, bounds.yMin);
            Vector2 bottomRight = new(bounds.xMax, bounds.yMax);
            Vector2 bottomLeft = new(bounds.xMin, bounds.yMax);
            return new[]
            {
                new[] { topLeft, topRight, bottomRight },
                new[] { topLeft, bottomRight, bottomLeft }
            };
        }
    }
}
