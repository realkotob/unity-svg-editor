using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Core.UI.Extensions;

namespace SvgEditor.DocumentModel
{
    internal sealed class SvgDocumentModelSerializer
    {
        private const string SVG_NAMESPACE_URI = "http://www.w3.org/2000/svg";
        private const string XMLNS_ATTRIBUTE_NAME = "xmlns";
        private const string XMLNS_ATTRIBUTE_PREFIX = "xmlns:";
        private const string XML_NAMESPACE_DECLARATION_URI = "http://www.w3.org/2000/xmlns/";
        private const string XML_NAMESPACE_URI = "http://www.w3.org/XML/1998/namespace";

        public bool TrySerialize(SvgDocumentModel documentModel, out string sourceText, out string error)
        {
            sourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model root is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(documentModel.Root.TagName))
            {
                error = "Document model root tag name is missing.";
                return false;
            }

            try
            {
                StringBuilder builder = new();
                using StringWriter stringWriter = new(builder);
                using XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Indent = false,
                    NewLineHandling = NewLineHandling.None
                });

                WriteNode(xmlWriter, documentModel, documentModel.Root, true);
                xmlWriter.Flush();
                sourceText = builder.ToString();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void WriteNode(
            XmlWriter xmlWriter,
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            bool isRoot)
        {
            string elementPrefix = node?.ElementPrefix ?? string.Empty;
            string namespaceUri = ResolveElementNamespace(documentModel, node, elementPrefix);
            xmlWriter.WriteStartElement(
                string.IsNullOrWhiteSpace(elementPrefix) ? null : elementPrefix,
                node.TagName,
                namespaceUri);

            if (isRoot)
                WriteNamespaceDeclarations(xmlWriter, documentModel, namespaceUri);

            WriteAttributes(xmlWriter, documentModel, node, node.RawAttributes, includeNamespaceDeclarations: !isRoot);

            if (!string.IsNullOrWhiteSpace(node.TextContent))
                xmlWriter.WriteString(node.TextContent);

            foreach (SvgNodeId childId in node.Children)
            {
                if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    throw new InvalidOperationException($"Could not resolve child node '{childId}'.");

                WriteNode(xmlWriter, documentModel, childNode, isRoot: false);
            }

            xmlWriter.WriteEndElement();
        }

        private static void WriteNamespaceDeclarations(
            XmlWriter xmlWriter,
            SvgDocumentModel documentModel,
            string rootNamespaceUri)
        {
            if (documentModel.Namespaces == null)
                return;

            foreach (var pair in documentModel.Namespaces.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                if (string.IsNullOrEmpty(pair.Key))
                {
                    if (!string.Equals(pair.Value, rootNamespaceUri, StringComparison.Ordinal))
                    {
                        xmlWriter.WriteAttributeString(XMLNS_ATTRIBUTE_NAME, localName: null, ns: XML_NAMESPACE_DECLARATION_URI, value: pair.Value);
                    }

                    continue;
                }

                xmlWriter.WriteAttributeString(XMLNS_ATTRIBUTE_NAME, pair.Key, XML_NAMESPACE_DECLARATION_URI, pair.Value);
            }
        }

        private static void WriteAttributes(
            XmlWriter xmlWriter,
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            IReadOnlyDictionary<string, string> attributes,
            bool includeNamespaceDeclarations)
        {
            if (attributes == null)
                return;

            if (includeNamespaceDeclarations)
            {
                foreach (var pair in attributes.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    if (!IsNamespaceDeclaration(pair.Key))
                        continue;

                    WriteNamespaceDeclaration(xmlWriter, pair.Key, pair.Value ?? string.Empty);
                }
            }

            foreach (var pair in attributes.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                if (IsNamespaceDeclaration(pair.Key))
                    continue;

                string value = pair.Value ?? string.Empty;
                int separatorIndex = pair.Key.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    xmlWriter.WriteAttributeString(pair.Key, value);
                    continue;
                }

                string prefix = pair.Key.Substring(0, separatorIndex);
                string localName = pair.Key.Substring(separatorIndex + 1);
                string namespaceUri = ResolveAttributeNamespace(documentModel, node, prefix);
                xmlWriter.WriteAttributeString(prefix, localName, namespaceUri, value);
            }
        }

        private static void WriteNamespaceDeclaration(XmlWriter xmlWriter, string attributeName, string value)
        {
            if (string.Equals(attributeName, XMLNS_ATTRIBUTE_NAME, StringComparison.Ordinal))
            {
                xmlWriter.WriteAttributeString(XMLNS_ATTRIBUTE_NAME, localName: null, ns: XML_NAMESPACE_DECLARATION_URI, value: value);
                return;
            }

            if (attributeName.StartsWith(XMLNS_ATTRIBUTE_PREFIX, StringComparison.Ordinal))
            {
                xmlWriter.WriteAttributeString(
                    XMLNS_ATTRIBUTE_NAME,
                    attributeName.Substring(XMLNS_ATTRIBUTE_PREFIX.Length),
                    XML_NAMESPACE_DECLARATION_URI,
                    value);
            }
        }

        private static string ResolveElementNamespace(SvgDocumentModel documentModel, SvgNodeModel node, string prefix)
        {
            if (!string.IsNullOrWhiteSpace(node?.ElementNamespaceUri))
            {
                return node.ElementNamespaceUri;
            }

            if (!string.IsNullOrWhiteSpace(prefix) &&
                documentModel?.Namespaces != null &&
                documentModel.Namespaces.TryGetValue(prefix, out string prefixedNamespaceUri) &&
                !string.IsNullOrWhiteSpace(prefixedNamespaceUri))
            {
                return prefixedNamespaceUri;
            }

            if (documentModel?.Namespaces != null &&
                documentModel.Namespaces.TryGetValue(string.Empty, out string namespaceUri) &&
                !string.IsNullOrWhiteSpace(namespaceUri))
            {
                return namespaceUri;
            }

            return SVG_NAMESPACE_URI;
        }

        private static string ResolveAttributeNamespace(SvgDocumentModel documentModel, SvgNodeModel node, string prefix)
        {
            if (string.Equals(prefix, "xml", StringComparison.Ordinal))
                return XML_NAMESPACE_URI;

            string namespaceAttributeName = XMLNS_ATTRIBUTE_PREFIX + prefix;
            SvgNodeModel currentNode = node;
            while (currentNode != null)
            {
                if (currentNode.RawAttributes != null &&
                    currentNode.RawAttributes.TryGetValue(namespaceAttributeName, out string scopedNamespaceUri) &&
                    !string.IsNullOrWhiteSpace(scopedNamespaceUri))
                {
                    return scopedNamespaceUri;
                }

                if (!documentModel.TryGetNode(currentNode.ParentId, out currentNode))
                {
                    currentNode = null;
                }
            }

            if (documentModel?.Namespaces != null &&
                documentModel.Namespaces.TryGetValue(prefix, out string namespaceUri) &&
                !string.IsNullOrWhiteSpace(namespaceUri))
            {
                return namespaceUri;
            }

            throw new InvalidOperationException($"Unknown attribute namespace prefix '{prefix}'.");
        }

        private static bool IsNamespaceDeclaration(string attributeName)
        {
            return string.Equals(attributeName, XMLNS_ATTRIBUTE_NAME, StringComparison.Ordinal) ||
                   attributeName.StartsWith(XMLNS_ATTRIBUTE_PREFIX, StringComparison.Ordinal);
        }
    }
}
