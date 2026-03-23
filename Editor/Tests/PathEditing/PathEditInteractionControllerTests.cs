using System.Reflection;
using NUnit.Framework;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.UI.Canvas;
using UnityEngine;

namespace SvgEditor.Editor.Tests.PathEditing
{
    public sealed class PathEditInteractionControllerTests
    {
        private static readonly MethodInfo TryMoveNodeMethod = typeof(PathEditInteractionController)
            .GetMethod("TryMoveNode", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo TryMoveHandleMethod = typeof(PathEditInteractionController)
            .GetMethod("TryMoveHandle", BindingFlags.NonPublic | BindingFlags.Static);

        [Test]
        public void TryMoveHandle_WhenMovingOutgoingQuadraticHandle_PromotesToFreeCubic()
        {
            PathData pathData = CreateSingleQuadraticPath();

            bool moved = InvokeTryMoveHandle(
                pathData,
                new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.Out),
                new Vector2(5f, 6f));

            Assert.That(moved, Is.True);

            PathNode segment = pathData.Subpaths[0].Nodes[0];
            Assert.That(segment.Command, Is.EqualTo('C'));
            Assert.That(segment.HandleMode, Is.EqualTo(PathHandleMode.Free));
            Assert.That(segment.Control0, Is.EqualTo(new Vector2(5f, 6f)));
            Assert.That(segment.Control1, Is.EqualTo(new Vector2(4f, 2f)));

            var serialized = PathDataSerializer.SerializeResult(pathData);
            Assert.That(serialized.IsSuccess, Is.True);
            Assert.That(serialized.Value, Does.Contain("C"));
            Assert.That(serialized.Value, Does.Not.Contain("Q"));
        }

        [Test]
        public void TryMoveHandle_WhenMovingIncomingQuadraticHandle_PromotesToFreeCubic()
        {
            PathData pathData = CreateSingleQuadraticPath();

            bool moved = InvokeTryMoveHandle(
                pathData,
                new PathHandleRef(new PathNodeRef(0, 1), PathHandleSlot.In),
                new Vector2(7f, 8f));

            Assert.That(moved, Is.True);

            PathNode segment = pathData.Subpaths[0].Nodes[0];
            Assert.That(segment.Command, Is.EqualTo('C'));
            Assert.That(segment.HandleMode, Is.EqualTo(PathHandleMode.Free));
            Assert.That(segment.Control0, Is.EqualTo(new Vector2(2f, 2f)));
            Assert.That(segment.Control1, Is.EqualTo(new Vector2(7f, 8f)));

            var serialized = PathDataSerializer.SerializeResult(pathData);
            Assert.That(serialized.IsSuccess, Is.True);
            Assert.That(serialized.Value, Does.Contain("C"));
            Assert.That(serialized.Value, Does.Not.Contain("Q"));
        }

        [Test]
        public void TryMoveNode_WhenMovingClosedStartAnchor_UpdatesStartAndClosingSegmentTogether()
        {
            PathData pathData = CreateClosedCubicPath();

            bool moved = InvokeTryMoveNode(
                pathData,
                new PathNodeRef(0, 0),
                new Vector2(2f, 1f));

            Assert.That(moved, Is.True);

            PathSubpath subpath = pathData.Subpaths[0];
            Assert.That(subpath.Start, Is.EqualTo(new Vector2(2f, 1f)));
            Assert.That(subpath.Nodes[3].Position, Is.EqualTo(new Vector2(2f, 1f)));
            Assert.That(subpath.Nodes[0].Control0, Is.EqualTo(new Vector2(3f, 1f)));
            Assert.That(subpath.Nodes[3].Control1, Is.EqualTo(new Vector2(1f, 1f)));
        }

        [Test]
        public void TryMoveHandle_WhenMovingIncomingHandleOfClosedStartAnchor_UpdatesClosingSegment()
        {
            PathData pathData = CreateClosedCubicPath();

            bool moved = InvokeTryMoveHandle(
                pathData,
                new PathHandleRef(new PathNodeRef(0, 0), PathHandleSlot.In),
                new Vector2(-2f, 2f));

            Assert.That(moved, Is.True);
            Assert.That(pathData.Subpaths[0].Nodes[3].Control1, Is.EqualTo(new Vector2(-2f, 2f)));
        }

        private static bool InvokeTryMoveNode(PathData pathData, PathNodeRef nodeRef, Vector2 newPosition)
        {
            Assert.That(TryMoveNodeMethod, Is.Not.Null);

            return (bool)TryMoveNodeMethod.Invoke(null, new object[] { pathData, nodeRef, newPosition });
        }

        private static bool InvokeTryMoveHandle(PathData pathData, PathHandleRef handleRef, Vector2 newPosition)
        {
            Assert.That(TryMoveHandleMethod, Is.Not.Null);

            return (bool)TryMoveHandleMethod.Invoke(null, new object[] { pathData, handleRef, newPosition });
        }

        private static PathData CreateSingleQuadraticPath()
        {
            var pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(
                new Vector2(0f, 0f),
                new[]
                {
                    new PathNode('Q', new Vector2(6f, 0f), new Vector2(3f, 3f), default, PathHandleMode.Free)
                }));
            return pathData;
        }

        private static PathData CreateClosedCubicPath()
        {
            var pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(
                new Vector2(0f, 0f),
                new[]
                {
                    new PathNode('C', new Vector2(4f, 0f), new Vector2(1f, 0f), new Vector2(4f, 1f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(4f, 4f), new Vector2(4f, 3f), new Vector2(3f, 4f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(0f, 4f), new Vector2(1f, 4f), new Vector2(0f, 3f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-1f, 0f), PathHandleMode.Free)
                },
                isClosed: true));
            return pathData;
        }
    }
}
