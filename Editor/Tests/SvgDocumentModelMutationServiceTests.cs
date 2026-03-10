using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class SvgDocumentModelMutationServiceTests
    {
        [Test]
        public void TryApplyAttributePatch_UpdatesFillAndOpacityOnLeafNode()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadFixtureModel("no-viewbox-basic.svg");
            SvgDocumentModelMutationService mutationService = new();

            bool success = mutationService.TryApplyAttributePatch(
                documentModel,
                new AttributePatchRequest
                {
                    TargetKey = "basic-rect",
                    Fill = "#123456",
                    FillOpacity = "0.5"
                },
                out SvgDocumentModel updatedDocumentModel,
                out string updatedSourceText,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(updatedSourceText, Does.Contain("fill=\"#123456\""));
            Assert.That(updatedSourceText, Does.Contain("fill-opacity=\"0.5\""));
            Assert.That(updatedDocumentModel.TryGetNodeByXmlId("basic-rect", out SvgNodeModel rectNode), Is.True);
            Assert.That(rectNode.RawAttributes["fill"], Is.EqualTo("#123456"));
            Assert.That(rectNode.RawAttributes["fill-opacity"], Is.EqualTo("0.5"));
        }

        [Test]
        public void TryApplyAttributePatch_RemovesEmptyStyleAttributes()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadModel("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" stroke=\"#ffffff\" stroke-linejoin=\"round\" /></svg>");
            SvgDocumentModelMutationService mutationService = new();

            bool success = mutationService.TryApplyAttributePatch(
                documentModel,
                new AttributePatchRequest
                {
                    TargetKey = "node",
                    StrokeLinejoin = string.Empty
                },
                out SvgDocumentModel updatedDocumentModel,
                out string updatedSourceText,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(updatedSourceText, Does.Not.Contain("stroke-linejoin"));
            Assert.That(updatedDocumentModel.TryGetNodeByXmlId("node", out SvgNodeModel node), Is.True);
            Assert.That(node.RawAttributes.ContainsKey("stroke-linejoin"), Is.False);
        }

        [Test]
        public void TryApplyAttributePatch_UpdatesRootTarget()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadFixtureModel("no-viewbox-basic.svg");
            SvgDocumentModelMutationService mutationService = new();

            bool success = mutationService.TryApplyAttributePatch(
                documentModel,
                new AttributePatchRequest
                {
                    TargetKey = AttributePatcher.ROOT_TARGET_KEY,
                    Opacity = "0.75"
                },
                out SvgDocumentModel updatedDocumentModel,
                out string updatedSourceText,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(updatedSourceText, Does.Contain("opacity=\"0.75\""));
            Assert.That(updatedDocumentModel.Root.RawAttributes["opacity"], Is.EqualTo("0.75"));
        }

        [Test]
        public void TryPrependElementTranslation_PrependsTransformOnLeafNode()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadModel(
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" transform=\"scale(2 2)\" /></svg>");
            SvgDocumentModelMutationService mutationService = new();

            bool success = mutationService.TryPrependElementTranslation(
                documentModel,
                "node",
                new UnityEngine.Vector2(3f, 4f),
                out SvgDocumentModel updatedDocumentModel,
                out string updatedSourceText,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(updatedSourceText, Does.Contain("transform=\"translate(3 4) scale(2 2)\""));
            Assert.That(updatedDocumentModel.TryGetNodeByXmlId("node", out SvgNodeModel node), Is.True);
            Assert.That(node.RawAttributes["transform"], Is.EqualTo("translate(3 4) scale(2 2)"));
        }

        [Test]
        public void TryPrependElementScale_PrependsScaleAroundTransform()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadModel(
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" transform=\"translate(1 2)\" /></svg>");
            SvgDocumentModelMutationService mutationService = new();

            bool success = mutationService.TryPrependElementScale(
                documentModel,
                "node",
                new UnityEngine.Vector2(2f, 3f),
                new UnityEngine.Vector2(5f, 6f),
                out SvgDocumentModel updatedDocumentModel,
                out string updatedSourceText,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(updatedSourceText, Does.Contain("translate(5 6) scale(2 3) translate(-5 -6) translate(1 2)"));
            Assert.That(updatedDocumentModel.TryGetNodeByXmlId("node", out SvgNodeModel node), Is.True);
            Assert.That(node.RawAttributes["transform"], Is.EqualTo("translate(5 6) scale(2 3) translate(-5 -6) translate(1 2)"));
        }
    }
}
