using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using SvgEditor.Core.Shared;

namespace SvgEditor.Core.Svg.Structure.Xml
{
    internal sealed class MaskRectRewriteCandidate
    {
        private static readonly HashSet<string> AllowedMaskAttributes = new(StringComparer.Ordinal)
        {
            "id",
            "fill"
        };

        private static readonly HashSet<string> AllowedRectRewriteAttributes = new(StringComparer.Ordinal)
        {
            "x",
            "y",
            "width",
            "height",
            "rx",
            "ry",
            "stroke",
            "stroke-width",
            "mask",
            "id",
            "transform",
            "opacity",
            "display",
            "visibility",
            "class"
        };

        private static readonly string[] CopiedAttributes =
        {
            "id",
            "transform",
            "opacity",
            "display",
            "visibility",
            "class"
        };

        private readonly XmlDocument _document;
        private readonly XmlElement _root;
        private readonly XmlElement _maskElement;
        private readonly XmlElement _strokeRect;
        private readonly RectShape _maskShape;
        private readonly string _strokeColor;
        private readonly float _strokeWidth;

        private MaskRectRewriteCandidate(
            XmlDocument document,
            XmlElement root,
            XmlElement maskElement,
            XmlElement strokeRect,
            RectShape maskShape,
            string strokeColor,
            float strokeWidth)
        {
            _document = document;
            _root = root;
            _maskElement = maskElement;
            _strokeRect = strokeRect;
            _maskShape = maskShape;
            _strokeColor = strokeColor;
            _strokeWidth = strokeWidth;
        }

        public static bool TryCreate(
            XmlDocument document,
            XmlElement root,
            XmlElement maskElement,
            out MaskRectRewriteCandidate candidate)
        {
            candidate = null;
            if (document == null ||
                root == null ||
                maskElement == null ||
                !XmlUtility.TryGetId(maskElement, out string maskId))
            {
                return false;
            }

            List<XmlElement> maskChildren = XmlUtility.GetElementChildren(maskElement);
            if (maskChildren.Count != 1)
            {
                return false;
            }

            XmlElement maskRect = maskChildren[0];
            if (!string.Equals(maskRect.LocalName, SvgTagName.RECT, StringComparison.OrdinalIgnoreCase) ||
                !TryParseRect(maskRect, out RectShape maskShape))
            {
                return false;
            }

            List<XmlElement> references = FindMaskReferences(root, maskElement, maskId);
            if (references.Count != 1)
            {
                return false;
            }

            XmlElement strokeRect = references[0];
            if (!TryValidateStrokeRect(strokeRect, maskShape, out string strokeColor, out float strokeWidth) ||
                HasUnsupportedAttributes(maskElement, AllowedMaskAttributes))
            {
                return false;
            }

            candidate = new MaskRectRewriteCandidate(
                document,
                root,
                maskElement,
                strokeRect,
                maskShape,
                strokeColor,
                strokeWidth);
            return true;
        }

        public void Apply()
        {
            XmlElement replacement = _document.CreateElement("path", _root.NamespaceURI);
            CopySafeAttributes(_strokeRect, replacement);
            replacement.SetAttribute(SvgAttributeName.INTERNAL_DISPLAY_TAG, SvgTagName.RECT);
            replacement.SetAttribute("fill", _strokeColor);
            replacement.SetAttribute("fill-rule", "evenodd");
            replacement.SetAttribute("d", RoundedRectPathBuilder.BuildInsetRingPath(_maskShape, _strokeWidth * 0.5f));

            XmlNode parentNode = _strokeRect.ParentNode;
            XmlNode maskParentNode = _maskElement.ParentNode;
            parentNode?.ReplaceChild(replacement, _strokeRect);
            maskParentNode?.RemoveChild(_maskElement);
            RemoveEmptyDefsAncestor(maskParentNode as XmlElement);
        }

        private static List<XmlElement> FindMaskReferences(XmlElement root, XmlElement maskElement, string maskId)
        {
            string maskReferenceValue = $"url(#{maskId})";

            return XmlUtility
                .EnumerateElementsDepthFirst(root)
                .Where(element => !ReferenceEquals(element, maskElement) &&
                                  string.Equals(element.GetAttribute("mask")?.Trim(), maskReferenceValue, StringComparison.Ordinal))
                .ToList();
        }

        private static bool TryValidateStrokeRect(
            XmlElement strokeRect,
            RectShape maskShape,
            out string strokeColor,
            out float strokeWidth)
        {
            strokeColor = string.Empty;
            strokeWidth = 0f;
            if (strokeRect == null ||
                !string.Equals(strokeRect.LocalName, SvgTagName.RECT, StringComparison.OrdinalIgnoreCase) ||
                !TryParseRect(strokeRect, out RectShape strokeShape) ||
                !maskShape.EqualsGeometry(strokeShape) ||
                !TryGetRequiredAttribute(strokeRect, "stroke", out strokeColor) ||
                !TryParseLength(strokeRect.GetAttribute("stroke-width"), out strokeWidth) ||
                strokeWidth <= 0f ||
                HasUnsupportedAttributes(strokeRect, AllowedRectRewriteAttributes))
            {
                return false;
            }

            return true;
        }

        private static bool HasUnsupportedAttributes(XmlElement element, HashSet<string> allowedAttributes)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (attribute == null)
                {
                    continue;
                }

                string attributeName = attribute.Name ?? string.Empty;
                if (!allowedAttributes.Contains(attributeName))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CopySafeAttributes(XmlElement source, XmlElement target)
        {
            foreach (string attributeName in CopiedAttributes)
            {
                string value = source.GetAttribute(attributeName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    target.SetAttribute(attributeName, value);
                }
            }
        }

        private static void RemoveEmptyDefsAncestor(XmlElement element)
        {
            if (element == null ||
                !string.Equals(element.LocalName, "defs", StringComparison.OrdinalIgnoreCase) ||
                XmlUtility.GetElementChildren(element).Count > 0)
            {
                return;
            }

            element.ParentNode?.RemoveChild(element);
        }

        private static bool TryParseRect(XmlElement element, out RectShape shape)
        {
            shape = default;
            if (element == null ||
                !TryParseLength(element.GetAttribute("x"), out float x) ||
                !TryParseLength(element.GetAttribute("y"), out float y) ||
                !TryParseLength(element.GetAttribute("width"), out float width) ||
                !TryParseLength(element.GetAttribute("height"), out float height))
            {
                return false;
            }

            float rx = 0f;
            float ry = 0f;
            bool hasRx = TryParseOptionalLength(element.GetAttribute("rx"), out rx);
            bool hasRy = TryParseOptionalLength(element.GetAttribute("ry"), out ry);
            if (hasRx && !hasRy)
            {
                ry = rx;
            }
            else if (hasRy && !hasRx)
            {
                rx = ry;
            }

            shape = new RectShape(x, y, width, height, rx, ry);
            return true;
        }

        private static bool TryGetRequiredAttribute(XmlElement element, string attributeName, out string value)
        {
            value = element?.GetAttribute(attributeName)?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryParseOptionalLength(string rawValue, out float value)
        {
            value = 0f;
            return !string.IsNullOrWhiteSpace(rawValue) && TryParseLength(rawValue, out value);
        }

        private static bool TryParseLength(string rawValue, out float value)
        {
            value = 0f;
            return !string.IsNullOrWhiteSpace(rawValue) &&
                   float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
