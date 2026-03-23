using System.Reflection;
using NUnit.Framework;
using SvgEditor.Core.Preview.Build;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Shared;

namespace SvgEditor.Tests.Editor
{
    public sealed class PreviewBuildRefactorTests
    {
        private const string DefsOnlySvg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" preserveAspectRatio=""none"">
                <defs>
                    <clipPath id=""clip-a"">
                        <rect x=""0"" y=""0"" width=""10"" height=""10"" />
                    </clipPath>
                </defs>
            </svg>";

        [Test]
        public void Prepare_AssignsSyntheticIdsToNonRootElementsWithoutIds()
        {
            Result<PreparedPreviewDocument> result = PreviewDocumentPreparation.Prepare(DefsOnlySvg);

            Assert.That(result.IsSuccess, Is.True, result.Error);

            PreparedPreviewDocument preparedDocument = result.Value;

            Assert.That(preparedDocument.Root.HasAttribute("id"), Is.False);
            Assert.That(preparedDocument.KeyByNodeId.Count, Is.EqualTo(3));
            Assert.That(preparedDocument.PreserveAspectRatioMode, Is.EqualTo(SvgPreserveAspectRatioMode.None));
        }

        [Test]
        public void SceneImportService_DoesNotExposeLegacyTryImportWrapper()
        {
            MethodInfo tryImportScene = typeof(SceneImportService).GetMethod(
                "TryImportScene",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            Assert.That(tryImportScene, Is.Null);
        }
    }
}
