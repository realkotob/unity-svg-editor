using System.Collections.Generic;
using System.Globalization;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Geometry;
using SvgEditor.Core.Svg.Model;
using UnityEngine;

namespace SvgEditor.Core.Svg.PathEditing
{
    internal static class PrimitivePathConversion
    {
        private const float CubicArcKappa = 0.552284749831f;

        public static bool TryCreateEditablePathData(SvgNodeModel node, out PathData pathData, out string error)
        {
            pathData = null;
            error = string.Empty;
            if (node?.RawAttributes == null)
            {
                error = "Path data is unavailable.";
                return false;
            }

            string normalizedTagName = (node.TagName ?? string.Empty).Trim();
            if (string.Equals(normalizedTagName, SvgTagName.PATH, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!node.RawAttributes.TryGetValue(SvgAttributeName.D, out string pathText))
                {
                    error = "Path data is unavailable.";
                    return false;
                }

                pathData = PathDataParser.Parse(pathText);
                return true;
            }

            if (string.Equals(normalizedTagName, SvgTagName.LINE, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!TryBuildLinePathData(node.RawAttributes, out pathData))
                {
                    error = "Path data is malformed.";
                    return false;
                }

                return true;
            }

            if (string.Equals(normalizedTagName, SvgTagName.RECT, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!TryBuildRectPathData(node.RawAttributes, out pathData))
                {
                    error = "Path data is malformed.";
                    return false;
                }

                return true;
            }

            if (string.Equals(normalizedTagName, SvgTagName.CIRCLE, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!TryBuildCirclePathData(node.RawAttributes, out pathData))
                {
                    error = "Path data is malformed.";
                    return false;
                }

                return true;
            }

            if (string.Equals(normalizedTagName, SvgTagName.ELLIPSE, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!TryBuildEllipsePathData(node.RawAttributes, out pathData))
                {
                    error = "Path data is malformed.";
                    return false;
                }

                return true;
            }

            bool isClosed = string.Equals(normalizedTagName, SvgTagName.POLYGON, System.StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(normalizedTagName, SvgTagName.POLYLINE, System.StringComparison.OrdinalIgnoreCase) && !isClosed)
            {
                error = "Path data is unavailable.";
                return false;
            }

            if (!node.RawAttributes.TryGetValue(SvgAttributeName.POINTS, out string pointsText) ||
                !PathGeometryParser.TryParsePoints(pointsText, out List<Vector2> points))
            {
                error = "Path data is malformed.";
                return false;
            }

            if (isClosed && points.Count > 1 && Approximately(points[0], points[^1]))
            {
                points.RemoveAt(points.Count - 1);
            }

            if (points.Count < 2)
            {
                error = "Path data is malformed.";
                return false;
            }

            var pathNodes = new List<PathNode>(points.Count - 1);
            for (int index = 1; index < points.Count; index++)
            {
                pathNodes.Add(new PathNode('L', points[index]));
            }

            pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(points[0], pathNodes, isClosed));
            return true;
        }

        public static bool TrySerializeAsPoints(PathData pathData, bool closed, out string pointsText)
        {
            pointsText = string.Empty;
            if (pathData == null ||
                pathData.IsMalformed ||
                pathData.HasUnsupportedCommands ||
                pathData.Subpaths.Count != 1 ||
                pathData.Subpaths[0] == null)
            {
                return false;
            }

            PathSubpath subpath = pathData.Subpaths[0];
            if (subpath.IsClosed != closed)
            {
                return false;
            }

            var points = new List<Vector2>(subpath.Nodes.Count + 1) { subpath.Start };
            for (int index = 0; index < subpath.Nodes.Count; index++)
            {
                PathNode node = subpath.Nodes[index];
                if (char.ToUpperInvariant(node.Command) != 'L')
                {
                    return false;
                }

                points.Add(node.Position);
            }

            if (points.Count < 2)
            {
                return false;
            }

            pointsText = SerializePoints(points);
            return true;
        }

        public static bool TrySerializeAsLine(PathData pathData, out Vector2 start, out Vector2 end)
        {
            start = default;
            end = default;
            if (!TryGetSingleLinearSubpath(pathData, out PathSubpath subpath) ||
                subpath.IsClosed ||
                subpath.Nodes.Count != 1)
            {
                return false;
            }

            start = subpath.Start;
            end = subpath.Nodes[0].Position;
            return true;
        }

