using System;
using System.Collections.Generic;
using SvgEditor.Document;

namespace SvgEditor.DocumentModel
{
    internal sealed class SvgDocumentModelMutationService
    {
        private readonly SvgDocumentModelSerializer _serializer = new();

        public bool CanApplyAttributePatch(AttributePatchRequest request)
        {
            if (request == null)
                return false;

            return request.Fill != null ||
                   request.Stroke != null ||
                   request.StrokeWidth != null ||
                   request.Opacity != null ||
                   request.FillOpacity != null ||
                   request.StrokeOpacity != null ||
                   request.StrokeLinecap != null ||
                   request.StrokeLinejoin != null ||
                   request.StrokeDasharray != null ||
                   request.CornerRadiusX != null ||
                   request.CornerRadiusY != null ||
                   request.Transform != null ||
                   request.Display != null;
        }

        public bool TryApplyAttributePatch(
            SvgDocumentModel documentModel,
            AttributePatchRequest request,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            if (!CanApplyAttributePatch(request))
            {
                error = "Patch request is not supported by the document model mutation path.";
                return false;
            }

            updatedDocumentModel = CloneDocumentModel(documentModel);
            if (!TryResolveTargetNode(updatedDocumentModel, request.TargetKey, out SvgNodeModel targetNode))
            {
                error = $"Could not find target '{request.TargetKey}'.";
                return false;
            }

            Dictionary<string, string> attributes = CloneAttributes(targetNode.RawAttributes);
            ApplyAttribute(attributes, "fill", request.Fill);
            ApplyAttribute(attributes, "stroke", request.Stroke);
            ApplyAttribute(attributes, "stroke-width", request.StrokeWidth);
            ApplyAttribute(attributes, "opacity", request.Opacity);
            ApplyAttribute(attributes, "fill-opacity", request.FillOpacity);
            ApplyAttribute(attributes, "stroke-opacity", request.StrokeOpacity);
            ApplyAttribute(attributes, "stroke-linecap", request.StrokeLinecap);
            ApplyAttribute(attributes, "stroke-linejoin", request.StrokeLinejoin);
            ApplyAttribute(attributes, "stroke-dasharray", request.StrokeDasharray);
            ApplyAttribute(attributes, "rx", request.CornerRadiusX);
            ApplyAttribute(attributes, "ry", request.CornerRadiusY);
            ApplyAttribute(attributes, "transform", request.Transform);
            ApplyAttribute(attributes, "display", request.Display);

            targetNode.RawAttributes = attributes;
            targetNode.References = RebuildReferences(attributes);

            if (!_serializer.TrySerialize(updatedDocumentModel, out updatedSourceText, out error))
                return false;

            updatedDocumentModel.SourceText = updatedSourceText;
            return true;
        }

        public bool TryReorderElementWithinSameParent(
            SvgDocumentModel documentModel,
            string elementKey,
            int targetChildIndex,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            if (!TryFindNodeByLegacyElementKey(documentModel, elementKey, out SvgNodeModel movedNode))
            {
                error = $"Could not find element '{elementKey}'.";
                return false;
            }

            if (!documentModel.TryGetNode(movedNode.ParentId, out SvgNodeModel parentNode) || parentNode == null)
            {
                error = "Moved element does not have a parent node.";
                return false;
            }

            return TryMoveElement(
                documentModel,
                elementKey,
                parentNode.LegacyElementKey,
                targetChildIndex,
                out updatedDocumentModel,
                out updatedSourceText,
                out error);
        }

