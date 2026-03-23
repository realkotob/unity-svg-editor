using NUnit.Framework;
using UnityEngine;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.Core.Svg.Source;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class PathEditingTests
    {
        [Test]
        public void Parse_NormalizesSmoothShorthandCommands_ForEditableSubpaths()
        {
            PathData pathData = PathDataParser.Parse("M 0 0 L 10 10 H 15 V 5 C 20 5 25 20 30 15 S 35 20 40 25 Q 45 30 50 35 T 55 40 Z");

            Assert.That(pathData.HasUnsupportedCommands, Is.False);
            Assert.That(pathData.IsMalformed, Is.False);
            Assert.That(pathData.Subpaths, Has.Count.EqualTo(1));

            PathSubpath subpath = pathData.Subpaths[0];
            Assert.That(subpath.Start, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(subpath.IsClosed, Is.True);
            Assert.That(subpath.Nodes, Has.Count.EqualTo(7));

            Assert.That(subpath.Nodes[0].Command, Is.EqualTo('L'));
            Assert.That(subpath.Nodes[0].Position, Is.EqualTo(new Vector2(10f, 10f)));

            Assert.That(subpath.Nodes[1].Command, Is.EqualTo('H'));
            Assert.That(subpath.Nodes[1].Position, Is.EqualTo(new Vector2(15f, 10f)));

            Assert.That(subpath.Nodes[2].Command, Is.EqualTo('V'));
            Assert.That(subpath.Nodes[2].Position, Is.EqualTo(new Vector2(15f, 5f)));

            Assert.That(subpath.Nodes[3].Command, Is.EqualTo('C'));
            Assert.That(subpath.Nodes[3].Control0, Is.EqualTo(new Vector2(20f, 5f)));
            Assert.That(subpath.Nodes[3].Control1, Is.EqualTo(new Vector2(25f, 20f)));
            Assert.That(subpath.Nodes[3].Position, Is.EqualTo(new Vector2(30f, 15f)));
            Assert.That(subpath.Nodes[3].HandleMode, Is.EqualTo(PathHandleMode.Free));

            Assert.That(subpath.Nodes[4].Command, Is.EqualTo('C'));
            Assert.That(subpath.Nodes[4].Control0, Is.EqualTo(new Vector2(35f, 10f)));
            Assert.That(subpath.Nodes[4].Control1, Is.EqualTo(new Vector2(35f, 20f)));
            Assert.That(subpath.Nodes[4].Position, Is.EqualTo(new Vector2(40f, 25f)));
            Assert.That(subpath.Nodes[4].HandleMode, Is.EqualTo(PathHandleMode.Free));

            Assert.That(subpath.Nodes[5].Command, Is.EqualTo('Q'));
            Assert.That(subpath.Nodes[5].Control0, Is.EqualTo(new Vector2(45f, 30f)));
            Assert.That(subpath.Nodes[5].Position, Is.EqualTo(new Vector2(50f, 35f)));

            Assert.That(subpath.Nodes[6].Command, Is.EqualTo('Q'));
            Assert.That(subpath.Nodes[6].Control0, Is.EqualTo(new Vector2(55f, 40f)));
            Assert.That(subpath.Nodes[6].Position, Is.EqualTo(new Vector2(55f, 40f)));
            Assert.That(subpath.Nodes[6].HandleMode, Is.EqualTo(PathHandleMode.Free));
        }

        [Test]
        public void Serialize_ReturnsDeterministicPathDataText()
        {
            PathData pathData = new();
            PathSubpath subpath = new
            (
                new Vector2(0f, 0f),
                new[]
                {
                    new PathNode('L', new Vector2(10f, 10f)),
                    new PathNode('C', new Vector2(30f, 15f), new Vector2(20f, 5f), new Vector2(25f, 10f), PathHandleMode.Free),
                    new PathNode('Q', new Vector2(50f, 35f), new Vector2(45f, 30f), default, PathHandleMode.Free),
                    new PathNode('T', new Vector2(55f, 40f), default, default, PathHandleMode.Mirrored)
                },
                isClosed: true
            );

            pathData.Subpaths.Add(subpath);

            string pathText = PathDataSerializer.Serialize(pathData);

            Assert.That(pathText, Is.EqualTo("M 0 0 L 10 10 C 20 5 25 10 30 15 Q 45 30 50 35 T 55 40 Z"));
        }

        [Test]
        public void TryApplyPathData_UpdatesOnlyPathDAttribute()
        {
            const string source = "<svg><path id=\"shape\" d=\"M 0 0 L 1 1\" fill=\"red\" stroke=\"blue\" /><rect id=\"other\" width=\"5\" height=\"6\" /></svg>";

            SvgLoader loader = new();
            Assert.That(loader.TryLoad(source, out SvgDocumentModel documentModel, out string loadError), Is.True, loadError);

            PathData updatedPathData = PathDataParser.Parse("M 2 2 L 3 3 Z");
            PathMutationService service = new();

            bool ok = service.TryApplyPathData(documentModel, "shape", updatedPathData, out MutationResult result);

            Assert.That(ok, Is.True, result.Error);
            Assert.That(result.UpdatedDocumentModel.TryGetNodeByXmlId("shape", out SvgNodeModel shapeNode), Is.True);
            Assert.That(shapeNode.RawAttributes["d"], Is.EqualTo("M 2 2 L 3 3 Z"));
            Assert.That(shapeNode.RawAttributes["fill"], Is.EqualTo("red"));
            Assert.That(shapeNode.RawAttributes["stroke"], Is.EqualTo("blue"));

            Assert.That(result.UpdatedDocumentModel.TryGetNodeByXmlId("other", out SvgNodeModel otherNode), Is.True);
            Assert.That(otherNode.RawAttributes["width"], Is.EqualTo("5"));
            Assert.That(otherNode.RawAttributes["height"], Is.EqualTo("6"));

            Assert.That(result.UpdatedSourceText, Does.Contain("d=\"M 2 2 L 3 3 Z\""));
            Assert.That(result.UpdatedSourceText, Does.Contain("fill=\"red\""));
            Assert.That(result.UpdatedSourceText, Does.Contain("stroke=\"blue\""));
            Assert.That(result.UpdatedSourceText, Does.Contain("rect"));
        }

        [Test]
        public void TryApplyPathData_ConvertsDisplayTagToPath_AfterEditingSanitizedRectPath()
        {
            const string source = "<svg><path id=\"shape\" d=\"M 0 0 L 1 1\" fill=\"red\" /></svg>";

            SvgLoader loader = new();
            Assert.That(loader.TryLoad(source, out SvgDocumentModel documentModel, out string loadError), Is.True, loadError);
            Assert.That(documentModel.TryGetNodeByXmlId("shape", out SvgNodeModel shapeNode), Is.True);

            shapeNode.DisplayTagName = SvgTagName.RECT;

            PathData updatedPathData = PathDataParser.Parse("M 2 2 L 3 3 Z");
            PathMutationService service = new();

            bool ok = service.TryApplyPathData(documentModel, "shape", updatedPathData, out MutationResult result);

            Assert.That(ok, Is.True, result.Error);
            Assert.That(result.UpdatedDocumentModel.TryGetNodeByXmlId("shape", out SvgNodeModel updatedShapeNode), Is.True);
            Assert.That(updatedShapeNode.TagName, Is.EqualTo(SvgTagName.PATH));
            Assert.That(updatedShapeNode.DisplayTagName, Is.EqualTo(SvgTagName.PATH));
        }

        [Test]
        [Timeout(1000)]
        public void Parse_SetsMalformedState_WhenTokensFollowCloseCommand()
        {
            PathData pathData = PathDataParser.Parse("M 0 0 Z 1 2");

            Assert.That(pathData.IsMalformed, Is.True);
            Assert.That(pathData.ParseError, Does.Contain("Unexpected"));
            Assert.That(pathData.HasUnsupportedCommands, Is.False);
        }

        [Test]
        [Timeout(1000)]
        public void Parse_SetsMalformedState_WhenSupportedCommandIsTruncated()
        {
            PathData pathData = PathDataParser.Parse("M 0 0 C 1 2 3 4");

            Assert.That(pathData.IsMalformed, Is.True);
            Assert.That(pathData.ParseError, Does.Contain("C"));
            Assert.That(pathData.HasUnsupportedCommands, Is.False);
        }

        [Test]
        public void TrySerialize_Fails_WhenPathDataContainsUnsupportedCommands()
        {
            PathData pathData = new();
            pathData.AddUnsupportedCommand('A');

            bool ok = PathDataSerializer.TrySerialize(pathData, out string _, out string error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("unsupported"));
        }

        [Test]
        public void TrySerialize_Fails_WhenPathDataIsMalformed()
        {
            PathData pathData = PathDataParser.Parse("M 0 0 L");

            bool ok = PathDataSerializer.TrySerialize(pathData, out string _, out string error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("malformed"));
        }

        [Test]
        public void TryApplyPathData_RejectsMalformedPathData()
        {
            const string source = "<svg><path id=\"shape\" d=\"M 0 0 L 1 1\" /></svg>";

            SvgLoader loader = new();
            Assert.That(loader.TryLoad(source, out SvgDocumentModel documentModel, out string loadError), Is.True, loadError);

            PathData pathData = PathDataParser.Parse("M 0 0 Z 1 2");
            PathMutationService service = new();

            bool ok = service.TryApplyPathData(documentModel, "shape", pathData, out MutationResult result);

            Assert.That(ok, Is.False);
            Assert.That(result.Error, Does.Contain("malformed"));
        }

        [Test]
        public void Serialize_PreservesSixDecimalPrecision()
        {
            PathData pathData = new();
            pathData.Subpaths.Add(new PathSubpath(
                new Vector2(0.123456f, 1.234567f),
                new[] { new PathNode('L', new Vector2(2.345678f, 3.456789f)) }));

            string pathText = PathDataSerializer.Serialize(pathData);

            Assert.That(pathText, Is.EqualTo("M 0.123456 1.234567 L 2.345678 3.456789"));
        }
    }
}
