using System;
using System.IO;
using System.Linq;

namespace UnitySvgEditor.Editor
{
    internal static class VectorImageAssetPresentationUtility
    {
        public static string BuildDisplayName(string assetPath)
        {
            return Path.GetFileNameWithoutExtension(assetPath);
        }

        public static string BuildLibraryName(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return "unknown";

            var normalized = assetPath.Replace('\\', '/');
            const string marker = "/Icons/";
            var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                var start = markerIndex + marker.Length;
                if (start < normalized.Length)
                {
                    var nextSlash = normalized.IndexOf('/', start);
                    if (nextSlash > start)
                        return normalized.Substring(start, nextSlash - start).ToLowerInvariant();
                }
            }

            var directory = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory))
                return "unknown";

            var lastSegment = directory.Split('/').LastOrDefault();
            return string.IsNullOrWhiteSpace(lastSegment)
                ? "unknown"
                : lastSegment.ToLowerInvariant();
        }

        public static string ResolveGroupKey(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "#";

            var firstChar = displayName.TrimStart()[0];
            return char.IsLetterOrDigit(firstChar)
                ? char.ToUpperInvariant(firstChar).ToString()
                : "#";
        }
    }
}
