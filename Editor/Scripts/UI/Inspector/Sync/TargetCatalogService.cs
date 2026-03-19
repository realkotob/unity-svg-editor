using System;
using System.Collections.Generic;
using UnityEngine;
using SvgEditor.Core.Svg;
using SvgEditor.Core.Svg.Model;
using SvgEditor.UI.Inspector.State;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.UI.Inspector
{
    internal sealed class TargetCatalogService
    {
        private readonly PanelState _inspectorPanelState;
        private readonly PanelView _view;
        private readonly Func<IPanelHost> _hostAccessor;
        private readonly Action _updateInteractivity;

        public TargetCatalogService(
            PanelState inspectorPanelState,
            PanelView view,
            Func<IPanelHost> hostAccessor,
            Action updateInteractivity)
        {
            _inspectorPanelState = inspectorPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _updateInteractivity = updateInteractivity;
        }

        private IPanelHost Host => _hostAccessor?.Invoke();

        public void ApplyCurrentStateToView()
        {
            if (!_view.IsBound)
            {
                return;
            }

            _view.ApplyState(_inspectorPanelState);
        }

        public void RefreshTargets()
        {
            RefreshTargets(ResolveCurrentDocumentModel());
        }

        public void RefreshTargets(SvgDocumentModel documentModel)
        {
            if (!_view.IsBound)
            {
                return;
            }

            IReadOnlyList<PatchTarget> targets = documentModel != null
                ? DocumentModelReader.ExtractTargets(documentModel)
                : Array.Empty<PatchTarget>();

            _inspectorPanelState.SetTargets(targets);
            ApplyCurrentStateToView();
            ApplySelectedTargetAttributes(documentModel);
        }

        public bool TrySelectTargetByKey(string targetKey, out string label)
        {
            label = string.Empty;
            if (string.IsNullOrWhiteSpace(targetKey) || !_view.IsBound)
            {
                return false;
            }

            if (!_inspectorPanelState.TrySelectTargetByKey(targetKey, out label))
            {
                return false;
            }

            ApplySelectedTargetAttributes();
            return true;
        }

        public string ResolveSelectedTargetKey()
        {
            return _inspectorPanelState.ResolveSelectedTargetKey();
        }

        private void ApplySelectedTargetAttributes()
        {
            ApplySelectedTargetAttributes(ResolveCurrentDocumentModel());
        }

        private void ApplySelectedTargetAttributes(SvgDocumentModel documentModel)
        {
            if (Host?.CurrentDocument == null)
            {
                return;
            }

            Result<SelectedTargetAttributes> result = ReadSelectedTargetAttributes(documentModel);
            if (result.IsFailure)
            {
                Host.UpdateSourceStatus($"Read target failed: {result.Error}");
                return;
            }

            SelectedTargetAttributes targetAttributes = result.Value;
            _inspectorPanelState.SyncFromAttributes(targetAttributes.Attributes, targetAttributes.TagName);
            SyncFramePositionFromPreview();
            _view.ApplyState(_inspectorPanelState);
            _updateInteractivity?.Invoke();
        }

        private Result<SelectedTargetAttributes> ReadSelectedTargetAttributes(SvgDocumentModel documentModel)
        {
            if (documentModel == null)
            {
                return Result.Failure<SelectedTargetAttributes>("Document model was not available.");
            }

            return DocumentModelReader.TryReadAttributes(
                documentModel,
                ResolveSelectedTargetKey(),
                out Dictionary<string, string> attributes,
                out string tagName,
                out string error)
                ? Result.Success(new SelectedTargetAttributes(attributes, tagName))
                : Result.Failure<SelectedTargetAttributes>(error);
        }

        private void SyncFramePositionFromPreview()
        {
            _inspectorPanelState.FramePositionEnabled = false;
            _inspectorPanelState.FrameX = 0f;
            _inspectorPanelState.FrameY = 0f;
            _inspectorPanelState.FrameWidth = 0f;
            _inspectorPanelState.FrameHeight = 0f;

            if (Host != null && Host.TryGetCurrentSelectionSceneRect(out Rect selectionSceneRect))
            {
                _inspectorPanelState.FramePositionEnabled = true;
                _inspectorPanelState.FrameX = selectionSceneRect.xMin;
                _inspectorPanelState.FrameY = selectionSceneRect.yMin;
                _inspectorPanelState.FrameWidth = selectionSceneRect.width;
                _inspectorPanelState.FrameHeight = selectionSceneRect.height;
                return;
            }

            string targetKey = ResolveSelectedTargetKey();
            if (Host == null ||
                string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgTargets.RootTargetKey, StringComparison.Ordinal) ||
                !Host.TryGetTargetSceneRect(targetKey, out Rect sceneRect))
            {
                return;
            }

            _inspectorPanelState.FramePositionEnabled = true;
            _inspectorPanelState.FrameX = sceneRect.xMin;
            _inspectorPanelState.FrameY = sceneRect.yMin;
            _inspectorPanelState.FrameWidth = sceneRect.width;
            _inspectorPanelState.FrameHeight = sceneRect.height;
        }

        private SvgDocumentModel ResolveCurrentDocumentModel()
        {
            if (Host?.CurrentDocument == null ||
                Host.CurrentDocument.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(Host.CurrentDocument.DocumentModelLoadError))
            {
                return null;
            }

            return Host.CurrentDocument.DocumentModel;
        }

        private readonly struct SelectedTargetAttributes
        {
            public SelectedTargetAttributes(Dictionary<string, string> attributes, string tagName)
            {
                Attributes = attributes;
                TagName = tagName;
            }

            public Dictionary<string, string> Attributes { get; }
            public string TagName { get; }
        }
    }
}
