using NUnit.Framework;
using System.IO;
using SvgEditor.Core.Preview.Build;
using SvgEditor.Core.Preview.Rendering;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.Core.Svg.Geometry;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Renderer;
using SvgEditor.UI.Canvas;
using Unity.VectorGraphics;
using UnityEngine;

namespace SvgEditor.Editor.Tests.PathEditing
{
    public sealed class ArcPathEditingTests
    {
        [Test]
        public void Parse_WhenPathContainsArc_ConvertsArcToEditableCubicSegments()
        {
            const string pathText = "M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z";

            PathData pathData = PathDataParser.Parse(pathText);

            Assert.That(pathData.HasUnsupportedCommands, Is.False);
            Assert.That(pathData.IsMalformed, Is.False, pathData.ParseError);
            Assert.That(pathData.Subpaths, Has.Count.EqualTo(1));
            Assert.That(pathData.Subpaths[0].Nodes.Count, Is.GreaterThan(1));
            Assert.That(pathData.Subpaths[0].Nodes.Exists(node => node.Command == 'C'), Is.True);
        }

        [Test]
        public void TryParsePathContours_WhenPathContainsArc_BuildsPreviewContours()
        {
            const string pathText = "M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z";

            bool parsed = PathGeometryParser.TryParsePathContours(pathText, out BezierContour[] contours);

            Assert.That(parsed, Is.True);
            Assert.That(contours, Is.Not.Null);
            Assert.That(contours.Length, Is.EqualTo(1));
            Assert.That(contours[0].Segments.Length, Is.GreaterThan(1));
        }

        [TestCase("01")]
        [TestCase("10")]
        public void Parse_WhenArcFlagsAreCompactWithoutSeparator_ParsesArc(string compactFlags)
        {
            string pathText = $"M0 0 A5 5 0 {compactFlags} 10 10";

            PathData pathData = PathDataParser.Parse(pathText);

            Assert.That(pathData.HasUnsupportedCommands, Is.False);
            Assert.That(pathData.IsMalformed, Is.False, pathData.ParseError);
            Assert.That(pathData.Subpaths, Has.Count.EqualTo(1));
            Assert.That(pathData.Subpaths[0].Nodes.Exists(node => node.Command == 'C'), Is.True);
        }

        [TestCase("01")]
        [TestCase("10")]
        public void TryParsePathContours_WhenArcFlagsAreCompactWithoutSeparator_BuildsPreviewContours(string compactFlags)
        {
            string pathText = $"M0 0 A5 5 0 {compactFlags} 10 10";

            bool parsed = PathGeometryParser.TryParsePathContours(pathText, out BezierContour[] contours);

            Assert.That(parsed, Is.True);
            Assert.That(contours, Is.Not.Null);
            Assert.That(contours.Length, Is.EqualTo(1));
            Assert.That(contours[0].Segments.Length, Is.GreaterThan(0));
        }

        [Test]
        public void TryEnter_WhenPathContainsArc_EntersPathEditSession()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><path id=\"pen\" fill=\"none\" d=\"M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z\" /></svg>";
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(svg, out var documentModel, out string error);
            Assert.That(loaded, Is.True, error);
            Assert.That(documentModel.TryGetNodeByXmlId("pen", out var node), Is.True);

            var controller = new PathEditEntryController(new ToolController(), new OverlayController());
            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: new DocumentSession
                {
                    DocumentModel = documentModel,
                    OriginalSourceText = svg,
                    WorkingSourceText = svg
                },
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered), result.StatusMessage);
            Assert.That(result.Session, Is.Not.Null);
            Assert.That(result.Session.PathData.Subpaths[0].Nodes.Exists(pathNode => pathNode.Command == 'C'), Is.True);
        }

        [Test]
        public void TryBuildSnapshot_WhenDocumentContainsArcAndLinePaths_BuildsPreviewVectorImage()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 21 21\"><g fill=\"none\" fill-rule=\"evenodd\" stroke=\"#FFFFFF\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path id=\"arc\" d=\"M10.5 16.5a5 5 0 0 0-5-5\"/><path id=\"line\" d=\"M5.5 5.5v11h11\"/></g></svg>";
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(svg, out var documentModel, out string error);
            Assert.That(loaded, Is.True, error);

            var snapshotBuilder = new SnapshotBuilder();
            bool built = snapshotBuilder.TryBuildSnapshot(documentModel, new Rect(0f, 0f, 128f, 128f), out var snapshot, out string buildError);

            Assert.That(built, Is.True, buildError);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.PreviewVectorImage, Is.Not.Null);
            Assert.That(snapshot.Elements, Is.Not.Null);
            Assert.That(snapshot.Elements.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void TryBuild_WhenDocumentContainsArcAndLinePaths_ProducesStrokeShapes()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 21 21\"><g fill=\"none\" fill-rule=\"evenodd\" stroke=\"#FFFFFF\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path id=\"arc\" d=\"M10.5 16.5a5 5 0 0 0-5-5\"/><path id=\"line\" d=\"M5.5 5.5v11h11\"/></g></svg>";
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(svg, out var documentModel, out string error);
            Assert.That(loaded, Is.True, error);

            var sceneBuilder = new SvgModelSceneBuilder();
            bool built = sceneBuilder.TryBuild(documentModel, out SvgModelSceneBuildResult result, out string buildError);

            Assert.That(built, Is.True, buildError);
            Assert.That(CountStrokeShapes(result.Scene.Root), Is.GreaterThan(0));
        }

        [Test]
        public void TryBuildSnapshot_WhenRegressionSuiteContainsArcAndPrimitives_BuildsPreviewVectorImage()
        {
            string assetPath = Path.Combine(Application.dataPath, "Resources/TestSvg/path-edit-regression-suite.svg");
            string svg = File.ReadAllText(assetPath);
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(svg, out var documentModel, out string error);
            Assert.That(loaded, Is.True, error);

            var snapshotBuilder = new SnapshotBuilder();
            bool built = snapshotBuilder.TryBuildSnapshot(documentModel, new Rect(0f, 0f, 520f, 440f), out var snapshot, out string buildError);

            Assert.That(built, Is.True, buildError);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.PreviewVectorImage, Is.Not.Null);
            Assert.That(snapshot.Elements, Is.Not.Null);
            Assert.That(snapshot.Elements.Count, Is.GreaterThanOrEqualTo(10));
        }

        private static int CountStrokeShapes(SceneNode node)
        {
            if (node == null)
            {
                return 0;
            }

            int count = 0;
            if (node.Shapes != null)
            {
                for (int index = 0; index < node.Shapes.Count; index++)
                {
                    Shape shape = node.Shapes[index];
                    if (shape != null && shape.PathProps.Stroke != null)
                    {
                        count++;
                    }
                }
            }

            if (node.Children != null)
            {
                for (int index = 0; index < node.Children.Count; index++)
                {
                    count += CountStrokeShapes(node.Children[index]);
                }
            }

            return count;
        }
    }
}
