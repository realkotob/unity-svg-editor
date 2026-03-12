using System;
using System.Collections.Generic;
using System.Xml;

namespace SvgEditor
{
    internal static class SvgDocumentXmlUtility
    {
        public static bool TryLoadDocument(string sourceText, out XmlDocument document, out string error)
        {
            document = null;
            error = string.Empty;

            try
            {
                document = new XmlDocument
                {
                    PreserveWhitespace = true
                };
                document.LoadXml(sourceText);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryGetRootElement(
            string sourceText,
            out XmlDocument document,
            out XmlElement root,
            out string error)
        {
            root = null;
            if (!TryLoadDocument(sourceText, out document, out error))
                return false;

            root = document.DocumentElement;
            if (root != null)
                return true;

            error = "SVG root element not found.";
            return false;
        }

        public static bool TryResolveElementDocument(
            string sourceText,
            string elementKey,
            out XmlDocument document,
            out XmlElement root,
            out XmlElement element,
            out string error)
        {
            element = null;
            if (!TryGetRootElement(sourceText, out document, out root, out error))
                return false;

            if (!TryFindElementByKey(root, root, elementKey, out element))
            {
                error = $"Could not find element '{elementKey}'.";
                return false;
            }

            return true;
        }

        public static IEnumerable<XmlElement> EnumerateElementsDepthFirst(XmlElement root)
        {
            if (root == null)
                yield break;

            Stack<XmlElement> stack = new();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                var children = GetElementChildren(current);
                for (var index = children.Count - 1; index >= 0; index--)
                    stack.Push(children[index]);
            }
        }

        public static bool TryFindElementById(XmlElement root, string elementId, out XmlElement result)
        {
            result = null;
            if (root == null || string.IsNullOrWhiteSpace(elementId))
                return false;

            foreach (var element in EnumerateElementsDepthFirst(root))
            {
                if (TryGetId(element, out var id) &&
                    string.Equals(id, elementId, StringComparison.Ordinal))
                {
                    result = element;
                    return true;
                }
            }

            return false;
        }

        public static List<XmlElement> GetElementChildren(XmlElement parent)
        {
            var children = new List<XmlElement>();
            if (parent == null)
                return children;

            foreach (XmlNode childNode in parent.ChildNodes)
            {
                if (childNode is XmlElement childElement)
                    children.Add(childElement);
            }

            return children;
        }

        public static bool TryFindElementByKey(XmlElement root, XmlElement current, string elementKey, out XmlElement result)
        {
            if (current == null)
            {
                result = null;
                return false;
            }

            if (string.Equals(BuildElementKey(current, root), elementKey, StringComparison.Ordinal))
            {
                result = current;
                return true;
            }

            foreach (XmlNode childNode in current.ChildNodes)
            {
                if (childNode is not XmlElement childElement)
                    continue;
                if (TryFindElementByKey(root, childElement, elementKey, out result))
                    return true;
            }

            result = null;
            return false;
        }

        public static bool TryGetId(XmlElement element, out string id)
        {
            id = string.Empty;
            if (element == null)
                return false;

            var value = element.GetAttribute(SvgAttributeName.ID)?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return false;

            id = value;
            return true;
        }

        public static string BuildElementKey(XmlElement element, XmlElement root)
        {
            if (TryGetId(element, out string id))
                return id;

            var segments = new Stack<string>();
            var current = element;
            while (current != null)
            {
                segments.Push($"{current.LocalName}[{GetElementIndex(current)}]");
                if (ReferenceEquals(current, root))
                    break;

                current = current.ParentNode as XmlElement;
            }

            return $"__auto__:{string.Join("/", segments)}";
        }

        public static int GetElementIndex(XmlElement element)
        {
            if (element?.ParentNode == null)
                return 0;

            var index = 0;
            foreach (XmlNode sibling in element.ParentNode.ChildNodes)
            {
                if (sibling is not XmlElement siblingElement)
                    continue;
                if (ReferenceEquals(siblingElement, element))
                    return index;

                index++;
            }

            return 0;
        }

        public static bool IsVisible(XmlElement element)
        {
            if (element == null)
                return true;

            var display = element.GetAttribute(SvgAttributeName.DISPLAY);
            if (string.Equals(display, "none", StringComparison.OrdinalIgnoreCase))
                return false;

            var visibility = element.GetAttribute(SvgAttributeName.VISIBILITY);
            return !string.Equals(visibility, "hidden", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(visibility, "collapse", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLayerCandidate(XmlElement element, XmlElement root)
        {
            if (element == null || root == null)
                return false;
            if (!string.Equals(element.LocalName, SvgTagName.GROUP, StringComparison.OrdinalIgnoreCase))
                return false;
            if (ReferenceEquals(element.ParentNode, root))
                return true;

            var groupMode = element.GetAttribute(SvgAttributeName.INKSCAPE_GROUPMODE);
            if (string.Equals(groupMode, "layer", StringComparison.OrdinalIgnoreCase))
                return true;

            var namespaceMode = element.GetAttribute(SvgAttributeName.GROUPMODE, "http://www.inkscape.org/namespaces/inkscape");
            return string.Equals(namespaceMode, "layer", StringComparison.OrdinalIgnoreCase);
        }
    }
}
