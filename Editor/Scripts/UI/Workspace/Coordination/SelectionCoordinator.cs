using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Canvas;
using SvgEditor.UI.Workspace.Host;
using SvgEditor.UI.Hierarchy;
using Core.UI.Extensions;

namespace SvgEditor.UI.Workspace.Coordination
{
    internal sealed class SelectionCoordinator
    {
        private const string HierarchyListElementName = "asset-hierarchy-list";

        private readonly IEditorWorkspaceHost _host;
        private readonly WorkspaceController _canvasWorkspaceController;
        private readonly HierarchyState _structurePanelState = new();
        private readonly HierarchyInteractionController _structureHierarchyInteractionController = new();

        private bool _isUpdatingStructureSelection;
        private HierarchyListView _hierarchyListView;

        public SelectionCoordinator(
            IEditorWorkspaceHost host,
            WorkspaceController canvasWorkspaceController)
        {
            _host = host;
            _canvasWorkspaceController = canvasWorkspaceController;
        }

        private DocumentSession CurrentDocument => _host.CurrentDocument;
        private HierarchyListView HierarchyListView => _hierarchyListView;

        public IReadOnlyList<TreeViewItemData<HierarchyNode>> HierarchyItems => _structurePanelState.HierarchyItems;
        public IReadOnlyList<HierarchyNode> Elements => _structurePanelState.Elements;
        public IReadOnlyList<string> SelectedElementKeys => _structurePanelState.SelectedElementKeys;
        public string SelectionRangeAnchorKey => _structurePanelState.SelectionRangeAnchorKey;
        public HierarchyNode SelectedHierarchyNode => FindHierarchyNode(_structurePanelState.SelectedElementKey);

        public void Bind(IHierarchyHost hierarchyHost, VisualElement root)
        {
            _hierarchyListView = root?.Q<HierarchyListView>(HierarchyListElementName);
            HierarchyListView?.BindRuntime(hierarchyHost, _structureHierarchyInteractionController, OnStructureElementSelectionChanged);
            HierarchyListView?.SetHierarchyItems(_structurePanelState.HierarchyItems);
        }

        public void Unbind()
        {
            HierarchyListView?.UnbindRuntime();
            _hierarchyListView = null;
        }

        public void RefreshStructureViews()
        {
            if (HierarchyListView == null)
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

            if (_structurePanelState.SelectedElementKeys.Count > 0)
            {
                SyncStructureSelection(SelectionKind.Element, syncPatchTarget: false);
                return;
            }

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

        public void ToggleStructureElementSelection(string elementKey, bool syncPatchTarget)
        {
            _structurePanelState.ToggleElementSelection(elementKey);
            SyncStructureSelection(
                _structurePanelState.SelectedElementKeys.Count > 0 ? SelectionKind.Element : SelectionKind.None,
                syncPatchTarget);
        }

        public void AddStructureElementSelectionRange(string elementKey, bool syncPatchTarget)
        {
            _structurePanelState.AddElementSelectionRange(elementKey);
            SyncStructureSelection(
                _structurePanelState.SelectedElementKeys.Count > 0 ? SelectionKind.Element : SelectionKind.None,
                syncPatchTarget);
        }

        public void ReplaceStructureElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget)
        {
            ApplySelectionKeys(elementKeys, syncPatchTarget);
        }

        public void AddStructureElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget)
        {
            if (elementKeys == null || elementKeys.Count == 0)
            {
                return;
            }

            List<string> mergedKeys = new(_structurePanelState.SelectedElementKeys);
            foreach (string elementKey in elementKeys)
            {
                if (string.IsNullOrWhiteSpace(elementKey) ||
                    mergedKeys.Contains(elementKey))
                {
                    continue;
                }

                mergedKeys.Add(elementKey);
            }

            ApplySelectionKeys(mergedKeys, syncPatchTarget);
        }

