using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly EditorWorkspaceQuickTransformHandler _quickTransformHandler;

        private bool _isUpdatingStructureSelection;

        private VisualElement RootVisualElement => _host.RootVisualElement;
        private DocumentSession CurrentDocument => _host.CurrentDocument;
        private AttributePatcher AttributePatcher => _host.AttributePatcher;

        public EditorWorkspaceCoordinator(IEditorWorkspaceHost host)
        {
            _host = host;
            _canvasWorkspaceController = new CanvasWorkspaceController(this, _structureService);
            _structureHierarchyController = new StructureHierarchyController(_structureService);
            _quickTransformHandler = new EditorWorkspaceQuickTransformHandler(_host, _structureService, _structurePanelState);
        }

        public void Bind(VisualElement stage, VisualElement frame, Toggle moveToolToggle)
        {
            Dispose();
            _canvasWorkspaceController.Bind(stage, frame, moveToolToggle);
            _shellBinder.Bind(
                RootVisualElement,
                _structureHierarchyController,
                this,
                OnStructureElementSelectionChanged,
                OnApplyLayerVisibilityClicked);
            _quickTransformHandler.Bind(_shellBinder);
            _structureHierarchyController.SetItems(_structurePanelState.HierarchyItems);
            UpdateStructureInteractivity(CurrentDocument != null);
        }

        public void Dispose()
        {
            _quickTransformHandler.Unbind();
            _shellBinder.Unbind(_structureHierarchyController, OnApplyLayerVisibilityClicked);
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
                if (_shellBinder.StructureStatusLabel != null)
                    _shellBinder.StructureStatusLabel.text = "No SVG structure loaded.";
                UpdateStructureInteractivity(false);
                UpdateSelectionVisual();
                return;
            }

            if (!_structureService.TryBuildSnapshot(CurrentDocument.WorkingSourceText, out StructureOutline snapshot, out string error))
            {
                _structurePanelState.Clear();
                _structureHierarchyController.SetItems(_structurePanelState.HierarchyItems);
                if (_shellBinder.StructureStatusLabel != null)
                    _shellBinder.StructureStatusLabel.text = $"Structure parse failed: {error}";
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
            SelectLayerByKey(_structurePanelState.SelectedLayerKey);
            UpdateSelectionSummary();
            _shellBinder.LayerVisibleToggle?.SetValueWithoutNotify(_structurePanelState.SelectedLayerVisible);
            if (_shellBinder.StructureStatusLabel != null)
            {
                _shellBinder.StructureStatusLabel.text =
                    $"Elements {_structurePanelState.Elements.Count} · Layers {_structurePanelState.Layers.Count}";
            }

            UpdateStructureInteractivity(true);
            UpdateSelectionVisual();
        }

        public void UpdateStructureInteractivity(bool hasDocument)
        {
            _structureHierarchyController.SetEnabled(hasDocument);

            var hasSelectedElement = hasDocument && !string.IsNullOrWhiteSpace(_structurePanelState.SelectedElementKey);
            var hasLayer = hasDocument && !string.IsNullOrWhiteSpace(_structurePanelState.SelectedLayerKey);

            _shellBinder.LayerVisibleToggle?.SetEnabled(hasLayer);
            _shellBinder.ApplyLayerVisibilityButton?.SetEnabled(hasLayer);
            _shellBinder.QuickApplyTransformButton?.SetEnabled(hasSelectedElement);
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
            _shellBinder.LayerVisibleToggle?.SetValueWithoutNotify(_structurePanelState.SelectedLayerVisible);
            UpdateSelectionSummary();
            if (selected?.CanUseAsTarget == true)
                _host.TrySelectPatchTargetByKey(selected.TargetKey);

            UpdateStructureInteractivity(CurrentDocument != null);
            UpdateSelectionVisual();
        }

        private void OnApplyLayerVisibilityClicked()
        {
            if (CurrentDocument == null)
                return;

            if (string.IsNullOrWhiteSpace(_structurePanelState.SelectedLayerKey))
            {
                _host.UpdateSourceStatus("Select a layer first.");
                return;
            }

            var visible = _shellBinder.LayerVisibleToggle?.value ?? true;
            _structurePanelState.SelectLayer(_structurePanelState.SelectedLayerKey);

            var request = new AttributePatchRequest
            {
                TargetKey = _structurePanelState.SelectedLayerKey,
                Display = visible ? string.Empty : "none"
            };

            TryApplyPatchRequest(request, visible ? "Layer visibility restored." : "Layer hidden.");
        }

        private void SelectStructureElementByKey(string elementKey)
        {
            _isUpdatingStructureSelection = true;
            _structureHierarchyController.SelectElementByKey(elementKey, _structurePanelState.HierarchyItems);
            _isUpdatingStructureSelection = false;
        }

        private void SelectLayerByKey(string layerKey)
        {
            _structurePanelState.SelectLayer(layerKey);
            _shellBinder.LayerVisibleToggle?.SetValueWithoutNotify(_structurePanelState.SelectedLayerVisible);
        }

        private void UpdateSelectionSummary()
        {
            RefreshSelectionSummary(_canvasWorkspaceController.SelectionKind);
        }

        private void RefreshSelectionSummary(CanvasSelectionKind selectionKind)
        {
            var selectedItem = FindStructureNode(_structurePanelState.SelectedElementKey);
            if (selectionKind == CanvasSelectionKind.Frame)
            {
                if (_shellBinder.SelectionNameLabel != null)
                    _shellBinder.SelectionNameLabel.text = GetCanvasFrameLabel();
                if (_shellBinder.SelectionMetaLabel != null)
                    _shellBinder.SelectionMetaLabel.text = "Canvas frame";
                if (_shellBinder.SelectionLayerLabel != null)
                    _shellBinder.SelectionLayerLabel.text = "Layer: n/a";
                return;
            }

            if (selectedItem == null)
            {
                if (_shellBinder.SelectionNameLabel != null)
                    _shellBinder.SelectionNameLabel.text = "No selection";
                if (_shellBinder.SelectionMetaLabel != null)
                    _shellBinder.SelectionMetaLabel.text = "Select an element from Layers.";
                if (_shellBinder.SelectionLayerLabel != null)
                    _shellBinder.SelectionLayerLabel.text = string.Empty;
                return;
            }

            if (_shellBinder.SelectionNameLabel != null)
                _shellBinder.SelectionNameLabel.text = BuildHierarchyLabel(selectedItem);
            if (_shellBinder.SelectionMetaLabel != null)
            {
                _shellBinder.SelectionMetaLabel.text = selectedItem.CanUseAsTarget
                    ? $"<{selectedItem.TagName}>  •  targetable"
                    : $"<{selectedItem.TagName}>  •  no id";
            }

            if (_shellBinder.SelectionLayerLabel != null)
            {
                _shellBinder.SelectionLayerLabel.text = string.IsNullOrWhiteSpace(selectedItem.LayerKey)
                    ? "Layer: none"
                    : $"Layer: #{selectedItem.LayerKey}";
            }
        }

        private static string BuildHierarchyLabel(StructureNode item)
        {
            string source = !string.IsNullOrWhiteSpace(item.TreeLabel) ? item.TreeLabel : item.DisplayName;
            if (string.IsNullOrWhiteSpace(source))
                return "<unnamed>";

            return source.Trim().Replace('_', ' ').Replace('-', ' ');
        }

        private StructureNode FindStructureNode(string elementKey)
        {
            return _structurePanelState.Elements.FirstOrDefault(item =>
                string.Equals(item.Key, elementKey, StringComparison.Ordinal));
        }

        private string GetCanvasFrameLabel()
        {
            if (CurrentDocument == null || string.IsNullOrWhiteSpace(CurrentDocument.AssetPath))
                return "Frame 1";

            return Path.GetFileNameWithoutExtension(CurrentDocument.AssetPath);
        }

        DocumentSession ICanvasWorkspaceHost.CurrentDocument => CurrentDocument;
        PreviewSnapshot ICanvasWorkspaceHost.PreviewSnapshot => _host.PreviewSnapshot;
        Image ICanvasWorkspaceHost.PreviewImage => _host.PreviewImage;
        string ICanvasWorkspaceHost.SelectedElementKey => _structurePanelState.SelectedElementKey;
        string ICanvasWorkspaceHost.FormatNumber(float value) => _host.FormatNumber(value);
        StructureNode ICanvasWorkspaceHost.FindStructureNode(string elementKey) => FindStructureNode(elementKey);
        void ICanvasWorkspaceHost.RefreshSelectionSummary(CanvasSelectionKind selectionKind) => RefreshSelectionSummary(selectionKind);
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
            _shellBinder.LayerVisibleToggle?.SetValueWithoutNotify(_structurePanelState.SelectedLayerVisible);

            if (syncPatchTarget && !string.IsNullOrWhiteSpace(selectedItem?.TargetKey))
                _host.TrySelectPatchTargetByKey(selectedItem.TargetKey);
        }
    }
}
