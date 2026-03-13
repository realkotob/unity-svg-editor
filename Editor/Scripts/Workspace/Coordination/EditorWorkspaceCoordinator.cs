using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Preview;
using SvgEditor.Workspace.Canvas;
using SvgEditor.Workspace.HierarchyPanel;
using SvgEditor.Workspace.Document;
using SvgEditor.Workspace.Host;
using SvgEditor.DocumentModel;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;

namespace SvgEditor.Workspace.Coordination
{
    internal sealed class EditorWorkspaceCoordinator : ICanvasWorkspaceHost, IHierarchyHost
    {
        private readonly IEditorWorkspaceHost _host;
        private readonly WorkspaceController _canvasWorkspaceController;
        private readonly ShellBinder _shellBinder = new();
        private readonly MutationCoordinator _mutationCoordinator;
        private readonly SelectionCoordinator _selectionCoordinator;

        private VisualElement RootVisualElement => _host.RootVisualElement;
        private DocumentSession CurrentDocument => _host.CurrentDocument;

        public EditorWorkspaceCoordinator(IEditorWorkspaceHost host)
        {
            _host = host;
            _canvasWorkspaceController = new WorkspaceController(this);
            _mutationCoordinator = new MutationCoordinator(host, () => CurrentDocument);
            _selectionCoordinator = new SelectionCoordinator(
                host,
                _canvasWorkspaceController,
                _shellBinder,
                () => CurrentDocument);
        }

        public void Bind(CanvasStageView canvasStageView, Toggle moveToolToggle)
        {
            Dispose();
            _canvasWorkspaceController.Bind(canvasStageView, moveToolToggle);
            _shellBinder.Bind(RootVisualElement);
            _selectionCoordinator.Bind(this);
            _selectionCoordinator.UpdateStructureInteractivity(CurrentDocument != null);
        }

        public void Dispose()
        {
            _selectionCoordinator.Unbind();
            _shellBinder.Unbind();
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

        internal void SyncSelectionFromInspectorTarget(string targetKey) => _selectionCoordinator.SyncSelectionFromInspectorTarget(targetKey);
    }
}
