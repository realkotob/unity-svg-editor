using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class EditorWorkspaceCoordinator : ICanvasWorkspaceHost, IStructureHierarchyHost
    {
        private readonly IEditorWorkspaceHost _host;
        private readonly SvgDocumentModelMutationService _documentModelMutationService = new();
        private readonly StructurePanelState _structurePanelState = new();
        private readonly CanvasWorkspaceController _canvasWorkspaceController;
        private readonly StructureHierarchyController _structureHierarchyController;
        private readonly EditorWorkspaceShellBinder _shellBinder = new();

        private bool _isUpdatingStructureSelection;

        private VisualElement RootVisualElement => _host.RootVisualElement;
        private DocumentSession CurrentDocument => _host.CurrentDocument;

        public EditorWorkspaceCoordinator(IEditorWorkspaceHost host)
        {
            _host = host;
            _canvasWorkspaceController = new CanvasWorkspaceController(this);
            _structureHierarchyController = new StructureHierarchyController();
        }

        public void Bind(CanvasStageView canvasStageView, Toggle moveToolToggle)
        {
            Dispose();
            _canvasWorkspaceController.Bind(canvasStageView, moveToolToggle);
            _shellBinder.Bind(
                RootVisualElement,
                _structureHierarchyController,
                this,
                OnStructureElementSelectionChanged);
            _structureHierarchyController.SetItems(_structurePanelState.HierarchyItems);
            UpdateStructureInteractivity(CurrentDocument != null);
        }

        public void Dispose()
        {
            _shellBinder.Unbind(_structureHierarchyController);
            _canvasWorkspaceController.Dispose();
        }

        public void RefreshStructureViews()
        {
            if (!_shellBinder.IsBound)
                return;

            if (CurrentDocument == null)
            {
                _structurePanelState.Clear();
                _structureHierarchyController.SetItems(_structurePanelState.HierarchyItems);
                UpdateStructureInteractivity(false);
                UpdateSelectionVisual();
                return;
            }

            if (!TryBuildStructureSnapshot(CurrentDocument, out StructureOutline snapshot, out string error))
            {
                _structurePanelState.Clear();
                _structureHierarchyController.SetItems(_structurePanelState.HierarchyItems);
                UpdateStructureInteractivity(true);
                UpdateSelectionVisual();
                return;
            }

            string selectedElementKey = _structurePanelState.SelectedElementKey;
            string selectedTargetKey = _host.ResolveSelectedPatchTargetKey();

            _structurePanelState.SetStructure(snapshot, selectedElementKey);
            _structureHierarchyController.SetItems(_structurePanelState.HierarchyItems);

            if (TryResolveSelection(_structurePanelState.Elements, selectedElementKey, selectedTargetKey, out StructureNode selectedItem, out CanvasSelectionKind selectionKind))
            {
                ApplySelectionState(selectedItem, selectionKind, syncPatchTarget: false);
                return;
            }

            ApplySelectionState(null, CanvasSelectionKind.None, syncPatchTarget: false);
        }

        private bool TryBuildStructureSnapshot(DocumentSession document, out StructureOutline snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;

            if (document?.DocumentModel == null)
            {
                error = "Document model is unavailable.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(document.DocumentModelLoadError))
            {
                error = document.DocumentModelLoadError;
                return false;
            }

            if (!string.Equals(document.DocumentModel.SourceText, document.WorkingSourceText, StringComparison.Ordinal))
            {
                error = "Document model is out of sync with the working source.";
                return false;
            }

            return StructureDocumentModelReader.TryBuildSnapshot(document.DocumentModel, out snapshot, out error);
        }

        public void UpdateStructureInteractivity(bool hasDocument)
        {
            _structureHierarchyController.SetEnabled(hasDocument);
        }

        public bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate)
        {
            if (CurrentDocument == null || request == null)
                return false;

            if (CurrentDocument.DocumentModel == null)
            {
                _host.UpdateSourceStatus("Patch failed: Document model is unavailable.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CurrentDocument.DocumentModelLoadError))
            {
                _host.UpdateSourceStatus($"Patch failed: {CurrentDocument.DocumentModelLoadError}");
                return false;
            }

            if (!string.Equals(CurrentDocument.DocumentModel.SourceText, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                _host.UpdateSourceStatus("Patch failed: Document model is out of sync with the working source.");
                return false;
            }

            if (!_documentModelMutationService.CanApplyAttributePatch(request))
            {
                _host.UpdateSourceStatus("Patch failed: Patch request is not supported by the document model mutation path.");
                return false;
            }

            if (!_documentModelMutationService.TryApplyAttributePatch(
                    CurrentDocument.DocumentModel,
                    request,
                    out SvgDocumentModel _,
                    out string patched,
                    out string error))
            {
                _host.UpdateSourceStatus($"Patch failed: {error}");
                return false;
            }

            if (string.Equals(patched, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                _host.UpdateSourceStatus("No patch changes were applied.");
                return false;
            }

            _host.ApplyUpdatedSource(patched, successStatus, recordingMode);
            return true;
        }

        public bool TryApplyTargetFrameRect(
            string targetKey,
            Rect targetSceneRect,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate)
        {
            if (CurrentDocument == null || string.IsNullOrWhiteSpace(targetKey))
                return false;

            PreviewElementGeometry targetElement = _host.PreviewSnapshot?.Elements?
                .FirstOrDefault(item => string.Equals(item?.TargetKey, targetKey, StringComparison.Ordinal));
            if (targetElement == null)
                return false;

            var currentSceneRect = targetElement.VisualBounds;
            if (CurrentDocument.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(CurrentDocument.DocumentModelLoadError) ||
                !string.Equals(CurrentDocument.DocumentModel.SourceText, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                return false;
            }

            SvgDocumentModel workingDocumentModel = CurrentDocument.DocumentModel;
            var updatedSource = CurrentDocument.WorkingSourceText;
            var hasChanged = false;

            if (currentSceneRect.width > Mathf.Epsilon &&
                currentSceneRect.height > Mathf.Epsilon &&
                (!Mathf.Approximately(currentSceneRect.width, targetSceneRect.width) ||
                 !Mathf.Approximately(currentSceneRect.height, targetSceneRect.height)))
            {
                var scale = new Vector2(
                    Mathf.Max(0f, targetSceneRect.width) / currentSceneRect.width,
                    Mathf.Max(0f, targetSceneRect.height) / currentSceneRect.height);
                var pivot = new Vector2(currentSceneRect.xMin, currentSceneRect.yMin);
                Vector2 parentPivot = ToParentSpacePoint(targetElement.ParentWorldTransform, pivot);
                bool scaleSucceeded = _documentModelMutationService.TryPrependElementScale(
                    workingDocumentModel,
                    targetElement.Key,
                    scale,
                    parentPivot,
                    out workingDocumentModel,
                    out updatedSource,
                    out _);

                if (!scaleSucceeded)
                {
                    return false;
                }

                hasChanged = true;
            }

            var sceneDelta = new Vector2(
                targetSceneRect.xMin - currentSceneRect.xMin,
                targetSceneRect.yMin - currentSceneRect.yMin);
            if (sceneDelta.sqrMagnitude > Mathf.Epsilon)
            {
                Vector2 parentDelta = ToParentSpaceDelta(targetElement.ParentWorldTransform, sceneDelta);
                bool translateSucceeded = _documentModelMutationService.TryPrependElementTranslation(
                    workingDocumentModel,
                    targetElement.Key,
                    parentDelta,
                    out workingDocumentModel,
                    out updatedSource,
                    out _);

                if (!translateSucceeded)
                {
                    return false;
                }

                hasChanged = true;
            }

            if (!hasChanged || string.Equals(updatedSource, CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
                return false;

            _host.ApplyUpdatedSource(updatedSource, successStatus, recordingMode);
            return true;
        }

        public void ResetCanvasView(bool clearSelection = false) => _canvasWorkspaceController.ResetCanvasView(clearSelection);

        public void FitCanvasView(bool clearSelection = false) => _canvasWorkspaceController.FitCanvasView(clearSelection);

        public void SyncCanvasFrameToPreview() => _canvasWorkspaceController.SyncCanvasFrameToPreview();

        public void ResetSelection() => _canvasWorkspaceController.ResetSelection();

        public void UpdateCanvasVisualState() => _canvasWorkspaceController.UpdateCanvasVisualState();

        public void UpdateSelectionVisual() => _canvasWorkspaceController.UpdateSelectionVisual();

        private void OnStructureElementSelectionChanged(StructureNode selected)
        {
            if (_isUpdatingStructureSelection)
                return;

            ApplySelectionState(
                selected,
                selected != null ? CanvasSelectionKind.Element : CanvasSelectionKind.None,
                syncPatchTarget: true);
        }

        private void SelectStructureElementByKey(string elementKey)
        {
            _isUpdatingStructureSelection = true;
            _structureHierarchyController.SelectElementByKey(elementKey, _structurePanelState.HierarchyItems);
            _isUpdatingStructureSelection = false;
        }

        private StructureNode FindStructureNode(string elementKey)
        {
            return _structurePanelState.Elements.FirstOrDefault(item =>
                string.Equals(item.Key, elementKey, StringComparison.Ordinal));
        }

        private StructureNode FindStructureNodeByTargetKey(string targetKey)
        {
            return _structurePanelState.Elements.FirstOrDefault(item =>
                string.Equals(item.TargetKey, targetKey, StringComparison.Ordinal));
        }

        private void ApplySelectionState(StructureNode selectedItem, CanvasSelectionKind selectionKind, bool syncPatchTarget)
        {
            _structurePanelState.SelectElement(selectedItem?.Key);
            _structurePanelState.SelectLayer(selectedItem?.LayerKey);
            _canvasWorkspaceController.SetSelectionKind(selectionKind);
            SelectStructureElementByKey(_structurePanelState.SelectedElementKey);

            if (syncPatchTarget && selectedItem?.CanUseAsTarget == true)
                _host.TrySelectPatchTargetByKey(selectedItem.TargetKey);

            UpdateStructureInteractivity(CurrentDocument != null);
            UpdateSelectionVisual();
        }

        internal static bool TryResolveSelection(
            IReadOnlyList<StructureNode> elements,
            string selectedElementKey,
            string selectedTargetKey,
            out StructureNode selectedItem,
            out CanvasSelectionKind selectionKind)
        {
            selectedItem = elements?.FirstOrDefault(item =>
                string.Equals(item.Key, selectedElementKey, StringComparison.Ordinal));
            if (selectedItem != null)
            {
                selectionKind = CanvasSelectionKind.Element;
                return true;
            }

            if (string.Equals(selectedTargetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                selectionKind = CanvasSelectionKind.Frame;
                return true;
            }

            selectedItem = elements?.FirstOrDefault(item =>
                string.Equals(item.TargetKey, selectedTargetKey, StringComparison.Ordinal));
            if (selectedItem != null)
            {
                selectionKind = CanvasSelectionKind.Element;
                return true;
            }

            selectionKind = CanvasSelectionKind.None;
            return false;
        }

        DocumentSession ICanvasWorkspaceHost.CurrentDocument => CurrentDocument;
        PreviewSnapshot ICanvasWorkspaceHost.PreviewSnapshot => _host.PreviewSnapshot;
        Image ICanvasWorkspaceHost.PreviewImage => _host.PreviewImage;
        string ICanvasWorkspaceHost.SelectedElementKey => _structurePanelState.SelectedElementKey;
        string ICanvasWorkspaceHost.FormatNumber(float value) => _host.FormatNumber(value);
        StructureNode ICanvasWorkspaceHost.FindStructureNode(string elementKey) => FindStructureNode(elementKey);
        void ICanvasWorkspaceHost.UpdateStructureInteractivity(bool hasDocument) => UpdateStructureInteractivity(hasDocument);
        void ICanvasWorkspaceHost.UpdateSourceStatus(string status) => _host.UpdateSourceStatus(status);
        void ICanvasWorkspaceHost.RefreshLivePreview(bool keepExistingPreviewOnFailure) => _host.RefreshLivePreview(keepExistingPreviewOnFailure);
        bool ICanvasWorkspaceHost.TryRefreshTransientPreview(SvgDocumentModel documentModel) => _host.TryRefreshTransientPreview(documentModel);
        void ICanvasWorkspaceHost.RefreshInspector() => _host.RefreshInspector();
        void ICanvasWorkspaceHost.RefreshInspector(SvgDocumentModel documentModel) => _host.RefreshInspector(documentModel);
        void ICanvasWorkspaceHost.ApplyUpdatedSource(string updatedSource, string successStatus) => _host.ApplyUpdatedSource(updatedSource, successStatus);

        DocumentSession IStructureHierarchyHost.CurrentDocument => CurrentDocument;
        IReadOnlyList<TreeViewItemData<StructureNode>> IStructureHierarchyHost.HierarchyItems => _structurePanelState.HierarchyItems;
        StructureNode IStructureHierarchyHost.FindStructureNode(string elementKey) => FindStructureNode(elementKey);
        void IStructureHierarchyHost.ApplyUpdatedSource(string updatedSource, string successStatus) => _host.ApplyUpdatedSource(updatedSource, successStatus);
        void IStructureHierarchyHost.UpdateSourceStatus(string status) => _host.UpdateSourceStatus(status);

        void ICanvasWorkspaceHost.ClearStructureSelectionFromCanvas()
        {
            ApplySelectionState(null, CanvasSelectionKind.None, syncPatchTarget: false);
        }

        void ICanvasWorkspaceHost.SelectFrameFromCanvas()
        {
            ApplySelectionState(null, CanvasSelectionKind.Frame, syncPatchTarget: false);
        }

        void ICanvasWorkspaceHost.SelectStructureElementFromCanvas(string elementKey, bool syncPatchTarget)
        {
            var selectedItem = FindStructureNode(elementKey);
            ApplySelectionState(
                selectedItem,
                selectedItem != null ? CanvasSelectionKind.Element : CanvasSelectionKind.None,
                syncPatchTarget);
        }

        internal void SyncSelectionFromInspectorTarget(string targetKey)
        {
            if (string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                ApplySelectionState(null, CanvasSelectionKind.Frame, syncPatchTarget: false);
                return;
            }

            var selectedItem = FindStructureNodeByTargetKey(targetKey);
            ApplySelectionState(
                selectedItem,
                selectedItem != null ? CanvasSelectionKind.Element : CanvasSelectionKind.None,
                syncPatchTarget: false);
        }

        private static Vector2 ToParentSpaceDelta(Matrix2D parentWorldTransform, Vector2 worldDelta)
        {
            return parentWorldTransform.Inverse().MultiplyVector(worldDelta);
        }

        private static Vector2 ToParentSpacePoint(Matrix2D parentWorldTransform, Vector2 worldPoint)
        {
            return parentWorldTransform.Inverse().MultiplyPoint(worldPoint);
        }
    }
}
