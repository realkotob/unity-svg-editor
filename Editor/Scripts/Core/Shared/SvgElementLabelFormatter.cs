namespace SvgEditor.Core.Shared
{
    internal static class SvgElementLabelFormatter
    {
        public static string BuildFromNode(string xmlId, string tagName, string fallback = "")
        {
            if (!string.IsNullOrWhiteSpace(xmlId))
            {
                return Normalize(xmlId);
            }

            if (!string.IsNullOrWhiteSpace(tagName))
            {
                return tagName;
            }

            return fallback ?? string.Empty;
        }

        public static string Normalize(string source, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return fallback ?? string.Empty;
            }

            return source.Trim().Replace('_', ' ').Replace('-', ' ');
        }
    }
}
