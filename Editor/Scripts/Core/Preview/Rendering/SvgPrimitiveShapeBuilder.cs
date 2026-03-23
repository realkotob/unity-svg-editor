using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Svg.Geometry;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Structure.Lookup;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.Core.Preview.Rendering
{
    internal sealed class SvgPrimitiveShapeBuilder
    {
        private readonly SvgShapeStyleBuilder _shapeStyleBuilder = new();

        public bool TryAddRectShape(
            SvgShapeBuilder.RenderBuildContext context,
            SvgNodeModel node,
            out string error)
        {
            error = string.Empty;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.X, out var x))
                x = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.Y, out var y))
                y = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.WIDTH, out var width) ||
                !AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.HEIGHT, out var height))
            {
                error = $"Rect '{node.LegacyElementKey}' is missing width/height.";
                return false;
            }

            var shape = _shapeStyleBuilder.CreateStyledShape(context.DocumentModel, node, context.NodesByXmlId);
            var rx = 0f;
            var ry = 0f;
            AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.RX, out rx);
            AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.RY, out ry);
            if (Mathf.Approximately(rx, 0f) && !Mathf.Approximately(ry, 0f))
                rx = ry;
            if (Mathf.Approximately(ry, 0f) && !Mathf.Approximately(rx, 0f))
                ry = rx;

            VectorUtils.MakeRectangleShape(
                shape,
                new Rect(x, y, width, height),
                new Vector2(rx, ry),
                new Vector2(rx, ry),
                new Vector2(rx, ry),
                new Vector2(rx, ry));
            context.SceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddCircleShape(
            SvgShapeBuilder.RenderBuildContext context,
            SvgNodeModel node,
            out string error)
        {
            error = string.Empty;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.CX, out var cx))
                cx = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.CY, out var cy))
                cy = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.R, out var radius))
            {
                error = $"Circle '{node.LegacyElementKey}' is missing radius.";
                return false;
            }

            var shape = _shapeStyleBuilder.CreateStyledShape(context.DocumentModel, node, context.NodesByXmlId);
            VectorUtils.MakeCircleShape(shape, new Vector2(cx, cy), radius);
            context.SceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddEllipseShape(
            SvgShapeBuilder.RenderBuildContext context,
            SvgNodeModel node,
            out string error)
        {
            error = string.Empty;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.CX, out var cx))
                cx = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.CY, out var cy))
                cy = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.RX, out var rx) ||
                !AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.RY, out var ry))
            {
                error = $"Ellipse '{node.LegacyElementKey}' is missing radius.";
                return false;
            }

            var shape = _shapeStyleBuilder.CreateStyledShape(context.DocumentModel, node, context.NodesByXmlId);
            VectorUtils.MakeEllipseShape(shape, new Vector2(cx, cy), rx, ry);
            context.SceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddLineShape(
            SvgShapeBuilder.RenderBuildContext context,
            SvgNodeModel node,
            out string error)
        {
            error = string.Empty;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.X1, out var x1))
                x1 = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.Y1, out var y1))
                y1 = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.X2, out var x2))
                x2 = 0f;
            if (!AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.Y2, out var y2))
                y2 = 0f;

            var shape = _shapeStyleBuilder.CreateStyledShape(context.DocumentModel, node, context.NodesByXmlId, allowDefaultFill: false);
            shape.Contours = new[]
            {
                new BezierContour
                {
                    Segments = VectorUtils.BezierSegmentToPath(VectorUtils.MakeLine(new Vector2(x1, y1), new Vector2(x2, y2))),
                    Closed = false
                }
            };
            shape.IsConvex = false;
            context.SceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddPathShape(
            SvgShapeBuilder.RenderBuildContext context,
            SvgNodeModel node,
            out string error)
        {
            error = string.Empty;
            if (!AttributeUtility.TryGetAttribute(node.RawAttributes, SvgAttributeName.D, out var pathData))
            {
                error = $"Path '{node.LegacyElementKey}' is missing geometry.";
                return false;
            }

            if (!PathGeometryParser.TryParsePathContours(pathData, out var contours))
            {
                error = $"Direct renderer does not yet support path data on '{node.LegacyElementKey}'.";
                return false;
            }

            bool allowDefaultFill = HasOpenContour(contours);
            var shape = _shapeStyleBuilder.CreateStyledShape(context.DocumentModel, node, context.NodesByXmlId, allowDefaultFill: allowDefaultFill);
            shape.Contours = contours;
            shape.IsConvex = false;
            context.SceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddPolylineShape(
            SvgShapeBuilder.RenderBuildContext context,
            SvgNodeModel node,
            out string error,
            bool closed)
        {
            error = string.Empty;
            if (!AttributeUtility.TryGetAttribute(node.RawAttributes, SvgAttributeName.POINTS, out var pointsText) ||
                !PathGeometryParser.TryParsePoints(pointsText, out var points) ||
                points.Count < 2)
            {
                error = $"Polyline data on '{node.LegacyElementKey}' was invalid.";
                return false;
            }

            var segments = new List<BezierSegment>();
            for (var index = 1; index < points.Count; index++)
            {
                segments.Add(VectorUtils.MakeLine(points[index - 1], points[index]));
            }

            if (closed && (points[0] - points[^1]).sqrMagnitude > Mathf.Epsilon)
                segments.Add(VectorUtils.MakeLine(points[^1], points[0]));

            var shape = _shapeStyleBuilder.CreateStyledShape(context.DocumentModel, node, context.NodesByXmlId, allowDefaultFill: true);
            shape.Contours = new[]
            {
                new BezierContour
                {
                    Segments = VectorUtils.BezierSegmentsToPath(segments.ToArray()),
                    Closed = closed
                }
            };
            shape.IsConvex = closed;
            context.SceneNode.Shapes.Add(shape);
            return true;
        }

        private static bool HasOpenContour(IReadOnlyList<BezierContour> contours)
        {
            if (contours == null)
            {
                return false;
            }

            for (var index = 0; index < contours.Count; index++)
            {
                if (!contours[index].Closed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
