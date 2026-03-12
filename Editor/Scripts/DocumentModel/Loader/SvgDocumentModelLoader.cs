using System;
using System.Collections.Generic;
using System.Xml;

namespace SvgEditor
{
    internal sealed class SvgDocumentModelLoader
    {
        public bool TryLoad(string sourceText, out SvgDocumentModel documentModel, out string error)
        {
            documentModel = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            if (!SvgDocumentXmlUtility.TryGetRootElement(sourceText, out _, out var root, out error))
                return false;

            var context = new BuildContext(root);
            CollectNamespaces(root, context.Namespaces);
            RegisterNode(context, root, default, 0, false, 0);

            documentModel = new SvgDocumentModel
            {
                SourceText = sourceText,
                RootId = SvgNodeId.Root,
                Nodes = context.Nodes,
                NodeOrder = context.NodeOrder,
                NodeIdsByXmlId = context.NodeIdsByXmlId,
                Namespaces = context.Namespaces,
                DefinitionNodeIds = context.DefinitionNodeIds
            };
            return true;
        }

        private static SvgNodeId RegisterNode(
            BuildContext context,
            XmlElement element,
            SvgNodeId parentId,
            int depth,
            bool isDefinitionContext,
            int siblingIndex)
        {
            bool isRoot = ReferenceEquals(element, context.Root);
            bool isDefinitionsContainer = string.Equals(element.LocalName, SvgTagName.DEFS, StringComparison.OrdinalIgnoreCase);
            bool isDefinitionNode = isDefinitionContext || isDefinitionsContainer;
            SvgDocumentXmlUtility.TryGetId(element, out string xmlId);

            var nodeId = isRoot
                ? SvgNodeId.Root
                : CreateNodeId(element, context.Root, xmlId);
            var rawAttributes = BuildAttributeMap(element);
            var references = CollectReferences(rawAttributes);
            var childIds = new List<SvgNodeId>();

            context.NodeOrder.Add(nodeId);
            if (isDefinitionNode && !isRoot)
                context.DefinitionNodeIds.Add(nodeId);

            var children = SvgDocumentXmlUtility.GetElementChildren(element);
            for (var childIndex = 0; childIndex < children.Count; childIndex++)
            {
                childIds.Add(RegisterNode(
                    context,
                    children[childIndex],
                    nodeId,
                    depth + 1,
                    isDefinitionNode,
                    childIndex));
            }

            string legacyElementKey = SvgDocumentXmlUtility.BuildElementKey(element, context.Root);
            var nodeModel = new SvgNodeModel
            {
                Id = nodeId,
                ParentId = parentId,
                TagName = element.LocalName,
                Kind = ResolveNodeKind(element),
                XmlId = xmlId ?? string.Empty,
                LegacyElementKey = legacyElementKey,
                LegacyTargetKey = isRoot
                    ? SvgDocumentTargets.RootTargetKey
                    : legacyElementKey,
                TextContent = IsTextTag(element.LocalName) ? BuildDirectTextContent(element) : string.Empty,
                Depth = depth,
                SiblingIndex = siblingIndex,
                IsDefinitionNode = isDefinitionNode && !isRoot,
                RawAttributes = rawAttributes,
                Children = childIds,
                References = references
            };

            context.Nodes[nodeId] = nodeModel;

            if (!string.IsNullOrWhiteSpace(xmlId))
                context.NodeIdsByXmlId[xmlId] = nodeId;

            return nodeId;
        }

        private static SvgNodeId CreateNodeId(XmlElement element, XmlElement root, string xmlId)
        {
            if (!string.IsNullOrWhiteSpace(xmlId))
                return SvgNodeId.FromXmlId(xmlId);

            return SvgNodeId.FromStructuralPath(BuildStructuralPath(element, root));
        }

        private static string BuildStructuralPath(XmlElement element, XmlElement root)
        {
            Stack<string> segments = new();
            var current = element;
            while (current != null)
            {
                segments.Push($"{current.LocalName}[{SvgDocumentXmlUtility.GetElementIndex(current)}]");
                if (ReferenceEquals(current, root))
                    break;

                current = current.ParentNode as XmlElement;
            }

            return string.Join("/", segments);
        }