        public static bool TrySerializeAsRect(PathData pathData, out Rect rect)
        {
            rect = default;
            if (!TryGetSingleLinearSubpath(pathData, out PathSubpath subpath) ||
                !subpath.IsClosed ||
                subpath.Nodes.Count != 3)
            {
                return false;
            }

            Vector2[] points =
            {
                subpath.Start,
                subpath.Nodes[0].Position,
                subpath.Nodes[1].Position,
                subpath.Nodes[2].Position
            };

            float minX = Mathf.Min(points[0].x, points[1].x, points[2].x, points[3].x);
            float maxX = Mathf.Max(points[0].x, points[1].x, points[2].x, points[3].x);
            float minY = Mathf.Min(points[0].y, points[1].y, points[2].y, points[3].y);
            float maxY = Mathf.Max(points[0].y, points[1].y, points[2].y, points[3].y);
            if (Approximately(minX, maxX) || Approximately(minY, maxY))
            {
                return false;
            }

            for (int index = 0; index < points.Length; index++)
            {
                Vector2 current = points[index];
                Vector2 next = points[(index + 1) % points.Length];
                bool sameX = Approximately(current.x, next.x);
                bool sameY = Approximately(current.y, next.y);
                if (sameX == sameY)
                {
                    return false;
                }

                if (!MatchesRectCorner(current, minX, maxX, minY, maxY))
                {
                    return false;
                }
            }

            rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private static string SerializePoints(IReadOnlyList<Vector2> points)
        {
            var tokens = new List<string>(points.Count * 2);
            for (int index = 0; index < points.Count; index++)
            {
                tokens.Add(FormatFloat(points[index].x));
                tokens.Add(FormatFloat(points[index].y));
            }

            return string.Join(" ", tokens);
        }

        private static bool TryBuildLinePathData(IReadOnlyDictionary<string, string> attributes, out PathData pathData)
        {
            pathData = null;
            float x1 = TryGetFloat(attributes, SvgAttributeName.X1, 0f);
            float y1 = TryGetFloat(attributes, SvgAttributeName.Y1, 0f);
            float x2 = TryGetFloat(attributes, SvgAttributeName.X2, 0f);
            float y2 = TryGetFloat(attributes, SvgAttributeName.Y2, 0f);

            pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(
                new Vector2(x1, y1),
                new[] { new PathNode('L', new Vector2(x2, y2)) }));
            return true;
        }