        public bool TryMoveElement(
            SvgDocumentModel documentModel,
            string elementKey,
            string targetParentKey,
            int targetChildIndex,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            updatedDocumentModel = CloneDocumentModel(documentModel);
            if (!TryFindNodeByLegacyElementKey(updatedDocumentModel, elementKey, out SvgNodeModel movedNode))
            {
                error = $"Could not find element '{elementKey}'.";
                return false;
            }

            if (!updatedDocumentModel.TryGetNode(movedNode.ParentId, out SvgNodeModel sourceParentNode) || sourceParentNode == null)
            {
                error = "Moved element does not have a parent node.";
                return false;
            }

            if (!TryFindNodeByLegacyElementKey(updatedDocumentModel, targetParentKey, out SvgNodeModel targetParentNode))
            {
                error = $"Could not find target parent '{targetParentKey}'.";
                return false;
            }

            if (targetParentNode.Id == movedNode.Id || IsSameOrDescendantOf(updatedDocumentModel, targetParentNode, movedNode.Id))
            {
                error = "Cannot move an element into itself or its descendant.";
                return false;
            }

            bool movingWithinSameParent = sourceParentNode.Id == targetParentNode.Id;
            List<SvgNodeId> sourceChildren = new(sourceParentNode.Children ?? Array.Empty<SvgNodeId>());
            int sourceIndex = sourceChildren.IndexOf(movedNode.Id);
            if (sourceIndex < 0)
            {
                error = "Could not resolve the moved element index.";
                return false;
            }

            if (movingWithinSameParent)
            {
                int clampedTargetIndex = Math.Clamp(targetChildIndex, 0, sourceChildren.Count);
                if (sourceIndex < clampedTargetIndex)
                    clampedTargetIndex--;

                if (clampedTargetIndex == sourceIndex)
                {
                    updatedSourceText = documentModel.SourceText;
                    updatedDocumentModel.SourceText = updatedSourceText;
                    return true;
                }

                sourceChildren.RemoveAt(sourceIndex);
                sourceChildren.Insert(clampedTargetIndex, movedNode.Id);
                sourceParentNode.Children = sourceChildren;
                RefreshSiblingOrder(updatedDocumentModel, sourceParentNode);
            }
            else
            {
                List<SvgNodeId> targetChildren = new(targetParentNode.Children ?? Array.Empty<SvgNodeId>());
                int clampedTargetIndex = Math.Clamp(targetChildIndex, 0, targetChildren.Count);

                sourceChildren.RemoveAt(sourceIndex);
                targetChildren.Insert(clampedTargetIndex, movedNode.Id);

                sourceParentNode.Children = sourceChildren;
                targetParentNode.Children = targetChildren;
                movedNode.ParentId = targetParentNode.Id;

                RefreshSiblingOrder(updatedDocumentModel, sourceParentNode);
                RefreshSiblingOrder(updatedDocumentModel, targetParentNode);
            }

            if (!_serializer.TrySerialize(updatedDocumentModel, out updatedSourceText, out error))
                return false;

            updatedDocumentModel.SourceText = updatedSourceText;
            return true;
        }

        public bool TryPrependElementTranslation(
            SvgDocumentModel documentModel,
            string elementKey,
            UnityEngine.Vector2 translation,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            if (UnityEngine.Mathf.Approximately(translation.x, 0f) &&
                UnityEngine.Mathf.Approximately(translation.y, 0f))
            {
                updatedDocumentModel = documentModel;
                updatedSourceText = documentModel.SourceText;
                return true;
            }

            updatedDocumentModel = CloneDocumentModel(documentModel);
            if (!TryFindNodeByLegacyElementKey(updatedDocumentModel, elementKey, out SvgNodeModel node))
            {
                error = $"Could not find element '{elementKey}'.";
                return false;
            }

            Dictionary<string, string> attributes = CloneAttributes(node.RawAttributes);
            string existingTransform = attributes.TryGetValue("transform", out string transform)
                ? transform ?? string.Empty
                : string.Empty;
            attributes["transform"] = PrependTransform(
                existingTransform,
                TransformStringBuilder.BuildTranslate(translation));
            node.RawAttributes = attributes;
            node.References = RebuildReferences(attributes);

            if (!_serializer.TrySerialize(updatedDocumentModel, out updatedSourceText, out error))
                return false;

            updatedDocumentModel.SourceText = updatedSourceText;
            return true;
        }

        public bool TryPrependElementScale(
            SvgDocumentModel documentModel,
            string elementKey,
            UnityEngine.Vector2 scale,
            UnityEngine.Vector2 pivot,
            out SvgDocumentModel updatedDocumentModel,
            out string updatedSourceText,
            out string error)
        {
            updatedDocumentModel = null;
            updatedSourceText = string.Empty;
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            if ((UnityEngine.Mathf.Approximately(scale.x, 1f) && UnityEngine.Mathf.Approximately(scale.y, 1f)) ||
                scale.x <= UnityEngine.Mathf.Epsilon ||
                scale.y <= UnityEngine.Mathf.Epsilon)
            {
                updatedDocumentModel = documentModel;
                updatedSourceText = documentModel.SourceText;
                return true;
            }

            updatedDocumentModel = CloneDocumentModel(documentModel);
            if (!TryFindNodeByLegacyElementKey(updatedDocumentModel, elementKey, out SvgNodeModel node))
            {
                error = $"Could not find element '{elementKey}'.";
                return false;
            }

            Dictionary<string, string> attributes = CloneAttributes(node.RawAttributes);
            string existingTransform = attributes.TryGetValue("transform", out string transform)
                ? transform ?? string.Empty
                : string.Empty;
            attributes["transform"] = PrependTransform(
                existingTransform,
                TransformStringBuilder.BuildScaleAround(scale, pivot));
            node.RawAttributes = attributes;
            node.References = RebuildReferences(attributes);

            if (!_serializer.TrySerialize(updatedDocumentModel, out updatedSourceText, out error))
                return false;

            updatedDocumentModel.SourceText = updatedSourceText;
            return true;
        }

