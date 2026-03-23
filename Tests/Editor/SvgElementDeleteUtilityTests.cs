using NUnit.Framework;
using SvgEditor.Core.Svg.Structure.Xml;

namespace SvgEditor.Editor.Tests
{
    public sealed class SvgElementDeleteUtilityTests
    {
        [Test]
        public void TryDeleteElements_RemovesSelectedElementsFromSource()
        {
            const string source = "<svg><g id=\"group\"><path id=\"path\" /></g><rect id=\"rect\" /></svg>";

            bool ok = SvgElementDeleteUtility.TryDeleteElements(
                source,
                new[] { "group" },
                out string updatedSource,
                out string error);

            Assert.That(ok, Is.True, error);
            Assert.That(updatedSource, Does.Not.Contain("group"));
            Assert.That(updatedSource, Does.Not.Contain("path"));
            Assert.That(updatedSource, Does.Contain("rect"));
        }
    }
}