        private static bool TryBuildRectPathData(IReadOnlyDictionary<string, string> attributes, out PathData pathData)
        {
            pathData = null;
            float x = TryGetFloat(attributes, SvgAttributeName.X, 0f);
            float y = TryGetFloat(attributes, SvgAttributeName.Y, 0f);
            if (!TryGetRequiredFloat(attributes, SvgAttributeName.WIDTH, out float width) ||
                !TryGetRequiredFloat(attributes, SvgAttributeName.HEIGHT, out float height) ||
                width <= 0f ||
                height <= 0f)
            {
                return false;
            }

            float rx = TryGetFloat(attributes, SvgAttributeName.RX, 0f);
            float ry = TryGetFloat(attributes, SvgAttributeName.RY, 0f);
            if (Approximately(rx, 0f) && !Approximately(ry, 0f))
            {
                rx = ry;
            }
            else if (Approximately(ry, 0f) && !Approximately(rx, 0f))
            {
                ry = rx;
            }

            rx = Mathf.Min(rx, width * 0.5f);
            ry = Mathf.Min(ry, height * 0.5f);
            if (rx > 0f && ry > 0f)
            {
                pathData = BuildRoundedRectPathData(x, y, width, height, rx, ry);
                return true;
            }

            pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(
                new Vector2(x, y),
                new[]
                {
                    new PathNode('L', new Vector2(x + width, y)),
                    new PathNode('L', new Vector2(x + width, y + height)),
                    new PathNode('L', new Vector2(x, y + height))
                },
                isClosed: true));
            return true;
        }

        private static bool TryBuildCirclePathData(IReadOnlyDictionary<string, string> attributes, out PathData pathData)
        {
            pathData = null;
            float cx = TryGetFloat(attributes, SvgAttributeName.CX, 0f);
            float cy = TryGetFloat(attributes, SvgAttributeName.CY, 0f);
            if (!TryGetRequiredFloat(attributes, SvgAttributeName.R, out float radius) || radius <= 0f)
            {
                return false;
            }

            pathData = BuildEllipsePathData(cx, cy, radius, radius);
            return true;
        }

        private static bool TryBuildEllipsePathData(IReadOnlyDictionary<string, string> attributes, out PathData pathData)
        {
            pathData = null;
            float cx = TryGetFloat(attributes, SvgAttributeName.CX, 0f);
            float cy = TryGetFloat(attributes, SvgAttributeName.CY, 0f);
            if (!TryGetRequiredFloat(attributes, SvgAttributeName.RX, out float rx) ||
                !TryGetRequiredFloat(attributes, SvgAttributeName.RY, out float ry) ||
                rx <= 0f ||
                ry <= 0f)
            {
                return false;
            }

            pathData = BuildEllipsePathData(cx, cy, rx, ry);
            return true;
        }

        private static bool TryGetSingleLinearSubpath(PathData pathData, out PathSubpath subpath)
        {
            subpath = null;
            if (pathData == null ||
                pathData.IsMalformed ||
                pathData.HasUnsupportedCommands ||
                pathData.Subpaths.Count != 1 ||
                pathData.Subpaths[0] == null)
            {
                return false;
            }

            subpath = pathData.Subpaths[0];
            for (int index = 0; index < subpath.Nodes.Count; index++)
            {
                if (char.ToUpperInvariant(subpath.Nodes[index].Command) != 'L')
                {
                    subpath = null;
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesRectCorner(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            bool matchesX = Approximately(point.x, minX) || Approximately(point.x, maxX);
            bool matchesY = Approximately(point.y, minY) || Approximately(point.y, maxY);
            return matchesX && matchesY;
        }

        private static PathData BuildEllipsePathData(float cx, float cy, float rx, float ry)
        {
            float kx = rx * CubicArcKappa;
            float ky = ry * CubicArcKappa;
            Vector2 top = new(cx, cy - ry);
            Vector2 right = new(cx + rx, cy);
            Vector2 bottom = new(cx, cy + ry);
            Vector2 left = new(cx - rx, cy);

            var pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(
                top,
                new[]
                {
                    new PathNode('C', right, new Vector2(cx + kx, cy - ry), new Vector2(cx + rx, cy - ky), PathHandleMode.Free),
                    new PathNode('C', bottom, new Vector2(cx + rx, cy + ky), new Vector2(cx + kx, cy + ry), PathHandleMode.Free),
                    new PathNode('C', left, new Vector2(cx - kx, cy + ry), new Vector2(cx - rx, cy + ky), PathHandleMode.Free),
                    new PathNode('C', top, new Vector2(cx - rx, cy - ky), new Vector2(cx - kx, cy - ry), PathHandleMode.Free)
                },
                isClosed: true));
            return pathData;
        }

        private static PathData BuildRoundedRectPathData(float x, float y, float width, float height, float rx, float ry)
        {
            float right = x + width;
            float bottom = y + height;
            float kx = rx * CubicArcKappa;
            float ky = ry * CubicArcKappa;
            Vector2 start = new(x + rx, y);

            var pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(
                start,
                new[]
                {
                    new PathNode('L', new Vector2(right - rx, y)),
                    new PathNode('C', new Vector2(right, y + ry), new Vector2(right - rx + kx, y), new Vector2(right, y + ry - ky), PathHandleMode.Free),
                    new PathNode('L', new Vector2(right, bottom - ry)),
                    new PathNode('C', new Vector2(right - rx, bottom), new Vector2(right, bottom - ry + ky), new Vector2(right - rx + kx, bottom), PathHandleMode.Free),
                    new PathNode('L', new Vector2(x + rx, bottom)),
                    new PathNode('C', new Vector2(x, bottom - ry), new Vector2(x + rx - kx, bottom), new Vector2(x, bottom - ry + ky), PathHandleMode.Free),
                    new PathNode('L', new Vector2(x, y + ry)),
                    new PathNode('C', start, new Vector2(x, y + ry - ky), new Vector2(x + rx - kx, y), PathHandleMode.Free)
                },
                isClosed: true));
            return pathData;
        }

        private static bool TryGetRequiredFloat(IReadOnlyDictionary<string, string> attributes, string name, out float value)
        {
            value = default;
            return attributes != null &&
                   attributes.TryGetValue(name, out string rawValue) &&
                   float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static float TryGetFloat(IReadOnlyDictionary<string, string> attributes, string name, float fallback)
        {
            return TryGetRequiredFloat(attributes, name, out float value) ? value : fallback;
        }

        private static string FormatFloat(float value)
        {
            if (Mathf.Abs(value) < 0.0000005f)
            {
                value = 0f;
            }

            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return (left - right).sqrMagnitude <= 0.000001f;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.000001f;
        }
    }
}
