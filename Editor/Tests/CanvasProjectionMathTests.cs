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

        [Test]
        public void TryGetFrameContentViewportRect_UsesSliceAlignment_ForOversizedImageRect()
        {
            var viewportState = new CanvasViewportState();
            viewportState.SetFrameRect(new Rect(0f, 0f, 200f, 100f));

            bool success = CanvasProjectionMath.TryGetFrameContentViewportRect(
                viewportState,
                new Rect(0f, 0f, 100f, 100f),
                new SvgPreserveAspectRatioMode(
                    SvgPreserveAspectRatioScaleMode.Slice,
                    SvgPreserveAspectRatioAlignX.Max,
                    SvgPreserveAspectRatioAlignY.Min),
                0f,
                0f,
                out Rect contentViewportRect);

            Assert.That(success, Is.True);
            Assert.That(contentViewportRect, Is.EqualTo(new Rect(0f, 0f, 200f, 200f)));
        }

        [Test]
        public void TryGetFrameVisibleViewportRect_ReturnsInnerFrame_ForSliceMode()
        {
            var viewportState = new CanvasViewportState();
            viewportState.SetFrameRect(new Rect(0f, 0f, 200f, 100f));

            bool success = CanvasProjectionMath.TryGetFrameVisibleViewportRect(
                viewportState,
                new Rect(0f, 0f, 100f, 100f),
                new SvgPreserveAspectRatioMode(
                    SvgPreserveAspectRatioScaleMode.Slice,
                    SvgPreserveAspectRatioAlignX.Max,
                    SvgPreserveAspectRatioAlignY.Min),
                0f,
                0f,
                out Rect visibleViewportRect);

            Assert.That(success, Is.True);
            Assert.That(visibleViewportRect, Is.EqualTo(new Rect(0f, 0f, 200f, 100f)));
        }

        [Test]
        public void BuildScaledSceneRect_RightHandle_OnlyChangesWidth()
        {
            Rect scaled = CanvasProjectionMath.BuildScaledSceneRect(
                new Rect(0f, 0f, 100f, 100f),
                new Rect(10f, 20f, 50f, 40f),
                new Rect(0f, 0f, 140f, 100f),
                CanvasHandle.Right);

            Assert.That(scaled, Is.EqualTo(new Rect(10f, 20f, 70f, 40f)));
        }

        [Test]
        public void TryBuildScaleTransform_TopHandle_UsesBottomCenterPivot()
        {
            bool success = CanvasProjectionMath.TryBuildScaleTransform(
                new Rect(0f, 0f, 100f, 100f),
                new Rect(10f, 20f, 50f, 40f),
                new Rect(0f, -20f, 100f, 120f),
                CanvasHandle.Top,
                out Vector2 scale,
                out Vector2 pivot);

            Assert.That(success, Is.True);
            Assert.That(scale, Is.EqualTo(new Vector2(1f, 1.2f)));
            Assert.That(pivot, Is.EqualTo(new Vector2(35f, 60f)));
        }
    }
}
