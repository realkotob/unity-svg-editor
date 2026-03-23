using NUnit.Framework;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Svg.Source;

namespace SvgEditor.Editor.Tests
{
    public sealed class TextEditingPolicyTests
    {
        [Test]
        public void RefreshDocumentModelSnapshot_WhenDocumentContainsTextAndShapes_KeepsModelEditingEnabled()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><g id=\"block\"><text x=\"1\" y=\"2\">label</text><circle id=\"shape\" cx=\"8\" cy=\"8\" r=\"4\" fill=\"#ff0000\" /></g></svg>";
            var service = new DocumentSourceService();
            var document = new DocumentSession
            {
                WorkingSourceText = svg,
                OriginalSourceText = svg
            };

            service.RefreshDocumentModelSnapshot(document, svg);

            Assert.That(document.DocumentModel, Is.Not.Null);
            Assert.That(document.DocumentModelLoadError, Is.Empty);
            Assert.That(document.ModelEditingBlockReason, Is.Empty);
            Assert.That(document.CanUseDocumentModelForEditing, Is.True);
        }

        [Test]
        public void TryApplyAttributePatch_WhenDocumentContainsText_PreservesTextContent()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><g id=\"block\"><text x=\"1\" y=\"2\">label</text><circle id=\"shape\" cx=\"8\" cy=\"8\" r=\"4\" fill=\"#ff0000\" /></g></svg>";
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(svg, out var documentModel, out string error);
            Assert.That(loaded, Is.True, error);

            var mutator = new SvgMutator();
            bool mutated = mutator.TryApplyAttributePatch(
                documentModel,
                new AttributePatchRequest
                {
                    TargetKey = "shape",
                    Fill = "#00ff00"
                },
                out MutationResult result);

            Assert.That(mutated, Is.True, result.Error);
            Assert.That(result.UpdatedSourceText, Does.Contain(">label</text>"));
            Assert.That(result.UpdatedSourceText, Does.Contain("fill=\"#00ff00\""));
        }
    }
}
