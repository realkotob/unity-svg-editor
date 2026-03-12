using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

using SvgEditor;
using SvgEditor.Preview.Build;

namespace SvgEditor.Preview.Geometry
{
    internal static class GeometryWorldContextBuilder
    {
        public static GeometryWorldContext Build(SVGParser.SceneInfo sceneInfo)
        {
            Dictionary<SceneNode, int> drawOrderByNode = BuildDrawOrderLookup(sceneInfo.Scene.Root);
            Dictionary<SceneNode, Matrix2D> worldTransformByNode = BuildWorldTransformLookup(sceneInfo);
            Dictionary<SceneNode, TessellatedNodeGeometry> worldGeometryByNode = BuildWorldGeometryLookup(
                sceneInfo.Scene.Root,
                worldTransformByNode);
            return new GeometryWorldContext(drawOrderByNode, worldTransformByNode, worldGeometryByNode);
        }

        public static GeometryWorldContext Build(Scene scene, Dictionary<SceneNode, float> nodeOpacity)
        {
            Dictionary<SceneNode, int> drawOrderByNode = BuildDrawOrderLookup(scene.Root);
            Dictionary<SceneNode, Matrix2D> worldTransformByNode = BuildWorldTransformLookup(scene, nodeOpacity);
            Dictionary<SceneNode, TessellatedNodeGeometry> worldGeometryByNode = BuildWorldGeometryLookup(
                scene.Root,
                worldTransformByNode);
            return new GeometryWorldContext(drawOrderByNode, worldTransformByNode, worldGeometryByNode);
        }

        public static IReadOnlyList<Vector2[]> BuildHitTriangles(
            SceneNode node,
            IReadOnlyDictionary<SceneNode, TessellatedNodeGeometry> worldGeometryByNode,
            out Rect bounds,
            out bool hasBounds)
        {
            bounds = default;
            hasBounds = false;
            if (node == null)
                return System.Array.Empty<Vector2[]>();

            List<Vector2[]> triangles = null;

            foreach (SceneNode descendant in VectorUtils.SceneNodes(node))
            {
                if (!worldGeometryByNode.TryGetValue(descendant, out TessellatedNodeGeometry geometry) ||
                    geometry.Triangles.Count == 0)
                {
                    continue;
                }

                triangles ??= new List<Vector2[]>();
                AddTriangles(triangles, geometry.Triangles);
                if (geometry.HasBounds)
                {
                    GeometryBoundsUtility.AccumulateBounds(ref hasBounds, ref bounds, geometry.Bounds);
                }
            }

            return triangles ?? (IReadOnlyList<Vector2[]>)System.Array.Empty<Vector2[]>();
        }

        private static Dictionary<SceneNode, int> BuildDrawOrderLookup(SceneNode root)
        {
            Dictionary<SceneNode, int> lookup = new();
            int drawOrder = 0;
            foreach (SceneNode node in VectorUtils.SceneNodes(root))
            {
                if (node == null)
                    continue;

                lookup[node] = drawOrder++;
            }

            return lookup;
        }

        private static Dictionary<SceneNode, Matrix2D> BuildWorldTransformLookup(SVGParser.SceneInfo sceneInfo)
        {
            Dictionary<SceneNode, Matrix2D> lookup = new();
            foreach (var item in VectorUtils.WorldTransformedSceneNodes(sceneInfo.Scene.Root, sceneInfo.NodeOpacity))
            {
                if (item.Node == null)
                    continue;

                lookup[item.Node] = item.WorldTransform;
            }

            return lookup;
        }

        private static Dictionary<SceneNode, Matrix2D> BuildWorldTransformLookup(
            Scene scene,
            Dictionary<SceneNode, float> nodeOpacity)
        {
            Dictionary<SceneNode, Matrix2D> lookup = new();
            foreach (var item in VectorUtils.WorldTransformedSceneNodes(scene.Root, nodeOpacity))
            {
                if (item.Node == null)
                    continue;

                lookup[item.Node] = item.WorldTransform;
            }

            return lookup;
        }

        private static Dictionary<SceneNode, TessellatedNodeGeometry> BuildWorldGeometryLookup(
            SceneNode root,
            IReadOnlyDictionary<SceneNode, Matrix2D> worldTransformByNode)
        {
            Dictionary<SceneNode, TessellatedNodeGeometry> lookup = new();
            foreach (SceneNode node in VectorUtils.SceneNodes(root))
            {
                if (node?.Shapes == null || node.Shapes.Count == 0)
                    continue;

                IReadOnlyList<Vector2[]> triangles = TessellateNodeToWorldTriangles(node, worldTransformByNode);
                bool hasBounds = GeometryBoundsUtility.TryBuildTriangleBounds(triangles, out Rect bounds);
                lookup[node] = new TessellatedNodeGeometry(triangles, bounds, hasBounds);
            }

            return lookup;
        }

        private static IReadOnlyList<Vector2[]> TessellateNodeToWorldTriangles(
            SceneNode node,
            IReadOnlyDictionary<SceneNode, Matrix2D> worldTransformByNode)
        {
            if (node == null || node.Shapes == null || node.Shapes.Count == 0)
                return System.Array.Empty<Vector2[]>();

            if (!worldTransformByNode.TryGetValue(node, out Matrix2D worldTransform))
                worldTransform = node.Transform;

            var tempNode = new SceneNode
            {
                Shapes = node.Shapes,
                Transform = Matrix2D.identity
            };

            var tempScene = new Scene
            {
                Root = tempNode
            };

            List<Vector2[]> triangles = new();
            IEnumerable<VectorUtils.Geometry> geometries = VectorUtils.TessellateScene(
                tempScene,
                PreviewBuildOptions.CreateTessellationOptions());

            foreach (var geometry in geometries)
            {
                if (geometry?.Vertices == null || geometry.Indices == null)
                    continue;

                for (int index = 0; index + 2 < geometry.Indices.Length; index += 3)
                {
                    Vector2 a = worldTransform.MultiplyPoint(geometry.Vertices[geometry.Indices[index]]);
                    Vector2 b = worldTransform.MultiplyPoint(geometry.Vertices[geometry.Indices[index + 1]]);
                    Vector2 c = worldTransform.MultiplyPoint(geometry.Vertices[geometry.Indices[index + 2]]);
                    triangles.Add(new[] { a, b, c });
                }
            }

            return triangles;
        }

        private static void AddTriangles(List<Vector2[]> destination, IReadOnlyList<Vector2[]> source)
        {
            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }
    }
}
