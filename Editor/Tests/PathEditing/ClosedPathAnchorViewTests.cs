using NUnit.Framework;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.UI.Canvas;
using Unity.VectorGraphics;
using UnityEngine;

namespace SvgEditor.Editor.Tests.PathEditing
{
    public sealed class ClosedPathAnchorViewTests
    {
        [Test]
        public void TryBuildSubpathView_WhenClosedCubicPath_UsesLogicalAnchorCountWithoutDuplicateClosingNode()
        {
            PathSubpath subpath = CreateClosedCubicLoop();

            bool built = PathEditSession.TryBuildSubpathView(
                subpath,
                Matrix2D.identity,
                point => point,
                out PathSubpathView view);

            Assert.That(built, Is.True);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Nodes, Has.Count.EqualTo(4));
            Assert.That(view.Nodes[0].Position, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(view.Nodes[1].Position, Is.EqualTo(new Vector2(4f, 0f)));
            Assert.That(view.Nodes[2].Position, Is.EqualTo(new Vector2(4f, 4f)));
            Assert.That(view.Nodes[3].Position, Is.EqualTo(new Vector2(0f, 4f)));
        }

        [Test]
        public void TryBuildSubpathView_WhenClosedCubicPath_AssignsIncomingHandleToLogicalStartNode()
        {
            PathSubpath subpath = CreateClosedCubicLoop();

            bool built = PathEditSession.TryBuildSubpathView(
                subpath,
                Matrix2D.identity,
                point => point,
                out PathSubpathView view);

            Assert.That(built, Is.True);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Nodes[0].HasInHandle, Is.True);
            Assert.That(view.Nodes[0].HasOutHandle, Is.True);
            Assert.That(view.Nodes[0].InHandle, Is.EqualTo(new Vector2(-1f, 0f)));
            Assert.That(view.Nodes[0].OutHandle, Is.EqualTo(new Vector2(1f, 0f)));
        }

        private static PathSubpath CreateClosedCubicLoop()
        {
            return new PathSubpath(
                new Vector2(0f, 0f),
                new[]
                {
                    new PathNode('C', new Vector2(4f, 0f), new Vector2(1f, 0f), new Vector2(4f, 1f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(4f, 4f), new Vector2(4f, 3f), new Vector2(3f, 4f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(0f, 4f), new Vector2(1f, 4f), new Vector2(0f, 3f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-1f, 0f), PathHandleMode.Free)
                },
                isClosed: true);
        }
    }
}
