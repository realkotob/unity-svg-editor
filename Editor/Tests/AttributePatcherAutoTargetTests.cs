using System.Linq;
using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class AttributePatcherAutoTargetTests
    {
        [Test]
        public void ExtractTargets_IncludesElementsWithoutStableIds()
        {
            var patcher = new AttributePatcher();
            var source = "<svg xmlns=\"http://www.w3.org/2000/svg\"><g><rect width=\"10\" height=\"20\" /></g></svg>";

            var targets = patcher.ExtractTargets(source);

            Assert.That(targets.Any(item => item.Key == "__auto__:svg[0]/g[0]"), Is.True);
            Assert.That(targets.Any(item => item.Key == "__auto__:svg[0]/g[0]/rect[0]"), Is.True);
        }

        [Test]
        public void TryReadAttributes_ResolvesAutoElementKey()
        {
            var patcher = new AttributePatcher();
            var source = "<svg xmlns=\"http://www.w3.org/2000/svg\"><g><rect width=\"10\" height=\"20\" transform=\"translate(4 5)\" /></g></svg>";

            var success = patcher.TryReadAttributes(
                source,
                "__auto__:svg[0]/g[0]/rect[0]",
                out var attributes,
                out var error);

            Assert.That(success, Is.True, error);
            Assert.That(attributes["width"], Is.EqualTo("10"));
            Assert.That(attributes["transform"], Is.EqualTo("translate(4 5)"));
        }
    }
}
