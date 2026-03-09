using System;
using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal sealed class AttributePatcher
    {
        public const string ROOT_TARGET_KEY = "__root__";

        public IReadOnlyList<PatchTarget> ExtractTargets(string sourceText)
        {
            List<PatchTarget> targets = new()
            {
                new()
                {
                    Key = ROOT_TARGET_KEY,
                    DisplayName = "Root <svg>"
                }
            };

            if (string.IsNullOrWhiteSpace(sourceText))
                return targets;

            HashSet<string> knownIds = new(StringComparer.Ordinal);
            if (AttributePatcherXmlPath.TryExtractTargets(sourceText, knownIds, targets))
                return targets;

            AttributePatcherRegexPath.ExtractTargets(sourceText, knownIds, targets);
            return targets;
        }

        public bool TryApplyAttributePatch(
            string sourceText,
            AttributePatchRequest request,
            out string patchedSourceText,
            out string error)
        {
            patchedSourceText = sourceText ?? string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            if (request == null)
            {
                error = "Patch request is null.";
                return false;
            }

            return AttributePatcherXmlPath.TryApplyAttributePatch(sourceText, request, out patchedSourceText, out error) ||
                   AttributePatcherRegexPath.TryApplyAttributePatch(sourceText, request, out patchedSourceText, out error);
        }

        public bool TryReadAttributes(
            string sourceText,
            string targetKey,
            out Dictionary<string, string> attributes,
            out string error)
        {
            attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            return AttributePatcherXmlPath.TryReadAttributes(sourceText, targetKey, attributes, out error) ||
                   AttributePatcherRegexPath.TryReadAttributes(sourceText, targetKey, attributes, out error);
        }
    }
}
