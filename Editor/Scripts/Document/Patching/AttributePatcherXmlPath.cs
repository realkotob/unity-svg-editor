using System;
using System.Collections.Generic;
using System.Xml;

namespace UnitySvgEditor.Editor
{
    internal static class AttributePatcherXmlPath
    {
        public static bool TryExtractTargets(
            string sourceText,
            ISet<string> knownIds,
            ICollection<PatchTarget> targets)
        {
            if (!SvgDocumentXmlUtility.TryGetRootElement(sourceText, out _, out var root, out _))
                return false;

            foreach (var element in SvgDocumentXmlUtility.EnumerateElementsDepthFirst(root))
            {
                if (!SvgDocumentXmlUtility.TryGetId(element, out var id) || !knownIds.Add(id))
                    continue;

                targets.Add(new PatchTarget
                {
                    Key = id,
                    DisplayName = $"#{id}  <{element.LocalName}>"
                });
            }

            return true;
        }

        public static bool TryApplyAttributePatch(
            string sourceText,
            AttributePatchRequest request,
            out string patchedSourceText,
            out string error)
        {
            patchedSourceText = sourceText ?? string.Empty;
            error = string.Empty;

            if (!TryResolveTargetElement(sourceText, request?.TargetKey, out XmlDocument document, out XmlElement targetElement, out error))
                return false;

            AttributePatchAttributeEditHelper.ApplyRequest(targetElement, request);
            patchedSourceText = document.OuterXml;
            return true;
        }

        public static bool TryReadAttributes(
            string sourceText,
            string targetKey,
            IDictionary<string, string> attributes,
            out string error)
        {
            error = string.Empty;
            if (!TryResolveTargetElement(sourceText, targetKey, out _, out XmlElement targetElement, out error))
                return false;

            if (targetElement.Attributes == null)
                return true;

            foreach (XmlAttribute attribute in targetElement.Attributes)
            {
                if (attribute == null || string.IsNullOrWhiteSpace(attribute.Name) || attributes.ContainsKey(attribute.Name))
                    continue;

                attributes.Add(attribute.Name, attribute.Value);
            }

            return true;
        }

        private static bool TryResolveTargetElement(
            string sourceText,
            string targetKey,
            out XmlDocument document,
            out XmlElement targetElement,
            out string error)
        {
            targetElement = null;
            if (!SvgDocumentXmlUtility.TryGetRootElement(sourceText, out document, out var root, out error))
                return false;

            var normalizedTarget = NormalizeTargetKey(targetKey);
            if (string.Equals(normalizedTarget, AttributePatcher.ROOT_TARGET_KEY, StringComparison.Ordinal))
            {
                targetElement = root;
                return true;
            }

            if (!SvgDocumentXmlUtility.TryFindElementById(root, normalizedTarget, out var xmlElement))
            {
                error = $"Could not find target '{normalizedTarget}'.";
                return false;
            }

            targetElement = xmlElement;
            return true;
        }

        private static string NormalizeTargetKey(string targetKey)
        {
            return string.IsNullOrWhiteSpace(targetKey)
                ? AttributePatcher.ROOT_TARGET_KEY
                : targetKey;
        }
    }
}
