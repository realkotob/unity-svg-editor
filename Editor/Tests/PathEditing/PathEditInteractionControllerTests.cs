using System.Reflection;
using NUnit.Framework;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.UI.Canvas;
using UnityEngine;

namespace SvgEditor.Editor.Tests.PathEditing
{
    public sealed class PathEditInteractionControllerTests
    {
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
    }
}
