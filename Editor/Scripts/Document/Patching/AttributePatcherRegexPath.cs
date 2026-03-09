using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnitySvgEditor.Editor
{
    internal static class AttributePatcherRegexPath
    {
        private static readonly Regex RootTagRegex =
            new(@"<svg\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex ElementWithIdTagRegex =
            new(
                @"<(?<name>[A-Za-z_][\w:\-\.]*)(?<attrs>[^<>]*?\bid\s*=\s*(?:""(?<id1>[^""]*)""|'(?<id2>[^']*)')[^<>]*?)(?<self>/?)>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex IdAttributeRegex =
            new(@"\bid\s*=\s*(?:""(?<id1>[^""]+)""|'(?<id2>[^']+)')", RegexOptions.IgnoreCase);

        public static void ExtractTargets(
            string sourceText,
            ISet<string> knownIds,
            ICollection<PatchTarget> targets)
        {
            foreach (Match match in IdAttributeRegex.Matches(sourceText))
            {
                var id = match.Groups["id1"].Success
                    ? match.Groups["id1"].Value
                    : match.Groups["id2"].Value;

                if (string.IsNullOrWhiteSpace(id) || !knownIds.Add(id))
                    continue;

                targets.Add(new PatchTarget
                {
                    Key = id,
                    DisplayName = $"#{id}"
                });
            }
        }

        public static bool TryApplyAttributePatch(
            string sourceText,
            AttributePatchRequest request,
            out string patchedSourceText,
            out string error)
        {
            patchedSourceText = sourceText ?? string.Empty;
            error = string.Empty;

            if (!TryFindTargetOpeningTag(
                    sourceText,
                    request?.TargetKey,
                    out int tagStartIndex,
                    out int tagLength,
                    out string originalTag))
            {
                error = $"Could not find target '{request?.TargetKey}'.";
                return false;
            }

            var updatedTag = AttributePatchAttributeEditHelper.ApplyRequest(originalTag, request);
            patchedSourceText = sourceText.Remove(tagStartIndex, tagLength).Insert(tagStartIndex, updatedTag);
            return true;
        }

        public static bool TryReadAttributes(
            string sourceText,
            string targetKey,
            IDictionary<string, string> attributes,
            out string error)
        {
            error = string.Empty;
            if (!TryFindTargetOpeningTag(sourceText, targetKey, out _, out _, out string openingTag))
            {
                error = $"Could not find target '{targetKey}'.";
                return false;
            }

            AttributePatchAttributeEditHelper.ReadAttributes(openingTag, attributes);
            return true;
        }

        private static bool TryFindTargetOpeningTag(
            string sourceText,
            string targetKey,
            out int tagStartIndex,
            out int tagLength,
            out string openingTag)
        {
            tagStartIndex = -1;
            tagLength = 0;
            openingTag = string.Empty;

            var normalizedTarget = NormalizeTargetKey(targetKey);
            if (string.Equals(normalizedTarget, AttributePatcher.ROOT_TARGET_KEY, StringComparison.Ordinal))
            {
                var rootMatch = RootTagRegex.Match(sourceText);
                if (!rootMatch.Success)
                    return false;

                tagStartIndex = rootMatch.Index;
                tagLength = rootMatch.Length;
                openingTag = rootMatch.Value;
                return true;
            }

            foreach (Match match in ElementWithIdTagRegex.Matches(sourceText))
            {
                var id = match.Groups["id1"].Success
                    ? match.Groups["id1"].Value
                    : match.Groups["id2"].Value;

                if (!string.Equals(id, normalizedTarget, StringComparison.Ordinal))
                    continue;

                tagStartIndex = match.Index;
                tagLength = match.Length;
                openingTag = match.Value;
                return true;
            }

            return false;
        }

        private static string NormalizeTargetKey(string targetKey)
        {
            return string.IsNullOrWhiteSpace(targetKey)
                ? AttributePatcher.ROOT_TARGET_KEY
                : targetKey;
        }
    }
}
