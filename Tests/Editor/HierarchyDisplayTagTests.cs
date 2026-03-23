using System.Linq;
using NUnit.Framework;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Svg.Structure.Xml;
using SvgEditor.UI.Hierarchy;

namespace SvgEditor.Editor.Tests
{
    public sealed class HierarchyDisplayTagTests
    {
        [Test]
        public void TryBuildSnapshot_UsesRectDisplayNameAndIcon_ForSanitizedMaskRectPath()
        {
            const string source = "<svg xmlns=\"http://www.w3.org/2000/svg\"><defs><mask id=\"mask-1\" fill=\"white\"><rect x=\"2\" y=\"6\" width=\"20\" height=\"5\" rx=\"1\"/></mask></defs><rect id=\"border\" x=\"2\" y=\"6\" width=\"20\" height=\"5\" rx=\"1\" stroke=\"white\" stroke-width=\"4\" mask=\"url(#mask-1)\"/></svg>";

            Assert.That(MaskArtifactSanitizer.TrySanitize(source, out string sanitizedSource, out bool changed, out string sanitizeError), Is.True, sanitizeError);
            Assert.That(changed, Is.True);

            SvgLoader loader = new();
            Assert.That(loader.TryLoad(sanitizedSource, out var documentModel, out string loadError), Is.True, loadError);
            Assert.That(HierarchyModelReader.TryBuildSnapshot(documentModel, out HierarchyOutline snapshot, out string hierarchyError), Is.True, hierarchyError);

            HierarchyNode borderNode = snapshot.Elements.Single(node => node.Key == "border");

            Assert.That(borderNode.TagName, Is.EqualTo(SvgTagName.RECT));
            Assert.That(borderNode.DisplayName, Does.Contain("<rect>"));
            Assert.That(HierarchyTreeUtility.ResolveHierarchyIconKind(borderNode.TagName), Is.EqualTo(IconKind.Square));
        }

        [Test]
        public void RefreshDocumentModelSnapshot_PreservesDisplayTag_ForPreviouslySanitizedMaskRectPath()
        {
            const string source = "<svg xmlns=\"http://www.w3.org/2000/svg\"><defs><mask id=\"mask-1\" fill=\"white\"><rect x=\"2\" y=\"6\" width=\"20\" height=\"5\" rx=\"1\"/></mask></defs><rect id=\"border\" x=\"2\" y=\"6\" width=\"20\" height=\"5\" rx=\"1\" stroke=\"white\" stroke-width=\"4\" mask=\"url(#mask-1)\"/></svg>";
            const string persistedPathSource = "<svg xmlns=\"http://www.w3.org/2000/svg\"><path id=\"border\" fill=\"white\" fill-rule=\"evenodd\" d=\"M 2 6 H 22 V 11 H 2 Z M 4 8 H 20 V 9 H 4 Z\"/></svg>";

            Assert.That(MaskArtifactSanitizer.TrySanitize(source, out string sanitizedSource, out bool changed, out string sanitizeError), Is.True, sanitizeError);
            Assert.That(changed, Is.True);

            DocumentSession document = new();
            DocumentSourceService sourceService = new();

            sourceService.RefreshDocumentModelSnapshot(document, sanitizedSource);
            sourceService.RefreshDocumentModelSnapshot(document, persistedPathSource);

            Assert.That(document.DocumentModel.TryGetNodeByXmlId("border", out var borderNode), Is.True);
            Assert.That(borderNode.DisplayTagName, Is.EqualTo(SvgTagName.RECT));
        }
    }
}
