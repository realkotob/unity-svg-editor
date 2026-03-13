using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using SvgEditor.Shared;

namespace SvgEditor.Document.Structure.Lookup
{
    internal static class SvgAttributeUtility
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
            value = 0f;
            return TryGetAttribute(attributes, name, out var text) &&
                   TryParseFloat(text, out value);
        }

        public static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
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
            return string.Equals(text?.Trim(), "none", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetOpacity(IReadOnlyDictionary<string, string> attributes, out float opacity)
        {
            opacity = 1f;
            return TryGetFloat(attributes, SvgAttributeName.OPACITY, out opacity);
        }
    }
}
