using System;
using System.Collections.Generic;
using UnityEngine;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class CanvasTransientDocumentModelSession
    {
        private readonly SvgDocumentModelSerializer _serializer = new();

        private SvgDocumentModel _workingDocumentModel;
        private SvgNodeId _activeNodeId;
        private string _baseTransform = string.Empty;
        private bool _hasPendingMutation;

        public bool HasPendingMutation => _hasPendingMutation;
        public SvgDocumentModel WorkingDocumentModel => _workingDocumentModel;

        public bool TryBegin(DocumentSession document, string elementKey)
        {
            End();
            if (document?.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(document.DocumentModelLoadError) ||
                string.IsNullOrWhiteSpace(elementKey))
            {
                return false;
            }

            if (!TryFindNodeByLegacyElementKey(document.DocumentModel, elementKey, out SvgNodeModel sourceNode))
                return false;

            _workingDocumentModel = CloneDocumentModel(document.DocumentModel);
            _activeNodeId = sourceNode.Id;
            _baseTransform = sourceNode.RawAttributes != null &&
                             sourceNode.RawAttributes.TryGetValue(SvgAttributeName.TRANSFORM, out string transform)
                ? transform ?? string.Empty
                : string.Empty;
            _hasPendingMutation = false;
            return _workingDocumentModel.TryGetNode(_activeNodeId, out _);
        }

        public bool TryApplyTranslation(Vector2 translation)
        {
            if (_workingDocumentModel == null)
                return false;

            if (translation.sqrMagnitude <= Mathf.Epsilon)
            {
                ApplyTransform(_baseTransform);
                _hasPendingMutation = false;
                return true;
            }

            ApplyTransform(PrependTransform(_baseTransform, TransformStringBuilder.BuildTranslate(translation)));
            _hasPendingMutation = true;
            return true;
        }

        public bool TryApplyScale(Vector2 scale, Vector2 pivot)
        {
            if (_workingDocumentModel == null)
                return false;
            if (scale.x <= Mathf.Epsilon || scale.y <= Mathf.Epsilon)
                return false;

            if (Mathf.Approximately(scale.x, 1f) && Mathf.Approximately(scale.y, 1f))
            {
                ApplyTransform(_baseTransform);
                _hasPendingMutation = false;
                return true;
            }

            ApplyTransform(PrependTransform(_baseTransform, TransformStringBuilder.BuildScaleAround(scale, pivot)));
            _hasPendingMutation = true;
            return true;
        }

        public bool TryApplyRotation(float angle, Vector2 pivot)
        {
            if (_workingDocumentModel == null)
                return false;

            if (Mathf.Approximately(angle, 0f))
            {
                ApplyTransform(_baseTransform);
                _hasPendingMutation = false;
                return true;
            }

            ApplyTransform(PrependTransform(_baseTransform, TransformStringBuilder.BuildRotateAround(angle, pivot)));
            _hasPendingMutation = true;
            return true;
        }

        public bool TryBuildCommittedSource(out string sourceText, out string error)
        {
            sourceText = string.Empty;
            error = string.Empty;

            if (_workingDocumentModel == null || !_hasPendingMutation)
                return false;

            if (!TrySerializeWorkingDocumentModel(out sourceText, out error))
                return false;

            _workingDocumentModel.SourceText = sourceText;
            return true;
        }

        public bool TryBuildPreviewDocumentModel(out SvgDocumentModel documentModel, out string error)
        {
            documentModel = null;
            error = string.Empty;

            if (_workingDocumentModel == null)
            {
                error = "Transient document model is unavailable.";
                return false;
            }

            if (!TrySerializeWorkingDocumentModel(out string sourceText, out error))
                return false;

            _workingDocumentModel.SourceText = sourceText;
            documentModel = _workingDocumentModel;
            return true;
        }

        public bool TryGetCurrentTransform(out string transform)
        {
            transform = string.Empty;
            if (_workingDocumentModel == null ||
                !_workingDocumentModel.TryGetNode(_activeNodeId, out SvgNodeModel node) ||
                node?.RawAttributes == null ||
                !node.RawAttributes.TryGetValue(SvgAttributeName.TRANSFORM, out string currentTransform))
            {
                return false;
            }

            transform = currentTransform ?? string.Empty;
            return true;
        }

        public void End()
        {
            _workingDocumentModel = null;
            _activeNodeId = default;
            _baseTransform = string.Empty;
            _hasPendingMutation = false;
        }

        private void ApplyTransform(string transformValue)
        {
            if (_workingDocumentModel == null ||
                !_workingDocumentModel.TryGetNode(_activeNodeId, out SvgNodeModel node) ||
                node == null)
            {
                return;
            }

            Dictionary<string, string> attributes = CloneAttributes(node.RawAttributes);
            if (string.IsNullOrWhiteSpace(transformValue))
                attributes.Remove(SvgAttributeName.TRANSFORM);
            else
                attributes[SvgAttributeName.TRANSFORM] = transformValue;

            node.RawAttributes = attributes;
        }

        private bool TrySerializeWorkingDocumentModel(out string sourceText, out string error)
        {
            sourceText = string.Empty;
            error = string.Empty;

            return _workingDocumentModel != null &&
                   _serializer.TrySerialize(_workingDocumentModel, out sourceText, out error);
        }

        private static bool TryFindNodeByLegacyElementKey(
            SvgDocumentModel documentModel,
            string elementKey,
            out SvgNodeModel node)
        {
            node = null;
            if (documentModel?.NodeOrder == null)
                return false;

            for (var index = 0; index < documentModel.NodeOrder.Count; index++)
            {
                SvgNodeId nodeId = documentModel.NodeOrder[index];
                if (!documentModel.TryGetNode(nodeId, out SvgNodeModel currentNode) || currentNode == null)
                    continue;

                if (string.Equals(currentNode.LegacyElementKey, elementKey, StringComparison.Ordinal))
                {
                    node = currentNode;
                    return true;
                }
            }

            return false;
        }

        private static string PrependTransform(string baseTransform, string transformSegment)
        {
            return string.IsNullOrWhiteSpace(baseTransform)
                ? transformSegment
                : $"{transformSegment} {baseTransform}";
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
    }
}
