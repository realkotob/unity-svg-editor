using System.Linq;
using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class SvgDocumentModelLoaderTests
    {
        [Test]
        public void TryLoad_LoadsBasicFixtureMetadataAndLegacyKeys()
        {
            var loader = new SvgDocumentModelLoader();
            string source = SvgFixtureTestUtility.LoadFixtureSource("no-viewbox-basic.svg");

            bool success = loader.TryLoad(source, out SvgDocumentModel documentModel, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(documentModel, Is.Not.Null);
            Assert.That(documentModel.Root, Is.Not.Null);
            Assert.That(documentModel.Root.Id, Is.EqualTo(SvgNodeId.Root));
            Assert.That(documentModel.Root.Kind, Is.EqualTo(SvgNodeKind.Root));
            Assert.That(documentModel.Root.LegacyTargetKey, Is.EqualTo(AttributePatcher.ROOT_TARGET_KEY));
            Assert.That(documentModel.Width, Is.EqualTo("160"));
            Assert.That(documentModel.Height, Is.EqualTo("80"));
            Assert.That(documentModel.ViewBox, Is.Empty);
            Assert.That(documentModel.Root.Children.Count, Is.EqualTo(3));

            bool foundRect = documentModel.TryGetNodeByXmlId("basic-rect", out SvgNodeModel rectNode);

            Assert.That(foundRect, Is.True);
            Assert.That(rectNode.Id, Is.EqualTo(SvgNodeId.FromXmlId("basic-rect")));
            Assert.That(rectNode.Kind, Is.EqualTo(SvgNodeKind.Shape));
            Assert.That(rectNode.LegacyElementKey, Is.EqualTo("basic-rect"));
            Assert.That(rectNode.RawAttributes["fill"], Is.EqualTo("#f97316"));
        }

        [Test]
        public void TryLoad_AssignsStructuralCanonicalKeyToNodesWithoutXmlId()
        {
            var loader = new SvgDocumentModelLoader();
            string source = SvgFixtureTestUtility.LoadFixtureSource("no-viewbox-basic.svg");

            bool success = loader.TryLoad(source, out SvgDocumentModel documentModel, out string error);

            Assert.That(success, Is.True, error);

            var backgroundNode = documentModel.NodeOrder
                .Select(nodeId => documentModel.Nodes[nodeId])
                .First(node => node.Depth == 1 && !node.HasXmlId);

            Assert.That(backgroundNode.Id, Is.EqualTo(SvgNodeId.FromStructuralPath("svg[0]/rect[0]")));
            Assert.That(backgroundNode.LegacyElementKey, Is.EqualTo("__auto__:svg[0]/rect[0]"));
            Assert.That(backgroundNode.LegacyTargetKey, Is.EqualTo("__auto__:svg[0]/rect[0]"));
        }

        [Test]
        public void TryLoad_PreservesNegativeViewBoxMetadata()
        {
            var loader = new SvgDocumentModelLoader();
            string source = SvgFixtureTestUtility.LoadFixtureSource("negative-coordinates.svg");

            bool success = loader.TryLoad(source, out SvgDocumentModel documentModel, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(documentModel.ViewBox, Is.EqualTo("-50 -30 200 140"));

            bool foundNode = documentModel.TryGetNodeByXmlId("neg-path", out SvgNodeModel pathNode);

            Assert.That(foundNode, Is.True);
            Assert.That(pathNode.RawAttributes["d"], Is.EqualTo("M -10 90 L 40 30 L 70 95 Z"));
        }

        [Test]
        public void TryLoad_PreservesTransformedParentTree()
        {
            var loader = new SvgDocumentModelLoader();
            string source = SvgFixtureTestUtility.LoadFixtureSource("transformed-parent.svg");

            bool success = loader.TryLoad(source, out SvgDocumentModel documentModel, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(documentModel.TryGetNodeByXmlId("parent-group", out SvgNodeModel groupNode), Is.True);
            Assert.That(groupNode.Kind, Is.EqualTo(SvgNodeKind.Group));
            Assert.That(groupNode.RawAttributes["transform"], Is.EqualTo("translate(40 24) scale(1.25 0.85)"));
            Assert.That(groupNode.Children.Count, Is.EqualTo(3));
            Assert.That(documentModel.TryGetNodeByXmlId("child-path", out SvgNodeModel pathNode), Is.True);
            Assert.That(pathNode.ParentId, Is.EqualTo(groupNode.Id));
            Assert.That(pathNode.RawAttributes["stroke-width"], Is.EqualTo("6"));
        }

        [Test]
        public void TryLoad_PreservesRawAttributesAcrossNestedLeafNodes()
        {
            var loader = new SvgDocumentModelLoader();
            string source = SvgFixtureTestUtility.LoadFixtureSource("tiny-stroke-overlap.svg");

            bool success = loader.TryLoad(source, out SvgDocumentModel documentModel, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(documentModel.TryGetNodeByXmlId("large-panel", out SvgNodeModel panelNode), Is.True);
            Assert.That(panelNode.RawAttributes["opacity"], Is.EqualTo("0.55"));
            Assert.That(panelNode.RawAttributes["rx"], Is.EqualTo("10"));
            Assert.That(documentModel.TryGetNodeByXmlId("stroke-only-line", out SvgNodeModel lineNode), Is.True);
            Assert.That(lineNode.RawAttributes["stroke-linecap"], Is.EqualTo("round"));
            Assert.That(lineNode.RawAttributes["stroke-width"], Is.EqualTo("4"));
        }

        [Test]
        public void TryLoad_CollectsDefinitionNodesAndFragmentReferences()
        {
            var loader = new SvgDocumentModelLoader();
            string source = SvgFixtureTestUtility.LoadFixtureSource("defs-use-basic.svg");

            bool success = loader.TryLoad(source, out SvgDocumentModel documentModel, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(documentModel.DefinitionNodeIds, Is.Not.Empty);
            Assert.That(documentModel.TryGetNodeByXmlId("grad-a", out SvgNodeModel gradientNode), Is.True);
            Assert.That(gradientNode.IsDefinitionNode, Is.True);
            Assert.That(documentModel.TryGetNodeByXmlId("badge-shape", out SvgNodeModel badgeShapeNode), Is.True);
            Assert.That(badgeShapeNode.IsDefinitionNode, Is.True);
            Assert.That(documentModel.TryGetNodeByXmlId("badge-instance", out SvgNodeModel useNode), Is.True);
            Assert.That(useNode.Kind, Is.EqualTo(SvgNodeKind.Use));
            Assert.That(useNode.References.Any(reference => reference.AttributeName == "href" && reference.FragmentId == "badge-shape"), Is.True);
            Assert.That(documentModel.TryGetNodeByXmlId("badge-instance-legacy", out SvgNodeModel legacyUseNode), Is.True);
            Assert.That(legacyUseNode.References.Any(reference => reference.AttributeName == "xlink:href" && reference.FragmentId == "badge-shape"), Is.True);

            var gradientConsumerNode = documentModel.NodeOrder
                .Select(nodeId => documentModel.Nodes[nodeId])
                .First(node => node.IsDefinitionNode && node.RawAttributes.TryGetValue("fill", out var fillValue) && fillValue == "url(#grad-a)");

            Assert.That(gradientConsumerNode.References.Any(reference => reference.FragmentId == "grad-a"), Is.True);
        }

        [Test]
        public void TryLoad_PreservesRootPreserveAspectRatioMetadata()
        {
            var loader = new SvgDocumentModelLoader();
            const string source = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"100\" viewBox=\"0 0 100 100\" preserveAspectRatio=\"xMaxYMin slice\"><rect id=\"r\" x=\"0\" y=\"0\" width=\"100\" height=\"100\" /></svg>";

            bool success = loader.TryLoad(source, out SvgDocumentModel documentModel, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(documentModel.PreserveAspectRatio, Is.EqualTo("xMaxYMin slice"));
        }
    }
}
