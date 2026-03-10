using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class InspectorDocumentModelReaderTests
    {
        [Test]
        public void ExtractTargets_UsesLegacyTargetKeysFromDocumentModel()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadFixtureModel("no-viewbox-basic.svg");

            IReadOnlyList<PatchTarget> targets = InspectorDocumentModelReader.ExtractTargets(documentModel);

            Assert.That(targets.Any(target => target.Key == "__auto__:svg[0]/rect[0]" && target.DisplayName == "rect  [1]"), Is.True);
            Assert.That(targets.Any(target => target.Key == "basic-rect" && target.DisplayName == "#basic-rect  <rect>"), Is.True);
            Assert.That(targets.Any(target => target.Key == "basic-circle" && target.DisplayName == "#basic-circle  <circle>"), Is.True);
        }

        [Test]
        public void TryReadAttributes_ResolvesRootAndAutoTargetKeys()
        {
            SvgDocumentModel documentModel = SvgFixtureTestUtility.LoadFixtureModel("no-viewbox-basic.svg");

            bool rootSuccess = InspectorDocumentModelReader.TryReadAttributes(
                documentModel,
                AttributePatcher.ROOT_TARGET_KEY,
                out Dictionary<string, string> rootAttributes,
                out string rootError);
            bool childSuccess = InspectorDocumentModelReader.TryReadAttributes(
                documentModel,
                "__auto__:svg[0]/rect[0]",
                out Dictionary<string, string> childAttributes,
                out string childError);

            Assert.That(rootSuccess, Is.True, rootError);
            Assert.That(rootAttributes["width"], Is.EqualTo("160"));
            Assert.That(rootAttributes["height"], Is.EqualTo("80"));
            Assert.That(childSuccess, Is.True, childError);
            Assert.That(childAttributes["fill"], Is.EqualTo("#0b1020"));
            Assert.That(childAttributes["width"], Is.EqualTo("160"));
        }
    }
}
