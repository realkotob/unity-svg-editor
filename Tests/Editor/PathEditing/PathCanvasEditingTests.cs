using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.UI.Canvas;
using SvgEditor.Core.Svg.PathEditing;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class PathCanvasEditingTests
    {
        [Test]
        public void SelectionState_SelectHandle_AddsOwningNodeAndKeepsSortedNodes()
        {
            PathSelectionState selection = new();

            selection.SelectNode(new PathNodeRef(1, 2));
            selection.SelectNode(new PathNodeRef(0, 3), additive: true);
            selection.SelectHandle(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.Out), additive: true);

            Assert.That(selection.Nodes.ToArray(), Is.EqualTo(new[]
            {
                new PathNodeRef(0, 1),
                new PathNodeRef(0, 3),
                new PathNodeRef(1, 2)
            }));
            Assert.That(selection.ActiveNode, Is.EqualTo(new PathNodeRef(0, 1)));
            Assert.That(selection.ActiveHandle, Is.EqualTo(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.Out)));
            Assert.That(selection.IsSelected(new PathNodeRef(0, 1)), Is.True);
            Assert.That(selection.HasActiveHandle(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.Out)), Is.True);
            Assert.That(selection.HasActiveHandle(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.In)), Is.False);
        }

        [Test]
        public void Session_SetGeometry_ClearsSelection_WhenExistingRefsBecomeStale()
        {
            PathEditSession session = CreateSession();
            session.Selection.SelectHandle(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.In));

            session.SetGeometry(new[]
            {
                new PathSubpathView(
                    new[]
                    {
                        new PathNodeView(
                            new Vector2(5f, 5f),
                            inHandle: default,
                            hasInHandle: false,
                            outHandle: default,
                            hasOutHandle: false)
                    },
                    new CanvasLineSegment[0],
                    isClosed: false)
            });

            Assert.That(session.Selection.Nodes, Is.Empty);
            Assert.That(session.Selection.ActiveNode, Is.Null);
            Assert.That(session.Selection.ActiveHandle, Is.Null);
        }

        [Test]
        public void Session_SetGeometry_CapturesGeometryDefensively()
        {
            List<PathNodeView> nodes = new()
            {
                new PathNodeView(
                    new Vector2(10f, 10f),
                    inHandle: default,
                    hasInHandle: false,
                    outHandle: new Vector2(14f, 10f),
                    hasOutHandle: true),
                new PathNodeView(
                    new Vector2(40f, 10f),
                    inHandle: new Vector2(36f, 10f),
                    hasInHandle: true,
                    outHandle: default,
                    hasOutHandle: false)
            };
            List<CanvasLineSegment> segments = new()
            {
                new CanvasLineSegment(new Vector2(10f, 10f), new Vector2(40f, 10f))
            };

            PathEditSession session = new("shape");
            session.SetGeometry(new[]
            {
                new PathSubpathView(nodes, segments, isClosed: false)
            });

            nodes.Clear();
            segments.Clear();

            Assert.That(session.Subpaths, Has.Count.EqualTo(1));
            Assert.That(session.Subpaths[0].Nodes.Count, Is.EqualTo(2));
            Assert.That(session.Subpaths[0].Segments.Count, Is.EqualTo(1));
        }

        [Test]
        public void HitTester_PrefersHandleOverNodeAndSegment()
        {
            PathEditSession session = CreateSession();

            bool hitFound = PathHitTester.TryHit(
                session.Subpaths,
                new Vector2(14.25f, 10.1f),
                nodeRadius: 5f,
                handleRadius: 2f,
                segmentDistance: 3f,
                out PathHit hit);

            Assert.That(hitFound, Is.True);
            Assert.That(hit.Kind, Is.EqualTo(PathHitKind.Handle));
            Assert.That(hit.Handle, Is.EqualTo(new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out)));
            Assert.That(hit.Node, Is.EqualTo(new PathNodeRef(0, 0)));
        }

        [Test]
        public void HitTester_EvaluatesBothHandlesOnSameNode_WhenOutHandleIsCloser()
        {
            PathSubpathView subpath = new(
                new[]
                {
                    new PathNodeView(
                        new Vector2(20f, 20f),
                        inHandle: new Vector2(18f, 20f),
                        hasInHandle: true,
                        outHandle: new Vector2(23f, 20f),
                        hasOutHandle: true)
                },
                new CanvasLineSegment[0],
                isClosed: false);

            bool hitFound = PathHitTester.TryHit(
                new[] { subpath },
                new Vector2(22.7f, 20f),
                nodeRadius: 4f,
                handleRadius: 5f,
                segmentDistance: 1f,
                out PathHit hit);

            Assert.That(hitFound, Is.True);
            Assert.That(hit.Kind, Is.EqualTo(PathHitKind.Handle));
            Assert.That(hit.Handle, Is.EqualTo(new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out)));
        }

        [Test]
        public void HitTester_ReturnsSegment_WhenPointMissesNodesAndHandles()
        {
            PathEditSession session = CreateSession();

            bool hitFound = PathHitTester.TryHit(
                session.Subpaths,
                new Vector2(27f, 11.5f),
                nodeRadius: 2f,
                handleRadius: 2f,
                segmentDistance: 2f,
                out PathHit hit);

            Assert.That(hitFound, Is.True);
            Assert.That(hit.Kind, Is.EqualTo(PathHitKind.Segment));
            Assert.That(hit.Segment, Is.EqualTo(new PathSegmentRef(0, 0)));
            Assert.That(hit.Node, Is.EqualTo(default(PathNodeRef)));
            Assert.That(hit.Handle, Is.EqualTo(default(PathHandleRef)));
        }

        [Test]
        public void HitTester_PrefersLowestNodeRef_WhenNodeDistancesTie()
        {
            PathSubpathView subpath = new(
                new[]
                {
                    new PathNodeView(new Vector2(10f, 10f), default, false, default, false),
                    new PathNodeView(new Vector2(14f, 10f), default, false, default, false)
                },
                new CanvasLineSegment[0],
                isClosed: false);

            bool hitFound = PathHitTester.TryHit(
                new[] { subpath },
                new Vector2(12f, 10f),
                nodeRadius: 3f,
                handleRadius: 1f,
                segmentDistance: 1f,
                out PathHit hit);

            Assert.That(hitFound, Is.True);
            Assert.That(hit.Kind, Is.EqualTo(PathHitKind.Node));
            Assert.That(hit.Node, Is.EqualTo(new PathNodeRef(0, 0)));
        }

        [Test]
        public void OverlayPresenter_BuildVisual_ReflectsCurrentSessionSelection()
        {
            PathEditSession session = CreateSession();
            session.Selection.SelectNode(new PathNodeRef(0, 1));

            PathOverlayVisual visual = PathOverlayPresenter.BuildVisual(session);

            Assert.That(visual.PathSegments, Has.Count.EqualTo(2));
            Assert.That(visual.HandleSegments, Has.Count.EqualTo(4));
            Assert.That(visual.Nodes, Has.Count.EqualTo(3));
            Assert.That(visual.Handles, Has.Count.EqualTo(4));

            PathOverlayNode selectedNode = visual.Nodes.Single(node => node.Node.Equals(new PathNodeRef(0, 1)));
            Assert.That(selectedNode.IsSelected, Is.True);
            Assert.That(selectedNode.IsActive, Is.True);

            Assert.That(visual.Handles.Any(handle => handle.Handle.Equals(new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out))), Is.True);
            Assert.That(visual.Handles.Any(handle => handle.Handle.Equals(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.In))), Is.True);
            Assert.That(visual.Handles.Any(handle => handle.Handle.Equals(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.Out))), Is.True);
            Assert.That(visual.Handles.Any(handle => handle.Handle.Equals(new PathHandleRef(new PathNodeRef(0, 2), PathHandleSlot.In))), Is.True);

            PathOverlayNode unselectedNode = visual.Nodes.Single(node => node.Node.Equals(new PathNodeRef(0, 2)));
            Assert.That(unselectedNode.IsSelected, Is.False);
        }

        [Test]
        public void OverlayPresenter_BuildVisual_ShowsOnlyAdjacentHandles_ForActiveAnchor()
        {
            PathEditSession session = CreateSession();
            session.Selection.SelectNode(new PathNodeRef(0, 0));

            PathOverlayVisual visual = PathOverlayPresenter.BuildVisual(session);

            Assert.That(visual.HandleSegments, Has.Count.EqualTo(2));
            Assert.That(visual.Handles, Has.Count.EqualTo(2));
            Assert.That(visual.Handles.Any(handle => handle.Handle.Equals(new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out))), Is.True);
            Assert.That(visual.Handles.Any(handle => handle.Handle.Equals(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.In))), Is.True);
            Assert.That(visual.Handles.Any(handle => handle.Handle.Equals(new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.Out))), Is.False);
            Assert.That(visual.Handles.Any(handle => handle.Handle.Equals(new PathHandleRef(new PathNodeRef(0, 2), PathHandleSlot.In))), Is.False);
        }

        [Test]
        public void Session_TryHit_UsesCurrentViewportGeometry()
        {
            PathEditSession session = CreateSession();

            bool hitFound = session.TryHit(
                new Vector2(40f, 10f),
                nodeRadius: 3f,
                handleRadius: 2f,
                segmentDistance: 1f,
                out PathHit hit);

            Assert.That(hitFound, Is.True);
            Assert.That(hit.Kind, Is.EqualTo(PathHitKind.Node));
            Assert.That(hit.Node, Is.EqualTo(new PathNodeRef(0, 1)));
        }

        [Test]
        public void Session_GeometrySnapshot_DoesNotExposeMutableArrays()
        {
            PathEditSession session = CreateSession();

            Assert.That(session.Subpaths[0].Nodes is PathNodeView[], Is.False);
            Assert.That(session.Subpaths[0].Segments is CanvasLineSegment[], Is.False);
        }

        [Test]
        public void Session_GeometrySnapshot_DoesNotExposeMutableOuterSubpathList()
        {
            PathEditSession session = CreateSession();

            Assert.That(session.Subpaths is List<PathSubpathView>, Is.False);
        }

        [Test]
        public void TryBuildSubpathView_SamplesCubicCurveIntoOverlaySegments()
        {
            PathData pathData = PathDataParser.Parse("M 0 0 C 0 10 10 10 10 0");

            bool ok = PathEditSession.TryBuildSubpathView(
                pathData.Subpaths[0],
                Matrix2D.identity,
                point => point,
                out PathSubpathView view);

            Assert.That(ok, Is.True);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Nodes, Has.Count.EqualTo(2));
            Assert.That(view.Segments.Count, Is.GreaterThan(1), "Curved overlays should be sampled into multiple line segments.");
            Assert.That(view.Segments[0].Start, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(view.Segments[^1].End, Is.EqualTo(new Vector2(10f, 0f)));
            Assert.That(
                view.Segments.Any(segment => segment.Start.y > 0f || segment.End.y > 0f),
                Is.True,
                "At least one sampled point should deviate from the straight anchor chord.");
        }

        [Test]
        public void TryBuildSubpathView_SplitsQuadraticOverlayHandlesAcrossAnchors()
        {
            PathData pathData = PathDataParser.Parse("M 0 0 Q 9 12 18 0");

            bool ok = PathEditSession.TryBuildSubpathView(
                pathData.Subpaths[0],
                Matrix2D.identity,
                point => point,
                out PathSubpathView view);

            Assert.That(ok, Is.True);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Nodes, Has.Count.EqualTo(2));
            Assert.That(view.Nodes[0].HasOutHandle, Is.True);
            Assert.That(view.Nodes[1].HasInHandle, Is.True);
            Assert.That(view.Nodes[0].OutHandle, Is.EqualTo(new Vector2(6f, 8f)));
            Assert.That(view.Nodes[1].InHandle, Is.EqualTo(new Vector2(12f, 8f)));
            Assert.That(view.Nodes[0].OutHandle, Is.Not.EqualTo(view.Nodes[1].InHandle));
        }

        [Test]
        public void TrySetPathData_PreservesPreviousSessionState_WhenReprojectionFails()
        {
            PathEditSession session = new(
                "shape",
                Matrix2D.identity,
                scenePoint => scenePoint.x <= 100f ? scenePoint : null);
            PathData initialPathData = PathDataParser.Parse("M 10 10 L 20 10");

            Assert.That(session.TrySetPathData(initialPathData, out string initialError), Is.True, initialError);
            PathData previousPathData = session.ClonePathData();
            Vector2 previousNodePosition = session.Subpaths[0].Nodes[1].Position;

            bool ok = session.TrySetPathData(PathDataParser.Parse("M 10 10 L 120 10"), out string error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("preview projection is unavailable"));
            Assert.That(session.PathData.Subpaths[0].Nodes[0].Position, Is.EqualTo(previousPathData.Subpaths[0].Nodes[0].Position));
            Assert.That(session.Subpaths[0].Nodes[1].Position, Is.EqualTo(previousNodePosition));
        }

        private static PathEditSession CreateSession()
        {
            PathEditSession session = new("shape");
            session.SetGeometry(new[]
            {
                new PathSubpathView(
                    new[]
                    {
                        new PathNodeView(
                            new Vector2(10f, 10f),
                            inHandle: default,
                            hasInHandle: false,
                            outHandle: new Vector2(14f, 10f),
                            hasOutHandle: true),
                        new PathNodeView(
                            new Vector2(40f, 10f),
                            inHandle: new Vector2(36f, 10f),
                            hasInHandle: true,
                            outHandle: new Vector2(44f, 14f),
                            hasOutHandle: true),
                        new PathNodeView(
                            new Vector2(70f, 20f),
                            inHandle: new Vector2(64f, 16f),
                            hasInHandle: true,
                            outHandle: default,
                            hasOutHandle: false)
                    },
                    new[]
                    {
                        new CanvasLineSegment(new Vector2(10f, 10f), new Vector2(40f, 10f)),
                        new CanvasLineSegment(new Vector2(40f, 10f), new Vector2(70f, 20f))
                    },
                    isClosed: false)
            });

            return session;
        }
    }
}
