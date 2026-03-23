using NUnit.Framework;
using SvgEditor.Core.Preview.Build;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Workspace.Document;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Editor.Tests
{
    public sealed class DocumentPreviewBindingTests
    {
        [Test]
        public void Bind_WhenSinglePreviewImageExists_BindsAndAppliesVectorImage()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 21 21\"><g fill=\"none\" fill-rule=\"evenodd\" stroke=\"#FFFFFF\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10.5 16.5a5 5 0 0 0-5-5\"/><path d=\"M5.5 5.5v11h11\"/></g></svg>";
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(svg, out var documentModel, out string error);
            Assert.That(loaded, Is.True, error);

            var snapshotBuilder = new SnapshotBuilder();
            bool built = snapshotBuilder.TryBuildSnapshot(documentModel, new Rect(0f, 0f, 128f, 128f), out var snapshot, out string buildError);
            Assert.That(built, Is.True, buildError);

            var root = new VisualElement();
            var previewImage = new Image { name = "preview-image" };
            root.Add(previewImage);

            var view = new DocumentLifecycleView();
            view.Bind(root);
            view.SetPreviewVectorImage(snapshot.PreviewVectorImage);

            Assert.That(view.PreviewImage, Is.SameAs(previewImage));
            Assert.That(previewImage.vectorImage, Is.Null);
            Assert.That(previewImage.style.backgroundImage.value.vectorImage, Is.SameAs(snapshot.PreviewVectorImage));
        }

        [Test]
        public void Bind_WhenMultiplePreviewImagesExist_UsesFirstImageMatch()
        {
            var root = new VisualElement();
            var firstPreviewImage = new Image { name = "preview-image" };
            var secondPreviewImage = new Image { name = "preview-image" };
            root.Add(firstPreviewImage);
            root.Add(secondPreviewImage);

            var view = new DocumentLifecycleView();
            view.Bind(root);

            Assert.That(view.PreviewImage, Is.SameAs(firstPreviewImage));
        }

        [Test]
        public void Snapshot_WhenAnglePreviewBuilt_HasRenderableVectorImage()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 21 21\"><g fill=\"none\" fill-rule=\"evenodd\" stroke=\"#FFFFFF\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10.5 16.5a5 5 0 0 0-5-5\"/><path d=\"M5.5 5.5v11h11\"/></g></svg>";
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(svg, out var documentModel, out string error);
            Assert.That(loaded, Is.True, error);

            var snapshotBuilder = new SnapshotBuilder();
            bool built = snapshotBuilder.TryBuildSnapshot(documentModel, new Rect(0f, 0f, 128f, 128f), out var snapshot, out string buildError);
            Assert.That(built, Is.True, buildError);

            VectorImage imported = Resources.Load<VectorImage>("Icons/Custom/angle");
            Assert.That(imported, Is.Not.Null);

            Assert.That(SceneImportService.HasRenderableGeometry(snapshot.PreviewVectorImage), Is.True);
            Assert.That(SceneImportService.HasRenderableGeometry(imported), Is.True);
        }
    }
}
