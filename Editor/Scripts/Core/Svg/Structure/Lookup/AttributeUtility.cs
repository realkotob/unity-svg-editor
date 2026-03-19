using System;
using System.Collections.Generic;
using SvgEditor.Core.Shared;
using UnityEngine;

namespace SvgEditor.Core.Svg.Structure.Lookup
{
    internal static class AttributeUtility
    {
        public static bool TryGetAttribute(IReadOnlyDictionary<string, string> attributes, string name, out string value)
        {
            value = string.Empty;
            return attributes != null &&
                   attributes.TryGetValue(name, out value) &&
                   !string.IsNullOrWhiteSpace(value);
        }

        public static bool TryGetFloat(IReadOnlyDictionary<string, string> attributes, string name, out float value)
        {
            return SvgLengthParser.TryParseAttribute(attributes, name, out value);
        }

        public static bool TryParseFloat(string text, out float value)
        {
            return SvgLengthParser.TryParse(text, out value);
        }

        public static bool TryParseColor(string text, out Color color)
        {
            color = default;
            var normalized = text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (!normalized.StartsWith("#", StringComparison.Ordinal) &&
                normalized.Length is 3 or 4 or 6 or 8)
            {
                normalized = $"#{normalized}";
            }

            return ColorUtility.TryParseHtmlString(normalized, out color);
        }

        public static bool IsDisabledPaintValue(string text)
        {
            return string.Equals(text?.Trim(), SvgText.NONE_VALUE, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetOpacity(IReadOnlyDictionary<string, string> attributes, out float opacity)
        {
            opacity = 1f;
            return TryGetFloat(attributes, SvgAttributeName.OPACITY, out opacity);
        }
    }
}
