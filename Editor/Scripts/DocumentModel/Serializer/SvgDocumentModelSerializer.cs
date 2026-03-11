using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgDocumentModelSerializer
    {
        private const string SvgNamespaceUri = "http://www.w3.org/2000/svg";
        private const string XmlNamespaceDeclarationUri = "http://www.w3.org/2000/xmlns/";
        private const string XmlNamespaceUri = "http://www.w3.org/XML/1998/namespace";

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
            string namespaceUri = ResolveElementNamespace(documentModel);
            xmlWriter.WriteStartElement(prefix: null, localName: node.TagName, ns: namespaceUri);

            if (isRoot)
                WriteNamespaceDeclarations(xmlWriter, documentModel, namespaceUri);

            WriteAttributes(xmlWriter, documentModel, node.RawAttributes);

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
                        xmlWriter.WriteAttributeString("xmlns", localName: null, ns: XmlNamespaceDeclarationUri, value: pair.Value);
                    }

                    continue;
                }

                xmlWriter.WriteAttributeString("xmlns", pair.Key, XmlNamespaceDeclarationUri, pair.Value);
            }
        }

        private static void WriteAttributes(
            XmlWriter xmlWriter,
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, string> attributes)
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
                string namespaceUri = ResolveAttributeNamespace(documentModel, prefix);
                xmlWriter.WriteAttributeString(prefix, localName, namespaceUri, value);
            }
        }

        private static string ResolveElementNamespace(SvgDocumentModel documentModel)
        {
            if (documentModel?.Namespaces != null &&
                documentModel.Namespaces.TryGetValue(string.Empty, out string namespaceUri) &&
                !string.IsNullOrWhiteSpace(namespaceUri))
            {
                return namespaceUri;
            }

            return SvgNamespaceUri;
        }

        private static string ResolveAttributeNamespace(SvgDocumentModel documentModel, string prefix)
        {
            if (string.Equals(prefix, "xml", StringComparison.Ordinal))
                return XmlNamespaceUri;

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
            return string.Equals(attributeName, "xmlns", StringComparison.Ordinal) ||
                   attributeName.StartsWith("xmlns:", StringComparison.Ordinal);
        }
    }
}