        public void SyncSelectionFromInspectorTarget(string targetKey)
        {
            if (string.Equals(targetKey, SvgTargets.RootTargetKey, StringComparison.Ordinal))
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

        public void PrepareElementSelectionFallback(string fallbackElementKey)
        {
            if (string.IsNullOrWhiteSpace(fallbackElementKey))
            {
                _structurePanelState.ClearElementSelection();
                return;
            }

            _structurePanelState.SetElementSelection(
                new[] { fallbackElementKey },
                fallbackElementKey,
                fallbackElementKey);
        }

        private void OnStructureElementSelectionChanged(IReadOnlyList<HierarchyNode> selectedItems, HierarchyNode primarySelectedItem)
        {
            if (_isUpdatingStructureSelection)
                return;

            ApplySelectionState(
                selectedItems,
                primarySelectedItem,
                selectedItems != null && selectedItems.Count > 0 ? SelectionKind.Element : SelectionKind.None,
                syncPatchTarget: true,
                selectionAnchorKey: _structureHierarchyInteractionController.SelectionAnchorElementKey);
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

            return HierarchyModelReader.TryBuildSnapshot(document.DocumentModel, out snapshot, out error);
        }

        private HierarchyNode FindHierarchyNodeByTargetKey(string targetKey)
        {
            return _structurePanelState.Elements.FirstOrDefault(item =>
                string.Equals(item.TargetKey, targetKey, StringComparison.Ordinal));
        }

        private void ApplySelectionState(HierarchyNode selectedItem, SelectionKind selectionKind, bool syncPatchTarget)
        {
            ApplySelectionState(
                selectedItem != null ? new[] { selectedItem } : Array.Empty<HierarchyNode>(),
                selectedItem,
                selectionKind,
                syncPatchTarget,
                _structurePanelState.SelectionRangeAnchorKey);
        }

        private void ApplySelectionState(
            IReadOnlyList<HierarchyNode> selectedItems,
            HierarchyNode primarySelectedItem,
            SelectionKind selectionKind,
            bool syncPatchTarget,
            string selectionAnchorKey)
        {
            if (selectionKind == SelectionKind.Element && primarySelectedItem != null)
            {
                _structurePanelState.SetElementSelection(
                    selectedItems.Select(item => item.Key),
                    primarySelectedItem.Key,
                    selectionAnchorKey);
            }
            else
            {
                _structurePanelState.ClearElementSelection();
            }

            SyncStructureSelection(selectionKind, syncPatchTarget);
        }

        private void SelectStructureElementByKey(string elementKey)
        {
            SelectStructureElements(_structurePanelState.SelectedElementKeys, elementKey);
        }

        private void SelectStructureElements(IReadOnlyList<string> elementKeys, string primaryElementKey)
        {
            _isUpdatingStructureSelection = true;
            HierarchyListView?.SelectElementsByKey(elementKeys, primaryElementKey, _structurePanelState.SelectionRangeAnchorKey);
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

            if (string.Equals(selectedTargetKey, SvgTargets.RootTargetKey, StringComparison.Ordinal))
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

        private void SyncStructureSelection(SelectionKind fallbackSelectionKind, bool syncPatchTarget)
        {
            HierarchyNode primarySelectedItem = FindHierarchyNode(_structurePanelState.SelectedElementKey);
            _structurePanelState.SelectLayer(primarySelectedItem?.LayerKey);

            SelectionKind selectionKind = primarySelectedItem != null
                ? SelectionKind.Element
                : fallbackSelectionKind;

            _canvasWorkspaceController.SetSelectionKind(selectionKind);
            SelectStructureElementByKey(_structurePanelState.SelectedElementKey);

            if (syncPatchTarget && primarySelectedItem?.CanUseAsTarget == true)
            {
                _host.TrySelectPatchTargetByKey(primarySelectedItem.TargetKey);
            }

            UpdateStructureInteractivity(CurrentDocument != null);
            _canvasWorkspaceController.UpdateSelectionVisual();
        }

        private void ApplySelectionKeys(IReadOnlyList<string> elementKeys, bool syncPatchTarget)
        {
            List<HierarchyNode> selectedItems = new();
            if (elementKeys != null)
            {
                foreach (string elementKey in elementKeys)
                {
                    HierarchyNode selectedItem = FindHierarchyNode(elementKey);
                    if (selectedItem != null)
                    {
                        selectedItems.Add(selectedItem);
                    }
                }
            }

            HierarchyNode primarySelectedItem = selectedItems.Count > 0
                ? selectedItems[selectedItems.Count - 1]
                : null;
            ApplySelectionState(
                selectedItems,
                primarySelectedItem,
                primarySelectedItem != null ? SelectionKind.Element : SelectionKind.None,
                syncPatchTarget,
                primarySelectedItem?.Key ?? string.Empty);
        }
    }
}
