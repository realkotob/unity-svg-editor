using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class DocumentRepositorySaveTests
    {
        [Test]
        public void TryResolveSourceTextToPersist_UsesSerializedDocumentModelWhenInSync()
        {
            const string sourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" fill=\"#ffffff\" /></svg>";
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadModel(sourceText);
            SvgNodeModel node = documentModel.Root.Children.Count > 0
                ? documentModel.Nodes[documentModel.Root.Children[0]]
                : null;
            node.RawAttributes = new System.Collections.Generic.Dictionary<string, string>(node.RawAttributes, System.StringComparer.Ordinal)
            {
                ["fill"] = "#123456"
            };

            DocumentSession document = new()
            {
                WorkingSourceText = sourceText,
                DocumentModel = documentModel,
                DocumentModelLoadError = string.Empty
            };

            DocumentRepository repository = new();
            bool success = repository.TryResolveSourceTextToPersist(document, out string persistedSourceText, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(persistedSourceText, Does.Contain("fill=\"#123456\""));
        }

        [Test]
        public void TryResolveSourceTextToPersist_FallsBackToWorkingSourceTextWhenModelIsUnavailable()
        {
            const string sourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" /></svg>";
            DocumentSession document = new()
            {
                WorkingSourceText = sourceText
            };

            DocumentRepository repository = new();
            bool success = repository.TryResolveSourceTextToPersist(document, out string persistedSourceText, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(persistedSourceText, Is.EqualTo(sourceText));
        }

        [Test]
        public void TryResolveSourceTextToPersist_FallsBackToWorkingSourceTextWhenModelIsStale()
        {
            const string sourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" /></svg>";
            DocumentSession document = new()
            {
                WorkingSourceText = sourceText,
                DocumentModel = SvgFixtureTestUtility.LoadModel("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"other\" /></svg>")
            };

            DocumentRepository repository = new();
            bool success = repository.TryResolveSourceTextToPersist(document, out string persistedSourceText, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(persistedSourceText, Is.EqualTo(sourceText));
        }
    }
}
