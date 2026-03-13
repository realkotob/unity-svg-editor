using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SvgEditor.Shared;

namespace SvgEditor.Document.Structure.Xml
{
    internal static class SvgSafeMaskArtifactSanitizer
    {
        public static bool TrySanitize(string sourceText, out string sanitizedSourceText, out bool changed, out string error)
        {
            sanitizedSourceText = sourceText ?? string.Empty;
            changed = false;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return true;
            }

            if (!SvgDocumentXmlUtility.TryGetRootElement(sourceText, out XmlDocument document, out XmlElement root, out error))
            {
                return false;
            }

            List<XmlElement> maskElements = SvgDocumentXmlUtility
                .EnumerateElementsDepthFirst(root)
                .Where(element => string.Equals(element.LocalName, SvgTagName.MASK, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (XmlElement maskElement in maskElements)
            {
                if (!SvgMaskRectRewriteCandidate.TryCreate(document, root, maskElement, out var candidate))
                {
                    continue;
                }

                candidate.Apply();
                changed = true;
            }

            if (changed)
            {
                sanitizedSourceText = document.OuterXml;
            }

            return true;
        }
    }
}
