using System.Collections.Generic;
using NUnit.Framework;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Canvas;
using Unity.VectorGraphics;
using UnityEngine;

namespace SvgEditor.Editor.Tests.PathEditing
{
    public sealed class PrimitivePathEditingTests
    {
        [Test]
        public void TryEnter_WhenPolylineElement_EntersPathEditWithLinearSubpath()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><polyline id=\"poly\" points=\"0,0 10,0 10,10\" /></svg>";
            DocumentSession documentSession = CreateDocumentSession(svg);
            SvgNodeModel node = GetNodeByXmlId(documentSession.DocumentModel, "poly");
            var controller = new PathEditEntryController(new ToolController(), new OverlayController());

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: documentSession,
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session, Is.Not.Null);
            Assert.That(result.Session.PathData.Subpaths, Has.Count.EqualTo(1));
            Assert.That(result.Session.PathData.Subpaths[0].Start, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes, Has.Count.EqualTo(2));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes[0].Command, Is.EqualTo('L'));
            Assert.That(result.Session.PathData.Subpaths[0].IsClosed, Is.False);
        }

        [Test]
        public void TryEnter_WhenPolygonElement_EntersPathEditWithClosedSubpath()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><polygon id=\"poly\" points=\"0,0 10,0 10,10 0,10\" /></svg>";
            DocumentSession documentSession = CreateDocumentSession(svg);
            SvgNodeModel node = GetNodeByXmlId(documentSession.DocumentModel, "poly");
            var controller = new PathEditEntryController(new ToolController(), new OverlayController());

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: documentSession,
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session, Is.Not.Null);
            Assert.That(result.Session.PathData.Subpaths, Has.Count.EqualTo(1));
            Assert.That(result.Session.PathData.Subpaths[0].IsClosed, Is.True);
            Assert.That(result.Session.PathData.Subpaths[0].Nodes, Has.Count.EqualTo(3));
        }

        [Test]
        public void TryEnter_WhenLineElement_EntersPathEditWithSingleSegment()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><line id=\"line\" x1=\"1\" y1=\"2\" x2=\"11\" y2=\"12\" /></svg>";
            DocumentSession documentSession = CreateDocumentSession(svg);
            SvgNodeModel node = GetNodeByXmlId(documentSession.DocumentModel, "line");
            var controller = new PathEditEntryController(new ToolController(), new OverlayController());

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: documentSession,
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session.PathData.Subpaths[0].Start, Is.EqualTo(new Vector2(1f, 2f)));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes, Has.Count.EqualTo(1));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes[0].Position, Is.EqualTo(new Vector2(11f, 12f)));
            Assert.That(result.Session.PathData.Subpaths[0].IsClosed, Is.False);
        }

        [Test]
        public void TryEnter_WhenPlainRectElement_EntersPathEditWithClosedRectSubpath()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"rect\" x=\"2\" y=\"3\" width=\"8\" height=\"5\" /></svg>";
            DocumentSession documentSession = CreateDocumentSession(svg);
            SvgNodeModel node = GetNodeByXmlId(documentSession.DocumentModel, "rect");
            var controller = new PathEditEntryController(new ToolController(), new OverlayController());

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: documentSession,
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session.PathData.Subpaths[0].Start, Is.EqualTo(new Vector2(2f, 3f)));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes, Has.Count.EqualTo(3));
            Assert.That(result.Session.PathData.Subpaths[0].IsClosed, Is.True);
        }

        [Test]
        public void TryEnter_WhenCircleElement_EntersPathEditWithCubicClosedSubpath()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><circle id=\"circle\" cx=\"8\" cy=\"9\" r=\"4\" /></svg>";
            DocumentSession documentSession = CreateDocumentSession(svg);
            SvgNodeModel node = GetNodeByXmlId(documentSession.DocumentModel, "circle");
            var controller = new PathEditEntryController(new ToolController(), new OverlayController());

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: documentSession,
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes, Has.Count.EqualTo(4));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes[0].Command, Is.EqualTo('C'));
            Assert.That(result.Session.PathData.Subpaths[0].IsClosed, Is.True);
        }

        [Test]
        public void TryEnter_WhenCircleElement_ExposesFourLogicalAnchorsInPathEditSession()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><circle id=\"circle\" cx=\"8\" cy=\"9\" r=\"4\" /></svg>";
            DocumentSession documentSession = CreateDocumentSession(svg);
            SvgNodeModel node = GetNodeByXmlId(documentSession.DocumentModel, "circle");
            var controller = new PathEditEntryController(new ToolController(), new OverlayController());

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: documentSession,
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session.Subpaths[0].Nodes, Has.Count.EqualTo(4));
            Assert.That(result.Session.Subpaths[0].Nodes[0].HasInHandle, Is.True);
            Assert.That(result.Session.Subpaths[0].Nodes[0].HasOutHandle, Is.True);
        }

        [Test]
        public void TryEnter_WhenEllipseElement_EntersPathEditWithCubicClosedSubpath()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><ellipse id=\"ellipse\" cx=\"8\" cy=\"9\" rx=\"6\" ry=\"4\" /></svg>";
            DocumentSession documentSession = CreateDocumentSession(svg);
            SvgNodeModel node = GetNodeByXmlId(documentSession.DocumentModel, "ellipse");
            var controller = new PathEditEntryController(new ToolController(), new OverlayController());

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: documentSession,
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes, Has.Count.EqualTo(4));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes[0].Command, Is.EqualTo('C'));
            Assert.That(result.Session.PathData.Subpaths[0].IsClosed, Is.True);
        }

        [Test]
        public void TryEnter_WhenRoundedRectElement_EntersPathEditWithMixedLinearAndCubicSegments()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"rect\" x=\"2\" y=\"3\" width=\"8\" height=\"5\" rx=\"2\" ry=\"2\" /></svg>";
            DocumentSession documentSession = CreateDocumentSession(svg);
            SvgNodeModel node = GetNodeByXmlId(documentSession.DocumentModel, "rect");
            var controller = new PathEditEntryController(new ToolController(), new OverlayController());

            PathEditEntryResult result = controller.TryEnter(new PathEditEntryRequest(
                clickCount: 2,
                currentDocument: documentSession,
                elementKey: node.LegacyElementKey,
                worldTransform: Matrix2D.identity,
                sceneToViewportPoint: point => point));

            Assert.That(result.Kind, Is.EqualTo(PathEditEntryResultKind.Entered));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes.Count, Is.GreaterThan(4));
            Assert.That(result.Session.PathData.Subpaths[0].Nodes.Exists(nodeEntry => nodeEntry.Command == 'C'), Is.True);
            Assert.That(result.Session.PathData.Subpaths[0].IsClosed, Is.True);
        }

        [Test]
        public void TryApplyPathData_WhenPolylineRemainsLinear_PreservesPolylinePoints()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><polyline id=\"poly\" points=\"0,0 10,0 10,10\" /></svg>";
            SvgDocumentModel documentModel = LoadDocument(svg);
            SvgNodeModel node = GetNodeByXmlId(documentModel, "poly");
            var service = new PathMutationService();

            bool applied = service.TryApplyPathData(
                documentModel,
                node.LegacyTargetKey,
                CreateLinearPathData(new Vector2(0f, 0f), new[] { new Vector2(12f, 0f), new Vector2(12f, 8f) }, isClosed: false),
                out MutationResult mutation);

            Assert.That(applied, Is.True, mutation.Error);
            SvgNodeModel updatedNode = GetNodeByXmlId(mutation.UpdatedDocumentModel, "poly");
            Assert.That(updatedNode.TagName, Is.EqualTo(SvgTagName.POLYLINE));
            Assert.That(updatedNode.RawAttributes.ContainsKey(SvgAttributeName.D), Is.False);
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.POINTS], Is.EqualTo("0 0 12 0 12 8"));
        }

        [Test]
        public void TryApplyPathData_WhenPolygonRemainsLinear_PreservesPolygonPoints()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><polygon id=\"poly\" points=\"0,0 10,0 10,10 0,10\" /></svg>";
            SvgDocumentModel documentModel = LoadDocument(svg);
            SvgNodeModel node = GetNodeByXmlId(documentModel, "poly");
            var service = new PathMutationService();

            bool applied = service.TryApplyPathData(
                documentModel,
                node.LegacyTargetKey,
                CreateLinearPathData(new Vector2(0f, 0f), new[] { new Vector2(10f, 0f), new Vector2(11f, 9f), new Vector2(0f, 10f) }, isClosed: true),
                out MutationResult mutation);

            Assert.That(applied, Is.True, mutation.Error);
            SvgNodeModel updatedNode = GetNodeByXmlId(mutation.UpdatedDocumentModel, "poly");
            Assert.That(updatedNode.TagName, Is.EqualTo(SvgTagName.POLYGON));
            Assert.That(updatedNode.RawAttributes.ContainsKey(SvgAttributeName.D), Is.False);
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.POINTS], Is.EqualTo("0 0 10 0 11 9 0 10"));
        }

        [Test]
        public void TryApplyPathData_WhenPolylineBecomesCurved_PromotesElementToPath()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><polyline id=\"poly\" points=\"0,0 10,0 10,10\" /></svg>";
            SvgDocumentModel documentModel = LoadDocument(svg);
            SvgNodeModel node = GetNodeByXmlId(documentModel, "poly");
            var service = new PathMutationService();

            var pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(
                new Vector2(0f, 0f),
                new[]
                {
                    new PathNode('C', new Vector2(10f, 10f), new Vector2(2f, 4f), new Vector2(8f, 6f), PathHandleMode.Free)
                }));

            bool applied = service.TryApplyPathData(documentModel, node.LegacyTargetKey, pathData, out MutationResult mutation);

            Assert.That(applied, Is.True, mutation.Error);
            SvgNodeModel updatedNode = GetNodeByXmlId(mutation.UpdatedDocumentModel, "poly");
            Assert.That(updatedNode.TagName, Is.EqualTo(SvgTagName.PATH));
            Assert.That(updatedNode.RawAttributes.ContainsKey(SvgAttributeName.POINTS), Is.False);
            Assert.That(updatedNode.RawAttributes.ContainsKey(SvgAttributeName.D), Is.True);
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.D], Does.Contain("C"));
        }

        [Test]
        public void TryApplyPathData_WhenLineRemainsLinear_PreservesLineAttributes()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><line id=\"line\" x1=\"1\" y1=\"2\" x2=\"11\" y2=\"12\" /></svg>";
            SvgDocumentModel documentModel = LoadDocument(svg);
            SvgNodeModel node = GetNodeByXmlId(documentModel, "line");
            var service = new PathMutationService();

            bool applied = service.TryApplyPathData(
                documentModel,
                node.LegacyTargetKey,
                CreateLinearPathData(new Vector2(2f, 3f), new[] { new Vector2(9f, 12f) }, isClosed: false),
                out MutationResult mutation);

            Assert.That(applied, Is.True, mutation.Error);
            SvgNodeModel updatedNode = GetNodeByXmlId(mutation.UpdatedDocumentModel, "line");
            Assert.That(updatedNode.TagName, Is.EqualTo(SvgTagName.LINE));
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.X1], Is.EqualTo("2"));
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.Y1], Is.EqualTo("3"));
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.X2], Is.EqualTo("9"));
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.Y2], Is.EqualTo("12"));
            Assert.That(updatedNode.RawAttributes.ContainsKey(SvgAttributeName.D), Is.False);
        }

        [Test]
        public void TryApplyPathData_WhenRectRemainsAxisAligned_PreservesRectAttributes()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"rect\" x=\"2\" y=\"3\" width=\"8\" height=\"5\" /></svg>";
            SvgDocumentModel documentModel = LoadDocument(svg);
            SvgNodeModel node = GetNodeByXmlId(documentModel, "rect");
            var service = new PathMutationService();

            bool applied = service.TryApplyPathData(
                documentModel,
                node.LegacyTargetKey,
                CreateClosedLinearPathData(
                    new Vector2(4f, 5f),
                    new[] { new Vector2(14f, 5f), new Vector2(14f, 12f), new Vector2(4f, 12f) }),
                out MutationResult mutation);

            Assert.That(applied, Is.True, mutation.Error);
            SvgNodeModel updatedNode = GetNodeByXmlId(mutation.UpdatedDocumentModel, "rect");
            Assert.That(updatedNode.TagName, Is.EqualTo(SvgTagName.RECT));
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.X], Is.EqualTo("4"));
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.Y], Is.EqualTo("5"));
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.WIDTH], Is.EqualTo("10"));
            Assert.That(updatedNode.RawAttributes[SvgAttributeName.HEIGHT], Is.EqualTo("7"));
            Assert.That(updatedNode.RawAttributes.ContainsKey(SvgAttributeName.D), Is.False);
        }

        [Test]
        public void TryApplyPathData_WhenCircleIsEdited_PromotesElementToPath()
        {
            const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><circle id=\"circle\" cx=\"8\" cy=\"9\" r=\"4\" /></svg>";
            SvgDocumentModel documentModel = LoadDocument(svg);
            SvgNodeModel node = GetNodeByXmlId(documentModel, "circle");
            var service = new PathMutationService();

            var pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(
                new Vector2(8f, 5f),
                new[]
                {
                    new PathNode('C', new Vector2(12f, 9f), new Vector2(10f, 5f), new Vector2(12f, 7f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(8f, 13f), new Vector2(12f, 11f), new Vector2(10f, 13f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(4f, 9f), new Vector2(6f, 13f), new Vector2(4f, 11f), PathHandleMode.Free),
                    new PathNode('C', new Vector2(8f, 5f), new Vector2(4f, 7f), new Vector2(6f, 5f), PathHandleMode.Free)
                },
                isClosed: true));

            bool applied = service.TryApplyPathData(documentModel, node.LegacyTargetKey, pathData, out MutationResult mutation);

            Assert.That(applied, Is.True, mutation.Error);
            SvgNodeModel updatedNode = GetNodeByXmlId(mutation.UpdatedDocumentModel, "circle");
            Assert.That(updatedNode.TagName, Is.EqualTo(SvgTagName.PATH));
            Assert.That(updatedNode.RawAttributes.ContainsKey(SvgAttributeName.D), Is.True);
            Assert.That(updatedNode.RawAttributes.ContainsKey(SvgAttributeName.R), Is.False);
        }

        private static DocumentSession CreateDocumentSession(string sourceText)
        {
            SvgDocumentModel documentModel = LoadDocument(sourceText);
            return new DocumentSession
            {
                DocumentModel = documentModel,
                OriginalSourceText = sourceText,
                WorkingSourceText = sourceText
            };
        }

        private static SvgDocumentModel LoadDocument(string sourceText)
        {
            var loader = new SvgLoader();
            bool loaded = loader.TryLoad(sourceText, out SvgDocumentModel documentModel, out string error);
            Assert.That(loaded, Is.True, error);
            return documentModel;
        }

        private static SvgNodeModel GetNodeByXmlId(SvgDocumentModel documentModel, string xmlId)
        {
            bool found = documentModel.TryGetNodeByXmlId(xmlId, out SvgNodeModel node);
            Assert.That(found, Is.True);
            Assert.That(node, Is.Not.Null);
            return node;
        }

        private static PathData CreateLinearPathData(Vector2 start, IReadOnlyList<Vector2> points, bool isClosed)
        {
            var nodes = new List<PathNode>(points.Count);
            for (int index = 0; index < points.Count; index++)
            {
                nodes.Add(new PathNode('L', points[index]));
            }

            var pathData = new PathData();
            pathData.Subpaths.Add(new PathSubpath(start, nodes, isClosed));
            return pathData;
        }

        private static PathData CreateClosedLinearPathData(Vector2 start, IReadOnlyList<Vector2> points)
        {
            return CreateLinearPathData(start, points, isClosed: true);
        }
    }
}
