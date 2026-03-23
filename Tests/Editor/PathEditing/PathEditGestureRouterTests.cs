using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Preview.Geometry;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Canvas;
using SvgEditor.UI.Workspace.Document;
using System.Reflection;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class PathEditGestureRouterTests
    {
        [Test]
        public void HandleCanvasPointerDown_EntersPathEditBeforeMove_WhenSelectedPathIsDoubleClicked()
        {
            ViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 100f, 100f));

            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            OverlayController overlayController = new();
            ToolController toolController = new();
            DragController dragController = new(sceneProjector);
            PointerDragSession dragSession = new();
            FakeCanvasPointerDragHost host = new(CreateDocument("M 0 0 L 10 10"), CreatePreviewSnapshot());
            SelectionSyncService selectionSyncService = new(host, overlayController, dragController);
            GestureRouter router = new(new GestureRouterDependencies(
                host,
                viewportState,
                overlayController,
                sceneProjector,
                toolController,
                dragController,
                selectionSyncService,
                dragSession,
                () => null));

            bool handled = router.HandleCanvasPointerDown(
                new CanvasPointerDownContext(
                    pointerId: 7,
                    button: (int)MouseButton.LeftMouse,
                    clickCount: 2,
                    modifiers: EventModifiers.None,
                    target: null),
                new Vector2(20f, 20f));

            Assert.That(handled, Is.True);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(dragSession.IsActive, Is.False);
            Assert.That(host.LastStatusMessage, Is.Empty);
        }

        [Test]
        public void HandleCanvasPointerDown_FirstDoubleClickOnGroupedPath_SelectsLeafWithoutEnteringPathEdit()
        {
            GestureRouter router = CreateGroupedRouter(
                leafTagName: "path",
                leafAttributes: "d=\"M 10 10 L 30 10\"",
                out ToolController toolController,
                out OverlayController overlayController,
                out PointerDragSession dragSession,
                out FakeCanvasPointerDragHost host,
                out _);

            bool handled = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 11, clickCount: 2),
                new Vector2(20f, 20f));

            Assert.That(handled, Is.True);
            Assert.That(host.SelectedElementKey, Is.EqualTo("shape"));
            Assert.That(host.SelectedElementKeys, Is.EqualTo(new[] { "shape" }));
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
            Assert.That(dragSession.IsActive, Is.False);
            Assert.That(host.LastStatusMessage, Is.Empty);
        }

        [Test]
        public void HandleCanvasPointerDown_SecondDoubleClickOnDrilledInGroupedPath_EntersPathEdit()
        {
            GestureRouter router = CreateGroupedRouter(
                leafTagName: "path",
                leafAttributes: "d=\"M 10 10 L 30 10\"",
                out ToolController toolController,
                out OverlayController overlayController,
                out PointerDragSession _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool firstHandled = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 12, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(firstHandled, Is.True);
            InvokeRouterPointerUp(router, pointerId: 12, new Vector2(20f, 20f), hasLocalPosition: true);

            bool secondHandled = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 13, clickCount: 2),
                new Vector2(20f, 20f));

            Assert.That(secondHandled, Is.True);
            Assert.That(host.SelectedElementKey, Is.EqualTo("shape"));
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.ElementKey, Is.EqualTo("shape"));
        }

        [Test]
        public void HandleCanvasPointerDown_SecondDoubleClickOnDrilledInGroupedNonPath_EntersPathEditForEditablePrimitive()
        {
            GestureRouter router = CreateGroupedRouter(
                leafTagName: "rect",
                leafAttributes: "x=\"10\" y=\"10\" width=\"20\" height=\"20\"",
                out ToolController toolController,
                out OverlayController overlayController,
                out PointerDragSession _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool firstHandled = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 14, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(firstHandled, Is.True);
            InvokeRouterPointerUp(router, pointerId: 14, new Vector2(20f, 20f), hasLocalPosition: true);

            bool secondHandled = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 15, clickCount: 2),
                new Vector2(20f, 20f));

            Assert.That(secondHandled, Is.True);
            Assert.That(host.SelectedElementKey, Is.EqualTo("shape"));
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(host.LastStatusMessage, Is.Empty);
        }

        [Test]
        public void HandleCanvasPointerDown_CommandDoubleClickOnGroupedPath_PreservesDirectSelectionPathEditBehavior()
        {
            GestureRouter router = CreateGroupedRouter(
                leafTagName: "path",
                leafAttributes: "d=\"M 10 10 L 30 10\"",
                out ToolController toolController,
                out OverlayController overlayController,
                out PointerDragSession dragSession,
                out FakeCanvasPointerDragHost host,
                out _);

            bool handled = router.HandleCanvasPointerDown(
                new CanvasPointerDownContext(
                    pointerId: 16,
                    button: (int)MouseButton.LeftMouse,
                    clickCount: 2,
                    modifiers: EventModifiers.Command,
                    target: null),
                new Vector2(20f, 20f));

            Assert.That(handled, Is.True);
            Assert.That(host.SelectedElementKey, Is.EqualTo("shape"));
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(dragSession.IsActive, Is.False);
        }

        [Test]
        public void HandleCanvasPointerDown_ClearsPriorBlockedStatus_AfterLaterSuccessfulEntry()
        {
            ViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 100f, 100f));

            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            OverlayController overlayController = new();
            ToolController toolController = new();
            DragController dragController = new(sceneProjector);
            PointerDragSession dragSession = new();
            FakeCanvasPointerDragHost host = new(CreateDocument("M 0 0 Z 1 2"), CreatePreviewSnapshot());
            SelectionSyncService selectionSyncService = new(host, overlayController, dragController);
            GestureRouter router = new(new GestureRouterDependencies(
                host,
                viewportState,
                overlayController,
                sceneProjector,
                toolController,
                dragController,
                selectionSyncService,
                dragSession,
                () => null));

            bool blocked = router.HandleCanvasPointerDown(
                new CanvasPointerDownContext(
                    pointerId: 7,
                    button: (int)MouseButton.LeftMouse,
                    clickCount: 2,
                    modifiers: EventModifiers.None,
                    target: null),
                new Vector2(20f, 20f));

            Assert.That(blocked, Is.True);
            Assert.That(host.LastStatusMessage, Does.Contain("read-only"));
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));

            host.SetCurrentDocument(CreateDocument("M 0 0 L 10 10"));

            bool entered = router.HandleCanvasPointerDown(
                new CanvasPointerDownContext(
                    pointerId: 8,
                    button: (int)MouseButton.LeftMouse,
                    clickCount: 2,
                    modifiers: EventModifiers.None,
                    target: null),
                new Vector2(20f, 20f));

            Assert.That(entered, Is.True);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(host.LastStatusMessage, Is.Empty);
        }

        [Test]
        public void HandleCanvasPointerDown_SelectsPathNodeAndHandle_WhenPathEditModeIsAlreadyActive()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out PointerDragSession dragSession,
                out FakeCanvasPointerDragHost _,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));

            bool selectedNode = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(10f, 10f));

            Assert.That(selectedNode, Is.True);
            Assert.That(dragSession.IsActive, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Selection.ActiveNode, Is.EqualTo(new PathNodeRef(0, 0)));
            Assert.That(overlayController.CurrentPathEditSession.Selection.ActiveHandle, Is.Null);

            bool releasedNode = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(10f, 10f), hasLocalPosition: true);
            Assert.That(releasedNode, Is.True);
            Assert.That(dragSession.IsActive, Is.False);

            router = CreateRouter(
                "M 10 10 C 14 10 26 10 30 10",
                out toolController,
                out overlayController,
                out dragSession,
                out _,
                out _);

            entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool selectedHandle = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(14f, 10f));

            Assert.That(selectedHandle, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Selection.ActiveNode, Is.EqualTo(new PathNodeRef(0, 0)));
            Assert.That(
                overlayController.CurrentPathEditSession.Selection.ActiveHandle,
                Is.EqualTo(new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out)));
        }

        [Test]
        public void RoutedPathDrag_UpdatesCommittedSourceAndOverlayGeometry_WhenNodeIsDragged()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out _,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(30f, 10f));
            Assert.That(pressed, Is.True);

            bool moved = InvokeRouterPointerMove(router, pointerId: 0, new Vector2(40f, 20f), EventModifiers.None);
            Assert.That(moved, Is.True);
            Assert.That(host.TransientPreviewRefreshCount, Is.GreaterThan(0));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(40f, 20f)));

            bool released = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(40f, 20f), hasLocalPosition: true);

            Assert.That(released, Is.True);
            Assert.That(host.LastAppliedSource, Does.Contain("d=\"M 10 10 L 40 20\""));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(40f, 20f)));
        }

        [Test]
        public void RoutedPathDrag_PreservesTrailingAnchorAcrossCommitAndResync_WhenMiddleAnchorOfHorizontalChainIsDragged()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 H 30 H 50",
                out ToolController toolController,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(30f, 10f));
            Assert.That(pressed, Is.True);
            Assert.That(
                overlayController.CurrentPathEditSession.Selection.ActiveNode,
                Is.EqualTo(new PathNodeRef(0, 1)));

            bool moved = InvokeRouterPointerMove(router, pointerId: 0, new Vector2(40f, 20f), EventModifiers.None);

            Assert.That(moved, Is.True);
            Assert.That(host.TransientPreviewRefreshCount, Is.GreaterThan(0));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(40f, 20f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[2].Position, Is.EqualTo(new Vector2(50f, 10f)));

            bool released = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(40f, 20f), hasLocalPosition: true);

            Assert.That(released, Is.True);
            Assert.That(host.LastAppliedSource, Does.Contain("d=\"M 10 10 L 40 20 L 50 10\""));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[2].Position, Is.EqualTo(new Vector2(50f, 10f)));

            CreatePathEditSessionSyncController(host, toolController, overlayController)
                .ResyncActiveSession(previewIsCurrent: true);

            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(40f, 20f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[2].Position, Is.EqualTo(new Vector2(50f, 10f)));
        }

        [Test]
        public void RoutedPathDrag_PreservesTrailingAnchorAcrossCommitAndResync_WhenMiddleAnchorOfVerticalChainIsDragged()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 V 30 V 50",
                out ToolController toolController,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(10f, 30f));
            Assert.That(pressed, Is.True);
            Assert.That(
                overlayController.CurrentPathEditSession.Selection.ActiveNode,
                Is.EqualTo(new PathNodeRef(0, 1)));

            bool moved = InvokeRouterPointerMove(router, pointerId: 0, new Vector2(20f, 40f), EventModifiers.None);

            Assert.That(moved, Is.True);
            Assert.That(host.TransientPreviewRefreshCount, Is.GreaterThan(0));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(20f, 40f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[2].Position, Is.EqualTo(new Vector2(10f, 50f)));

            bool released = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(20f, 40f), hasLocalPosition: true);

            Assert.That(released, Is.True);
            Assert.That(host.LastAppliedSource, Does.Contain("d=\"M 10 10 L 20 40 L 10 50\""));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[2].Position, Is.EqualTo(new Vector2(10f, 50f)));

            CreatePathEditSessionSyncController(host, toolController, overlayController)
                .ResyncActiveSession(previewIsCurrent: true);

            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(20f, 40f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[2].Position, Is.EqualTo(new Vector2(10f, 50f)));
        }

        [Test]
        public void RoutedPathDrag_UpdatesCommittedSource_WhenHandleIsDragged()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 C 14 10 26 10 30 10",
                out _,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(14f, 10f));
            Assert.That(pressed, Is.True);

            bool moved = InvokeRouterPointerMove(router, pointerId: 0, new Vector2(18f, 12f), EventModifiers.None);
            Assert.That(moved, Is.True);
            Assert.That(
                overlayController.CurrentPathEditSession.Selection.ActiveHandle,
                Is.EqualTo(new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out)));

            bool released = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(18f, 12f), hasLocalPosition: true);

            Assert.That(released, Is.True);
            Assert.That(host.LastAppliedSource, Does.Contain("d=\"M 10 10 C 18 12 26 10 30 10\""));
        }

        [Test]
        public void RoutedPathDrag_UpdatesLinkedHandlesDuringLivePreview_WhenMiddleAnchorIsDragged()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 C 14 10 10 30 30 30 S 70 50 70 30",
                out _,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(30f, 30f));
            Assert.That(pressed, Is.True);
            Assert.That(
                overlayController.CurrentPathEditSession.Selection.ActiveNode,
                Is.EqualTo(new PathNodeRef(0, 1)));
            Assert.That(overlayController.CurrentPathEditSession.Selection.ActiveHandle, Is.Null);

            bool moved = InvokeRouterPointerMove(router, pointerId: 0, new Vector2(40f, 40f), EventModifiers.None);

            Assert.That(moved, Is.True);
            Assert.That(host.TransientPreviewRefreshCount, Is.GreaterThan(0));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(40f, 40f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].InHandle, Is.EqualTo(new Vector2(20f, 40f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].OutHandle, Is.EqualTo(new Vector2(60f, 40f)));

            bool released = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(40f, 40f), hasLocalPosition: true);

            Assert.That(released, Is.True);
            Assert.That(host.LastAppliedSource, Does.Contain("d=\"M 10 10 C 14 10 20 40 40 40 C 60 40 70 50 70 30\""));
        }

        [Test]
        public void RoutedPathDrag_KeepsOppositeHandleFixedDuringLivePreview_WhenMiddleAnchorIncomingHandleIsDragged()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 C 14 10 26 10 30 10 S 46 10 50 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(26f, 10f));
            Assert.That(pressed, Is.True);
            Assert.That(
                overlayController.CurrentPathEditSession.Selection.ActiveHandle,
                Is.EqualTo(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.In)));

            bool moved = InvokeRouterPointerMove(router, pointerId: 0, new Vector2(24f, 18f), EventModifiers.None);

            Assert.That(moved, Is.True);
            Assert.That(host.TransientPreviewRefreshCount, Is.GreaterThan(0));
            Assert.That(overlayController.CurrentPathEditSession.PathData.Subpaths[0].Nodes[1].Command, Is.EqualTo('C'));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].InHandle, Is.EqualTo(new Vector2(24f, 18f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].OutHandle, Is.EqualTo(new Vector2(34f, 10f)));

            bool released = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(24f, 18f), hasLocalPosition: true);

            Assert.That(released, Is.True);
            Assert.That(host.LastAppliedSource, Does.Contain("d=\"M 10 10 C 14 10 24 18 30 10 C 34 10 46 10 50 10\""));

            CreatePathEditSessionSyncController(host, toolController, overlayController)
                .ResyncActiveSession(previewIsCurrent: true);

            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].InHandle, Is.EqualTo(new Vector2(24f, 18f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].OutHandle, Is.EqualTo(new Vector2(34f, 10f)));
        }

        [Test]
        public void RoutedPathDrag_BreaksSmoothShorthand_WhenSmoothSegmentIncomingHandleIsDragged()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 C 14 10 26 10 30 10 S 46 10 50 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(46f, 10f));
            Assert.That(pressed, Is.True);
            Assert.That(
                overlayController.CurrentPathEditSession.Selection.ActiveHandle,
                Is.EqualTo(new PathHandleRef(new PathNodeRef(0, 2), PathHandleSlot.In)));

            bool moved = InvokeRouterPointerMove(router, pointerId: 0, new Vector2(44f, 18f), EventModifiers.None);

            Assert.That(moved, Is.True);
            Assert.That(host.TransientPreviewRefreshCount, Is.GreaterThan(0));
            Assert.That(overlayController.CurrentPathEditSession.PathData.Subpaths[0].Nodes[1].Command, Is.EqualTo('C'));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].OutHandle, Is.EqualTo(new Vector2(34f, 10f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[2].InHandle, Is.EqualTo(new Vector2(44f, 18f)));

            bool released = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(44f, 18f), hasLocalPosition: true);

            Assert.That(released, Is.True);
            Assert.That(host.LastAppliedSource, Does.Contain("d=\"M 10 10 C 14 10 26 10 30 10 C 34 10 44 18 50 10\""));

            CreatePathEditSessionSyncController(host, toolController, overlayController)
                .ResyncActiveSession(previewIsCurrent: true);

            Assert.That(overlayController.CurrentPathEditSession.PathData.Subpaths[0].Nodes[1].Command, Is.EqualTo('C'));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].OutHandle, Is.EqualTo(new Vector2(34f, 10f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[2].InHandle, Is.EqualTo(new Vector2(44f, 18f)));
        }

        [Test]
        public void HandleEscapeKey_ExitsPathEditMode_WhenIdle()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out _,
                out _,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool handled = InvokeRouterEscape(router);

            Assert.That(handled, Is.True);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
        }

        [Test]
        public void HandleCanvasPointerDown_SelectingDifferentObject_ExitsPathEditMode_AndSelectsThatObject()
        {
            GestureRouter router = CreatePathAndRectRouter(
                out ToolController toolController,
                out OverlayController overlayController,
                out PointerDragSession dragSession,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);

            bool switched = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(120f, 20f));

            Assert.That(switched, Is.True);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
            Assert.That(dragSession.IsActive, Is.False);
            Assert.That(host.SelectedElementKey, Is.EqualTo("other"));
        }

        [Test]
        public void HandleEscapeKey_CancelsActivePathDrag_WhenDragging()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out PointerDragSession dragSession,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(30f, 10f));
            Assert.That(pressed, Is.True);

            bool moved = InvokeRouterPointerMove(router, pointerId: 0, new Vector2(40f, 20f), EventModifiers.None);
            Assert.That(moved, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(40f, 20f)));

            bool handled = InvokeRouterEscape(router);

            Assert.That(handled, Is.True);
            Assert.That(dragSession.IsActive, Is.False);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(30f, 10f)));
            Assert.That(host.RefreshLivePreviewCount, Is.GreaterThan(0));
            Assert.That(host.LastAppliedSource, Is.Empty);
        }

        [Test]
        public void CommittedPathEdit_CanUndoAndRedo_ThroughDocumentHistory()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out _,
                out _,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(30f, 10f));
            Assert.That(pressed, Is.True);
            Assert.That(InvokeRouterPointerMove(router, pointerId: 0, new Vector2(40f, 20f), EventModifiers.None), Is.True);
            Assert.That(InvokeRouterPointerUp(router, pointerId: 0, new Vector2(40f, 20f), hasLocalPosition: true), Is.True);

            Assert.That(host.CurrentDocument.WorkingSourceText, Does.Contain("d=\"M 10 10 L 40 20\""));
            Assert.That(host.TryUndo(), Is.True);
            Assert.That(host.CurrentDocument.WorkingSourceText, Does.Contain("d=\"M 10 10 L 30 10\""));
            Assert.That(host.TryRedo(), Is.True);
            Assert.That(host.CurrentDocument.WorkingSourceText, Does.Contain("d=\"M 10 10 L 40 20\""));
        }

        [Test]
        public void ResyncActiveSession_RebuildsActivePathEditSession_AfterUndo_WhenTargetPathRemainsSupported()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            Assert.That(router.HandleCanvasPointerDown(CreatePointerDownContext(pointerId: 0, clickCount: 2), new Vector2(20f, 20f)), Is.True);
            Assert.That(router.HandleCanvasPointerDown(CreatePointerDownContext(pointerId: 0, clickCount: 1), new Vector2(30f, 10f)), Is.True);
            Assert.That(InvokeRouterPointerMove(router, pointerId: 0, new Vector2(40f, 20f), EventModifiers.None), Is.True);
            Assert.That(InvokeRouterPointerUp(router, pointerId: 0, new Vector2(40f, 20f), hasLocalPosition: true), Is.True);

            overlayController.CurrentPathEditSession.Selection.SelectNode(new PathNodeRef(0, 1));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(40f, 20f)));

            Assert.That(host.TryUndo(), Is.True);
            CreatePathEditSessionSyncController(host, toolController, overlayController)
                .ResyncActiveSession(previewIsCurrent: true);

            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(30f, 10f)));
            Assert.That(overlayController.CurrentPathEditSession.Selection.ActiveNode, Is.EqualTo(new PathNodeRef(0, 1)));
        }

        [TestCase("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>", "no longer available")]
        [TestCase("<svg xmlns=\"http://www.w3.org/2000/svg\"><path id=\"shape\" d=\"M 0 0 Z 1 2\" /></svg>", "malformed")]
        public void ResyncActiveSession_ClearsPathEditMode_WhenRefreshLeavesTargetUnavailable(string refreshedSource, string expectedStatusFragment)
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            Assert.That(router.HandleCanvasPointerDown(CreatePointerDownContext(pointerId: 0, clickCount: 2), new Vector2(20f, 20f)), Is.True);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);

            host.SetCurrentDocument(CreateDocumentFromSource(refreshedSource));
            CreatePathEditSessionSyncController(host, toolController, overlayController)
                .ResyncActiveSession(previewIsCurrent: true);

            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
            Assert.That(host.LastStatusMessage, Does.Contain(expectedStatusFragment));
        }

        [Test]
        public void ResyncActiveSession_ReprojectsOverlayGeometry_AfterPreviewRefresh()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out _,
                out FakeCanvasPointerDragHost host,
                out _);

            Assert.That(router.HandleCanvasPointerDown(CreatePointerDownContext(pointerId: 0, clickCount: 2), new Vector2(20f, 20f)), Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[0].Position, Is.EqualTo(new Vector2(10f, 10f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(30f, 10f)));

            host.SetPreviewSnapshot(CreatePreviewSnapshot(Matrix2D.Translate(new Vector2(50f, 5f))));
            CreatePathEditSessionSyncController(host, toolController, overlayController)
                .ResyncActiveSession(previewIsCurrent: true);

            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[0].Position, Is.EqualTo(new Vector2(60f, 15f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(80f, 15f)));
        }

        [Test]
        public void ResyncPathEditSession_RebuildsHandles_AfterExternalSourceAndGeometryUpdate()
        {
            ViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 100f, 100f));
            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            OverlayController overlayController = new();
            FakeCanvasPointerDragHost host = new(
                CreateDocument("M 10 10 C 14 10 26 10 30 10"),
                CreatePreviewSnapshot());
            PointerDragController pointerDragController = new(host, viewportState, overlayController, sceneProjector);
            ToolController toolController = GetPrivateField<ToolController>(pointerDragController, "_toolController");
            PathEditEntryController entryController = new(toolController, overlayController);

            PathEditEntryResult entered = entryController.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: host.CurrentDocument,
                elementKey: "shape",
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: scenePoint =>
                    sceneProjector.TryScenePointToViewportPoint(host.PreviewSnapshot, scenePoint, out Vector2 viewportPoint)
                        ? viewportPoint
                        : null));

            Assert.That(entered.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            overlayController.CurrentPathEditSession.Selection.SelectHandle(new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out));

            host.ApplyUpdatedSource(BuildSvgSource("M 10 10 C 24 20 36 20 50 25"), "Moved <path>.");
            host.SetPreviewSnapshot(CreatePreviewSnapshot(Matrix2D.Translate(new Vector2(5f, 0f))));

            string status = pointerDragController.ResyncPathEditSession(previewIsCurrent: true);

            Assert.That(status, Is.Empty);
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(
                overlayController.CurrentPathEditSession.Selection.ActiveHandle,
                Is.EqualTo(new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[0].Position, Is.EqualTo(new Vector2(15f, 10f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[0].OutHandle, Is.EqualTo(new Vector2(29f, 20f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].InHandle, Is.EqualTo(new Vector2(41f, 20f)));
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(55f, 25f)));
        }

        [Test]
        public void Bind_PreservesToolChangeHandling_ForPathEditSessionCleanup()
        {
            ViewportState viewportState = new();
            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            OverlayController overlayController = new();
            FakeCanvasPointerDragHost host = new(
                CreateDocument("M 10 10 L 30 10"),
                CreatePreviewSnapshot());
            PointerDragController pointerDragController = new(host, viewportState, overlayController, sceneProjector);
            ToolController toolController = GetPrivateField<ToolController>(pointerDragController, "_toolController");

            pointerDragController.Bind(canvasStageView: null, moveToolToggle: new Toggle());
            toolController.SetActiveTool(ToolKind.PathEdit);
            overlayController.SetPathEditSession(new PathEditSession("shape"));

            toolController.SetActiveTool(ToolKind.Move);

            Assert.That(overlayController.HasPathEditSession, Is.False);
        }

        [Test]
        public void UpdateCanvasVisualState_DoesNotAbandonActivePathDrag_DuringTransientPreviewRefresh()
        {
            ViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 100f, 100f));
            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            OverlayController overlayController = new();
            FakeCanvasWorkspaceHost host = new(
                CreateDocument("M 10 10 L 30 10"),
                CreatePreviewSnapshot());
            InteractionController interactionController = new(host, viewportState, overlayController, sceneProjector);
            PointerDragController pointerDragController = GetPrivateField<PointerDragController>(interactionController, "_pointerDragController");
            GestureRouter router = GetPrivateField<GestureRouter>(pointerDragController, "_gestureRouter");

            Assert.That(
                router.HandleCanvasPointerDown(
                    CreatePointerDownContext(pointerId: 0, clickCount: 2),
                    new Vector2(20f, 20f)),
                Is.True);
            Assert.That(
                router.HandleCanvasPointerDown(
                    CreatePointerDownContext(pointerId: 0, clickCount: 1),
                    new Vector2(30f, 10f)),
                Is.True);
            Assert.That(
                InvokeRouterPointerMove(router, pointerId: 0, new Vector2(35f, 15f), EventModifiers.None),
                Is.True);

            interactionController.UpdateCanvasVisualState();

            Assert.That(
                InvokeRouterPointerMove(router, pointerId: 0, new Vector2(40f, 20f), EventModifiers.None),
                Is.True);
            Assert.That(
                InvokeRouterPointerUp(router, pointerId: 0, new Vector2(40f, 20f), hasLocalPosition: true),
                Is.True);
            Assert.That(host.LastAppliedSource, Does.Contain("d=\"M 10 10 L 40 20\""));
        }

        [Test]
        public void HandleLoadFailure_ClearsActivePathEditSession_ThroughSyncPath()
        {
            FakeCanvasPointerDragHost host = new(
                CreateDocument("M 10 10 L 30 10"),
                CreatePreviewSnapshot());
            ToolController toolController = new();
            OverlayController overlayController = new();
            PathEditEntryController entryController = new(toolController, overlayController);

            PathEditEntryResult entered = entryController.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: host.CurrentDocument,
                elementKey: "shape",
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(entered.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            PathEditSessionSyncController syncController = CreatePathEditSessionSyncController(host, toolController, overlayController);
            int interactivityUpdates = 0;
            DocumentSyncService service = new(
                new DocumentLifecycleView(),
                previewService: null,
                inspectorPanelController: new SvgEditor.UI.Inspector.PanelController(new SvgEditor.UI.Inspector.PanelState()),
                currentDocumentAccessor: () => null,
                workspaceCoordinatorAccessor: () => null,
                updateEditorInteractivity: () => interactivityUpdates++,
                resyncPathEditSession: syncController.ResyncActiveSession);

            service.HandleLoadFailure("reload failed");

            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
            Assert.That(host.LastStatusMessage, Does.Contain("Path edit ended"));
            Assert.That(interactivityUpdates, Is.EqualTo(1));
        }

        [Test]
        public void ResyncActiveSession_ClearsPathEditMode_WhenPreviewIsNotCurrent()
        {
            FakeCanvasPointerDragHost host = new(
                CreateDocument("M 10 10 L 30 10"),
                CreatePreviewSnapshot());
            ToolController toolController = new();
            OverlayController overlayController = new();
            PathEditEntryController entryController = new(toolController, overlayController);

            PathEditEntryResult entered = entryController.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: host.CurrentDocument,
                elementKey: "shape",
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(entered.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));

            string status = CreatePathEditSessionSyncController(host, toolController, overlayController)
                .ResyncActiveSession(previewIsCurrent: false);

            Assert.That(status, Does.Contain("preview is unavailable"));
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.Move));
            Assert.That(overlayController.HasPathEditSession, Is.False);
            Assert.That(host.LastStatusMessage, Does.Contain("preview is unavailable"));
        }

        [Test]
        public void CommitFailure_RestoresCoherentSessionState_AndSurfacesStatus()
        {
            GestureRouter router = CreateRouter(
                "M 10 10 L 30 10",
                out ToolController toolController,
                out OverlayController overlayController,
                out PointerDragSession dragSession,
                out FakeCanvasPointerDragHost host,
                out _);

            bool entered = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 2),
                new Vector2(20f, 20f));
            Assert.That(entered, Is.True);

            bool pressed = router.HandleCanvasPointerDown(
                CreatePointerDownContext(pointerId: 0, clickCount: 1),
                new Vector2(30f, 10f));
            Assert.That(pressed, Is.True);
            Assert.That(InvokeRouterPointerMove(router, pointerId: 0, new Vector2(40f, 20f), EventModifiers.None), Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(40f, 20f)));

            host.SetCurrentDocument(new DocumentSession
            {
                WorkingSourceText = host.CurrentDocument.WorkingSourceText,
                DocumentModel = null,
                DocumentModelLoadError = "Document model unavailable."
            });

            bool released = InvokeRouterPointerUp(router, pointerId: 0, new Vector2(40f, 20f), hasLocalPosition: true);

            Assert.That(released, Is.True);
            Assert.That(dragSession.IsActive, Is.False);
            Assert.That(toolController.ActiveTool, Is.EqualTo(ToolKind.PathEdit));
            Assert.That(overlayController.HasPathEditSession, Is.True);
            Assert.That(overlayController.CurrentPathEditSession.Subpaths[0].Nodes[1].Position, Is.EqualTo(new Vector2(30f, 10f)));
            Assert.That(host.LastAppliedSource, Is.Empty);
            Assert.That(host.LastStatusMessage, Does.Contain("Path edit commit failed"));
        }

        private static DocumentSession CreateDocument(string pathData)
        {
            return CreateDocumentFromSource(BuildSvgSource(pathData));
        }

        private static DocumentSession CreateDocumentFromSource(string sourceText)
        {
            SvgLoader loader = new();
            Assert.That(loader.TryLoad(sourceText, out SvgDocumentModel documentModel, out string error), Is.True, error);
            return new DocumentSession
            {
                WorkingSourceText = sourceText,
                DocumentModel = documentModel
            };
        }

        private static PathEditSessionSyncController CreatePathEditSessionSyncController(
            FakeCanvasPointerDragHost host,
            ToolController toolController,
            OverlayController overlayController)
        {
            ViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 100f, 100f));
            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            return new PathEditSessionSyncController(host, sceneProjector, toolController, overlayController);
        }

        private static PreviewSnapshot CreatePreviewSnapshot(Matrix2D worldTransform = default)
        {
            Matrix2D resolvedWorldTransform = worldTransform.Equals(default(Matrix2D))
                ? Matrix2D.identity
                : worldTransform;
            return new PreviewSnapshot
            {
                ProjectionRect = new Rect(0f, 0f, 100f, 100f),
                VisualContentBounds = new Rect(0f, 0f, 100f, 100f),
                Elements = new[]
                {
                    new PreviewElementGeometry
                    {
                        Key = "shape",
                        TargetKey = "shape",
                        VisualBounds = new Rect(10f, 10f, 20f, 20f),
                        DrawOrder = 1,
                        WorldTransform = resolvedWorldTransform,
                        ParentWorldTransform = Matrix2D.identity,
                        HitGeometry = new[]
                        {
                            new[]
                            {
                                new Vector2(10f, 10f),
                                new Vector2(30f, 10f),
                                new Vector2(30f, 30f)
                            },
                            new[]
                            {
                                new Vector2(10f, 10f),
                                new Vector2(30f, 30f),
                                new Vector2(10f, 30f)
                            }
                        }
                    }
                }
            };
        }

        private static PreviewSnapshot CreateGroupedPreviewSnapshot(Matrix2D worldTransform = default)
        {
            Matrix2D resolvedWorldTransform = worldTransform.Equals(default(Matrix2D))
                ? Matrix2D.identity
                : worldTransform;
            return new PreviewSnapshot
            {
                ProjectionRect = new Rect(0f, 0f, 100f, 100f),
                VisualContentBounds = new Rect(0f, 0f, 100f, 100f),
                Elements = new[]
                {
                    new PreviewElementGeometry
                    {
                        Key = "group",
                        TargetKey = "group",
                        VisualBounds = new Rect(10f, 10f, 40f, 40f),
                        DrawOrder = 1,
                        WorldTransform = resolvedWorldTransform,
                        ParentWorldTransform = Matrix2D.identity,
                        HitGeometry = new[]
                        {
                            new[]
                            {
                                new Vector2(10f, 10f),
                                new Vector2(50f, 10f),
                                new Vector2(50f, 50f)
                            },
                            new[]
                            {
                                new Vector2(10f, 10f),
                                new Vector2(50f, 50f),
                                new Vector2(10f, 50f)
                            }
                        }
                    },
                    new PreviewElementGeometry
                    {
                        Key = "shape",
                        TargetKey = "shape",
                        VisualBounds = new Rect(10f, 10f, 20f, 20f),
                        DrawOrder = 2,
                        WorldTransform = resolvedWorldTransform,
                        ParentWorldTransform = Matrix2D.identity,
                        HitGeometry = new[]
                        {
                            new[]
                            {
                                new Vector2(10f, 10f),
                                new Vector2(30f, 10f),
                                new Vector2(30f, 30f)
                            },
                            new[]
                            {
                                new Vector2(10f, 10f),
                                new Vector2(30f, 30f),
                                new Vector2(10f, 30f)
                            }
                        }
                    }
                }
            };
        }

        private static CanvasPointerDownContext CreatePointerDownContext(int pointerId, int clickCount)
        {
            return new CanvasPointerDownContext(
                pointerId: pointerId,
                button: (int)MouseButton.LeftMouse,
                clickCount: clickCount,
                modifiers: EventModifiers.None,
                target: null);
        }

        private static GestureRouter CreateRouter(
            string pathData,
            out ToolController toolController,
            out OverlayController overlayController,
            out PointerDragSession dragSession,
            out FakeCanvasPointerDragHost host,
            out VisualElement overlay)
        {
            ViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 100f, 100f));

            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            overlayController = new OverlayController();
            toolController = new ToolController();
            DragController dragController = new(sceneProjector);
            dragSession = new PointerDragSession();
            VisualElement overlayElement = new();
            overlay = overlayElement;
            host = new FakeCanvasPointerDragHost(CreateDocumentFromSource(BuildSvgSource(pathData)), CreatePreviewSnapshot());
            SelectionSyncService selectionSyncService = new(host, overlayController, dragController);
            return new GestureRouter(new GestureRouterDependencies(
                host,
                viewportState,
                overlayController,
                sceneProjector,
                toolController,
                dragController,
                selectionSyncService,
                dragSession,
                () => overlayElement));
        }

        private static GestureRouter CreateGroupedRouter(
            string leafTagName,
            string leafAttributes,
            out ToolController toolController,
            out OverlayController overlayController,
            out PointerDragSession dragSession,
            out FakeCanvasPointerDragHost host,
            out VisualElement overlay)
        {
            ViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 100f, 100f));

            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            overlayController = new OverlayController();
            toolController = new ToolController();
            DragController dragController = new(sceneProjector);
            dragSession = new PointerDragSession();
            VisualElement overlayElement = new();
            overlay = overlayElement;
            host = new FakeCanvasPointerDragHost(
                CreateDocumentFromSource(BuildGroupedSvgSource(leafTagName, leafAttributes)),
                CreateGroupedPreviewSnapshot(),
                CreateGroupedNodes(leafTagName),
                selectedElementKey: "group");
            SelectionSyncService selectionSyncService = new(host, overlayController, dragController);
            return new GestureRouter(new GestureRouterDependencies(
                host,
                viewportState,
                overlayController,
                sceneProjector,
                toolController,
                dragController,
                selectionSyncService,
                dragSession,
                () => overlayElement));
        }

        private static GestureRouter CreatePathAndRectRouter(
            out ToolController toolController,
            out OverlayController overlayController,
            out PointerDragSession dragSession,
            out FakeCanvasPointerDragHost host,
            out VisualElement overlay)
        {
            ViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 160f, 100f));

            SceneProjector sceneProjector = new(
                viewportState,
                new ElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            overlayController = new OverlayController();
            toolController = new ToolController();
            DragController dragController = new(sceneProjector);
            dragSession = new PointerDragSession();
            VisualElement overlayElement = new();
            overlay = overlayElement;
            host = new FakeCanvasPointerDragHost(
                CreateDocumentFromSource("<svg xmlns=\"http://www.w3.org/2000/svg\"><path id=\"shape\" d=\"M 10 10 L 30 10\" /><rect id=\"other\" x=\"110\" y=\"10\" width=\"20\" height=\"20\" /></svg>"),
                CreatePathAndRectPreviewSnapshot(),
                new Dictionary<string, HierarchyNode>
                {
                    ["shape"] = new HierarchyNode
                    {
                        Key = "shape",
                        TargetKey = "shape",
                        TagName = "path"
                    },
                    ["other"] = new HierarchyNode
                    {
                        Key = "other",
                        TargetKey = "other",
                        TagName = "rect"
                    }
                });
            SelectionSyncService selectionSyncService = new(host, overlayController, dragController);
            return new GestureRouter(new GestureRouterDependencies(
                host,
                viewportState,
                overlayController,
                sceneProjector,
                toolController,
                dragController,
                selectionSyncService,
                dragSession,
                () => overlayElement));
        }

        private static string BuildSvgSource(string pathData)
        {
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\"><path id=\"shape\" d=\"{pathData}\" /></svg>";
        }

        private static string BuildGroupedSvgSource(string leafTagName, string leafAttributes)
        {
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\"><g id=\"group\"><{leafTagName} id=\"shape\" {leafAttributes} /></g></svg>";
        }

        private static Dictionary<string, HierarchyNode> CreateGroupedNodes(string leafTagName)
        {
            return new Dictionary<string, HierarchyNode>
            {
                ["group"] = new HierarchyNode
                {
                    Key = "group",
                    TargetKey = "group",
                    TagName = "g"
                },
                ["shape"] = new HierarchyNode
                {
                    Key = "shape",
                    ParentKey = "group",
                    TargetKey = "shape",
                    TagName = leafTagName
                }
            };
        }

        private static PreviewSnapshot CreatePathAndRectPreviewSnapshot()
        {
            return new PreviewSnapshot
            {
                ProjectionRect = new Rect(0f, 0f, 160f, 100f),
                Elements = new List<PreviewElementGeometry>
                {
                    new()
                    {
                        Key = "shape",
                        TargetKey = "shape",
                        VisualBounds = new Rect(10f, 10f, 20f, 10f),
                        WorldTransform = Matrix2D.identity,
                        ParentWorldTransform = Matrix2D.identity,
                        DrawOrder = 1
                    },
                    new()
                    {
                        Key = "other",
                        TargetKey = "other",
                        VisualBounds = new Rect(110f, 10f, 20f, 20f),
                        WorldTransform = Matrix2D.identity,
                        ParentWorldTransform = Matrix2D.identity,
                        DrawOrder = 2
                    }
                }
            };
        }

        private static bool InvokeRouterPointerMove(GestureRouter router, int pointerId, Vector2 localPosition, EventModifiers modifiers)
        {
            MethodInfo method = typeof(GestureRouter).GetMethod("HandleCanvasPointerMove", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected GestureRouter.HandleCanvasPointerMove(int, Vector2, EventModifiers) to exist.");
            return (bool)method.Invoke(router, new object[] { pointerId, localPosition, modifiers });
        }

        private static bool InvokeRouterPointerUp(GestureRouter router, int pointerId, Vector2 localPosition, bool hasLocalPosition)
        {
            MethodInfo method = typeof(GestureRouter).GetMethod("HandleCanvasPointerUp", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected GestureRouter.HandleCanvasPointerUp(int, Vector2, bool) to exist.");
            return (bool)method.Invoke(router, new object[] { pointerId, localPosition, hasLocalPosition });
        }

        private static bool InvokeRouterEscape(GestureRouter router)
        {
            MethodInfo method = typeof(GestureRouter).GetMethod("HandleEscapeKey", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected GestureRouter.HandleEscapeKey() to exist.");
            return (bool)method.Invoke(router, null);
        }

        private static T GetPrivateField<T>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {instance.GetType().Name}.");
            return (T)field.GetValue(instance);
        }

        private sealed class FakeCanvasPointerDragHost : ICanvasPointerDragHost
        {
            private readonly Dictionary<string, HierarchyNode> _nodes;
            private readonly DocumentEditHistoryService _history = new();

            public FakeCanvasPointerDragHost(
                DocumentSession currentDocument,
                PreviewSnapshot previewSnapshot,
                IReadOnlyDictionary<string, HierarchyNode> nodes = null,
                string selectedElementKey = "shape")
            {
                CurrentDocument = currentDocument;
                PreviewSnapshot = previewSnapshot;
                _nodes = nodes != null
                    ? new Dictionary<string, HierarchyNode>(nodes)
                    : new Dictionary<string, HierarchyNode>
                    {
                        ["shape"] = new HierarchyNode
                        {
                            Key = "shape",
                            TargetKey = "shape",
                            TagName = "path"
                        }
                    };
                SelectedElementKey = selectedElementKey ?? string.Empty;
                SelectedElementKeys = string.IsNullOrWhiteSpace(selectedElementKey)
                    ? System.Array.Empty<string>()
                    : new[] { selectedElementKey };
                SelectionKind = string.IsNullOrWhiteSpace(selectedElementKey)
                    ? SelectionKind.None
                    : SelectionKind.Element;
                _history.Reset(CurrentDocument);
            }

            public DocumentSession CurrentDocument { get; private set; }
            public PreviewSnapshot PreviewSnapshot { get; private set; }
            public string SelectedElementKey { get; private set; }
            public IReadOnlyList<string> SelectedElementKeys { get; private set; }
            public SelectionKind SelectionKind { get; set; }
            public bool HasDefinitionProxySelection => false;
            public string LastStatusMessage { get; private set; } = string.Empty;
            public string LastAppliedSource { get; private set; } = string.Empty;
            public int RefreshLivePreviewCount { get; private set; }
            public int TransientPreviewRefreshCount { get; private set; }

            public void RefreshLivePreview(bool keepExistingPreviewOnFailure)
            {
                RefreshLivePreviewCount++;
            }

            public bool TryRefreshTransientPreview(SvgDocumentModel documentModel)
            {
                TransientPreviewRefreshCount++;
                return documentModel != null;
            }

            public void RefreshInspector()
            {
            }

            public void RefreshInspector(SvgDocumentModel documentModel)
            {
            }

            public void ApplyUpdatedSource(string updatedSource, string successStatus)
            {
                _history.RecordChange(CurrentDocument?.WorkingSourceText, updatedSource, HistoryRecordingMode.Immediate);
                CurrentDocument = CreateDocumentFromSource(updatedSource);
                LastAppliedSource = updatedSource ?? string.Empty;
                LastStatusMessage = successStatus ?? string.Empty;
            }

            public void UpdateSourceStatus(string status)
            {
                LastStatusMessage = status ?? string.Empty;
            }

            public void SetCurrentDocument(DocumentSession currentDocument)
            {
                CurrentDocument = currentDocument;
                _history.Reset(CurrentDocument);
            }

            public void SetPreviewSnapshot(PreviewSnapshot previewSnapshot)
            {
                PreviewSnapshot = previewSnapshot;
            }

            public bool TryUndo()
            {
                if (!_history.TryUndo(CurrentDocument?.WorkingSourceText, out string restoredSource))
                {
                    return false;
                }

                CurrentDocument = CreateDocumentFromSource(restoredSource);
                return true;
            }

            public bool TryRedo()
            {
                if (!_history.TryRedo(CurrentDocument?.WorkingSourceText, out string restoredSource))
                {
                    return false;
                }

                CurrentDocument = CreateDocumentFromSource(restoredSource);
                return true;
            }

            public HierarchyNode FindHierarchyNode(string elementKey)
            {
                return elementKey != null && _nodes.TryGetValue(elementKey, out HierarchyNode node)
                    ? node
                    : null;
            }

            public void SelectFrame()
            {
            }

            public void SelectElement(string elementKey, bool syncPatchTarget)
            {
                SelectedElementKey = elementKey ?? string.Empty;
                SelectedElementKeys = string.IsNullOrWhiteSpace(elementKey)
                    ? System.Array.Empty<string>()
                    : new[] { elementKey };
                SelectionKind = string.IsNullOrWhiteSpace(elementKey)
                    ? SelectionKind.None
                    : SelectionKind.Element;
            }

            public void ToggleElementSelection(string elementKey, bool syncPatchTarget)
            {
            }

            public void ReplaceElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget)
            {
            }

            public void AddElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget)
            {
            }

            public void ClearSelection()
            {
                SelectedElementKey = string.Empty;
                SelectedElementKeys = System.Array.Empty<string>();
                SelectionKind = SelectionKind.None;
            }

            public void UpdateStructureInteractivity(bool hasDocument)
            {
            }

            public void UpdateCanvasVisualState()
            {
            }

            public void UpdateViewportVisualState()
            {
            }

            public void UpdateSelectionVisual()
            {
            }

            public void SetHoveredElement(string elementKey)
            {
            }

            public void ClearHover()
            {
            }

            public void UpdateHoverVisual()
            {
            }

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

            public void SelectDefinitionProxy(CanvasDefinitionOverlayVisual overlay)
            {
            }

            public void ClearDefinitionProxySelection()
            {
            }
        }

        private sealed class FakeCanvasWorkspaceHost : ICanvasWorkspaceHost
        {
            private readonly Dictionary<string, HierarchyNode> _nodes;

            public FakeCanvasWorkspaceHost(
                DocumentSession currentDocument,
                PreviewSnapshot previewSnapshot,
                IReadOnlyDictionary<string, HierarchyNode> nodes = null,
                string selectedElementKey = "shape")
            {
                CurrentDocument = currentDocument;
                PreviewSnapshot = previewSnapshot;
                PreviewImage = new Image();
                _nodes = nodes != null
                    ? new Dictionary<string, HierarchyNode>(nodes)
                    : new Dictionary<string, HierarchyNode>
                    {
                        ["shape"] = new HierarchyNode
                        {
                            Key = "shape",
                            TargetKey = "shape",
                            TagName = "path"
                        }
                    };
                SelectedElementKey = selectedElementKey ?? string.Empty;
                SelectedElementKeys = string.IsNullOrWhiteSpace(selectedElementKey)
                    ? System.Array.Empty<string>()
                    : new[] { selectedElementKey };
                SelectedHierarchyNode = string.IsNullOrWhiteSpace(selectedElementKey)
                    ? null
                    : _nodes[selectedElementKey];
            }

            public DocumentSession CurrentDocument { get; private set; }
            public PreviewSnapshot PreviewSnapshot { get; private set; }
            public Image PreviewImage { get; }
            public string SelectedElementKey { get; private set; }
            public IReadOnlyList<string> SelectedElementKeys { get; private set; }
            public HierarchyNode SelectedHierarchyNode { get; private set; }
            public string LastStatusMessage { get; private set; } = string.Empty;
            public string LastAppliedSource { get; private set; } = string.Empty;

            public string FormatNumber(float value)
            {
                return value.ToString("0.###", CultureInfo.InvariantCulture);
            }

            public HierarchyNode FindHierarchyNode(string elementKey)
            {
                return elementKey != null && _nodes.TryGetValue(elementKey, out HierarchyNode node)
                    ? node
                    : null;
            }

            public void ClearStructureSelectionFromCanvas()
            {
                SelectedElementKey = string.Empty;
                SelectedElementKeys = System.Array.Empty<string>();
                SelectedHierarchyNode = null;
            }

            public void SelectFrameFromCanvas()
            {
            }

            public void SelectStructureElementFromCanvas(string elementKey, bool syncPatchTarget)
            {
                SelectedElementKey = elementKey ?? string.Empty;
                SelectedElementKeys = string.IsNullOrWhiteSpace(elementKey)
                    ? System.Array.Empty<string>()
                    : new[] { elementKey };
                SelectedHierarchyNode = FindHierarchyNode(elementKey);
            }

            public void ToggleStructureElementSelectionFromCanvas(string elementKey, bool syncPatchTarget)
            {
            }

            public void ReplaceStructureElementSelectionFromCanvas(IReadOnlyList<string> elementKeys, bool syncPatchTarget)
            {
            }

            public void AddStructureElementSelectionFromCanvas(IReadOnlyList<string> elementKeys, bool syncPatchTarget)
            {
            }

            public void UpdateStructureInteractivity(bool hasDocument)
            {
            }

            public void UpdateSourceStatus(string status)
            {
                LastStatusMessage = status ?? string.Empty;
            }

            public void UpdateViewportVisualState()
            {
            }

            public void RefreshLivePreview(bool keepExistingPreviewOnFailure)
            {
            }

            public bool TryRefreshTransientPreview(SvgDocumentModel documentModel)
            {
                return documentModel != null;
            }

            public void RefreshInspector()
            {
            }

            public void RefreshInspector(SvgDocumentModel documentModel)
            {
            }

            public void ApplyUpdatedSource(string updatedSource, string successStatus)
            {
                CurrentDocument = CreateDocumentFromSource(updatedSource);
                LastAppliedSource = updatedSource ?? string.Empty;
                LastStatusMessage = successStatus ?? string.Empty;
            }
        }
    }
}
