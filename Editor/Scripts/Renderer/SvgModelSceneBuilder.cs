using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgModelSceneBuilder
    {
        public bool TryBuildReferenceOverlayScenes(
            SvgDocumentModel documentModel,
            string elementKey,
            out IReadOnlyList<CanvasDefinitionOverlayScene> overlays,
            out string error)
        {
            overlays = Array.Empty<CanvasDefinitionOverlayScene>();
            error = string.Empty;

            if (documentModel?.Root == null || string.IsNullOrWhiteSpace(elementKey))
                return true;

            if (!TryFindNodeByLegacyElementKey(documentModel, elementKey, out SvgNodeModel node) || node == null)
                return true;

            Dictionary<string, SvgNodeModel> nodesByXmlId = BuildNodeLookupByXmlId(documentModel);
            List<CanvasDefinitionOverlayScene> resolved = new();

            if (!TryBuildReferenceOverlayScene(documentModel, nodesByXmlId, node, CanvasDefinitionOverlayKind.Mask, out CanvasDefinitionOverlayScene maskOverlay, out error))
                return false;
            if (maskOverlay != null)
                resolved.Add(maskOverlay);

            if (!TryBuildReferenceOverlayScene(documentModel, nodesByXmlId, node, CanvasDefinitionOverlayKind.ClipPath, out CanvasDefinitionOverlayScene clipOverlay, out error))
                return false;
            if (clipOverlay != null)
                resolved.Add(clipOverlay);

            overlays = resolved;
            return true;
        }

        public bool TryBuild(
            SvgDocumentModel documentModel,
            out SvgModelSceneBuildResult result,
            out string error)
        {
            result = new SvgModelSceneBuildResult();
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model root is unavailable.";
                return false;
            }

            SceneNode rootSceneNode = new()
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = Matrix2D.identity
            };

            result.Scene = new Scene
            {
                Root = rootSceneNode
            };
            result.DocumentViewportRect = ResolveDocumentViewport(documentModel);
            result.PreserveAspectRatioMode = SvgPreserveAspectRatioMode.Parse(documentModel.PreserveAspectRatio);

            Dictionary<string, SvgNodeModel> nodesByXmlId = BuildNodeLookupByXmlId(documentModel);

            if (!TryBuildChildren(documentModel, nodesByXmlId, documentModel.Root, rootSceneNode, result, out error))
                return false;

            return true;
        }

        private bool TryBuildChildren(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel parentNode,
            SceneNode parentSceneNode,
            SvgModelSceneBuildResult result,
            out string error)
        {
            error = string.Empty;
            if (parentNode?.Children == null)
                return true;

            for (int index = 0; index < parentNode.Children.Count; index++)
            {
                SvgNodeId childId = parentNode.Children[index];
                if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                if (!TryBuildNode(documentModel, nodesByXmlId, childNode, out SceneNode sceneNode, result, out error))
                    return false;

                if (sceneNode != null)
                    parentSceneNode.Children.Add(sceneNode);
            }

            return true;
        }

        private bool TryBuildNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            out SceneNode sceneNode,
            SvgModelSceneBuildResult result,
            out string error)
        {
            sceneNode = null;
            error = string.Empty;

            if (node == null || node.IsDefinitionNode || node.Kind == SvgNodeKind.Definitions || IsHidden(node))
                return true;

            sceneNode = new SceneNode
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = ParseTransform(node.RawAttributes)
            };

            if (!TryAttachMask(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryAttachClipper(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryBuildShapes(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryBuildChildren(documentModel, nodesByXmlId, node, sceneNode, result, out error))
                return false;

            if (TryGetOpacity(node.RawAttributes, out float opacity) && !Mathf.Approximately(opacity, 1f))
                result.NodeOpacities[sceneNode] = opacity;

            if (!node.Id.IsRoot)
                result.NodeMappings[sceneNode] = (node.LegacyElementKey, node.LegacyTargetKey);

            return true;
        }

        private bool TryAttachMask(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!TryGetAttribute(node?.RawAttributes, "mask", out string maskValue))
                return true;

            if (!TryExtractFragmentId(maskValue, out string fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out SvgNodeModel maskNode) ||
                !string.Equals(maskNode.TagName, "mask", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!TryBuildMaskClipNode(documentModel, nodesByXmlId, maskNode, out SceneNode maskClipNode, out error))
                return false;

            sceneNode.Clipper = maskClipNode;
            return true;
        }

        private bool TryBuildReferenceOverlayScene(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            CanvasDefinitionOverlayKind kind,
            out CanvasDefinitionOverlayScene overlay,
            out string error)
        {
            overlay = null;
            error = string.Empty;
            if (node == null || nodesByXmlId == null)
                return true;

            string attributeName = kind == CanvasDefinitionOverlayKind.Mask ? "mask" : "clip-path";
            string expectedTag = kind == CanvasDefinitionOverlayKind.Mask ? "mask" : "clipPath";
            if (!TryGetAttribute(node.RawAttributes, attributeName, out string rawValue) ||
                !TryExtractFragmentId(rawValue, out string fragmentId) ||
                !nodesByXmlId.TryGetValue(fragmentId, out SvgNodeModel referenceNode) ||
                !string.Equals(referenceNode.TagName, expectedTag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            SceneNode sceneNode;
            bool built = kind == CanvasDefinitionOverlayKind.Mask
                ? TryBuildMaskClipNode(documentModel, nodesByXmlId, referenceNode, out sceneNode, out error)
                : TryBuildClipNode(documentModel, nodesByXmlId, referenceNode, out sceneNode, out error);
            if (!built)
                return false;

            if (sceneNode == null)
                return true;

            overlay = new CanvasDefinitionOverlayScene
            {
                Kind = kind,
                ReferenceId = fragmentId,
                DefinitionElementKey = referenceNode.LegacyElementKey,
                RootNode = sceneNode
            };
            return true;
        }

        private bool TryBuildShapes(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (node == null || sceneNode == null)
                return true;

            switch (node.TagName)
            {
                case "g":
                case "svg":
                    return true;
                case "rect":
                    return TryAddRectShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case "circle":
                    return TryAddCircleShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case "ellipse":
                    return TryAddEllipseShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case "line":
                    return TryAddLineShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case "polyline":
                    return TryAddPolylineShape(documentModel, node, nodesByXmlId, sceneNode, out error, closed: false);
                case "polygon":
                    return TryAddPolylineShape(documentModel, node, nodesByXmlId, sceneNode, out error, closed: true);
                case "path":
                    return TryAddPathShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case "text":
                case "tspan":
                case "textPath":
                    return true;
                case "use":
                    return TryAddUseNode(documentModel, nodesByXmlId, node, sceneNode, out error);
                default:
                    return true;
            }
        }

        private bool TryAddRectShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!TryGetFloat(node.RawAttributes, "x", out float x))
                x = 0f;
            if (!TryGetFloat(node.RawAttributes, "y", out float y))
                y = 0f;
            if (!TryGetFloat(node.RawAttributes, "width", out float width) ||
                !TryGetFloat(node.RawAttributes, "height", out float height))
            {
                error = $"Rect '{node.LegacyElementKey}' is missing width/height.";
                return false;
            }

            Shape shape = CreateStyledShape(documentModel, node, nodesByXmlId);
            float rx = 0f;
            float ry = 0f;
            TryGetFloat(node.RawAttributes, "rx", out rx);
            TryGetFloat(node.RawAttributes, "ry", out ry);
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

        private bool TryAddCircleShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!TryGetFloat(node.RawAttributes, "cx", out float cx))
                cx = 0f;
            if (!TryGetFloat(node.RawAttributes, "cy", out float cy))
                cy = 0f;
            if (!TryGetFloat(node.RawAttributes, "r", out float radius))
            {
                error = $"Circle '{node.LegacyElementKey}' is missing radius.";
                return false;
            }

            Shape shape = CreateStyledShape(documentModel, node, nodesByXmlId);
            VectorUtils.MakeCircleShape(shape, new Vector2(cx, cy), radius);
            sceneNode.Shapes.Add(shape);
            return true;
        }

        private bool TryAddEllipseShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!TryGetFloat(node.RawAttributes, "cx", out float cx))
                cx = 0f;
            if (!TryGetFloat(node.RawAttributes, "cy", out float cy))
                cy = 0f;
            if (!TryGetFloat(node.RawAttributes, "rx", out float rx) ||
                !TryGetFloat(node.RawAttributes, "ry", out float ry))
            {
                error = $"Ellipse '{node.LegacyElementKey}' is missing radius.";
                return false;
            }

            Shape shape = CreateStyledShape(documentModel, node, nodesByXmlId);
            VectorUtils.MakeEllipseShape(shape, new Vector2(cx, cy), rx, ry);
            sceneNode.Shapes.Add(shape);
            return true;
        }

        private bool TryAddLineShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!TryGetFloat(node.RawAttributes, "x1", out float x1))
                x1 = 0f;
            if (!TryGetFloat(node.RawAttributes, "y1", out float y1))
                y1 = 0f;
            if (!TryGetFloat(node.RawAttributes, "x2", out float x2))
                x2 = 0f;
            if (!TryGetFloat(node.RawAttributes, "y2", out float y2))
                y2 = 0f;

            Shape shape = CreateStyledShape(documentModel, node, nodesByXmlId, allowDefaultFill: false);
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

        private bool TryAddPathShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!TryGetAttribute(node.RawAttributes, "d", out string pathData))
            {
                error = $"Path '{node.LegacyElementKey}' is missing geometry.";
                return false;
            }

            if (!TryParsePathContours(pathData, out BezierContour[] contours))
            {
                error = $"Direct renderer does not yet support path data on '{node.LegacyElementKey}'.";
                return false;
            }

            Shape shape = CreateStyledShape(documentModel, node, nodesByXmlId, allowDefaultFill: false);
            shape.Contours = contours;
            shape.IsConvex = false;
            sceneNode.Shapes.Add(shape);
            return true;
        }

        private bool TryAddPolylineShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SceneNode sceneNode,
            out string error,
            bool closed)
        {
            error = string.Empty;
            if (!TryGetAttribute(node.RawAttributes, "points", out string pointsText) ||
                !TryParsePoints(pointsText, out List<Vector2> points) ||
                points.Count < 2)
            {
                error = $"Polyline data on '{node.LegacyElementKey}' was invalid.";
                return false;
            }

            List<BezierSegment> segments = new();
            for (int index = 1; index < points.Count; index++)
            {
                segments.Add(VectorUtils.MakeLine(points[index - 1], points[index]));
            }

            if (closed && (points[0] - points[^1]).sqrMagnitude > Mathf.Epsilon)
                segments.Add(VectorUtils.MakeLine(points[^1], points[0]));

            Shape shape = CreateStyledShape(documentModel, node, nodesByXmlId, allowDefaultFill: closed);
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

        private bool TryAddUseNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel useNode,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!TryResolveUseReference(useNode, nodesByXmlId, out SvgNodeModel referencedNode))
            {
                error = $"Direct renderer could not resolve <use> target for '{useNode.LegacyElementKey}'.";
                return false;
            }

            if (!TryGetFloat(useNode.RawAttributes, "x", out float x))
                x = 0f;
            if (!TryGetFloat(useNode.RawAttributes, "y", out float y))
                y = 0f;
            if (!Mathf.Approximately(x, 0f) || !Mathf.Approximately(y, 0f))
                sceneNode.Transform = Matrix2D.Translate(new Vector2(x, y)) * sceneNode.Transform;

            if (!TryBuildReferencedNode(documentModel, nodesByXmlId, referencedNode, out SceneNode referencedSceneNode, out error))
                return false;

            if (referencedSceneNode != null)
                sceneNode.Children.Add(referencedSceneNode);

            return true;
        }

        private bool TryBuildReferencedNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            out SceneNode sceneNode,
            out string error)
        {
            sceneNode = null;
            error = string.Empty;

            if (node == null || IsHidden(node))
                return true;

            sceneNode = new SceneNode
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = ParseTransform(node.RawAttributes)
            };

            if (!TryAttachMask(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryAttachClipper(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryBuildShapes(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (node.Children != null)
            {
                for (int index = 0; index < node.Children.Count; index++)
                {
                    SvgNodeId childId = node.Children[index];
                    if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                        continue;

                    if (!TryBuildReferencedNode(documentModel, nodesByXmlId, childNode, out SceneNode childSceneNode, out error))
                        return false;

                    if (childSceneNode != null)
                        sceneNode.Children.Add(childSceneNode);
                }
            }

            return true;
        }

        private bool TryAttachClipper(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!TryGetAttribute(node?.RawAttributes, "clip-path", out string clipPathValue))
                return true;

            if (!TryExtractFragmentId(clipPathValue, out string fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out SvgNodeModel clipNode) ||
                !string.Equals(clipNode.TagName, "clipPath", StringComparison.OrdinalIgnoreCase))
            {
                error = $"Direct renderer could not resolve clipPath for '{node?.LegacyElementKey}'.";
                return false;
            }

            if (!TryBuildClipNode(documentModel, nodesByXmlId, clipNode, out SceneNode clipSceneNode, out error))
                return false;

            sceneNode.Clipper = clipSceneNode;
            return true;
        }

        private bool TryBuildClipNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel clipNode,
            out SceneNode sceneNode,
            out string error)
        {
            sceneNode = new SceneNode
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = ParseTransform(clipNode?.RawAttributes)
            };
            error = string.Empty;

            if (clipNode == null)
            {
                error = "Clip node was null.";
                return false;
            }

            for (int index = 0; index < clipNode.Children.Count; index++)
            {
                SvgNodeId childId = clipNode.Children[index];
                if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null || IsHidden(childNode))
                    continue;

                SceneNode clipChildNode = new()
                {
                    Children = new List<SceneNode>(),
                    Shapes = new List<Shape>(),
                    Transform = ParseTransform(childNode.RawAttributes)
                };

                if (!TryBuildShapes(documentModel, nodesByXmlId, childNode, clipChildNode, out error))
                    return false;

                if (clipChildNode.Shapes.Count > 0 || clipChildNode.Children.Count > 0)
                    sceneNode.Children.Add(clipChildNode);
            }

            return true;
        }

        private bool TryBuildMaskClipNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel maskNode,
            out SceneNode sceneNode,
            out string error)
        {
            sceneNode = new SceneNode
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = ParseTransform(maskNode?.RawAttributes)
            };
            error = string.Empty;

            if (maskNode == null)
            {
                error = "Mask node was null.";
                return false;
            }

            for (int index = 0; index < maskNode.Children.Count; index++)
            {
                SvgNodeId childId = maskNode.Children[index];
                if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) ||
                    childNode == null ||
                    IsHidden(childNode) ||
                    !ShouldIncludeMaskNode(childNode))
                {
                    continue;
                }

                SceneNode maskChildNode = new()
                {
                    Children = new List<SceneNode>(),
                    Shapes = new List<Shape>(),
                    Transform = ParseTransform(childNode.RawAttributes)
                };

                if (!TryBuildShapes(documentModel, nodesByXmlId, childNode, maskChildNode, out error))
                    return false;

                if (maskChildNode.Shapes.Count > 0 || maskChildNode.Children.Count > 0)
                    sceneNode.Children.Add(maskChildNode);
            }

            return sceneNode.Children.Count > 0;
        }

        private Shape CreateStyledShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            bool allowDefaultFill = true)
        {
            Shape shape = new();
            shape.Fill = BuildFill(documentModel, node, nodesByXmlId, allowDefaultFill);
            shape.PathProps = BuildPathProperties(node);
            shape.FillTransform = Matrix2D.identity;
            return shape;
        }

        private IFill BuildFill(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            bool allowDefaultFill)
        {
            if (TryGetAttribute(node.RawAttributes, "fill", out string fillValue))
            {
                if (string.Equals(fillValue, "none", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (fillValue.Contains("url(", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryBuildGradientFill(documentModel, fillValue, nodesByXmlId, node, out IFill gradientFill))
                        return gradientFill;

                    return null;
                }

                if (TryParseColor(fillValue, out Color color))
                {
                    return new SolidFill
                    {
                        Color = color,
                        Opacity = ResolveFillOpacity(node),
                        Mode = ResolveFillMode(node)
                    };
                }
            }

            if (!allowDefaultFill)
                return null;

            return new SolidFill
            {
                Color = Color.black,
                Opacity = ResolveFillOpacity(node),
                Mode = ResolveFillMode(node)
            };
        }

        private bool TryBuildGradientFill(
            SvgDocumentModel documentModel,
            string fillValue,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel consumerNode,
            out IFill fill)
        {
            fill = null;
            if (!TryExtractFragmentId(fillValue, out string fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out SvgNodeModel gradientNode) ||
                !(string.Equals(gradientNode.TagName, "linearGradient", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(gradientNode.TagName, "radialGradient", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            List<GradientStop> stops = new();
            for (int index = 0; gradientNode.Children != null && index < gradientNode.Children.Count; index++)
            {
                SvgNodeId childId = gradientNode.Children[index];
                if ((documentModel == null || !documentModel.TryGetNode(childId, out SvgNodeModel stopNode)) ||
                    stopNode == null ||
                    !string.Equals(stopNode.TagName, "stop", StringComparison.OrdinalIgnoreCase) ||
                    !TryGetAttribute(stopNode.RawAttributes, "offset", out string offsetText) ||
                    !TryGetAttribute(stopNode.RawAttributes, "stop-color", out string stopColorText) ||
                    !TryParseColor(stopColorText, out Color stopColor))
                {
                    continue;
                }

                float stopOpacity = 1f;
                if (TryGetFloat(stopNode.RawAttributes, "stop-opacity", out float resolvedStopOpacity))
                    stopOpacity = Mathf.Clamp01(resolvedStopOpacity);
                stopColor.a *= stopOpacity;

                if (!TryParseOffset(offsetText, out float stopPercentage))
                    continue;

                stops.Add(new GradientStop
                {
                    Color = stopColor,
                    StopPercentage = stopPercentage
                });
            }

            if (stops.Count == 0)
                return false;

            GradientFillType gradientType = string.Equals(gradientNode.TagName, "radialGradient", StringComparison.OrdinalIgnoreCase)
                ? GradientFillType.Radial
                : GradientFillType.Linear;

            fill = new GradientFill
            {
                Type = gradientType,
                Stops = stops.ToArray(),
                Mode = ResolveFillMode(consumerNode),
                Opacity = ResolveFillOpacity(consumerNode),
                Addressing = AddressMode.Clamp
            };
            return true;
        }

        private PathProperties BuildPathProperties(SvgNodeModel node)
        {
            Stroke stroke = BuildStroke(node);
            return new PathProperties
            {
                Stroke = stroke,
                Head = ResolvePathEnding(node.RawAttributes, "stroke-linecap"),
                Tail = ResolvePathEnding(node.RawAttributes, "stroke-linecap"),
                Corners = ResolvePathCorner(node.RawAttributes, "stroke-linejoin")
            };
        }

        private Stroke BuildStroke(SvgNodeModel node)
        {
            if (!TryGetAttribute(node.RawAttributes, "stroke", out string strokeValue) ||
                string.Equals(strokeValue, "none", StringComparison.OrdinalIgnoreCase) ||
                strokeValue.Contains("url(", StringComparison.OrdinalIgnoreCase) ||
                !TryParseColor(strokeValue, out Color strokeColor))
            {
                return null;
            }

            float strokeWidth = 1f;
            TryGetFloat(node.RawAttributes, "stroke-width", out strokeWidth);
            float[] pattern = TryParseDasharray(node.RawAttributes, out float[] dashPattern)
                ? dashPattern
                : null;

            return new Stroke
            {
                Fill = new SolidFill
                {
                    Color = strokeColor,
                    Opacity = ResolveStrokeOpacity(node),
                    Mode = FillMode.NonZero
                },
                HalfThickness = Mathf.Max(0f, strokeWidth) * 0.5f,
                Pattern = pattern,
                PatternOffset = 0f,
                TippedCornerLimit = 4f
            };
        }

        private static Rect ResolveDocumentViewport(SvgDocumentModel documentModel)
        {
            if (TryParseViewBox(documentModel?.ViewBox, out Rect viewBox))
                return viewBox;

            float width = ParseLength(documentModel?.Width);
            float height = ParseLength(documentModel?.Height);
            return width > Mathf.Epsilon && height > Mathf.Epsilon
                ? new Rect(0f, 0f, width, height)
                : default;
        }

        private static bool TryParseViewBox(string viewBoxText, out Rect viewBox)
        {
            viewBox = default;
            if (string.IsNullOrWhiteSpace(viewBoxText))
                return false;

            string[] tokens = viewBoxText.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 4 ||
                !TryParseFloat(tokens[0], out float minX) ||
                !TryParseFloat(tokens[1], out float minY) ||
                !TryParseFloat(tokens[2], out float width) ||
                !TryParseFloat(tokens[3], out float height))
            {
                return false;
            }

            viewBox = new Rect(minX, minY, width, height);
            return true;
        }

        private static float ParseLength(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0f;

            int length = 0;
            while (length < value.Length && ("+-0123456789.eE".IndexOf(value[length]) >= 0))
                length++;

            return length > 0 && TryParseFloat(value.Substring(0, length), out float parsed)
                ? parsed
                : 0f;
        }

        private static bool TryParsePathContours(string pathData, out BezierContour[] contours)
        {
            contours = Array.Empty<BezierContour>();
            if (string.IsNullOrWhiteSpace(pathData))
                return false;

            List<BezierContour> builtContours = new();
            List<BezierSegment> currentSegments = null;
            Vector2 currentPoint = Vector2.zero;
            Vector2 subpathStart = Vector2.zero;
            Vector2 lastCubicControl = Vector2.zero;
            Vector2 lastQuadraticControl = Vector2.zero;
            bool hasLastCubicControl = false;
            bool hasLastQuadraticControl = false;
            char currentCommand = '\0';
            int index = 0;

            while (index < pathData.Length)
            {
                SkipPathSeparators(pathData, ref index);
                if (index >= pathData.Length)
                    break;

                char token = pathData[index];
                if (char.IsLetter(token))
                {
                    currentCommand = token;
                    index++;
                }
                else if (currentCommand == '\0')
                {
                    return false;
                }

                switch (currentCommand)
                {
                    case 'M':
                    case 'm':
                    {
                        if (!TryReadPoint(pathData, ref index, currentCommand == 'm', currentPoint, out Vector2 movePoint))
                            return false;

                        if (!TryFinalizeContour(currentSegments, closed: false, builtContours, ref currentPoint, subpathStart))
                            return false;

                        currentSegments = new List<BezierSegment>();
                        currentPoint = movePoint;
                        subpathStart = movePoint;
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        currentCommand = currentCommand == 'm' ? 'l' : 'L';

                        while (TryReadPoint(pathData, ref index, currentCommand == 'l', currentPoint, out Vector2 implicitLinePoint))
                        {
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, implicitLinePoint));
                            currentPoint = implicitLinePoint;
                        }
                        break;
                    }
                    case 'L':
                    case 'l':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                            return false;

                        while (TryReadPoint(pathData, ref index, currentCommand == 'l', currentPoint, out Vector2 linePoint))
                        {
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, linePoint));
                            currentPoint = linePoint;
                        }
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    case 'H':
                    case 'h':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                            return false;

                        while (TryReadFloatToken(pathData, ref index, out float xValue))
                        {
                            Vector2 nextPoint = new(
                                currentCommand == 'h' ? currentPoint.x + xValue : xValue,
                                currentPoint.y);
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, nextPoint));
                            currentPoint = nextPoint;
                        }
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    case 'V':
                    case 'v':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                            return false;

                        while (TryReadFloatToken(pathData, ref index, out float yValue))
                        {
                            Vector2 nextPoint = new(
                                currentPoint.x,
                                currentCommand == 'v' ? currentPoint.y + yValue : yValue);
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, nextPoint));
                            currentPoint = nextPoint;
                        }
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    case 'C':
                    case 'c':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                            return false;

                        while (TryReadCurvePoints(pathData, ref index, currentCommand == 'c', currentPoint, out Vector2 c1, out Vector2 c2, out Vector2 endPoint))
                        {
                            currentSegments.Add(new BezierSegment
                            {
                                P0 = currentPoint,
                                P1 = c1,
                                P2 = c2,
                                P3 = endPoint
                            });
                            currentPoint = endPoint;
                            lastCubicControl = c2;
                            hasLastCubicControl = true;
                            hasLastQuadraticControl = false;
                        }
                        break;
                    }
                    case 'S':
                    case 's':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                            return false;

                        while (TryReadSmoothCurvePoints(pathData, ref index, currentCommand == 's', currentPoint, out Vector2 c2, out Vector2 endPoint))
                        {
                            Vector2 c1 = hasLastCubicControl
                                ? (currentPoint * 2f) - lastCubicControl
                                : currentPoint;
                            currentSegments.Add(new BezierSegment
                            {
                                P0 = currentPoint,
                                P1 = c1,
                                P2 = c2,
                                P3 = endPoint
                            });
                            currentPoint = endPoint;
                            lastCubicControl = c2;
                            hasLastCubicControl = true;
                            hasLastQuadraticControl = false;
                        }
                        break;
                    }
                    case 'Q':
                    case 'q':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                            return false;

                        while (TryReadQuadraticPoints(pathData, ref index, currentCommand == 'q', currentPoint, out Vector2 controlPoint, out Vector2 endPoint))
                        {
                            currentSegments.Add(VectorUtils.QuadraticToCubic(currentPoint, controlPoint, endPoint));
                            currentPoint = endPoint;
                            lastQuadraticControl = controlPoint;
                            hasLastQuadraticControl = true;
                            hasLastCubicControl = false;
                        }
                        break;
                    }
                    case 'T':
                    case 't':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                            return false;

                        while (TryReadPoint(pathData, ref index, currentCommand == 't', currentPoint, out Vector2 endPoint))
                        {
                            Vector2 controlPoint = hasLastQuadraticControl
                                ? (currentPoint * 2f) - lastQuadraticControl
                                : currentPoint;
                            currentSegments.Add(VectorUtils.QuadraticToCubic(currentPoint, controlPoint, endPoint));
                            currentPoint = endPoint;
                            lastQuadraticControl = controlPoint;
                            hasLastQuadraticControl = true;
                            hasLastCubicControl = false;
                        }
                        break;
                    }
                    case 'Z':
                    case 'z':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                            return false;

                        if ((currentPoint - subpathStart).sqrMagnitude > Mathf.Epsilon)
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, subpathStart));

                        if (!TryFinalizeContour(currentSegments, closed: true, builtContours, ref currentPoint, subpathStart))
                            return false;

                        currentSegments = null;
                        currentPoint = subpathStart;
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    default:
                        return false;
                }
            }

            if (!TryFinalizeContour(currentSegments, closed: false, builtContours, ref currentPoint, subpathStart))
                return false;

            contours = builtContours.ToArray();
            return contours.Length > 0;
        }

        private static bool TryEnsurePathStarted(
            List<BezierSegment> currentSegments,
            Vector2 subpathStart,
            ref Vector2 currentPoint)
        {
            if (currentSegments == null)
                return false;

            if (currentSegments.Count == 0)
                currentPoint = subpathStart;

            return true;
        }

        private static bool TryFinalizeContour(
            List<BezierSegment> currentSegments,
            bool closed,
            List<BezierContour> builtContours,
            ref Vector2 currentPoint,
            Vector2 subpathStart)
        {
            if (currentSegments == null)
                return true;

            if (currentSegments.Count == 0)
            {
                currentPoint = subpathStart;
                return true;
            }

            builtContours.Add(new BezierContour
            {
                Segments = VectorUtils.BezierSegmentsToPath(currentSegments.ToArray()),
                Closed = closed
            });
            currentPoint = subpathStart;
            return true;
        }

        private static bool TryReadPoint(
            string pathData,
            ref int index,
            bool relative,
            Vector2 origin,
            out Vector2 point)
        {
            point = Vector2.zero;
            if (!TryReadFloatToken(pathData, ref index, out float x) ||
                !TryReadFloatToken(pathData, ref index, out float y))
            {
                return false;
            }

            point = relative ? origin + new Vector2(x, y) : new Vector2(x, y);
            return true;
        }

        private static bool TryReadCurvePoints(
            string pathData,
            ref int index,
            bool relative,
            Vector2 origin,
            out Vector2 c1,
            out Vector2 c2,
            out Vector2 endPoint)
        {
            c1 = Vector2.zero;
            c2 = Vector2.zero;
            endPoint = Vector2.zero;

            if (!TryReadPoint(pathData, ref index, relative, origin, out c1) ||
                !TryReadPoint(pathData, ref index, relative, origin, out c2) ||
                !TryReadPoint(pathData, ref index, relative, origin, out endPoint))
            {
                return false;
            }

            return true;
        }

        private static bool TryReadSmoothCurvePoints(
            string pathData,
            ref int index,
            bool relative,
            Vector2 origin,
            out Vector2 c2,
            out Vector2 endPoint)
        {
            c2 = Vector2.zero;
            endPoint = Vector2.zero;

            if (!TryReadPoint(pathData, ref index, relative, origin, out c2) ||
                !TryReadPoint(pathData, ref index, relative, origin, out endPoint))
            {
                return false;
            }

            return true;
        }

        private static bool TryReadQuadraticPoints(
            string pathData,
            ref int index,
            bool relative,
            Vector2 origin,
            out Vector2 controlPoint,
            out Vector2 endPoint)
        {
            controlPoint = Vector2.zero;
            endPoint = Vector2.zero;

            if (!TryReadPoint(pathData, ref index, relative, origin, out controlPoint) ||
                !TryReadPoint(pathData, ref index, relative, origin, out endPoint))
            {
                return false;
            }

            return true;
        }

        private static void SkipPathSeparators(string pathData, ref int index)
        {
            while (index < pathData.Length &&
                   (char.IsWhiteSpace(pathData[index]) || pathData[index] == ','))
            {
                index++;
            }
        }

        private static bool TryReadFloatToken(string text, ref int index, out float value)
        {
            value = 0f;
            SkipPathSeparators(text, ref index);
            if (index >= text.Length)
                return false;

            int start = index;
            bool hasExponent = false;
            bool hasDecimal = false;

            if (text[index] == '+' || text[index] == '-')
                index++;

            while (index < text.Length)
            {
                char ch = text[index];
                if (char.IsDigit(ch))
                {
                    index++;
                    continue;
                }

                if (ch == '.' && !hasDecimal)
                {
                    hasDecimal = true;
                    index++;
                    continue;
                }

                if ((ch == 'e' || ch == 'E') && !hasExponent)
                {
                    hasExponent = true;
                    index++;
                    if (index < text.Length && (text[index] == '+' || text[index] == '-'))
                        index++;
                    continue;
                }

                break;
            }

            return index > start &&
                   TryParseFloat(text.Substring(start, index - start), out value);
        }

        private static bool TryParsePoints(string pointsText, out List<Vector2> points)
        {
            points = new List<Vector2>();
            if (string.IsNullOrWhiteSpace(pointsText))
                return false;

            int index = 0;
            while (index < pointsText.Length)
            {
                if (!TryReadFloatToken(pointsText, ref index, out float x))
                    break;
                if (!TryReadFloatToken(pointsText, ref index, out float y))
                    return false;

                points.Add(new Vector2(x, y));
            }

            return points.Count >= 2;
        }

        private static Matrix2D ParseTransform(IReadOnlyDictionary<string, string> attributes)
        {
            if (!TryGetAttribute(attributes, "transform", out string transformText) ||
                string.IsNullOrWhiteSpace(transformText))
            {
                return Matrix2D.identity;
            }

            if (!TryParseTransformMatrix(transformText, out Matrix2D matrix))
                return Matrix2D.identity;

            return matrix;
        }

        private static bool TryParseTransformMatrix(string transformText, out Matrix2D matrix)
        {
            matrix = Matrix2D.identity;
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
                    return false;

                string command = transformText.Substring(nameStart, index - nameStart).ToLowerInvariant();
                while (index < transformText.Length && char.IsWhiteSpace(transformText[index]))
                    index++;
                if (index >= transformText.Length || transformText[index] != '(')
                    return false;

                index++;
                int closeIndex = transformText.IndexOf(')', index);
                if (closeIndex < 0)
                    return false;

                if (!TryParseTransformArguments(transformText.Substring(index, closeIndex - index), out List<float> args))
                    return false;

                index = closeIndex + 1;
                if (!TryBuildCommandMatrix(command, args, out Matrix2D commandMatrix))
                    return false;

                matrix = matrix * commandMatrix;
            }

            return true;
        }

        private static bool TryParseTransformArguments(string argsText, out List<float> args)
        {
            args = new List<float>();
            string[] tokens = argsText.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < tokens.Length; index++)
            {
                if (!TryParseFloat(tokens[index], out float value))
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
                    if (args.Count is < 1 or > 2)
                        return false;
                    matrix = Matrix2D.Translate(new Vector2(args[0], args.Count > 1 ? args[1] : 0f));
                    return true;
                case "scale":
                    if (args.Count is < 1 or > 2)
                        return false;
                    matrix = Matrix2D.Scale(new Vector2(args[0], args.Count > 1 ? args[1] : args[0]));
                    return true;
                case "rotate":
                    if (args.Count != 1 && args.Count != 3)
                        return false;
                    matrix = BuildRotationMatrix(args[0], args.Count == 3 ? new Vector2(args[1], args[2]) : Vector2.zero, args.Count == 3);
                    return true;
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

        private static Matrix2D BuildRotationMatrix(float degrees, Vector2 pivot, bool aroundPivot)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            Matrix2D rotation = new(
                new Vector2(cos, sin),
                new Vector2(-sin, cos),
                Vector2.zero);

            if (!aroundPivot)
                return rotation;

            return Matrix2D.Translate(pivot) * rotation * Matrix2D.Translate(-pivot);
        }

        private static bool TryParseDasharray(IReadOnlyDictionary<string, string> attributes, out float[] pattern)
        {
            pattern = null;
            if (!TryGetAttribute(attributes, "stroke-dasharray", out string dasharray) ||
                string.IsNullOrWhiteSpace(dasharray))
            {
                return false;
            }

            string[] tokens = dasharray.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<float> values = new(tokens.Length);
            for (int index = 0; index < tokens.Length; index++)
            {
                if (!TryParseFloat(tokens[index], out float value))
                    return false;

                values.Add(Mathf.Max(0f, value));
            }

            pattern = values.Count > 0 ? values.ToArray() : null;
            return pattern != null;
        }

        private static PathEnding ResolvePathEnding(IReadOnlyDictionary<string, string> attributes, string attributeName)
        {
            if (!TryGetAttribute(attributes, attributeName, out string value))
                return PathEnding.Chop;

            return value switch
            {
                "round" => PathEnding.Round,
                "square" => PathEnding.Square,
                _ => PathEnding.Chop
            };
        }

        private static PathCorner ResolvePathCorner(IReadOnlyDictionary<string, string> attributes, string attributeName)
        {
            if (!TryGetAttribute(attributes, attributeName, out string value))
                return PathCorner.Tipped;

            return value switch
            {
                "round" => PathCorner.Round,
                "bevel" => PathCorner.Beveled,
                _ => PathCorner.Tipped
            };
        }

        private static FillMode ResolveFillMode(SvgNodeModel node)
        {
            return TryGetAttribute(node?.RawAttributes, "fill-rule", out string fillRule) &&
                   string.Equals(fillRule, "evenodd", StringComparison.OrdinalIgnoreCase)
                ? FillMode.OddEven
                : FillMode.NonZero;
        }

        private static float ResolveFillOpacity(SvgNodeModel node)
        {
            return ResolveOpacity(node?.RawAttributes, "fill-opacity");
        }

        private static float ResolveStrokeOpacity(SvgNodeModel node)
        {
            return ResolveOpacity(node?.RawAttributes, "stroke-opacity");
        }

        private static float ResolveOpacity(IReadOnlyDictionary<string, string> attributes, string attributeName)
        {
            float value = 1f;
            if (TryGetFloat(attributes, attributeName, out float resolved))
                value *= Mathf.Clamp01(resolved);

            return Mathf.Clamp01(value);
        }

        private static bool TryGetOpacity(IReadOnlyDictionary<string, string> attributes, out float opacity)
        {
            opacity = 1f;
            return TryGetFloat(attributes, "opacity", out opacity);
        }

        private static bool TryResolveUseReference(
            SvgNodeModel useNode,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            out SvgNodeModel referencedNode)
        {
            referencedNode = null;
            if (useNode?.References == null || nodesByXmlId == null)
                return false;

            for (int index = 0; index < useNode.References.Count; index++)
            {
                SvgNodeReference reference = useNode.References[index];
                if (string.IsNullOrWhiteSpace(reference?.FragmentId))
                    continue;

                if (nodesByXmlId.TryGetValue(reference.FragmentId, out referencedNode))
                    return referencedNode != null;
            }

            return false;
        }

        private static Dictionary<string, SvgNodeModel> BuildNodeLookupByXmlId(SvgDocumentModel documentModel)
        {
            Dictionary<string, SvgNodeModel> lookup = new(StringComparer.Ordinal);
            if (documentModel?.NodeIdsByXmlId == null)
                return lookup;

            foreach (KeyValuePair<string, SvgNodeId> pair in documentModel.NodeIdsByXmlId)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) ||
                    !documentModel.TryGetNode(pair.Value, out SvgNodeModel node) ||
                    node == null)
                {
                    continue;
                }

                lookup[pair.Key] = node;
            }

            return lookup;
        }

        private static bool TryFindNodeByLegacyElementKey(SvgDocumentModel documentModel, string elementKey, out SvgNodeModel node)
        {
            node = null;
            if (documentModel?.NodeOrder == null || string.IsNullOrWhiteSpace(elementKey))
                return false;

            for (int index = 0; index < documentModel.NodeOrder.Count; index++)
            {
                SvgNodeId nodeId = documentModel.NodeOrder[index];
                if (!documentModel.TryGetNode(nodeId, out SvgNodeModel candidate) || candidate == null)
                    continue;

                if (string.Equals(candidate.LegacyElementKey, elementKey, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtractFragmentId(string fillValue, out string fragmentId)
        {
            fragmentId = string.Empty;
            if (string.IsNullOrWhiteSpace(fillValue))
                return false;

            int hashIndex = fillValue.IndexOf('#');
            int closeIndex = fillValue.IndexOf(')', hashIndex + 1);
            if (hashIndex < 0 || closeIndex <= hashIndex)
                return false;

            fragmentId = fillValue.Substring(hashIndex + 1, closeIndex - hashIndex - 1).Trim();
            return !string.IsNullOrWhiteSpace(fragmentId);
        }

        private static bool TryParseOffset(string offsetText, out float offset)
        {
            offset = 0f;
            if (string.IsNullOrWhiteSpace(offsetText))
                return false;

            string normalized = offsetText.Trim();
            if (normalized.EndsWith("%", StringComparison.Ordinal))
            {
                return TryParseFloat(normalized[..^1], out float percent) &&
                       TryNormalizeStop(percent / 100f, out offset);
            }

            return TryParseFloat(normalized, out float raw) &&
                   TryNormalizeStop(raw, out offset);
        }

        private static bool TryNormalizeStop(float value, out float offset)
        {
            offset = Mathf.Clamp01(value);
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsHidden(SvgNodeModel node)
        {
            return TryGetAttribute(node?.RawAttributes, "display", out string display) &&
                   string.Equals(display, "none", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldIncludeMaskNode(SvgNodeModel node)
        {
            if (!TryGetAttribute(node?.RawAttributes, "fill", out string fillValue))
                return false;

            if (!TryParseColor(fillValue, out Color color))
                return false;

            float opacity = ResolveOpacity(node?.RawAttributes, "fill-opacity");
            float luminance = (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);
            return opacity > 0.5f && luminance > 0.5f;
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
                   TryParseFloat(text, out value);
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseColor(string text, out Color color)
        {
            color = default;
            string normalized = text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            if (!normalized.StartsWith("#", StringComparison.Ordinal) &&
                normalized.Length is 3 or 4 or 6 or 8)
            {
                normalized = $"#{normalized}";
            }

            return ColorUtility.TryParseHtmlString(normalized, out color);
        }
    }
}