        private static IReadOnlyDictionary<string, string> BuildAttributeMap(XmlElement element)
        {
            Dictionary<string, string> attributes = new(StringComparer.Ordinal);
            if (element?.Attributes == null)
                return attributes;

            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (attribute == null || string.IsNullOrWhiteSpace(attribute.Name))
                    continue;

                attributes[attribute.Name] = attribute.Value ?? string.Empty;
            }

            return attributes;
        }

        private static IReadOnlyList<SvgNodeReference> CollectReferences(
            IReadOnlyDictionary<string, string> attributes)
        {
            List<SvgNodeReference> references = new();
            if (attributes == null)
                return references;

            foreach (var pair in attributes)
            {
                if (!TryExtractFragmentId(pair.Value, out var fragmentId))
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

        private static SvgNodeKind ResolveNodeKind(XmlElement element)
        {
            if (element == null)
                return SvgNodeKind.Other;

            string localName = element.LocalName;
            if (string.Equals(localName, SvgTagName.SVG, StringComparison.OrdinalIgnoreCase))
                return SvgNodeKind.Root;
            if (string.Equals(localName, SvgTagName.DEFS, StringComparison.OrdinalIgnoreCase))
                return SvgNodeKind.Definitions;
            if (string.Equals(localName, SvgTagName.GROUP, StringComparison.OrdinalIgnoreCase))
                return SvgNodeKind.Group;
            if (string.Equals(localName, SvgTagName.USE, StringComparison.OrdinalIgnoreCase))
                return SvgNodeKind.Use;
            if (IsTextTag(localName))
                return SvgNodeKind.Text;
            if (IsShapeTag(localName))
                return SvgNodeKind.Shape;

            return SvgNodeKind.Other;
        }

        private static bool IsTextTag(string localName)
        {
            return string.Equals(localName, SvgTagName.TEXT, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(localName, SvgTagName.TSPAN, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(localName, SvgTagName.TEXT_PATH, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildDirectTextContent(XmlElement element)
        {
            if (element == null || !element.HasChildNodes)
                return string.Empty;

            System.Text.StringBuilder builder = new();
            foreach (XmlNode child in element.ChildNodes)
            {
                if (child == null || child.NodeType != XmlNodeType.Text)
                    continue;

                builder.Append(child.InnerText);
            }

            return builder.ToString().Trim();
        }

        private static bool IsShapeTag(string localName)
        {
            return string.Equals(localName, SvgTagName.RECT, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(localName, SvgTagName.CIRCLE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(localName, SvgTagName.ELLIPSE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(localName, SvgTagName.LINE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(localName, SvgTagName.POLYLINE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(localName, SvgTagName.POLYGON, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(localName, SvgTagName.PATH, StringComparison.OrdinalIgnoreCase);
        }

        private static void CollectNamespaces(XmlElement root, IDictionary<string, string> namespaces)
        {
            if (root?.Attributes == null || namespaces == null)
                return;

            foreach (XmlAttribute attribute in root.Attributes)
            {
                if (attribute == null)
                    continue;

                if (string.Equals(attribute.Name, "xmlns", StringComparison.Ordinal))
                {
                    namespaces[string.Empty] = attribute.Value ?? string.Empty;
                    continue;
                }

                if (string.Equals(attribute.Prefix, "xmlns", StringComparison.Ordinal))
                    namespaces[attribute.LocalName] = attribute.Value ?? string.Empty;
            }
        }

        private sealed class BuildContext
        {
            public BuildContext(XmlElement root)
            {
                Root = root;
            }

            public XmlElement Root { get; }
            public Dictionary<SvgNodeId, SvgNodeModel> Nodes { get; } = new();
            public List<SvgNodeId> NodeOrder { get; } = new();
            public Dictionary<string, SvgNodeId> NodeIdsByXmlId { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, string> Namespaces { get; } = new(StringComparer.Ordinal);
            public List<SvgNodeId> DefinitionNodeIds { get; } = new();
        }
    }
}
