using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorTargetCatalogService
    {
        private readonly InspectorPanelState _inspectorPanelState;
        private readonly InspectorPanelView _view;
        private readonly Func<IInspectorPanelHost> _hostAccessor;
        private readonly Action _updateInteractivity;

        public InspectorTargetCatalogService(
            InspectorPanelState inspectorPanelState,
            InspectorPanelView view,
            Func<IInspectorPanelHost> hostAccessor,
            Action updateInteractivity)
        {
            _inspectorPanelState = inspectorPanelState;
            _view = view;
            _hostAccessor = hostAccessor;
            _updateInteractivity = updateInteractivity;
        }

        private IInspectorPanelHost Host => _hostAccessor?.Invoke();

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
                ? InspectorDocumentModelReader.ExtractTargets(documentModel)
                : Array.Empty<PatchTarget>();

            _inspectorPanelState.SetTargets(targets);
            ApplyCurrentStateToView();
            ReadSelectedTargetAttributes(documentModel);
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

            ReadSelectedTargetAttributes();
            return true;
        }

        public string ResolveSelectedTargetKey()
        {
            return _inspectorPanelState.ResolveSelectedTargetKey();
        }

        private void ReadSelectedTargetAttributes()
        {
            ReadSelectedTargetAttributes(ResolveCurrentDocumentModel());
        }

        private void ReadSelectedTargetAttributes(SvgDocumentModel documentModel)
        {
            if (Host?.CurrentDocument == null)
            {
                return;
            }

            string error = documentModel == null
                ? "Document model was not available."
                : string.Empty;

            if (documentModel == null ||
                !InspectorDocumentModelReader.TryReadAttributes(
                    documentModel,
                    ResolveSelectedTargetKey(),
                    out Dictionary<string, string> attributes,
                    out string tagName,
                    out error))
            {
                Host.UpdateSourceStatus($"Read target failed: {error}");
                return;
            }

            _inspectorPanelState.SyncFromAttributes(attributes, tagName);
            SyncFramePositionFromPreview();
            _view.ApplyState(_inspectorPanelState);
            _updateInteractivity?.Invoke();
        }

        private void SyncFramePositionFromPreview()
        {
            _inspectorPanelState.FramePositionEnabled = false;
            _inspectorPanelState.FrameX = 0f;
            _inspectorPanelState.FrameY = 0f;

            string targetKey = ResolveSelectedTargetKey();
            if (Host == null ||
                string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal) ||
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
    }
}