        private static bool TryResolveTargetNode(
            SvgDocumentModel documentModel,
            string targetKey,
            out SvgNodeModel node)
        {
            node = null;
            if (documentModel?.Root == null)
                return false;

            string normalizedTargetKey = string.IsNullOrWhiteSpace(targetKey)
                ? SvgDocumentTargets.RootTargetKey
                : targetKey;
            if (string.Equals(normalizedTargetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                node = documentModel.Root;
                return true;
            }

            foreach (SvgNodeId nodeId in documentModel.NodeOrder)
            {
                if (!documentModel.TryGetNode(nodeId, out SvgNodeModel candidate) || candidate == null)
                    continue;

                if (string.Equals(candidate.LegacyTargetKey, normalizedTargetKey, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindNodeByLegacyElementKey(
            SvgDocumentModel documentModel,
            string elementKey,
            out SvgNodeModel node)
        {
            node = null;
            if (documentModel?.NodeOrder == null)
                return false;

            foreach (SvgNodeId nodeId in documentModel.NodeOrder)
            {
                if (!documentModel.TryGetNode(nodeId, out SvgNodeModel candidate) || candidate == null)
                    continue;

                if (string.Equals(candidate.LegacyElementKey, elementKey, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string PrependTransform(string existingTransform, string transformSegment)
        {
            return string.IsNullOrWhiteSpace(existingTransform)
                ? transformSegment
                : $"{transformSegment} {existingTransform}";
        }

        private static void ApplyAttribute(Dictionary<string, string> attributes, string attributeName, string value)
        {
            if (value == null)
                return;

            if (string.IsNullOrWhiteSpace(value))
            {
                attributes.Remove(attributeName);
                return;
            }

            attributes[attributeName] = value.Trim();
        }

        private static List<SvgNodeReference> RebuildReferences(IReadOnlyDictionary<string, string> attributes)
        {
            List<SvgNodeReference> references = new();
            if (attributes == null)
                return references;

            foreach (var pair in attributes)
            {
                if (!TryExtractFragmentId(pair.Value, out string fragmentId))
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

        private static SvgDocumentModel CloneDocumentModel(SvgDocumentModel source)
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

        private static Dictionary<string, string> CloneAttributes(IReadOnlyDictionary<string, string> source)
        {
            Dictionary<string, string> attributes = new(StringComparer.Ordinal);
            if (source == null)
                return attributes;

            foreach (var pair in source)
                attributes[pair.Key] = pair.Value ?? string.Empty;

            return attributes;
        }

        private static List<SvgNodeReference> CloneReferences(IReadOnlyList<SvgNodeReference> source)
        {
            List<SvgNodeReference> references = new();
            if (source == null)
                return references;

            for (var index = 0; index < source.Count; index++)
            {
                SvgNodeReference reference = source[index];
                references.Add(new SvgNodeReference
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

        private static void RefreshSiblingOrder(SvgDocumentModel documentModel, SvgNodeModel parentNode)
        {
            if (documentModel?.Nodes == null || parentNode?.Children == null)
                return;

            for (var index = 0; index < parentNode.Children.Count; index++)
            {
                SvgNodeId childNodeId = parentNode.Children[index];
                if (!documentModel.TryGetNode(childNodeId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                childNode.SiblingIndex = index;
            }
        }

        private static bool IsSameOrDescendantOf(SvgDocumentModel documentModel, SvgNodeModel node, SvgNodeId ancestorId)
        {
            for (SvgNodeModel current = node; current != null;)
            {
                if (current.Id == ancestorId)
                    return true;

                if (current.ParentId == default || !documentModel.TryGetNode(current.ParentId, out current))
                    break;
            }

            return false;
        }
    }
}
