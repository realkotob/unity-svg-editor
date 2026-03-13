using System;
using System.Collections.Generic;
using UnityEngine;
using SvgEditor.DocumentModel;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Lookup;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class TransientDocumentSession
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

            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(document.DocumentModel, elementKey, out SvgNodeModel sourceNode))
                return false;

            _workingDocumentModel = SvgDocumentModelCloneUtility.CloneDocumentModel(document.DocumentModel);
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

            Dictionary<string, string> attributes = SvgDocumentModelCloneUtility.CloneAttributes(node.RawAttributes);
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

        private static string PrependTransform(string baseTransform, string transformSegment)
        {
            return string.IsNullOrWhiteSpace(baseTransform)
                ? transformSegment
                : $"{transformSegment} {baseTransform}";
        }
    }
}
