using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Preview.Geometry;

namespace SvgEditor.Tests.Editor
{
    public sealed class PreviewGeometryRefactorTests
    {
        [Test]
        public void BuildElementBounds_ReturnsEmpty_WhenSceneInfoIsMissing()
        {
            IReadOnlyList<PreviewElementGeometry> elements = SnapshotGeometryBuilder.BuildElementBounds(
                default,
                new Dictionary<string, (string Key, string TargetKey)>());

            Assert.That(elements, Is.Empty);
        }

        [Test]
        public void BuildHitTriangles_AccumulatesBoundsAcrossDescendants()
        {
            SceneNode root = new()
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = Matrix2D.identity
            };

            SceneNode childA = new()
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = Matrix2D.identity
            };

            SceneNode childB = new()
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = Matrix2D.identity
            };

            root.Children.Add(childA);
            root.Children.Add(childB);

            var worldGeometryByNode = new Dictionary<SceneNode, TessellatedNodeGeometry>
            {
                [childA] = new(
                    new[]
                    {
                        new[]
                        {
                            new Vector2(0f, 0f),
                            new Vector2(2f, 0f),
                            new Vector2(0f, 1f)
                        }
                    },
                    Rect.MinMaxRect(0f, 0f, 2f, 1f),
                    hasBounds: true),
                [childB] = new(
                    new[]
                    {
                        new[]
                        {
                            new Vector2(3f, 3f),
                            new Vector2(4f, 3f),
                            new Vector2(3f, 5f)
                        }
                    },
                    Rect.MinMaxRect(3f, 3f, 4f, 5f),
                    hasBounds: true)
            };

            IReadOnlyList<Vector2[]> triangles = GeometryWorldContextBuilder.BuildHitTriangles(
                root,
                worldGeometryByNode,
                out Rect bounds,
                out bool hasBounds);

            Assert.That(hasBounds, Is.True);
            Assert.That(triangles.Count, Is.EqualTo(2));
            Assert.That(bounds, Is.EqualTo(Rect.MinMaxRect(0f, 0f, 4f, 5f)));
        }

        [Test]
        public void SnapshotGeometryBuilder_DoesNotExposeVisualContentBoundsFacade()
        {
            MethodInfo visualContentBoundsFacade = typeof(SnapshotGeometryBuilder).GetMethod(
                "TryBuildVisualContentBounds",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            Assert.That(visualContentBoundsFacade, Is.Null);
        }
    }
}
