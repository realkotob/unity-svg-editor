using System.Linq;
using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class SvgDocumentModelReorderMutationTests
    {
        [Test]
        public void TryReorderElementWithinSameParent_ReordersSiblingsAndSerializes()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadModel(
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><g id=\"parent\"><rect id=\"a\" /><rect id=\"b\" /><rect id=\"c\" /></g></svg>");
            SvgDocumentModelMutationService mutationService = new();

            bool success = mutationService.TryReorderElementWithinSameParent(
                documentModel,
                "b",
                0,
                out SvgDocumentModel updatedDocumentModel,
                out string updatedSourceText,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(updatedSourceText, Does.Contain("id=\"b\""));
            Assert.That(updatedDocumentModel.TryGetNodeByXmlId("parent", out SvgNodeModel parentNode), Is.True);
            string[] childOrder = parentNode.Children
                .Select(childId => updatedDocumentModel.Nodes[childId].XmlId)
                .ToArray();
            Assert.That(childOrder, Is.EqualTo(new[] { "b", "a", "c" }));
        }

        [Test]
        public void TryReorderElementWithinSameParent_ReturnsOriginalSource_WhenTargetIndexMatchesSource()
        {
            string sourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><g id=\"parent\"><rect id=\"a\" /><rect id=\"b\" /></g></svg>";
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadModel(sourceText);
            SvgDocumentModelMutationService mutationService = new();

            bool success = mutationService.TryReorderElementWithinSameParent(
                documentModel,
                "a",
                0,
                out SvgDocumentModel updatedDocumentModel,
                out string updatedSourceText,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(updatedSourceText, Is.EqualTo(sourceText));
            Assert.That(updatedDocumentModel.SourceText, Is.EqualTo(sourceText));
        }
    }
}
