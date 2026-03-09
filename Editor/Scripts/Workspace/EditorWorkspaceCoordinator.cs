using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class EditorWorkspaceCoordinator : ICanvasWorkspaceHost, IStructureHierarchyHost
    {
        private readonly IEditorWorkspaceHost _host;
        private readonly StructureEditor _structureService = new();
        private readonly StructurePanelState _structurePanelState = new();
        private readonly CanvasWorkspaceController _canvasWorkspaceController;
        private readonly StructureHierarchyController _structureHierarchyController;
        private readonly EditorWorkspaceShellBinder _shellBinder = new();

        private bool _isUpdatingStructureSelection;

        private VisualElement RootVisualElement => _host.RootVisualElement;
        private DocumentSession CurrentDocument => _host.CurrentDocument;
        private AttributePatcher AttributePatcher => _host.AttributePatcher;

        public EditorWorkspaceCoordinator(IEditorWorkspaceHost host)
        {
            _host = host;
            _canvasWorkspaceController = new CanvasWorkspaceController(this, _structureService);
            _structureHierarchyController = new StructureHierarchyController(_structureService);
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

            if (!_structureService.TryBuildSnapshot(CurrentDocument.WorkingSourceText, out StructureOutline snapshot, out string error))
            {
                _structurePanelState.Clear();
                _structureHierarchyController.SetItems(_structurePanelState.HierarchyItems);
                UpdateStructureInteractivity(true);
                UpdateSelectionVisual();
                return;
            }

            var preferredElementKey = !string.IsNullOrWhiteSpace(_structurePanelState.SelectedElementKey)
                ? _structurePanelState.SelectedElementKey
                : _host.ResolveSelectedPatchTargetKey();

            _structurePanelState.SetStructure(snapshot, preferredElementKey);
            _structureHierarchyController.SetItems(_structurePanelState.HierarchyItems);
            SelectStructureElementByKey(_structurePanelState.SelectedElementKey);

            UpdateStructureInteractivity(true);
            UpdateSelectionVisual();
        }

        public void UpdateStructureInteractivity(bool hasDocument)
        {
            _structureHierarchyController.SetEnabled(hasDocument);
        }

        public bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus)
        {
            if (CurrentDocument == null || request == null)
                return false;

            if (!AttributePatcher.TryApplyAttributePatch(
                    CurrentDocument.WorkingSourceText,
                    request,
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

            _host.ApplyUpdatedSource(patched, successStatus);
            return true;
        }

        public void ResetCanvasView(bool clearSelection = false) => _canvasWorkspaceController.ResetCanvasView(clearSelection);

        public void SyncCanvasFrameToPreview() => _canvasWorkspaceController.SyncCanvasFrameToPreview();

        public void ResetSelection() => _canvasWorkspaceController.ResetSelection();

        public void UpdateCanvasVisualState() => _canvasWorkspaceController.UpdateCanvasVisualState();

        public void UpdateSelectionVisual() => _canvasWorkspaceController.UpdateSelectionVisual();

        private void OnStructureElementSelectionChanged(StructureNode selected)
        {
            if (_isUpdatingStructureSelection)
                return;

            _structurePanelState.SelectElement(selected?.Key);
            _structurePanelState.SelectLayer(selected?.LayerKey);
            _canvasWorkspaceController.SetSelectionKind(
                selected != null
                    ? CanvasSelectionKind.Element
                    : CanvasSelectionKind.None);
            SelectStructureElementByKey(_structurePanelState.SelectedElementKey);
            if (selected?.CanUseAsTarget == true)
                _host.TrySelectPatchTargetByKey(selected.TargetKey);

            UpdateStructureInteractivity(CurrentDocument != null);
            UpdateSelectionVisual();
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

        DocumentSession ICanvasWorkspaceHost.CurrentDocument => CurrentDocument;
        PreviewSnapshot ICanvasWorkspaceHost.PreviewSnapshot => _host.PreviewSnapshot;
        Image ICanvasWorkspaceHost.PreviewImage => _host.PreviewImage;
        string ICanvasWorkspaceHost.SelectedElementKey => _structurePanelState.SelectedElementKey;
        string ICanvasWorkspaceHost.FormatNumber(float value) => _host.FormatNumber(value);
        StructureNode ICanvasWorkspaceHost.FindStructureNode(string elementKey) => FindStructureNode(elementKey);
        void ICanvasWorkspaceHost.UpdateStructureInteractivity(bool hasDocument) => UpdateStructureInteractivity(hasDocument);
        void ICanvasWorkspaceHost.UpdateSourceStatus(string status) => _host.UpdateSourceStatus(status);
        void ICanvasWorkspaceHost.RefreshLivePreview(bool keepExistingPreviewOnFailure) => _host.RefreshLivePreview(keepExistingPreviewOnFailure);
        bool ICanvasWorkspaceHost.TryRefreshTransientPreview(string sourceText) => _host.TryRefreshTransientPreview(sourceText);
        void ICanvasWorkspaceHost.ApplyUpdatedSource(string updatedSource, string successStatus) => _host.ApplyUpdatedSource(updatedSource, successStatus);

        DocumentSession IStructureHierarchyHost.CurrentDocument => CurrentDocument;
        IReadOnlyList<TreeViewItemData<StructureNode>> IStructureHierarchyHost.HierarchyItems => _structurePanelState.HierarchyItems;
        StructureNode IStructureHierarchyHost.FindStructureNode(string elementKey) => FindStructureNode(elementKey);
        void IStructureHierarchyHost.ApplyUpdatedSource(string updatedSource, string successStatus) => _host.ApplyUpdatedSource(updatedSource, successStatus);
        void IStructureHierarchyHost.UpdateSourceStatus(string status) => _host.UpdateSourceStatus(status);

        void ICanvasWorkspaceHost.ClearStructureSelectionFromCanvas()
        {
            _structurePanelState.SelectElement(string.Empty);
            _structurePanelState.SelectLayer(string.Empty);
            SelectStructureElementByKey(string.Empty);
        }

        void ICanvasWorkspaceHost.SelectFrameFromCanvas()
        {
            _structurePanelState.SelectElement(string.Empty);
            _structurePanelState.SelectLayer(string.Empty);
            SelectStructureElementByKey(string.Empty);
        }

        void ICanvasWorkspaceHost.SelectStructureElementFromCanvas(string elementKey, bool syncPatchTarget)
        {
            var selectedItem = FindStructureNode(elementKey);
            _structurePanelState.SelectElement(selectedItem?.Key);
            _structurePanelState.SelectLayer(selectedItem?.LayerKey);
            SelectStructureElementByKey(_structurePanelState.SelectedElementKey);

            if (syncPatchTarget && !string.IsNullOrWhiteSpace(selectedItem?.TargetKey))
                _host.TrySelectPatchTargetByKey(selectedItem.TargetKey);
        }
    }
}
