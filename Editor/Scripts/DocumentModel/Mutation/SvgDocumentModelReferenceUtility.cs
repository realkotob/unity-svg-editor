using System;
using System.Collections.Generic;

namespace SvgEditor.DocumentModel
{
    internal static class SvgDocumentModelReferenceUtility
    {
        public static List<SvgNodeReference> RebuildReferences(IReadOnlyDictionary<string, string> attributes)
        {
            List<SvgNodeReference> references = new();
            if (attributes == null)
                return references;

            foreach (var pair in attributes)
            {
                if (!TryExtractFragmentId(pair.Value, out string fragmentId))
                    continue;

                references.Add(new SvgNodeReference
                {
                    AttributeName = pair.Key,
                    RawValue = pair.Value,
                    FragmentId = fragmentId
                });
            }

            return references;
        }

        private static bool TryExtractFragmentId(string rawValue, out string fragmentId)
        {
            fragmentId = string.Empty;
            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            string trimmed = rawValue.Trim();
            if (trimmed.Length > 1 && trimmed[0] == '#')
            {
                fragmentId = trimmed.Substring(1).Trim();
                return !string.IsNullOrWhiteSpace(fragmentId);
            }

            const string urlPrefix = "url(#";
            int startIndex = trimmed.IndexOf(urlPrefix, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
                return false;

            startIndex += urlPrefix.Length;
            int endIndex = trimmed.IndexOf(')', startIndex);
            if (endIndex <= startIndex)
                return false;

            fragmentId = trimmed.Substring(startIndex, endIndex - startIndex).Trim();
            return !string.IsNullOrWhiteSpace(fragmentId);
        }
    }
}
