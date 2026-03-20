using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Preview;
using SvgEditor.UI.Canvas;
using SvgEditor.UI.Workspace.Document;
using SvgEditor.UI.Workspace.Host;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.UI.Hierarchy;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Svg.Structure.Xml;
using Core.UI.Extensions;

namespace SvgEditor.UI.Workspace.Coordination
{
    internal sealed class EditorWorkspaceCoordinator : ICanvasWorkspaceHost, IHierarchyHost
    {
        private readonly IEditorWorkspaceHost _host;
        private readonly WorkspaceController _canvasWorkspaceController;
        private readonly MutationCoordinator _mutationCoordinator;
        private readonly SelectionCoordinator _selectionCoordinator;

        private VisualElement RootVisualElement => _host.RootVisualElement;
        private DocumentSession CurrentDocument => _host.CurrentDocument;

        public EditorWorkspaceCoordinator(IEditorWorkspaceHost host)
        {
            _host = host;
            _canvasWorkspaceController = new WorkspaceController(this);
            _mutationCoordinator = new MutationCoordinator(host);
            _selectionCoordinator = new SelectionCoordinator(
                host,
                _canvasWorkspaceController);
        }

        public void Bind(CanvasStageView canvasStageView, Toggle moveToolToggle)
        {
            Dispose();
            _canvasWorkspaceController.Bind(canvasStageView, moveToolToggle);
            _selectionCoordinator.Bind(this, RootVisualElement);
            _selectionCoordinator.UpdateStructureInteractivity(CurrentDocument != null);
        }

        public void Dispose()
        {
            _selectionCoordinator.Unbind();
            _canvasWorkspaceController.Dispose();
        }

        public void RefreshStructureViews() => _selectionCoordinator.RefreshStructureViews();

        public void UpdateStructureInteractivity(bool hasDocument) => _selectionCoordinator.UpdateStructureInteractivity(hasDocument);

        public bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate) =>
            _mutationCoordinator.TryApplyPatchRequest(request, successStatus, recordingMode);

        public bool TryApplyTargetFrameRect(TargetFrameRectRequest request) =>
            _mutationCoordinator.TryApplyTargetFrameRect(request);

        public void ResetCanvasView(bool clearSelection = false) => _canvasWorkspaceController.ResetCanvasView(clearSelection);

        public void FitCanvasView(bool clearSelection = false) => _canvasWorkspaceController.FitCanvasView(clearSelection);

        public void SyncCanvasFrameToPreview() => _canvasWorkspaceController.SyncCanvasFrameToPreview();

        public void ResetSelection() => _canvasWorkspaceController.ResetSelection();

        public void UpdateCanvasVisualState() => _canvasWorkspaceController.UpdateCanvasVisualState();

        public void UpdateSelectionVisual() => _canvasWorkspaceController.UpdateSelectionVisual();

        public bool TryCancelActiveDrag() => _canvasWorkspaceController.TryCancelActiveDrag();
        public bool TryDeleteSelectedElements()
        {
            if (CurrentDocument == null)
            {
                return false;
            }

            if (!CurrentDocument.CanUseDocumentModelForEditing)
            {
                _host.UpdateSourceStatus($"Delete failed: {CurrentDocument.ResolveModelEditingFailureReason()}");
                return true;
            }

            if (_selectionCoordinator.SelectedElementKeys.Count == 0)
            {
                return false;
            }

            HierarchyDeletePlan plan = HierarchyDeletePlanner.Plan(
                _selectionCoordinator.Elements,
                _selectionCoordinator.SelectedElementKeys,
                _selectionCoordinator.SelectedHierarchyNode?.Key ?? _selectionCoordinator.ResolveCanvasSelectedElementKey());

            if (plan.DeleteKeys.Count == 0)
            {
                return false;
            }

            if (!SvgElementDeleteUtility.TryDeleteElements(
                    CurrentDocument.WorkingSourceText,
                    plan.DeleteKeys,
                    out string updatedSourceText,
                    out string error))
            {
                _host.UpdateSourceStatus($"Delete failed: {error}");
                return true;
            }

            _selectionCoordinator.PrepareElementSelectionFallback(plan.FallbackElementKey);
            _host.ApplyUpdatedSource(updatedSourceText, BuildDeleteStatus(plan.DeleteKeys.Count));
            return true;
        }

        internal IReadOnlyList<string> SelectedElementKeys => _selectionCoordinator.SelectedElementKeys;

