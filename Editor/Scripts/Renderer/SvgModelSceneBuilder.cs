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

            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(documentModel, elementKey, out var node) || node == null)
                return true;

            var nodesByXmlId = SvgNodeLookupUtility.BuildNodeLookupByXmlId(documentModel);
            var resolved = new List<CanvasDefinitionOverlayScene>();

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

            var nodesByXmlId = SvgNodeLookupUtility.BuildNodeLookupByXmlId(documentModel);

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
                Transform = SvgTransformParser.Parse(node.RawAttributes)
            };

            if (!TryAttachMask(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryAttachClipper(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryBuildShapes(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryBuildChildren(documentModel, nodesByXmlId, node, sceneNode, result, out error))
                return false;

            if (SvgAttributeUtility.TryGetOpacity(node.RawAttributes, out var opacity) && !Mathf.Approximately(opacity, 1f))
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
            if (!SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, "mask", out var maskValue))
                return true;

            if (!SvgNodeLookupUtility.TryExtractFragmentId(maskValue, out var fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out var maskNode) ||
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
            if (!SvgAttributeUtility.TryGetAttribute(node.RawAttributes, attributeName, out var rawValue) ||
                !SvgNodeLookupUtility.TryExtractFragmentId(rawValue, out var fragmentId) ||
                !nodesByXmlId.TryGetValue(fragmentId, out var referenceNode) ||
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
            if (!SvgAttributeUtility.TryGetAttribute(node.RawAttributes, "d", out var pathData))
            {
                error = $"Path '{node.LegacyElementKey}' is missing geometry.";
                return false;
            }

            if (!SvgPathGeometryParser.TryParsePathContours(pathData, out BezierContour[] contours))
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
            if (!SvgAttributeUtility.TryGetAttribute(node.RawAttributes, "points", out var pointsText) ||
                !SvgPathGeometryParser.TryParsePoints(pointsText, out List<Vector2> points) ||
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
            if (!SvgNodeLookupUtility.TryResolveUseReference(useNode, nodesByXmlId, out var referencedNode))
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
                Transform = SvgTransformParser.Parse(node.RawAttributes)
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
            if (!SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, "clip-path", out var clipPathValue))
                return true;

            if (!SvgNodeLookupUtility.TryExtractFragmentId(clipPathValue, out var fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out var clipNode) ||
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
                Transform = SvgTransformParser.Parse(clipNode?.RawAttributes)
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
                    Transform = SvgTransformParser.Parse(childNode.RawAttributes)
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
                Transform = SvgTransformParser.Parse(maskNode?.RawAttributes)
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
                    Transform = SvgTransformParser.Parse(childNode.RawAttributes)
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
            shape.PathProps = BuildPathProperties(documentModel, node);
            shape.FillTransform = Matrix2D.identity;
            return shape;
        }

        private IFill BuildFill(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            bool allowDefaultFill)
        {
            if (SvgInheritedAttributeResolver.TryGetInheritedAttribute(documentModel, node, "fill", out var fillValue))
            {
                if (string.Equals(fillValue, "none", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (fillValue.Contains("url(", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryBuildGradientFill(documentModel, fillValue, nodesByXmlId, node, out IFill gradientFill))
                        return gradientFill;

                    return null;
                }

                if (SvgAttributeUtility.TryParseColor(fillValue, out var color))
                {
                    return new SolidFill
                    {
                        Color = color,
                        Opacity = SvgInheritedAttributeResolver.ResolveFillOpacity(documentModel, node),
                        Mode = SvgInheritedAttributeResolver.ResolveFillMode(documentModel, node)
                    };
                }
            }

            if (!allowDefaultFill)
                return null;

            return new SolidFill
            {
                Color = Color.black,
                Opacity = SvgInheritedAttributeResolver.ResolveFillOpacity(documentModel, node),
                Mode = SvgInheritedAttributeResolver.ResolveFillMode(documentModel, node)
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
            if (!SvgNodeLookupUtility.TryExtractFragmentId(fillValue, out var fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out var gradientNode) ||
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
                    !SvgAttributeUtility.TryGetAttribute(stopNode.RawAttributes, "offset", out var offsetText) ||
                    !SvgAttributeUtility.TryGetAttribute(stopNode.RawAttributes, "stop-color", out var stopColorText) ||
                    !SvgAttributeUtility.TryParseColor(stopColorText, out var stopColor))
                {
                    continue;
                }

                float stopOpacity = 1f;
                if (SvgAttributeUtility.TryGetFloat(stopNode.RawAttributes, "stop-opacity", out var resolvedStopOpacity))
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
                Mode = ResolveFillMode(documentModel, consumerNode),
                Opacity = SvgInheritedAttributeResolver.ResolveFillOpacity(documentModel, consumerNode),
                Addressing = AddressMode.Clamp
            };
            return true;
        }

        private PathProperties BuildPathProperties(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            var stroke = BuildStroke(documentModel, node);
            return new PathProperties
            {
                Stroke = stroke,
                Head = SvgInheritedAttributeResolver.ResolvePathEnding(documentModel, node, "stroke-linecap"),
                Tail = SvgInheritedAttributeResolver.ResolvePathEnding(documentModel, node, "stroke-linecap"),
                Corners = SvgInheritedAttributeResolver.ResolvePathCorner(documentModel, node, "stroke-linejoin")
            };
        }

        private Stroke BuildStroke(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            if (!SvgInheritedAttributeResolver.TryGetInheritedAttribute(documentModel, node, "stroke", out var strokeValue) ||
                string.Equals(strokeValue, "none", StringComparison.OrdinalIgnoreCase) ||
                strokeValue.Contains("url(", StringComparison.OrdinalIgnoreCase) ||
                !SvgAttributeUtility.TryParseColor(strokeValue, out var strokeColor))
            {
                return null;
            }

            var strokeWidth = 1f;
            SvgInheritedAttributeResolver.TryGetInheritedFloat(documentModel, node, "stroke-width", out strokeWidth);
            float[] pattern = TryParseDasharray(documentModel, node, out float[] dashPattern)
                ? dashPattern
                : null;

            return new Stroke
            {
                Fill = new SolidFill
                {
                    Color = strokeColor,
                    Opacity = SvgInheritedAttributeResolver.ResolveStrokeOpacity(documentModel, node),
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
                !SvgAttributeUtility.TryParseFloat(tokens[0], out var minX) ||
                !SvgAttributeUtility.TryParseFloat(tokens[1], out var minY) ||
                !SvgAttributeUtility.TryParseFloat(tokens[2], out var width) ||
                !SvgAttributeUtility.TryParseFloat(tokens[3], out var height))
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

            return length > 0 && SvgAttributeUtility.TryParseFloat(value.Substring(0, length), out var parsed)
                ? parsed
                : 0f;
        }


        private static bool TryParseDasharray(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            out float[] pattern)
        {
            pattern = null;
            if (!SvgInheritedAttributeResolver.TryGetInheritedAttribute(documentModel, node, "stroke-dasharray", out var dasharray) ||
                string.IsNullOrWhiteSpace(dasharray))
            {
                return false;
            }

            var tokens = dasharray.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var values = new List<float>(tokens.Length);
            for (var index = 0; index < tokens.Length; index++)
            {
                if (!SvgAttributeUtility.TryParseFloat(tokens[index], out var value))
                    return false;

                values.Add(Mathf.Max(0f, value));
            }

            pattern = values.Count > 0 ? values.ToArray() : null;
            return pattern != null;
        }

        private static bool TryParseOffset(string offsetText, out float offset)
        {
            offset = 0f;
            if (string.IsNullOrWhiteSpace(offsetText))
                return false;

            var normalized = offsetText.Trim();
            if (normalized.EndsWith("%", StringComparison.Ordinal))
            {
                return SvgAttributeUtility.TryParseFloat(normalized[..^1], out var percent) &&
                       TryNormalizeStop(percent / 100f, out offset);
            }

            return SvgAttributeUtility.TryParseFloat(normalized, out var raw) &&
                   TryNormalizeStop(raw, out offset);
        }

        private static bool TryNormalizeStop(float value, out float offset)
        {
            offset = Mathf.Clamp01(value);
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsHidden(SvgNodeModel node)
        {
            return SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, "display", out var display) &&
                   string.Equals(display, "none", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldIncludeMaskNode(SvgNodeModel node)
        {
            if (!SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, "fill", out var fillValue))
                return false;

            if (!SvgAttributeUtility.TryParseColor(fillValue, out var color))
                return false;

            var opacity = 1f;
            if (SvgAttributeUtility.TryGetFloat(node?.RawAttributes, "fill-opacity", out var resolvedOpacity))
                opacity = Mathf.Clamp01(resolvedOpacity);
            var luminance = (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);
            return opacity > 0.5f && luminance > 0.5f;
        }

        private static FillMode ResolveFillMode(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            return SvgInheritedAttributeResolver.ResolveFillMode(documentModel, node);
        }

        private static bool TryGetFloat(IReadOnlyDictionary<string, string> attributes, string name, out float value)
        {
            return SvgAttributeUtility.TryGetFloat(attributes, name, out value);
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return SvgAttributeUtility.TryParseFloat(text, out value);
        }

    }
}
