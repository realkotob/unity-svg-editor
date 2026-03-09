using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal static class PreviewSnapshotGeometryBuilder
    {
        public static IReadOnlyList<PreviewElementGeometry> BuildElementBounds(
            SVGParser.SceneInfo sceneInfo,
            IReadOnlyDictionary<string, (string Key, string TargetKey)> keyByNodeId)
        {
            var drawOrderByNode = BuildDrawOrderLookup(sceneInfo.Scene.Root);
            var worldTransformByNode = BuildWorldTransformLookup(sceneInfo);
            var elements = new List<PreviewElementGeometry>();

            foreach (var pair in sceneInfo.NodeIDs)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) ||
                    pair.Value == null ||
                    !keyByNodeId.TryGetValue(pair.Key, out var mapping))
                {
                    continue;
                }

                IReadOnlyList<Vector2[]> hitTriangles = BuildHitTriangles(pair.Value, worldTransformByNode);
                var sceneBounds = TryBuildTriangleBounds(hitTriangles, out Rect triangleBounds)
                    ? triangleBounds
                    : VectorUtils.SceneNodeBounds(pair.Value);
                elements.Add(new PreviewElementGeometry
                {
                    Key = mapping.Key,
                    TargetKey = mapping.TargetKey,
                    SceneBounds = sceneBounds,
                    DrawOrder = drawOrderByNode.TryGetValue(pair.Value, out int order) ? order : -1,
                    HitTriangles = hitTriangles
                });
            }

            return elements
                .OrderBy(item => item.DrawOrder)
                .ToList();
        }

        private static Dictionary<SceneNode, int> BuildDrawOrderLookup(SceneNode root)
        {
            var lookup = new Dictionary<SceneNode, int>();
            var drawOrder = 0;
            foreach (var node in VectorUtils.SceneNodes(root))
            {
                if (node == null)
                    continue;

                lookup[node] = drawOrder++;
            }

            return lookup;
        }

        private static Dictionary<SceneNode, Matrix2D> BuildWorldTransformLookup(SVGParser.SceneInfo sceneInfo)
        {
            var lookup = new Dictionary<SceneNode, Matrix2D>();
            foreach (var item in VectorUtils.WorldTransformedSceneNodes(sceneInfo.Scene.Root, sceneInfo.NodeOpacity))
            {
                if (item.Node == null)
                    continue;

                lookup[item.Node] = item.WorldTransform;
            }

            return lookup;
        }

        private static IReadOnlyList<Vector2[]> BuildHitTriangles(
            SceneNode node,
            IReadOnlyDictionary<SceneNode, Matrix2D> worldTransformByNode)
        {
            if (node == null)
                return Array.Empty<Vector2[]>();
            List<Vector2[]> triangles = new();

            foreach (SceneNode descendant in VectorUtils.SceneNodes(node))
            {
                if (descendant?.Shapes == null || descendant.Shapes.Count == 0)
                    continue;

                if (!worldTransformByNode.TryGetValue(descendant, out Matrix2D worldTransform))
                    worldTransform = descendant.Transform;

                var tempNode = new SceneNode
                {
                    Shapes = descendant.Shapes,
                    Transform = worldTransform
                };

                var tempScene = new Scene
                {
                    Root = tempNode
                };

                IEnumerable<VectorUtils.Geometry> geometries = VectorUtils.TessellateScene(
                    tempScene,
                    PreviewBuildOptions.CreateTessellationOptions());

                foreach (var geometry in geometries)
                {
                    if (geometry?.Vertices == null || geometry.Indices == null)
                        continue;

                    for (var i = 0; i + 2 < geometry.Indices.Length; i += 3)
                    {
                        var a = geometry.Vertices[geometry.Indices[i]];
                        var b = geometry.Vertices[geometry.Indices[i + 1]];
                        var c = geometry.Vertices[geometry.Indices[i + 2]];
                        triangles.Add(new[] { a, b, c });
                    }
                }
            }

            return triangles;
        }

        private static bool TryBuildTriangleBounds(IReadOnlyList<Vector2[]> triangles, out Rect bounds)
        {
            bounds = default;
            if (triangles == null || triangles.Count == 0)
                return false;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            foreach (Vector2[] triangle in triangles)
            {
                if (triangle == null || triangle.Length < 3)
                    continue;

                for (var i = 0; i < triangle.Length; i++)
                {
                    min = Vector2.Min(min, triangle[i]);
                    max = Vector2.Max(max, triangle[i]);
                }
            }

            if (min.x == float.MaxValue || min.y == float.MaxValue)
                return false;

            bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }
    }
}
