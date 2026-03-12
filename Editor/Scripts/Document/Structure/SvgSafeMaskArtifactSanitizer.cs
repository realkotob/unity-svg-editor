using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using SvgEditor.Shared;

namespace SvgEditor.Document
{
    internal static class SvgSafeMaskArtifactSanitizer
    {
        public static bool TrySanitize(string sourceText, out string sanitizedSourceText, out bool changed, out string error)
        {
            sanitizedSourceText = sourceText ?? string.Empty;
            changed = false;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
                return true;

            if (!SvgDocumentXmlUtility.TryGetRootElement(sourceText, out XmlDocument document, out XmlElement root, out error))
                return false;

            List<XmlElement> maskElements = SvgDocumentXmlUtility
                .EnumerateElementsDepthFirst(root)
                .Where(element => string.Equals(element.LocalName, SvgTagName.MASK, StringComparison.OrdinalIgnoreCase))
                .ToList();

            for (int index = 0; index < maskElements.Count; index++)
            {
                if (TryRewriteMaskedRectStroke(document, root, maskElements[index]))
                    changed = true;
            }

            if (changed)
                sanitizedSourceText = document.OuterXml;

            return true;
        }

        private static bool TryRewriteMaskedRectStroke(XmlDocument document, XmlElement root, XmlElement maskElement)
        {
            if (document == null ||
                root == null ||
                maskElement == null ||
                !SvgDocumentXmlUtility.TryGetId(maskElement, out string maskId))
            {
                return false;
            }

            List<XmlElement> maskChildren = SvgDocumentXmlUtility.GetElementChildren(maskElement);
            if (maskChildren.Count != 1)
                return false;

            XmlElement maskRect = maskChildren[0];
            if (!string.Equals(maskRect.LocalName, SvgTagName.RECT, StringComparison.OrdinalIgnoreCase) ||
                !TryParseRect(maskRect, out RectShape maskShape))
            {
                return false;
            }

            string maskReferenceValue = $"url(#{maskId})";
            List<XmlElement> references = SvgDocumentXmlUtility
                .EnumerateElementsDepthFirst(root)
                .Where(element => !ReferenceEquals(element, maskElement) &&
                                  string.Equals(element.GetAttribute("mask")?.Trim(), maskReferenceValue, StringComparison.Ordinal))
                .ToList();

            if (references.Count != 1)
                return false;

            XmlElement strokeRect = references[0];
            if (!string.Equals(strokeRect.LocalName, SvgTagName.RECT, StringComparison.OrdinalIgnoreCase) ||
                !TryParseRect(strokeRect, out RectShape strokeShape) ||
                !maskShape.EqualsGeometry(strokeShape) ||
                !TryGetRequiredAttribute(strokeRect, "stroke", out string strokeColor) ||
                !TryParseLength(strokeRect.GetAttribute("stroke-width"), out float strokeWidth) ||
                strokeWidth <= 0f ||
                HasUnsupportedRectRewriteAttributes(strokeRect) ||
                HasUnsupportedMaskAttributes(maskElement))
            {
                return false;
            }

            XmlElement replacement = document.CreateElement("path", root.NamespaceURI);
            CopySafeAttributes(strokeRect, replacement);
            replacement.SetAttribute("fill", strokeColor);
            replacement.SetAttribute("fill-rule", "evenodd");
            replacement.SetAttribute("d", BuildInsetRingPath(maskShape, strokeWidth * 0.5f));

            XmlNode parentNode = strokeRect.ParentNode;
            if (parentNode == null)
                return false;

            XmlNode maskParentNode = maskElement.ParentNode;
            parentNode.ReplaceChild(replacement, strokeRect);
            maskParentNode?.RemoveChild(maskElement);
            RemoveEmptyDefsAncestor(maskParentNode as XmlElement);
            return true;
        }

        private static void RemoveEmptyDefsAncestor(XmlElement element)
        {
            if (element == null ||
                !string.Equals(element.LocalName, "defs", StringComparison.OrdinalIgnoreCase) ||
                SvgDocumentXmlUtility.GetElementChildren(element).Count > 0)
            {
                return;
            }

            element.ParentNode?.RemoveChild(element);
        }

