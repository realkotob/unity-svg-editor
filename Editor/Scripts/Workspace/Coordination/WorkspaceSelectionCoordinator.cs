using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using SvgEditor.Workspace.Canvas;
using SvgEditor.Workspace.HierarchyPanel;
using SvgEditor.Document;

namespace SvgEditor.Workspace
{
    internal sealed class WorkspaceSelectionCoordinator
    {
        private readonly IEditorWorkspaceHost _host;
        private readonly WorkspaceController _canvasWorkspaceController;
        private readonly WorkspaceShellBinder _shellBinder;
        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly HierarchyState _structurePanelState = new();
        private readonly HierarchyInteractionController _structureHierarchyInteractionController = new();

        private bool _isUpdatingStructureSelection;

        public WorkspaceSelectionCoordinator(
            IEditorWorkspaceHost host,
            WorkspaceController canvasWorkspaceController,
            WorkspaceShellBinder shellBinder,
            Func<DocumentSession> currentDocumentAccessor)
        {
            _host = host;
            _canvasWorkspaceController = canvasWorkspaceController;
            _shellBinder = shellBinder;
            _currentDocumentAccessor = currentDocumentAccessor;
        }

        private DocumentSession CurrentDocument => _currentDocumentAccessor?.Invoke();
        private HierarchyListView HierarchyListView => _shellBinder.HierarchyListView;

        public IReadOnlyList<TreeViewItemData<HierarchyNode>> HierarchyItems => _structurePanelState.HierarchyItems;
        public HierarchyNode SelectedHierarchyNode => FindHierarchyNode(_structurePanelState.SelectedElementKey);

        public void Bind(IHierarchyHost hierarchyHost)
        {
            HierarchyListView?.BindRuntime(hierarchyHost, _structureHierarchyInteractionController, OnStructureElementSelectionChanged);
            HierarchyListView?.SetHierarchyItems(_structurePanelState.HierarchyItems);
        }

        public void Unbind()
        {
            HierarchyListView?.UnbindRuntime();
        }

        public void RefreshStructureViews()
        {
            if (!_shellBinder.IsBound)
                return;

            if (CurrentDocument == null)
            {
                _structurePanelState.Clear();
                HierarchyListView?.SetHierarchyItems(_structurePanelState.HierarchyItems);
                UpdateStructureInteractivity(false);
                _canvasWorkspaceController.UpdateSelectionVisual();
                return;
            }

            if (!TryBuildStructureSnapshot(CurrentDocument, out HierarchyOutline snapshot, out string _))
            {
                _structurePanelState.Clear();
                HierarchyListView?.SetHierarchyItems(_structurePanelState.HierarchyItems);
                UpdateStructureInteractivity(true);
                _canvasWorkspaceController.UpdateSelectionVisual();
                return;
            }

            string selectedElementKey = _structurePanelState.SelectedElementKey;
            string selectedTargetKey = _host.ResolveSelectedPatchTargetKey();

            _structurePanelState.SetStructure(snapshot, selectedElementKey);
            HierarchyListView?.SetHierarchyItems(_structurePanelState.HierarchyItems);

            if (TryResolveSelection(_structurePanelState.Elements, selectedElementKey, selectedTargetKey, out HierarchyNode selectedItem, out SelectionKind selectionKind))
            {
                ApplySelectionState(selectedItem, selectionKind, syncPatchTarget: false);
                return;
            }

            ApplySelectionState(null, SelectionKind.None, syncPatchTarget: false);
        }

        public void UpdateStructureInteractivity(bool hasDocument)
        {
            HierarchyListView?.SetEnabled(hasDocument);
        }

        public HierarchyNode FindHierarchyNode(string elementKey)
        {
            return _structurePanelState.Elements.FirstOrDefault(item =>
                string.Equals(item.Key, elementKey, StringComparison.Ordinal));
        }

