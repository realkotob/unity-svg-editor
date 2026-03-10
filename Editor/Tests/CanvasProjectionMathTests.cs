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

        [Test]
        public void TryGetFrameContentViewportRect_UsesFullInnerRect_ForPreserveAspectRatioNone()
        {
            var viewportState = new CanvasViewportState();
            viewportState.SetFrameRect(new Rect(0f, 0f, 200f, 100f));

            bool success = CanvasProjectionMath.TryGetFrameContentViewportRect(
                viewportState,
                new Rect(0f, 0f, 100f, 100f),
                SvgPreserveAspectRatioMode.None,
                0f,
                0f,
                out Rect contentViewportRect);

            Assert.That(success, Is.True);
            Assert.That(contentViewportRect, Is.EqualTo(new Rect(0f, 0f, 200f, 100f)));
        }

        [Test]
        public void TryGetFrameContentViewportRect_KeepsMeetFit_ForDefaultPreserveAspectRatio()
        {
            var viewportState = new CanvasViewportState();
            viewportState.SetFrameRect(new Rect(0f, 0f, 200f, 100f));

            bool success = CanvasProjectionMath.TryGetFrameContentViewportRect(
                viewportState,
                new Rect(0f, 0f, 100f, 100f),
                SvgPreserveAspectRatioMode.Meet,
                0f,
                0f,
                out Rect contentViewportRect);

            Assert.That(success, Is.True);
            Assert.That(contentViewportRect, Is.EqualTo(new Rect(50f, 0f, 100f, 100f)));
        }
    }
}
