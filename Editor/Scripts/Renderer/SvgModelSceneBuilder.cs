using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgModelSceneBuilder
    {
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
                case "path":
                    return TryAddPathShape(documentModel, node, nodesByXmlId, sceneNode, out error);
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
                    Segments = new[]
                    {
                        CreateLinearSegment(new Vector2(x1, y1)),
                        CreateLinearSegment(new Vector2(x2, y2))
                    },
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
                !string.Equals(gradientNode.TagName, "linearGradient", StringComparison.OrdinalIgnoreCase))
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

            fill = new GradientFill
            {
                Type = GradientFillType.Linear,
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
            List<BezierPathSegment> currentSegments = null;
            Vector2 currentPoint = Vector2.zero;
            Vector2 subpathStart = Vector2.zero;
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

                        if (currentSegments != null && currentSegments.Count > 0)
                        {
                            builtContours.Add(new BezierContour
                            {
                                Segments = currentSegments.ToArray(),
                                Closed = false
                            });
                        }

                        currentSegments = new List<BezierPathSegment>
                        {
                            CreateLinearSegment(movePoint)
                        };
                        currentPoint = movePoint;
                        subpathStart = movePoint;
                        currentCommand = currentCommand == 'm' ? 'l' : 'L';
                        break;
                    }
                    case 'L':
                    case 'l':
                    {
                        if (!TryEnsurePathStarted(currentSegments))
                            return false;

                        if (!TryReadPoint(pathData, ref index, currentCommand == 'l', currentPoint, out Vector2 linePoint))
                            return false;

                        currentSegments.Add(CreateLinearSegment(linePoint));
                        currentPoint = linePoint;
                        break;
                    }
                    case 'H':
                    case 'h':
                    {
                        if (!TryEnsurePathStarted(currentSegments))
                            return false;

                        if (!TryReadFloatToken(pathData, ref index, out float xValue))
                            return false;

                        currentPoint = new Vector2(
                            currentCommand == 'h' ? currentPoint.x + xValue : xValue,
                            currentPoint.y);
                        currentSegments.Add(CreateLinearSegment(currentPoint));
                        break;
                    }
                    case 'V':
                    case 'v':
                    {
                        if (!TryEnsurePathStarted(currentSegments))
                            return false;

                        if (!TryReadFloatToken(pathData, ref index, out float yValue))
                            return false;

                        currentPoint = new Vector2(
                            currentPoint.x,
                            currentCommand == 'v' ? currentPoint.y + yValue : yValue);
                        currentSegments.Add(CreateLinearSegment(currentPoint));
                        break;
                    }
                    case 'Z':
                    case 'z':
                    {
                        if (!TryEnsurePathStarted(currentSegments))
                            return false;

                        builtContours.Add(new BezierContour
                        {
                            Segments = currentSegments.ToArray(),
                            Closed = true
                        });
                        currentSegments = null;
                        currentPoint = subpathStart;
                        break;
                    }
                    default:
                        return false;
                }
            }

            if (currentSegments != null && currentSegments.Count > 0)
            {
                builtContours.Add(new BezierContour
                {
                    Segments = currentSegments.ToArray(),
                    Closed = false
                });
            }

            contours = builtContours.ToArray();
            return contours.Length > 0;
        }

        private static bool TryEnsurePathStarted(List<BezierPathSegment> currentSegments)
        {
            return currentSegments != null && currentSegments.Count > 0;
        }

        private static BezierPathSegment CreateLinearSegment(Vector2 point)
        {
            return new BezierPathSegment
            {
                P0 = point,
                P1 = point,
                P2 = point
            };
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
