using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace UnitySvgEditor.Editor
{
    internal static class AttributePatchAttributeEditHelper
    {
        private static readonly Regex AttributeParseRegex =
            new(
                @"(?<name>[A-Za-z_:][\w:\-\.]*)\s*=\s*(?:""(?<v1>[^""]*)""|'(?<v2>[^']*)')",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public static void ApplyRequest(XmlElement element, AttributePatchRequest request)
        {
            if (element == null || request == null)
                return;

            foreach (var entry in EnumerateRequestAttributes(request))
                SetOrRemoveAttribute(element, entry.name, entry.value);
        }

        public static string ApplyRequest(string openingTag, AttributePatchRequest request)
        {
            var updatedTag = openingTag;
            if (string.IsNullOrWhiteSpace(updatedTag) || request == null)
                return updatedTag;

            foreach (var entry in EnumerateRequestAttributes(request))
                updatedTag = ApplyAttributeEdit(updatedTag, entry.name, entry.value);

            return updatedTag;
        }

        public static void ReadAttributes(string openingTag, IDictionary<string, string> attributes)
        {
            if (string.IsNullOrWhiteSpace(openingTag) || attributes == null)
                return;

            foreach (Match match in AttributeParseRegex.Matches(openingTag))
            {
                var name = match.Groups["name"].Value;
                if (string.IsNullOrWhiteSpace(name) || attributes.ContainsKey(name))
                    continue;

                var value = match.Groups["v1"].Success
                    ? match.Groups["v1"].Value
                    : match.Groups["v2"].Value;
                attributes.Add(name, value);
            }
        }

        private static IEnumerable<(string name, string value)> EnumerateRequestAttributes(AttributePatchRequest request)
        {
            yield return ("fill", request.Fill);
            yield return ("stroke", request.Stroke);
            yield return ("stroke-width", request.StrokeWidth);
            yield return ("opacity", request.Opacity);
            yield return ("fill-opacity", request.FillOpacity);
            yield return ("stroke-opacity", request.StrokeOpacity);
            yield return ("stroke-linecap", request.StrokeLinecap);
            yield return ("stroke-linejoin", request.StrokeLinejoin);
            yield return ("stroke-dasharray", request.StrokeDasharray);
            yield return ("transform", request.Transform);
            yield return ("display", request.Display);
        }

        private static void SetOrRemoveAttribute(XmlElement element, string attributeName, string attributeValue)
        {
            if (element == null || string.IsNullOrWhiteSpace(attributeName) || attributeValue == null)
                return;

            if (string.IsNullOrWhiteSpace(attributeValue))
            {
                element.RemoveAttribute(attributeName);
                return;
            }

            element.SetAttribute(attributeName, attributeValue.Trim());
        }

        private static string ApplyAttributeEdit(string openingTag, string attributeName, string attributeValue)
        {
            if (string.IsNullOrWhiteSpace(openingTag) || attributeValue == null)
                return openingTag;

            var attrPattern = $@"\b{Regex.Escape(attributeName)}\s*=\s*(?:""[^""]*""|'[^']*')";
            if (string.IsNullOrWhiteSpace(attributeValue))
                return Regex.Replace(openingTag, $@"\s+{attrPattern}", string.Empty, RegexOptions.IgnoreCase);

            var sanitizedValue = EscapeXmlAttribute(attributeValue.Trim());
            var replacement = $"{attributeName}=\"{sanitizedValue}\"";
            if (Regex.IsMatch(openingTag, attrPattern, RegexOptions.IgnoreCase))
                return Regex.Replace(openingTag, attrPattern, replacement, RegexOptions.IgnoreCase);

            var insertIndex = openingTag.LastIndexOf("/>", System.StringComparison.Ordinal);
            if (insertIndex < 0)
                insertIndex = openingTag.LastIndexOf('>');
            if (insertIndex < 0)
                return openingTag;

            return openingTag.Insert(insertIndex, $" {replacement}");
        }

        private static string EscapeXmlAttribute(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}
