using NUnit.Framework;
using UnityEngine;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class PreviewSnapshotContractTests
    {
        [Test]
        public void CanvasViewportRect_UsesProjectionRect_WhenProjectionRectIsAvailable()
        {
            var snapshot = new PreviewSnapshot
            {
                DocumentViewportRect = new Rect(0f, 0f, 128f, 128f),
                ProjectionRect = new Rect(-24f, 12f, 256f, 192f),
                VisualContentBounds = new Rect(-12f, 4f, 96f, 72f)
            };

            Assert.That(snapshot.HasDocumentViewport, Is.True);
            Assert.That(snapshot.HasProjectionRect, Is.True);
            Assert.That(snapshot.CanvasViewportRect, Is.EqualTo(snapshot.ProjectionRect));
        }

        [Test]
        public void CanvasViewportRect_FallsBackToVisualContentBounds_WhenProjectionRectIsMissing()
        {
            var snapshot = new PreviewSnapshot
            {
                DocumentViewportRect = new Rect(0f, 0f, 128f, 128f),
                ProjectionRect = default,
                VisualContentBounds = new Rect(-32f, -16f, 240f, 180f)
            };

            Assert.That(snapshot.HasProjectionRect, Is.False);
            Assert.That(snapshot.CanvasViewportRect, Is.EqualTo(snapshot.VisualContentBounds));
        }

        [Test]
        public void ResolveProjectionRect_PrefersDocumentViewport_ThenPreferredViewport_ThenSceneBounds()
        {
            Rect documentViewport = new Rect(0f, 0f, 100f, 80f);
            Rect preferredViewport = new Rect(-20f, -10f, 240f, 180f);
            Rect sceneBounds = new Rect(-40f, -30f, 320f, 240f);

            Assert.That(
                PreviewSnapshotSceneImportService.ResolveProjectionRect(documentViewport, sceneBounds, preferredViewport),
                Is.EqualTo(documentViewport));

            Assert.That(
                PreviewSnapshotSceneImportService.ResolveProjectionRect(default, sceneBounds, preferredViewport),
                Is.EqualTo(preferredViewport));

            Assert.That(
                PreviewSnapshotSceneImportService.ResolveProjectionRect(default, sceneBounds, default),
                Is.EqualTo(sceneBounds));
        }
    }
}
