using System;
using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal sealed class InspectorTargetSyncService
    {
        private readonly AttributePatcher _attributePatcher;
        private readonly InspectorPanelState _inspectorPanelState;
        private readonly InspectorPanelView _view;
        private readonly Func<IInspectorPanelHost> _hostAccessor;
        private readonly Action _updateInteractivity;
        private bool _suppressSelectionSync;

        public InspectorTargetSyncService(
            AttributePatcher attributePatcher,
            InspectorPanelState inspectorPanelState,
            InspectorPanelView view,
            Func<IInspectorPanelHost> hostAccessor,
            Action updateInteractivity)
        {
            _attributePatcher = attributePatcher;
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

        public void RefreshTargets(string sourceText)
        {
            if (!_view.IsBound)
            {
                return;
            }

            IReadOnlyList<PatchTarget> targets = TryResolveDocumentModel(sourceText, out SvgDocumentModel documentModel)
                ? InspectorDocumentModelReader.ExtractTargets(documentModel)
                : _attributePatcher.ExtractTargets(ResolveSourceText(sourceText));

            _inspectorPanelState.SetTargets(targets);
            ApplyCurrentStateToView();
            ReadSelectedTargetAttributes(sourceText);
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

        public string ResolveSelectedTargetKey() => _inspectorPanelState.ResolveSelectedTargetKey();

        public void ReadSelectedTargetAttributes()
        {
            ReadSelectedTargetAttributes(null);
        }

        private void ReadSelectedTargetAttributes(string sourceTextOverride)
        {
            if (Host?.CurrentDocument == null)
            {
                return;
            }

            var sourceText = string.IsNullOrWhiteSpace(sourceTextOverride)
                ? Host.CurrentDocument.WorkingSourceText
                : sourceTextOverride;

            if (!TryReadAttributesFromModelOrFallback(
                    sourceText,
                    ResolveSelectedTargetKey(),
                    out Dictionary<string, string> attributes,
                    out string error))
            {
                Host.UpdateSourceStatus($"Read target failed: {error}");
                return;
            }

            _inspectorPanelState.SyncFromAttributes(attributes);
            SyncFramePositionFromPreview();
            _view.ApplyState(_inspectorPanelState);
            _updateInteractivity?.Invoke();
        }

        public void BuildTransformFromHelper()
        {
            var transform = SyncTransformTextFromHelper();

            Host?.UpdateSourceStatus(string.IsNullOrWhiteSpace(transform)
                ? "Transform helper produced an empty value."
                : "Transform string updated.");
            _updateInteractivity?.Invoke();
        }

        public string SyncTransformTextFromHelper()
        {
            if (!_view.IsBound)
            {
                return string.Empty;
            }

            _view.CaptureState(_inspectorPanelState);
            var transform = _inspectorPanelState.BuildTransformFromHelper();
            _view.SetTransformText(transform);
            return transform;
        }

        public bool SyncTransformHelperFromText()
        {
            if (!_view.IsBound)
            {
                return false;
            }

            _view.CaptureState(_inspectorPanelState);
            if (!_inspectorPanelState.TrySyncTransformHelperFromText())
            {
                return false;
            }

            _view.ApplyState(_inspectorPanelState);
            return true;
        }

        public void ApplyFrameRectFromView()
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            var targetKey = ResolveSelectedTargetKey();
            if (string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, AttributePatcher.ROOT_TARGET_KEY, StringComparison.Ordinal) ||
                !Host.TryGetTargetSceneRect(targetKey, out _))
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            var desiredSceneRect = new UnityEngine.Rect(
                _inspectorPanelState.FrameX,
                _inspectorPanelState.FrameY,
                Math.Max(0f, _inspectorPanelState.FrameWidth),
                Math.Max(0f, _inspectorPanelState.FrameHeight));

            Host.TryApplyTargetFrameRect(targetKey, desiredSceneRect, "Frame rect updated.");
        }

        public void ApplyPatchToSource()
        {
            ApplyPatchToSource("Patch applied to source.");
        }

        public void ApplyPatchToSource(string successStatus)
        {
            if (Host?.CurrentDocument == null)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            var request = _inspectorPanelState.BuildPatchRequest();
            Host.TryApplyPatchRequest(
                request,
                string.IsNullOrWhiteSpace(successStatus) ? "Patch applied to source." : successStatus);
        }

        public void ApplyImmediatePatch(InspectorPanelView.ImmediateApplyField field)
        {
            if (Host?.CurrentDocument == null || !_view.IsBound)
            {
                return;
            }

            _view.CaptureState(_inspectorPanelState);
            var request = _inspectorPanelState.BuildPatchRequest(field);
            Host.TryApplyPatchRequest(request, "Inspector changes applied.");
        }

        private void SyncFramePositionFromPreview()
        {
            _inspectorPanelState.FramePositionEnabled = false;
            _inspectorPanelState.FrameX = 0f;
            _inspectorPanelState.FrameY = 0f;

            var targetKey = ResolveSelectedTargetKey();
            if (Host == null ||
                string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(targetKey, AttributePatcher.ROOT_TARGET_KEY, StringComparison.Ordinal) ||
                !Host.TryGetTargetSceneRect(targetKey, out var sceneRect))
            {
                return;
            }

            _inspectorPanelState.FramePositionEnabled = true;
            _inspectorPanelState.FrameX = sceneRect.xMin;
            _inspectorPanelState.FrameY = sceneRect.yMin;
            _inspectorPanelState.FrameWidth = sceneRect.width;
            _inspectorPanelState.FrameHeight = sceneRect.height;
        }

        private bool TryReadAttributesFromModelOrFallback(
            string sourceText,
            string targetKey,
            out Dictionary<string, string> attributes,
            out string error)
        {
            if (TryResolveDocumentModel(sourceText, out SvgDocumentModel documentModel) &&
                InspectorDocumentModelReader.TryReadAttributes(documentModel, targetKey, out attributes, out error))
            {
                return true;
            }

            return _attributePatcher.TryReadAttributes(
                sourceText,
                targetKey,
                out attributes,
                out error);
        }

        private bool TryResolveDocumentModel(string sourceTextOverride, out SvgDocumentModel documentModel)
        {
            documentModel = null;
            if (Host?.CurrentDocument == null ||
                Host.CurrentDocument.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(Host.CurrentDocument.DocumentModelLoadError))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(sourceTextOverride) &&
                !string.Equals(sourceTextOverride, Host.CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                return false;
            }

            documentModel = Host.CurrentDocument.DocumentModel;
            return documentModel != null;
        }

        private string ResolveSourceText(string sourceTextOverride)
        {
            if (!string.IsNullOrWhiteSpace(sourceTextOverride))
                return sourceTextOverride;

            return Host?.CurrentDocument?.WorkingSourceText ?? string.Empty;
        }
    }
}
