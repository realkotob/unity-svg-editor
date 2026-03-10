using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class VectorImageAssetPresentationUtilityTests
    {
        [Test]
        public void IsDeveloperFixtureAsset_ReturnsTrue_ForFixturePath()
        {
            const string assetPath = "Assets/unity-svg-editor/Editor/Tests/Fixtures/preserve-aspect-none-stretch.svg";

            Assert.That(
                VectorImageAssetPresentationUtility.IsDeveloperFixtureAsset(assetPath),
                Is.True);
        }

        [Test]
        public void IsDeveloperFixtureAsset_ReturnsFalse_ForRegularAssetPath()
        {
            const string assetPath = "Assets/Icons/sample.svg";

            Assert.That(
                VectorImageAssetPresentationUtility.IsDeveloperFixtureAsset(assetPath),
                Is.False);
        }

        [Test]
        public void ResolveGroupKey_UsesFixturesHeader_ForDeveloperFixture()
        {
            Assert.That(
                VectorImageAssetPresentationUtility.ResolveGroupKey("negative-coordinates", isDeveloperFixture: true),
                Is.EqualTo("Fixtures"));
        }
    }
}
