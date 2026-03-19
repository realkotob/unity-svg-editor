using System.Collections.Generic;
using System.Globalization;

namespace SvgEditor.Core.Shared
{
    internal static class SvgLengthParser
    {
        public static bool TryParse(string rawValue, out float value)
        {
            value = 0f;
            return !string.IsNullOrWhiteSpace(rawValue) &&
                   float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryParseOptional(string rawValue, out float value)
        {
            value = 0f;
            return !string.IsNullOrWhiteSpace(rawValue) && TryParse(rawValue, out value);
        }

        public static bool TryParseAttribute(IReadOnlyDictionary<string, string> attributes, string attributeName, out float value)
        {
            value = 0f;
            return attributes != null &&
                   attributes.TryGetValue(attributeName, out string rawValue) &&
                   TryParse(rawValue, out value);
        }
    }
}
