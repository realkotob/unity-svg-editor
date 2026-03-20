using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SvgEditor.Core.Svg.Structure.Xml
{
    internal static class SvgElementDeleteUtility
    {
        public static bool TryDeleteElements(
            string sourceText,
            IReadOnlyList<string> elementKeys,
            out string updatedSourceText,
            out string error)
        {
            updatedSourceText = sourceText ?? string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            if (elementKeys == null || elementKeys.Count == 0)
            {
                error = "No SVG elements are selected.";
                return false;
            }

            if (!XmlUtility.TryGetRootElement(sourceText, out XmlDocument document, out XmlElement root, out error))
            {
                return false;
            }

            List<XmlElement> elementsToDelete = new();
            foreach (string elementKey in elementKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Distinct(StringComparer.Ordinal))
            {
                if (!XmlUtility.TryFindElementByKey(root, root, elementKey, out XmlElement element))
                {
                    error = $"Could not find SVG element '{elementKey}'.";
                    return false;
                }

                if (ReferenceEquals(element, root))
                {
                    error = "Cannot delete the SVG root element.";
                    return false;
                }

                elementsToDelete.Add(element);
            }

            foreach (XmlElement element in elementsToDelete)
            {
                element.ParentNode?.RemoveChild(element);
            }

            updatedSourceText = document.OuterXml;
            return true;
        }
    }
}
