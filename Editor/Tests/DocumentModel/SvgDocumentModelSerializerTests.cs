using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class SvgDocumentModelSerializerTests
    {
        [Test]
        public void TrySerialize_ReturnsFalse_WhenDocumentModelIsNull()
        {
            var serializer = new SvgDocumentModelSerializer();

            bool success = serializer.TrySerialize(null, out string sourceText, out string error);

            Assert.That(success, Is.False);
            Assert.That(sourceText, Is.Empty);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void TrySerialize_ProducesWellFormedSvgFromLoadedFixture()
        {
            var serializer = new SvgDocumentModelSerializer();
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadFixtureModel("no-viewbox-basic.svg");

            bool success = serializer.TrySerialize(documentModel, out string sourceText, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(SvgDocumentXmlUtility.TryGetRootElement(sourceText, out _, out var root, out var xmlError), Is.True, xmlError);
            Assert.That(root.LocalName, Is.EqualTo("svg"));
            Assert.That(root.GetAttribute("width"), Is.EqualTo("160"));
            Assert.That(root.GetAttribute("height"), Is.EqualTo("80"));
        }

        [Test]
        public void TrySerialize_PreservesRootMetadataAndNamespaces()
        {
            const string source = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"200\" height=\"100\" viewBox=\"0 0 100 100\" preserveAspectRatio=\"xMaxYMin slice\"><use id=\"u\" xlink:href=\"#badge\" x=\"4\" y=\"7\" /></svg>";
            var serializer = new SvgDocumentModelSerializer();
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadModel(source);

            bool success = serializer.TrySerialize(documentModel, out string serialized, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(serialized, Does.Contain("preserveAspectRatio=\"xMaxYMin slice\""));
            Assert.That(serialized, Does.Contain("xmlns:xlink=\"http://www.w3.org/1999/xlink\""));
            Assert.That(serialized, Does.Contain("xlink:href=\"#badge\""));
        }

        [Test]
        public void TrySerialize_PreservesDefsUseReferences()
        {
            var serializer = new SvgDocumentModelSerializer();
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadFixtureModel("defs-use-basic.svg");

            bool success = serializer.TrySerialize(documentModel, out string sourceText, out string error);

            Assert.That(success, Is.True, error);
            Assert.That(sourceText, Does.Contain("<defs"));
            Assert.That(sourceText, Does.Contain("href=\"#badge-shape\""));
            Assert.That(sourceText, Does.Contain("xlink:href=\"#badge-shape\""));
            Assert.That(sourceText, Does.Contain("url(#grad-a)"));
        }
    }
}
