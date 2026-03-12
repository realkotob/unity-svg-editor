using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgShapeStyleBuilder
    {
        public Shape CreateStyledShape(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            bool allowDefaultFill = true)
        {
            var shape = new Shape
            {
                Fill = BuildFill(documentModel, node, nodesByXmlId, allowDefaultFill),
                PathProps = BuildPathProperties(documentModel, node),
                FillTransform = Matrix2D.identity
            };
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
                {
                    return null;
                }

                if (fillValue.Contains("url(", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryBuildGradientFill(documentModel, fillValue, nodesByXmlId, node, out var gradientFill))
                    {
                        return gradientFill;
                    }

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
            {
                return null;
            }

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

            var stops = new List<GradientStop>();
            for (var index = 0; gradientNode.Children != null && index < gradientNode.Children.Count; index++)
            {
                var childId = gradientNode.Children[index];
                if ((documentModel == null || !documentModel.TryGetNode(childId, out var stopNode)) ||
                    stopNode == null ||
                    !string.Equals(stopNode.TagName, "stop", StringComparison.OrdinalIgnoreCase) ||
                    !SvgAttributeUtility.TryGetAttribute(stopNode.RawAttributes, "offset", out var offsetText) ||
                    !SvgAttributeUtility.TryGetAttribute(stopNode.RawAttributes, "stop-color", out var stopColorText) ||
                    !SvgAttributeUtility.TryParseColor(stopColorText, out var stopColor))
                {
                    continue;
                }

                var stopOpacity = 1f;
                if (SvgAttributeUtility.TryGetFloat(stopNode.RawAttributes, "stop-opacity", out var resolvedStopOpacity))
                {
                    stopOpacity = Mathf.Clamp01(resolvedStopOpacity);
                }

                stopColor.a *= stopOpacity;

                if (!TryParseOffset(offsetText, out var stopPercentage))
                {
                    continue;
                }

                stops.Add(new GradientStop
                {
                    Color = stopColor,
                    StopPercentage = stopPercentage
                });
            }

            if (stops.Count == 0)
            {
                return false;
            }

            var gradientType = string.Equals(gradientNode.TagName, "radialGradient", StringComparison.OrdinalIgnoreCase)
                ? GradientFillType.Radial
                : GradientFillType.Linear;

            fill = new GradientFill
            {
                Type = gradientType,
                Stops = stops.ToArray(),
                Mode = SvgInheritedAttributeResolver.ResolveFillMode(documentModel, consumerNode),
                Opacity = SvgInheritedAttributeResolver.ResolveFillOpacity(documentModel, consumerNode),
                Addressing = AddressMode.Clamp
            };
            return true;
        }

        private static PathProperties BuildPathProperties(SvgDocumentModel documentModel, SvgNodeModel node)
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

        private static Stroke BuildStroke(SvgDocumentModel documentModel, SvgNodeModel node)
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
            float[] pattern = TryParseDasharray(documentModel, node, out var dashPattern)
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
                {
                    return false;
                }

                values.Add(Mathf.Max(0f, value));
            }

            pattern = values.Count > 0 ? values.ToArray() : null;
            return pattern != null;
        }

        private static bool TryParseOffset(string offsetText, out float offset)
        {
            offset = 0f;
            if (string.IsNullOrWhiteSpace(offsetText))
            {
                return false;
            }

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
    }
}