        public string ResolveCanvasSelectedElementKey()
        {
            HierarchyNode selectedNode = FindHierarchyNode(_structurePanelState.SelectedElementKey);
            if (selectedNode?.IsDefinitionProxy == true && !string.IsNullOrWhiteSpace(selectedNode.SourceElementKey))
                return selectedNode.SourceElementKey;

            return _structurePanelState.SelectedElementKey;
        }

        public void ClearStructureSelectionFromCanvas()
        {
            ApplySelectionState(null, SelectionKind.None, syncPatchTarget: false);
        }

        public void SelectFrameFromCanvas()
        {
            ApplySelectionState(null, SelectionKind.Frame, syncPatchTarget: false);
        }

        public void SelectStructureElementFromCanvas(string elementKey, bool syncPatchTarget)
        {
            HierarchyNode selectedItem = FindHierarchyNode(elementKey);
            ApplySelectionState(
                selectedItem,
                selectedItem != null ? SelectionKind.Element : SelectionKind.None,
                syncPatchTarget);
        }

        public void SyncSelectionFromInspectorTarget(string targetKey)
        {
            if (string.Equals(targetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                ApplySelectionState(null, SelectionKind.Frame, syncPatchTarget: false);
                return;
            }

            HierarchyNode selectedItem = FindHierarchyNodeByTargetKey(targetKey);
            ApplySelectionState(
                selectedItem,
                selectedItem != null ? SelectionKind.Element : SelectionKind.None,
                syncPatchTarget: false);
        }

        private void OnStructureElementSelectionChanged(HierarchyNode selected)
        {
            if (_isUpdatingStructureSelection)
                return;

            ApplySelectionState(
                selected,
                selected != null ? SelectionKind.Element : SelectionKind.None,
                syncPatchTarget: true);
        }

        private bool TryBuildStructureSnapshot(DocumentSession document, out HierarchyOutline snapshot, out string error)
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

            return HierarchyDocumentModelReader.TryBuildSnapshot(document.DocumentModel, out snapshot, out error);
        }

        private HierarchyNode FindHierarchyNodeByTargetKey(string targetKey)
        {
            return _structurePanelState.Elements.FirstOrDefault(item =>
                string.Equals(item.TargetKey, targetKey, StringComparison.Ordinal));
        }

        private void ApplySelectionState(HierarchyNode selectedItem, SelectionKind selectionKind, bool syncPatchTarget)
        {
            _structurePanelState.SelectElement(selectedItem?.Key);
            _structurePanelState.SelectLayer(selectedItem?.LayerKey);
            _canvasWorkspaceController.SetSelectionKind(selectionKind);
            SelectStructureElementByKey(_structurePanelState.SelectedElementKey);

            if (syncPatchTarget && selectedItem?.CanUseAsTarget == true)
                _host.TrySelectPatchTargetByKey(selectedItem.TargetKey);

            UpdateStructureInteractivity(CurrentDocument != null);
            _canvasWorkspaceController.UpdateSelectionVisual();
        }

        private void SelectStructureElementByKey(string elementKey)
        {
            _isUpdatingStructureSelection = true;
            HierarchyListView?.SelectElementByKey(elementKey);
            _isUpdatingStructureSelection = false;
        }

        internal static bool TryResolveSelection(
            IReadOnlyList<HierarchyNode> elements,
            string selectedElementKey,
            string selectedTargetKey,
            out HierarchyNode selectedItem,
            out SelectionKind selectionKind)
        {
            selectedItem = elements?.FirstOrDefault(item =>
                string.Equals(item.Key, selectedElementKey, StringComparison.Ordinal));
            if (selectedItem != null)
            {
                selectionKind = SelectionKind.Element;
                return true;
            }

            if (string.Equals(selectedTargetKey, SvgDocumentTargets.RootTargetKey, StringComparison.Ordinal))
            {
                selectionKind = SelectionKind.Frame;
                return true;
            }

            selectedItem = elements?.FirstOrDefault(item =>
                string.Equals(item.TargetKey, selectedTargetKey, StringComparison.Ordinal));
            if (selectedItem != null)
            {
                selectionKind = SelectionKind.Element;
                return true;
            }

            selectionKind = SelectionKind.None;
            return false;
        }
    }
}