        private static bool HasUnsupportedMaskAttributes(XmlElement maskElement)
        {
            foreach (XmlAttribute attribute in maskElement.Attributes)
            {
                if (attribute == null)
                    continue;

                string attributeName = attribute.Name ?? string.Empty;
                if (string.Equals(attributeName, "id", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "fill", StringComparison.Ordinal))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool HasUnsupportedRectRewriteAttributes(XmlElement strokeRect)
        {
            foreach (XmlAttribute attribute in strokeRect.Attributes)
            {
                if (attribute == null)
                    continue;

                string attributeName = attribute.Name ?? string.Empty;
                if (string.Equals(attributeName, "x", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "y", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "width", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "height", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "rx", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "ry", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "stroke", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "stroke-width", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "mask", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(attributeName, "id", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "transform", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "opacity", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "display", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "visibility", StringComparison.Ordinal) ||
                    string.Equals(attributeName, "class", StringComparison.Ordinal))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void CopySafeAttributes(XmlElement source, XmlElement target)
        {
            string[] safeAttributes =
            {
                "id",
                "transform",
                "opacity",
                "display",
                "visibility",
                "class"
            };

            for (int index = 0; index < safeAttributes.Length; index++)
            {
                string attributeName = safeAttributes[index];
                string value = source.GetAttribute(attributeName);
                if (!string.IsNullOrWhiteSpace(value))
                    target.SetAttribute(attributeName, value);
            }
        }

        private static string BuildInsetRingPath(RectShape shape, float inset)
        {
            if (inset <= 0f)
                return BuildRoundedRectPath(shape);

            float innerWidth = shape.Width - (inset * 2f);
            float innerHeight = shape.Height - (inset * 2f);
            if (innerWidth <= 0f || innerHeight <= 0f)
                return BuildRoundedRectPath(shape);

            RectShape innerShape = new RectShape(
                shape.X + inset,
                shape.Y + inset,
                innerWidth,
                innerHeight,
                Math.Max(0f, shape.Rx - inset),
                Math.Max(0f, shape.Ry - inset));

            return $"{BuildRoundedRectPath(shape)} {BuildRoundedRectPath(innerShape)}";
        }

        private static string BuildRoundedRectPath(RectShape shape)
        {
            float right = shape.X + shape.Width;
            float bottom = shape.Y + shape.Height;
            float rx = Math.Min(shape.Rx, shape.Width * 0.5f);
            float ry = Math.Min(shape.Ry, shape.Height * 0.5f);

            if (rx <= 0f || ry <= 0f)
            {
                return $"M {Format(shape.X)} {Format(shape.Y)} H {Format(right)} V {Format(bottom)} H {Format(shape.X)} Z";
            }

            return
                $"M {Format(shape.X + rx)} {Format(shape.Y)} " +
                $"H {Format(right - rx)} " +
                $"A {Format(rx)} {Format(ry)} 0 0 1 {Format(right)} {Format(shape.Y + ry)} " +
                $"V {Format(bottom - ry)} " +
                $"A {Format(rx)} {Format(ry)} 0 0 1 {Format(right - rx)} {Format(bottom)} " +
                $"H {Format(shape.X + rx)} " +
                $"A {Format(rx)} {Format(ry)} 0 0 1 {Format(shape.X)} {Format(bottom - ry)} " +
                $"V {Format(shape.Y + ry)} " +
                $"A {Format(rx)} {Format(ry)} 0 0 1 {Format(shape.X + rx)} {Format(shape.Y)} Z";
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
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
                ry = rx;
            else if (hasRy && !hasRx)
                rx = ry;

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
            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            return TryParseLength(rawValue, out value);
        }

        private static bool TryParseLength(string rawValue, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            return float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private struct RectShape
        {
            public RectShape(float x, float y, float width, float height, float rx, float ry)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                Rx = rx;
                Ry = ry;
            }

            public float X { get; }
            public float Y { get; }
            public float Width { get; }
            public float Height { get; }
            public float Rx { get; }
            public float Ry { get; }

            public bool EqualsGeometry(RectShape other)
            {
                return AreEqual(X, other.X) &&
                       AreEqual(Y, other.Y) &&
                       AreEqual(Width, other.Width) &&
                       AreEqual(Height, other.Height) &&
                       AreEqual(Rx, other.Rx) &&
                       AreEqual(Ry, other.Ry);
            }

            private static bool AreEqual(float left, float right)
            {
                return Math.Abs(left - right) <= 0.001f;
            }
        }
    }
}