        internal static bool TryResolveSelection(
            IReadOnlyList<HierarchyNode> elements,
            string selectedElementKey,
            string selectedTargetKey,
            out HierarchyNode selectedItem,
            out SelectionKind selectionKind) =>
            SelectionCoordinator.TryResolveSelection(
                elements,
                selectedElementKey,
                selectedTargetKey,
                out selectedItem,
                out selectionKind);

        DocumentSession ICanvasWorkspaceHost.CurrentDocument => CurrentDocument;
        PreviewSnapshot ICanvasWorkspaceHost.PreviewSnapshot => _host.PreviewSnapshot;
        Image ICanvasWorkspaceHost.PreviewImage => _host.PreviewImage;
        string ICanvasWorkspaceHost.SelectedElementKey => _selectionCoordinator.ResolveCanvasSelectedElementKey();
        IReadOnlyList<string> ICanvasWorkspaceHost.SelectedElementKeys => _selectionCoordinator.SelectedElementKeys;
        HierarchyNode ICanvasWorkspaceHost.SelectedHierarchyNode => _selectionCoordinator.SelectedHierarchyNode;
        string ICanvasWorkspaceHost.FormatNumber(float value) => _host.FormatNumber(value);
        HierarchyNode ICanvasWorkspaceHost.FindHierarchyNode(string elementKey) => _selectionCoordinator.FindHierarchyNode(elementKey);
        void ICanvasWorkspaceHost.UpdateStructureInteractivity(bool hasDocument) => _selectionCoordinator.UpdateStructureInteractivity(hasDocument);
        void ICanvasWorkspaceHost.UpdateSourceStatus(string status) => _host.UpdateSourceStatus(status);
        void ICanvasWorkspaceHost.RefreshLivePreview(bool keepExistingPreviewOnFailure) => _host.RefreshLivePreview(keepExistingPreviewOnFailure);
        bool ICanvasWorkspaceHost.TryRefreshTransientPreview(SvgDocumentModel documentModel) => _host.TryRefreshTransientPreview(documentModel);
        void ICanvasWorkspaceHost.RefreshInspector() => _host.RefreshInspector();
        void ICanvasWorkspaceHost.RefreshInspector(SvgDocumentModel documentModel) => _host.RefreshInspector(documentModel);
        void ICanvasWorkspaceHost.ApplyUpdatedSource(string updatedSource, string successStatus) => _host.ApplyUpdatedSource(updatedSource, successStatus);

        DocumentSession IHierarchyHost.CurrentDocument => CurrentDocument;
        IReadOnlyList<TreeViewItemData<HierarchyNode>> IHierarchyHost.HierarchyItems => _selectionCoordinator.HierarchyItems;
        HierarchyNode IHierarchyHost.FindHierarchyNode(string elementKey) => _selectionCoordinator.FindHierarchyNode(elementKey);
        void IHierarchyHost.ApplyUpdatedSource(string updatedSource, string successStatus) => _host.ApplyUpdatedSource(updatedSource, successStatus);
        void IHierarchyHost.UpdateSourceStatus(string status) => _host.UpdateSourceStatus(status);

        void ICanvasWorkspaceHost.ClearStructureSelectionFromCanvas() => _selectionCoordinator.ClearStructureSelectionFromCanvas();

        void ICanvasWorkspaceHost.SelectFrameFromCanvas() => _selectionCoordinator.SelectFrameFromCanvas();

        void ICanvasWorkspaceHost.SelectStructureElementFromCanvas(string elementKey, bool syncPatchTarget) =>
            _selectionCoordinator.SelectStructureElementFromCanvas(elementKey, syncPatchTarget);

        void ICanvasWorkspaceHost.ToggleStructureElementSelectionFromCanvas(string elementKey, bool syncPatchTarget) =>
            _selectionCoordinator.ToggleStructureElementSelection(elementKey, syncPatchTarget);

        void ICanvasWorkspaceHost.ReplaceStructureElementSelectionFromCanvas(IReadOnlyList<string> elementKeys, bool syncPatchTarget) =>
            _selectionCoordinator.ReplaceStructureElementSelection(elementKeys, syncPatchTarget);

        void ICanvasWorkspaceHost.AddStructureElementSelectionFromCanvas(IReadOnlyList<string> elementKeys, bool syncPatchTarget) =>
            _selectionCoordinator.AddStructureElementSelection(elementKeys, syncPatchTarget);

        internal void SyncSelectionFromInspectorTarget(string targetKey) => _selectionCoordinator.SyncSelectionFromInspectorTarget(targetKey);

        private static string BuildDeleteStatus(int deleteCount)
        {
            return deleteCount == 1
                ? "Deleted 1 SVG element."
                : $"Deleted {deleteCount} SVG elements.";
        }
    }
}
