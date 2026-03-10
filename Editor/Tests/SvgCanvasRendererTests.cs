using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class SvgCanvasRendererTests
    {
        [Test]
        public void TryBuildRenderDocument_UsesDocumentModelNodeOrderAndLegacyKeys()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadFixtureModel("no-viewbox-basic.svg");
            SvgCanvasRenderer renderer = new();

            bool success = renderer.TryBuildRenderDocument(documentModel, out SvgRenderDocument renderDocument, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(renderDocument.DrawOrder.Count, Is.EqualTo(documentModel.NodeOrder.Count));
            Assert.That(renderDocument.TryGetNode(SvgNodeId.FromXmlId("basic-rect"), out SvgRenderNode rectNode), Is.True);
            Assert.That(rectNode.ElementKey, Is.EqualTo("basic-rect"));
            Assert.That(rectNode.TargetKey, Is.EqualTo("basic-rect"));
            Assert.That(rectNode.DrawOrder, Is.EqualTo(renderDocument.DrawOrder.ToList().IndexOf(rectNode.NodeId)));
        }

        [Test]
        public void TryBuildPreviewSnapshot_BuildsSnapshotFromDocumentModel()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadFixtureModel("transformed-parent.svg");
            SvgCanvasRenderer renderer = new();

            bool success = renderer.TryBuildPreviewSnapshot(
                documentModel,
                preferredViewportRect: default,
                out PreviewSnapshot snapshot,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.PreviewVectorImage, Is.Not.Null);
            Assert.That(snapshot.Elements, Is.Not.Empty);
            Assert.That(snapshot.Elements.Any(element => element.Key == "parent-group"), Is.True);
        }
    }
}
