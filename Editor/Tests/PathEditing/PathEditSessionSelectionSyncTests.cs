using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Canvas;
using UnityEngine;

namespace SvgEditor.Editor.Tests.PathEditing
{
    public sealed class PathEditSessionSelectionSyncTests
    {
        private static readonly MethodInfo SyncSelectionMethod = typeof(PathEditSessionSyncController)
            .GetMethod("SyncActiveSessionToSelection", BindingFlags.Instance | BindingFlags.Public);

        [Test]
        public void SyncActiveSessionToSelection_WhenSelectionChanges_EndsPathEditSession()
        {
            var host = new TestCanvasPointerDragHost
            {
                SelectionKind = SelectionKind.Element,
                SelectedElementKeys = new[] { "icon-b" }
            };
            var toolController = new ToolController();
            var overlayController = new OverlayController();
            var controller = new PathEditSessionSyncController(
                host,
                sceneProjector: null,
                toolController: toolController,
                overlayController: overlayController);

            toolController.SetActiveTool(ToolKind.PathEdit);
            overlayController.SetPathEditSession(new PathEditSession("icon-a"));

            InvokeSyncSelection(controller);

            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
        }

        [Test]
        public void SyncActiveSessionToSelection_WhenSelectionClearsDuringDocumentSwitch_EndsPathEditSession()
        {
            var host = new TestCanvasPointerDragHost
            {
                SelectionKind = SelectionKind.None,
                SelectedElementKeys = Array.Empty<string>()
            };
            var toolController = new ToolController();
            var overlayController = new OverlayController();
            var controller = new PathEditSessionSyncController(
                host,
                sceneProjector: null,
                toolController: toolController,
                overlayController: overlayController);

            toolController.SetActiveTool(ToolKind.PathEdit);
            overlayController.SetPathEditSession(new PathEditSession("icon-a"));

            InvokeSyncSelection(controller);

            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
        }

        [Test]
        public void SyncActiveSessionToSelection_WhenSelectionStaysOnSameElement_KeepsPathEditSession()
        {
            var host = new TestCanvasPointerDragHost
            {
                SelectionKind = SelectionKind.Element,
                SelectedElementKeys = new[] { "icon-a" }
            };
            var toolController = new ToolController();
            var overlayController = new OverlayController();
            var controller = new PathEditSessionSyncController(
                host,
                sceneProjector: null,
                toolController: toolController,
                overlayController: overlayController);

            toolController.SetActiveTool(ToolKind.PathEdit);
            overlayController.SetPathEditSession(new PathEditSession("icon-a"));

            InvokeSyncSelection(controller);

            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession?.ElementKey, Is.EqualTo("icon-a"));
        }

        private static void InvokeSyncSelection(PathEditSessionSyncController controller)
        {
            Assert.That(SyncSelectionMethod, Is.Not.Null, "Expected a selection sync entry point for path edit sessions.");
            SyncSelectionMethod.Invoke(controller, Array.Empty<object>());
        }

        private sealed class TestCanvasPointerDragHost : ICanvasPointerDragHost
        {
            public DocumentSession CurrentDocument => null;
            public PreviewSnapshot PreviewSnapshot => null;
            public string SelectedElementKey => SelectedElementKeys.Count > 0 ? SelectedElementKeys[^1] : string.Empty;
            public IReadOnlyList<string> SelectedElementKeys { get; set; } = Array.Empty<string>();
            public SelectionKind SelectionKind { get; set; }
            public bool HasDefinitionProxySelection => false;

            public void RefreshLivePreview(bool keepExistingPreviewOnFailure) { }
            public bool TryRefreshTransientPreview(SvgDocumentModel documentModel) => false;
            public void RefreshInspector() { }
            public void RefreshInspector(SvgDocumentModel documentModel) { }
            public void ApplyUpdatedSource(string updatedSource, string successStatus) { }
            public void UpdateSourceStatus(string status) { }
            public HierarchyNode FindHierarchyNode(string elementKey) => null;
            public void SelectFrame() { }
            public void SelectElement(string elementKey, bool syncPatchTarget) { }
            public void ToggleElementSelection(string elementKey, bool syncPatchTarget) { }
            public void ReplaceElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget) { }
            public void AddElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget) { }
            public void ClearSelection() { }
            public void UpdateStructureInteractivity(bool hasDocument) { }
            public void UpdateViewportVisualState() { }
            public void UpdateCanvasVisualState() { }
            public void UpdateSelectionVisual() { }
            public void SetHoveredElement(string elementKey) { }
            public void ClearHover() { }
            public void UpdateHoverVisual() { }
            public bool TryHitTestDefinitionOverlay(Vector2 localPoint, out CanvasDefinitionOverlayVisual overlay)
            {
                overlay = null;
                return false;
            }

            public bool TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual overlay)
            {
                overlay = null;
                return false;
            }

            public void SelectDefinitionProxy(CanvasDefinitionOverlayVisual overlay) { }
            public void ClearDefinitionProxySelection() { }
        }
    }
}
