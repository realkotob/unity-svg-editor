using System;
using System.Collections.Generic;
using System.Xml;
using SvgEditor.Core.Svg;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Structure.Xml;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Source
{
    internal sealed class SvgLoader
    {
        private XmlElement _root;
        private readonly Dictionary<SvgNodeId, SvgNodeModel> _nodes = new();
        private readonly List<SvgNodeId> _nodeOrder = new();
        private readonly Dictionary<string, SvgNodeId> _nodeIdsByXmlId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _namespaces = new(StringComparer.Ordinal);
        private readonly List<SvgNodeId> _definitionNodeIds = new();

        public Result<SvgDocumentModel> Load(string sourceText)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return Result.Failure<SvgDocumentModel>("SVG source is empty.");
            }

            if (!XmlUtility.TryGetRootElement(sourceText, out _, out XmlElement root, out string error))
                return Result.Failure<SvgDocumentModel>(error);

            ResetBuildState(root);
            CollectNamespaces(root, _namespaces);
            RegisterNode(root, TraversalFrame.Root);

            return Result.Success(CreateModelSnapshot(sourceText));
        }

        public bool TryLoad(string sourceText, out SvgDocumentModel documentModel, out string error)
        {
            Result<SvgDocumentModel> result = Load(sourceText);
            documentModel = result.GetValueOrDefault();
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        private void ResetBuildState(XmlElement root)
        {
            _root = root;
            _nodes.Clear();
            _nodeOrder.Clear();
            _nodeIdsByXmlId.Clear();
            _namespaces.Clear();
            _definitionNodeIds.Clear();
        }

        private SvgDocumentModel CreateModelSnapshot(string sourceText)
        {
            return new SvgDocumentModel
            {
                SourceText = sourceText,
                RootId = SvgNodeId.Root,
                Nodes = new Dictionary<SvgNodeId, SvgNodeModel>(_nodes),
                NodeOrder = new List<SvgNodeId>(_nodeOrder),
                NodeIdsByXmlId = new Dictionary<string, SvgNodeId>(_nodeIdsByXmlId, StringComparer.Ordinal),
                Namespaces = new Dictionary<string, string>(_namespaces, StringComparer.Ordinal),
                DefinitionNodeIds = new List<SvgNodeId>(_definitionNodeIds)
            };
        }

        private SvgNodeId RegisterNode(XmlElement element, TraversalFrame frame)
        {
            bool isRoot = ReferenceEquals(element, _root);
            bool isDefinitionsContainer = string.Equals(element.LocalName, SvgTagName.DEFS, StringComparison.OrdinalIgnoreCase);
            bool isDefinitionNode = frame.IsInsideDefinitions || isDefinitionsContainer;
            XmlUtility.TryGetId(element, out string xmlId);

            var nodeId = isRoot
                ? SvgNodeId.Root
                : CreateNodeId(element, _root, xmlId);
            var rawAttributes = BuildAttributeMap(element);
            var references = CollectReferences(rawAttributes);
            var childIds = new List<SvgNodeId>();

            _nodeOrder.Add(nodeId);
            if (isDefinitionNode && !isRoot)
                _definitionNodeIds.Add(nodeId);

            var children = XmlUtility.GetElementChildren(element);
            for (var childIndex = 0; childIndex < children.Count; childIndex++)
            {
                childIds.Add(RegisterNode(children[childIndex], frame.Next(nodeId, childIndex, isDefinitionNode)));
            }

            string legacyElementKey = XmlUtility.BuildElementKey(element, _root);
            string displayTagName = ResolveDisplayTagName(element);
            var nodeModel = new SvgNodeModel
            {
                Id = nodeId,
                ParentId = frame.ParentId,
                TagName = element.LocalName,
                DisplayTagName = displayTagName,
                ElementPrefix = element.Prefix ?? string.Empty,
                ElementNamespaceUri = element.NamespaceURI ?? string.Empty,
                Kind = ResolveNodeKind(element),
                XmlId = xmlId ?? string.Empty,
                LegacyElementKey = legacyElementKey,
                LegacyTargetKey = isRoot
                    ? SvgTargets.RootTargetKey
                    : legacyElementKey,
                TextContent = IsTextTag(element.LocalName) ? BuildDirectTextContent(element) : string.Empty,
                Depth = frame.Depth,
                SiblingIndex = frame.SiblingIndex,
                IsDefinitionNode = isDefinitionNode && !isRoot,
                RawAttributes = rawAttributes,
                Children = childIds,
                References = references
            };

            _nodes[nodeId] = nodeModel;

            if (!string.IsNullOrWhiteSpace(xmlId))
                _nodeIdsByXmlId[xmlId] = nodeId;

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
                segments.Push($"{current.LocalName}[{XmlUtility.GetElementIndex(current)}]");
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
                if (attribute == null ||
                    string.IsNullOrWhiteSpace(attribute.Name) ||
                    string.Equals(attribute.Name, SvgAttributeName.INTERNAL_DISPLAY_TAG, StringComparison.Ordinal))
                {
                    continue;
                }

                attributes[attribute.Name] = attribute.Value ?? string.Empty;
            }

            return attributes;
        }

        private static string ResolveDisplayTagName(XmlElement element)
        {
            if (element == null)
                return string.Empty;

            string displayTagName = element.GetAttribute(SvgAttributeName.INTERNAL_DISPLAY_TAG)?.Trim();
            return string.IsNullOrWhiteSpace(displayTagName)
                ? element.LocalName
                : displayTagName;
        }

        private static IReadOnlyList<NodeReference> CollectReferences(
            IReadOnlyDictionary<string, string> attributes)
        {
            List<NodeReference> references = new();
            if (attributes == null)
                return references;

            foreach (var pair in attributes)
            {
                if (!SvgFragmentReferenceUtility.TryExtractFragmentId(pair.Value, out var fragmentId))
                    continue;

                references.Add(new NodeReference
                {
                    AttributeName = pair.Key,
                    RawValue = pair.Value,
                    FragmentId = fragmentId
                });
            }

            return references;
        }

        private static SvgNodeCategory ResolveNodeKind(XmlElement element)
        {
            if (element == null)
                return SvgNodeCategory.Other;

            string localName = element.LocalName;
            if (string.Equals(localName, SvgTagName.SVG, StringComparison.OrdinalIgnoreCase))
                return SvgNodeCategory.Root;
            if (string.Equals(localName, SvgTagName.DEFS, StringComparison.OrdinalIgnoreCase))
                return SvgNodeCategory.Definitions;
            if (string.Equals(localName, SvgTagName.GROUP, StringComparison.OrdinalIgnoreCase))
                return SvgNodeCategory.Group;
            if (string.Equals(localName, SvgTagName.USE, StringComparison.OrdinalIgnoreCase))
                return SvgNodeCategory.Use;
            if (IsTextTag(localName))
                return SvgNodeCategory.Text;
            if (IsShapeTag(localName))
                return SvgNodeCategory.Shape;

            return SvgNodeCategory.Other;
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

        private readonly struct TraversalFrame
        {
            public static TraversalFrame Root => new(default, 0, 0, false);

            public TraversalFrame(SvgNodeId parentId, int depth, int siblingIndex, bool isInsideDefinitions)
            {
                ParentId = parentId;
                Depth = depth;
                SiblingIndex = siblingIndex;
                IsInsideDefinitions = isInsideDefinitions;
            }

            public SvgNodeId ParentId { get; }
            public int Depth { get; }
            public int SiblingIndex { get; }
            public bool IsInsideDefinitions { get; }

            public TraversalFrame Next(SvgNodeId parentId, int siblingIndex, bool isInsideDefinitions)
            {
                return new TraversalFrame(parentId, Depth + 1, siblingIndex, isInsideDefinitions);
            }
        }
    }
}
