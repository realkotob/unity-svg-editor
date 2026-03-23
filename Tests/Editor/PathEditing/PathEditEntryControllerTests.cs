using System.Collections.Generic;
using NUnit.Framework;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Canvas;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class PathEditEntryControllerTests
    {
        [Test]
        public void TryEnter_IgnoresSingleClick_AndLeavesExistingToolAndStateUnchanged()
        {
            ToolController toolController = new();
            OverlayController overlayController = new();
            PathEditEntryController controller = new(toolController, overlayController);

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 1,
                currentDocument: CreateDocument("shape", "path", "M 0 0 L 10 0"),
                elementKey: "shape",
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Ignored));
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
        }

        [Test]
        public void TryEnter_BlocksMalformedPathData_WithClearStatus()
        {
            ToolController toolController = new();
            OverlayController overlayController = new();
            PathEditEntryController controller = new(toolController, overlayController);

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: CreateDocument("shape", "path", "M 0 0 Z 1 2"),
                elementKey: "shape",
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.BlockedMalformedPathData));
            Assert.That(result.StatusMessage, Does.Contain("malformed"));
            Assert.That(result.StatusMessage, Does.Contain("read-only"));
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
        }

        [Test]
        public void TryEnter_ActivatesPathEditSession_AndPublishesOverlayState_ForSupportedPath()
        {
            ToolController toolController = new();
            OverlayController overlayController = new();
            PathEditEntryController controller = new(toolController, overlayController);

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: CreateDocument("shape", "path", "M 0 0 C 2 3 8 3 10 0 L 15 5 Z"),
                elementKey: "shape",
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session, Is.Not.Null);
            Assert.That(result.Session.ElementKey, Is.EqualTo("shape"));
            Assert.That(result.Session.Subpaths, Has.Count.EqualTo(1));
            Assert.That(result.Session.Subpaths[0].Nodes, Has.Count.EqualTo(3));
            Assert.That(result.Session.Subpaths[0].Segments.Count, Is.GreaterThan(3));
            Assert.That(result.Session.Subpaths[0].Nodes[0].HasOutHandle, Is.True);
            Assert.That(result.Session.Subpaths[0].Nodes[1].HasInHandle, Is.True);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession, Is.SameAs(result.Session));
        }

        [Test]
        public void BindMoveTool_DoesNotCreateToolbarBinding_ForPathEdit()
        {
            ToolController controller = new();
            Toggle moveToggle = new();

            controller.BindMoveTool(moveToggle);

            Assert.That(controller.IsToolBound(ToolKind.Move), Is.True);
            Assert.That(controller.IsToolBound(ToolKind.PathEdit), Is.False);
        }

        [Test]
        public void UpdateVisualState_DoesNotMarkOverlayReadonly_WhenPathEditIsActive()
        {
            ToolController controller = new();
            VisualElement overlay = new();

            controller.SetActiveTool(ToolKind.PathEdit);
            controller.UpdateVisualState(overlay);

            Assert.That(overlay.ClassListContains("svg-editor__canvas-overlay--readonly"), Is.False);
        }

        private static DocumentSession CreateDocument(string elementKey, string tagName, string pathData)
        {
            SvgNodeId nodeId = SvgNodeId.FromXmlId("shape");
            SvgNodeModel pathNode = new()
            {
                Id = nodeId,
                ParentId = SvgNodeId.Root,
                XmlId = "shape",
                LegacyElementKey = elementKey,
                LegacyTargetKey = elementKey,
                TagName = tagName,
                RawAttributes = new Dictionary<string, string>
                {
                    [SvgAttributeName.D] = pathData
                }
            };
            SvgNodeModel rootNode = new()
            {
                Id = SvgNodeId.Root,
                TagName = "svg",
                Children = new[] { nodeId }
            };

            SvgDocumentModel documentModel = new()
            {
                SourceText = "<svg />",
                Nodes = new Dictionary<SvgNodeId, SvgNodeModel>
                {
                    [SvgNodeId.Root] = rootNode,
                    [nodeId] = pathNode
                },
                NodeOrder = new[] { SvgNodeId.Root, nodeId },
                NodeIdsByXmlId = new Dictionary<string, SvgNodeId>
                {
                    ["shape"] = nodeId
                }
            };

            return new DocumentSession
            {
                WorkingSourceText = "<svg />",
                DocumentModel = documentModel
            };
        }
    }
}
