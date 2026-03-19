using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Serialization
{
    internal sealed class SvgSerializer
    {
        private const string SVG_NAMESPACE_URI = "http://www.w3.org/2000/svg";
        private const string XMLNS_ATTRIBUTE_NAME = "xmlns";
        private const string XMLNS_ATTRIBUTE_PREFIX = "xmlns:";
        private const string XML_NAMESPACE_DECLARATION_URI = "http://www.w3.org/2000/xmlns/";
        private const string XML_NAMESPACE_URI = "http://www.w3.org/XML/1998/namespace";
        private SvgDocumentModel _documentModel;

        public Result<string> Serialize(SvgDocumentModel documentModel)
        {
            if (documentModel?.Root == null)
            {
                return Result.Failure<string>("Document model root is missing.");
            }

            if (string.IsNullOrWhiteSpace(documentModel.Root.TagName))
            {
                return Result.Failure<string>("Document model root tag name is missing.");
            }

            _documentModel = documentModel;
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

                WriteNode(xmlWriter, documentModel.Root, true);
                xmlWriter.Flush();
                return Result.Success(builder.ToString());
            }
            catch (Exception ex)
            {
                return Result.Failure<string>(ex.Message);
            }
            finally
            {
                _documentModel = null;
            }
        }

        public bool TrySerialize(SvgDocumentModel documentModel, out string sourceText, out string error)
        {
            Result<string> result = Serialize(documentModel);
            sourceText = result.GetValueOrDefault(string.Empty);
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        private void WriteNode(XmlWriter xmlWriter, SvgNodeModel node, bool isRoot)
        {
            string elementPrefix = node?.ElementPrefix ?? string.Empty;
            string namespaceUri = ResolveElementNamespace(node, elementPrefix);
            xmlWriter.WriteStartElement(
                string.IsNullOrWhiteSpace(elementPrefix) ? null : elementPrefix,
                node.TagName,
                namespaceUri);

            if (isRoot)
                WriteNamespaceDeclarations(xmlWriter, namespaceUri);

            if (!isRoot)
            {
                WriteScopedNamespaceDeclarations(xmlWriter, node.RawAttributes);
            }

            WriteAttributes(xmlWriter, node, node.RawAttributes);

            if (!string.IsNullOrWhiteSpace(node.TextContent))
                xmlWriter.WriteString(node.TextContent);

            foreach (SvgNodeId childId in node.Children)
            {
                if (!_documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    throw new InvalidOperationException($"Could not resolve child node '{childId}'.");

                WriteNode(xmlWriter, childNode, isRoot: false);
            }

            xmlWriter.WriteEndElement();
        }

        private void WriteNamespaceDeclarations(XmlWriter xmlWriter, string rootNamespaceUri)
        {
            if (_documentModel.Namespaces == null)
                return;

            foreach (var pair in _documentModel.Namespaces.OrderBy(entry => entry.Key, StringComparer.Ordinal))
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

        private static void WriteScopedNamespaceDeclarations(XmlWriter xmlWriter, IReadOnlyDictionary<string, string> attributes)
        {
            if (attributes == null)
                return;

            foreach (var pair in attributes.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                if (!IsNamespaceDeclaration(pair.Key))
                    continue;

                WriteNamespaceDeclaration(xmlWriter, pair.Key, pair.Value ?? string.Empty);
            }
        }

        private void WriteAttributes(XmlWriter xmlWriter, SvgNodeModel node, IReadOnlyDictionary<string, string> attributes)
        {
            if (attributes == null)
                return;

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
                string namespaceUri = ResolveAttributeNamespace(node, prefix);
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

        private string ResolveElementNamespace(SvgNodeModel node, string prefix)
        {
            if (!string.IsNullOrWhiteSpace(node?.ElementNamespaceUri))
            {
                return node.ElementNamespaceUri;
            }

            if (!string.IsNullOrWhiteSpace(prefix) &&
                _documentModel?.Namespaces != null &&
                _documentModel.Namespaces.TryGetValue(prefix, out string prefixedNamespaceUri) &&
                !string.IsNullOrWhiteSpace(prefixedNamespaceUri))
            {
                return prefixedNamespaceUri;
            }

            if (_documentModel?.Namespaces != null &&
                _documentModel.Namespaces.TryGetValue(string.Empty, out string namespaceUri) &&
                !string.IsNullOrWhiteSpace(namespaceUri))
            {
                return namespaceUri;
            }

            return SVG_NAMESPACE_URI;
        }

        private string ResolveAttributeNamespace(SvgNodeModel node, string prefix)
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

                if (!_documentModel.TryGetNode(currentNode.ParentId, out currentNode))
                {
                    currentNode = null;
                }
            }

            if (_documentModel?.Namespaces != null &&
                _documentModel.Namespaces.TryGetValue(prefix, out string namespaceUri) &&
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
