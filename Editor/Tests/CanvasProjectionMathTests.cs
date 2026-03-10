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
                centerAnchor: false,
                out Vector2 scale,
                out Vector2 pivot);

            Assert.That(success, Is.True);
            Assert.That(scale, Is.EqualTo(new Vector2(1f, 1.2f)));
            Assert.That(pivot, Is.EqualTo(new Vector2(35f, 60f)));
        }

        [Test]
        public void TryBuildScaleTransform_UsesElementCenterPivot_WhenCenterAnchorIsEnabled()
        {
            bool success = CanvasProjectionMath.TryBuildScaleTransform(
                new Rect(0f, 0f, 100f, 80f),
                new Rect(10f, 20f, 50f, 40f),
                new Rect(-50f, -40f, 200f, 160f),
                CanvasHandle.BottomRight,
                centerAnchor: true,
                out Vector2 scale,
                out Vector2 pivot);

            Assert.That(success, Is.True);
            Assert.That(scale, Is.EqualTo(new Vector2(2f, 2f)));
            Assert.That(pivot, Is.EqualTo(new Vector2(35f, 40f)));
        }

        [Test]
        public void GetResizeViewportRect_AlwaysUsesUniformScale_ForCornerHandle()
        {
            Rect uniformRect = CanvasProjectionMath.GetResizeViewportRect(
                new Rect(0f, 0f, 100f, 80f),
                new Rect(0f, 0f, 150f, 90f),
                CanvasHandle.BottomRight,
                uniformScale: false,
                centerAnchor: false);

            Assert.That(uniformRect, Is.EqualTo(new Rect(0f, 0f, 150f, 120f)));
        }

        [Test]
        public void GetResizeViewportRect_UsesUniformScale_ForEdgeHandleWhenShiftIsPressed()
        {
            Rect resizedRect = CanvasProjectionMath.GetResizeViewportRect(
                new Rect(0f, 0f, 100f, 80f),
                new Rect(0f, 0f, 150f, 80f),
                CanvasHandle.Right,
                uniformScale: true,
                centerAnchor: false);

            Assert.That(resizedRect, Is.EqualTo(new Rect(0f, -20f, 150f, 120f)));
        }

        [Test]
        public void GetResizeViewportRect_KeepsEdgeResizeNonUniform_WithoutShift()
        {
            Rect resizedRect = CanvasProjectionMath.GetResizeViewportRect(
                new Rect(0f, 0f, 100f, 80f),
                new Rect(0f, 0f, 150f, 80f),
                CanvasHandle.Right,
                uniformScale: false,
                centerAnchor: false);

            Assert.That(resizedRect, Is.EqualTo(new Rect(0f, 0f, 150f, 80f)));
        }

        [Test]
        public void GetResizeViewportRect_UsesUniformScale_ForLeftEdgeHandleWhenShiftIsPressed()
        {
            Rect resizedRect = CanvasProjectionMath.GetResizeViewportRect(
                new Rect(0f, 0f, 100f, 80f),
                new Rect(-50f, 0f, 150f, 80f),
                CanvasHandle.Left,
                uniformScale: true,
                centerAnchor: false);

            Assert.That(resizedRect, Is.EqualTo(new Rect(-50f, -20f, 150f, 120f)));
        }

        [Test]
        public void GetResizeViewportRect_UsesCenterAnchor_ForCornerHandleWhenAltIsPressed()
        {
            Rect resizedRect = CanvasProjectionMath.GetResizeViewportRect(
                new Rect(0f, 0f, 100f, 80f),
                new Rect(0f, 0f, 150f, 90f),
                CanvasHandle.BottomRight,
                uniformScale: false,
                centerAnchor: true);

            Assert.That(resizedRect, Is.EqualTo(new Rect(-50f, -40f, 200f, 160f)));
        }

        [Test]
        public void GetResizeViewportRect_UsesCenterAnchor_ForEdgeHandleWhenAltIsPressed()
        {
            Rect resizedRect = CanvasProjectionMath.GetResizeViewportRect(
                new Rect(0f, 0f, 100f, 80f),
                new Rect(0f, -30f, 100f, 110f),
                CanvasHandle.Top,
                uniformScale: false,
                centerAnchor: true);

            Assert.That(resizedRect, Is.EqualTo(new Rect(0f, -30f, 100f, 140f)));
        }

        [Test]
        public void GetResizeViewportRect_UsesCenterAnchorUniformScale_ForEdgeHandleWhenAltShiftIsPressed()
        {
            Rect resizedRect = CanvasProjectionMath.GetResizeViewportRect(
                new Rect(0f, 0f, 100f, 80f),
                new Rect(0f, 0f, 150f, 80f),
                CanvasHandle.Right,
                uniformScale: true,
                centerAnchor: true);

            Assert.That(resizedRect, Is.EqualTo(new Rect(-50f, -40f, 200f, 160f)));
        }
    }
}
