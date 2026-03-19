using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Structure.Lookup;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.Core.Preview.Rendering
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
            if (InheritedAttributeResolver.TryGetInheritedAttribute(documentModel, node, SvgAttributeName.FILL, out var fillValue))
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

                if (AttributeUtility.TryParseColor(fillValue, out var color))
                {
                    return new SolidFill
                    {
                        Color = color,
                        Opacity = InheritedAttributeResolver.ResolveFillOpacity(documentModel, node),
                        Mode = InheritedAttributeResolver.ResolveFillMode(documentModel, node)
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
                Opacity = InheritedAttributeResolver.ResolveFillOpacity(documentModel, node),
                Mode = InheritedAttributeResolver.ResolveFillMode(documentModel, node)
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
            if (!NodeLookup.TryExtractFragmentId(fillValue, out var fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out var gradientNode) ||
                !(string.Equals(gradientNode.TagName, SvgTagName.LINEAR_GRADIENT, StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(gradientNode.TagName, SvgTagName.RADIAL_GRADIENT, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var stops = new List<GradientStop>();
            for (var index = 0; gradientNode.Children != null && index < gradientNode.Children.Count; index++)
            {
                var childId = gradientNode.Children[index];
                if ((documentModel == null || !documentModel.TryGetNode(childId, out var stopNode)) ||
                    stopNode == null ||
                    !string.Equals(stopNode.TagName, SvgTagName.STOP, StringComparison.OrdinalIgnoreCase) ||
                    !AttributeUtility.TryGetAttribute(stopNode.RawAttributes, SvgAttributeName.OFFSET, out var offsetText) ||
                    !AttributeUtility.TryGetAttribute(stopNode.RawAttributes, SvgAttributeName.STOP_COLOR, out var stopColorText) ||
                    !AttributeUtility.TryParseColor(stopColorText, out var stopColor))
                {
                    continue;
                }

                var stopOpacity = 1f;
                if (AttributeUtility.TryGetFloat(stopNode.RawAttributes, SvgAttributeName.STOP_OPACITY, out var resolvedStopOpacity))
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

            var gradientType = string.Equals(gradientNode.TagName, SvgTagName.RADIAL_GRADIENT, StringComparison.OrdinalIgnoreCase)
                ? GradientFillType.Radial
                : GradientFillType.Linear;

            fill = new GradientFill
            {
                Type = gradientType,
                Stops = stops.ToArray(),
                Mode = InheritedAttributeResolver.ResolveFillMode(documentModel, consumerNode),
                Opacity = InheritedAttributeResolver.ResolveFillOpacity(documentModel, consumerNode),
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
                Head = InheritedAttributeResolver.ResolvePathEnding(documentModel, node, SvgAttributeName.STROKE_LINECAP),
                Tail = InheritedAttributeResolver.ResolvePathEnding(documentModel, node, SvgAttributeName.STROKE_LINECAP),
                Corners = InheritedAttributeResolver.ResolvePathCorner(documentModel, node, SvgAttributeName.STROKE_LINEJOIN)
            };
        }

        private static Stroke BuildStroke(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            if (!InheritedAttributeResolver.TryGetInheritedAttribute(documentModel, node, SvgAttributeName.STROKE, out var strokeValue) ||
                string.Equals(strokeValue, "none", StringComparison.OrdinalIgnoreCase) ||
                strokeValue.Contains("url(", StringComparison.OrdinalIgnoreCase) ||
                !AttributeUtility.TryParseColor(strokeValue, out var strokeColor))
            {
                return null;
            }

            var strokeWidth = 1f;
            InheritedAttributeResolver.TryGetInheritedFloat(documentModel, node, SvgAttributeName.STROKE_WIDTH, out strokeWidth);
            float[] pattern = TryParseDasharray(documentModel, node, out var dashPattern)
                ? dashPattern
                : null;

            return new Stroke
            {
                Fill = new SolidFill
                {
                    Color = strokeColor,
                    Opacity = InheritedAttributeResolver.ResolveStrokeOpacity(documentModel, node),
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
            if (!InheritedAttributeResolver.TryGetInheritedAttribute(documentModel, node, SvgAttributeName.STROKE_DASHARRAY, out var dasharray) ||
                string.IsNullOrWhiteSpace(dasharray))
            {
                return false;
            }

            var tokens = dasharray.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var values = new List<float>(tokens.Length);
            for (var index = 0; index < tokens.Length; index++)
            {
                if (!AttributeUtility.TryParseFloat(tokens[index], out var value))
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
                return AttributeUtility.TryParseFloat(normalized[..^1], out var percent) &&
                       TryNormalizeStop(percent / 100f, out offset);
            }

            return AttributeUtility.TryParseFloat(normalized, out var raw) &&
                   TryNormalizeStop(raw, out offset);
        }

        private static bool TryNormalizeStop(float value, out float offset)
        {
            offset = Mathf.Clamp01(value);
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
