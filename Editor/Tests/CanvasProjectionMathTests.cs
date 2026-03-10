using NUnit.Framework;
using UnityEngine;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class CanvasProjectionMathTests
    {
        [Test]
        public void GetPreviewSceneRect_UsesProjectionRect()
        {
            var snapshot = new PreviewSnapshot
            {
                DocumentViewportRect = new Rect(0f, 0f, 64f, 64f),
                ProjectionRect = new Rect(-48f, -24f, 256f, 128f),
                VisualContentBounds = new Rect(-20f, -10f, 96f, 72f)
            };

            Assert.That(
                CanvasProjectionMath.GetPreviewSceneRect(snapshot),
                Is.EqualTo(snapshot.ProjectionRect));
        }
    }
}
