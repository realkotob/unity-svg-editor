using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgPrimitiveShapeBuilder
    {
        private readonly SvgShapeStyleBuilder _shapeStyleBuilder = new();

        public bool TryAddRectShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.X, out var x))
                x = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.Y, out var y))
                y = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.WIDTH, out var width) ||
                !SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.HEIGHT, out var height))
            {
                error = $"Rect '{node.LegacyElementKey}' is missing width/height.";
                return false;
            }

            var shape = _shapeStyleBuilder.CreateStyledShape(documentModel, node, nodesByXmlId);
            var rx = 0f;
            var ry = 0f;
            SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.RX, out rx);
            SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.RY, out ry);
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
            sceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddCircleShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.CX, out var cx))
                cx = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.CY, out var cy))
                cy = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.R, out var radius))
            {
                error = $"Circle '{node.LegacyElementKey}' is missing radius.";
                return false;
            }

            var shape = _shapeStyleBuilder.CreateStyledShape(documentModel, node, nodesByXmlId);
            VectorUtils.MakeCircleShape(shape, new Vector2(cx, cy), radius);
            sceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddEllipseShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.CX, out var cx))
                cx = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.CY, out var cy))
                cy = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.RX, out var rx) ||
                !SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.RY, out var ry))
            {
                error = $"Ellipse '{node.LegacyElementKey}' is missing radius.";
                return false;
            }

            var shape = _shapeStyleBuilder.CreateStyledShape(documentModel, node, nodesByXmlId);
            VectorUtils.MakeEllipseShape(shape, new Vector2(cx, cy), rx, ry);
            sceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddLineShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.X1, out var x1))
                x1 = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.Y1, out var y1))
                y1 = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.X2, out var x2))
                x2 = 0f;
            if (!SvgAttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.Y2, out var y2))
                y2 = 0f;

            var shape = _shapeStyleBuilder.CreateStyledShape(documentModel, node, nodesByXmlId, allowDefaultFill: false);
            shape.Contours = new[]
            {
                new BezierContour
                {
                    Segments = VectorUtils.BezierSegmentToPath(VectorUtils.MakeLine(new Vector2(x1, y1), new Vector2(x2, y2))),
                    Closed = false
                }
            };
            shape.IsConvex = false;
            sceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddPathShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!SvgAttributeUtility.TryGetAttribute(node.RawAttributes, SvgAttributeName.D, out var pathData))
            {
                error = $"Path '{node.LegacyElementKey}' is missing geometry.";
                return false;
            }

            if (!SvgPathGeometryParser.TryParsePathContours(pathData, out var contours))
            {
                error = $"Direct renderer does not yet support path data on '{node.LegacyElementKey}'.";
                return false;
            }

            var shape = _shapeStyleBuilder.CreateStyledShape(documentModel, node, nodesByXmlId, allowDefaultFill: false);
            shape.Contours = contours;
            shape.IsConvex = false;
            sceneNode.Shapes.Add(shape);
            return true;
        }

        public bool TryAddPolylineShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error,
            bool closed)
        {
            error = string.Empty;
            if (!SvgAttributeUtility.TryGetAttribute(node.RawAttributes, SvgAttributeName.POINTS, out var pointsText) ||
                !SvgPathGeometryParser.TryParsePoints(pointsText, out var points) ||
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

            var shape = _shapeStyleBuilder.CreateStyledShape(documentModel, node, nodesByXmlId, allowDefaultFill: closed);
            shape.Contours = new[]
            {
                new BezierContour
                {
                    Segments = VectorUtils.BezierSegmentsToPath(segments.ToArray()),
                    Closed = closed
                }
            };
            shape.IsConvex = closed;
            sceneNode.Shapes.Add(shape);
            return true;
        }
    }
}
