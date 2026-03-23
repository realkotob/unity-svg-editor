using System;
using System.Collections.Generic;
using SvgEditor.Core.Svg.Model;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Mutation
{
    internal static class ModelClone
    {
        public static SvgDocumentModel Create(SvgDocumentModel source)
        {
            Dictionary<SvgNodeId, SvgNodeModel> nodes = new();
            foreach (var pair in source.Nodes)
            {
                SvgNodeModel sourceNode = pair.Value;
                nodes.Add(pair.Key, new SvgNodeModel
                {
                    Id = sourceNode.Id,
                    ParentId = sourceNode.ParentId,
                    TagName = sourceNode.TagName,
                    DisplayTagName = sourceNode.DisplayTagName,
                    ElementPrefix = sourceNode.ElementPrefix,
                    ElementNamespaceUri = sourceNode.ElementNamespaceUri,
                    Kind = sourceNode.Kind,
                    XmlId = sourceNode.XmlId,
                    LegacyElementKey = sourceNode.LegacyElementKey,
                    LegacyTargetKey = sourceNode.LegacyTargetKey,
                    TextContent = sourceNode.TextContent,
                    Depth = sourceNode.Depth,
                    SiblingIndex = sourceNode.SiblingIndex,
                    IsDefinitionNode = sourceNode.IsDefinitionNode,
                    RawAttributes = CloneAttributes(sourceNode.RawAttributes),
                    Children = new List<SvgNodeId>(sourceNode.Children ?? Array.Empty<SvgNodeId>()),
                    References = CloneReferences(sourceNode.References)
                });
            }

            return new SvgDocumentModel
            {
                SourceText = source.SourceText,
                RootId = source.RootId,
                Nodes = nodes,
                NodeOrder = new List<SvgNodeId>(source.NodeOrder ?? Array.Empty<SvgNodeId>()),
                NodeIdsByXmlId = CloneNodeIdLookup(source.NodeIdsByXmlId),
                Namespaces = CloneNamespaceLookup(source.Namespaces),
                DefinitionNodeIds = new List<SvgNodeId>(source.DefinitionNodeIds ?? Array.Empty<SvgNodeId>())
            };
        }

        public static Dictionary<string, string> CloneAttributes(IReadOnlyDictionary<string, string> source)
        {
            Dictionary<string, string> attributes = new(StringComparer.Ordinal);
            if (source == null)
                return attributes;

            foreach (var pair in source)
                attributes[pair.Key] = pair.Value ?? string.Empty;

            return attributes;
        }

        private static List<NodeReference> CloneReferences(IReadOnlyList<NodeReference> source)
        {
            List<NodeReference> references = new();
            if (source == null)
                return references;

            for (var index = 0; index < source.Count; index++)
            {
                NodeReference reference = source[index];
                references.Add(new NodeReference
                {
                    AttributeName = reference.AttributeName,
                    RawValue = reference.RawValue,
                    FragmentId = reference.FragmentId
                });
            }

            return references;
        }

        private static Dictionary<string, SvgNodeId> CloneNodeIdLookup(IReadOnlyDictionary<string, SvgNodeId> source)
        {
            Dictionary<string, SvgNodeId> lookup = new(StringComparer.Ordinal);
            if (source == null)
                return lookup;

            foreach (var pair in source)
                lookup[pair.Key] = pair.Value;

            return lookup;
        }

        private static Dictionary<string, string> CloneNamespaceLookup(IReadOnlyDictionary<string, string> source)
        {
            Dictionary<string, string> namespaces = new(StringComparer.Ordinal);
            if (source == null)
                return namespaces;

            foreach (var pair in source)
                namespaces[pair.Key] = pair.Value ?? string.Empty;

            return namespaces;
        }
    }
}
