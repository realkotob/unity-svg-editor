using NUnit.Framework;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Preview.Build;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Workspace.Coordination;
using SvgEditor.UI.Workspace.Document;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Editor.Tests
{
    public sealed class DocumentPreviewServiceTests
    {
        [Test]
        public void ApplyCurrentPreviewState_WhenCurrentDocumentChanged_RebuildsPreviewInsteadOfReusingStaleSnapshot()
        {
            const string svgA = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 21 21\"><g fill=\"none\" fill-rule=\"evenodd\" stroke=\"#FFFFFF\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10.5 16.5a5 5 0 0 0-5-5\"/><path d=\"M5.5 5.5v11h11\"/></g></svg>";
            const string svgB = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect x=\"2\" y=\"3\" width=\"8\" height=\"5\" fill=\"#0ea5e9\" /></svg>";

            DocumentSession currentDocument = CreateDocumentSession(svgA);
            var root = new VisualElement();
            var previewImage = new Image { name = "preview-image" };
            root.Add(previewImage);

            var view = new DocumentLifecycleView();
            view.Bind(root);

            var service = new DocumentPreviewService(
                new SnapshotBuilder(),
                view,
                () => currentDocument,
                () => null);

            service.RefreshLivePreview(keepExistingPreviewOnFailure: false);
            Object firstVectorImage = service.PreviewSnapshot?.PreviewVectorImage;
            Assert.That(firstVectorImage, Is.Not.Null);

            currentDocument = CreateDocumentSession(svgB);
            service.ApplyCurrentPreviewState();

            Assert.That(service.PreviewSnapshot?.PreviewVectorImage, Is.Not.Null);
            Assert.That(service.PreviewSnapshot.PreviewVectorImage, Is.Not.SameAs(firstVectorImage));

            service.Dispose();
        }

        [Test]
        public void ApplyCurrentPreviewState_WhenTransientPreviewExistsForSameDocument_ReappliesExistingSnapshot()
        {
            const string svgA = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 21 21\"><g fill=\"none\" fill-rule=\"evenodd\" stroke=\"#FFFFFF\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10.5 16.5a5 5 0 0 0-5-5\"/><path d=\"M5.5 5.5v11h11\"/></g></svg>";
            const string svgB = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect x=\"2\" y=\"3\" width=\"8\" height=\"5\" fill=\"#0ea5e9\" /></svg>";

            DocumentSession currentDocument = CreateDocumentSession(svgA);
            currentDocument.AssetPath = "Assets/Test/icon.svg";
            var root = new VisualElement();
            root.Add(new Image { name = "preview-image" });

            var view = new DocumentLifecycleView();
            view.Bind(root);

            var service = new DocumentPreviewService(
                new SnapshotBuilder(),
                view,
                () => currentDocument,
                () => null);

            bool transientBuilt = service.TryRefreshTransientPreview(CreateDocumentSession(svgB).DocumentModel);
            Assert.That(transientBuilt, Is.True);
            PreviewSnapshot transientSnapshot = service.PreviewSnapshot;
            Assert.That(transientSnapshot, Is.Not.Null);

            service.ApplyCurrentPreviewState();

            Assert.That(service.PreviewSnapshot, Is.SameAs(transientSnapshot));
            service.Dispose();
        }

        private static DocumentSession CreateDocumentSession(string svg)
        {
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(svg, out var documentModel, out string error);
            Assert.That(loaded, Is.True, error);

            return new DocumentSession
            {
                DocumentModel = documentModel,
                OriginalSourceText = svg,
                WorkingSourceText = svg
            };
        }
    }
}
