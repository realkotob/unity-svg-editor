using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal static class PreviewSnapshotGeometryBuilder
    {
        private readonly struct TessellatedNodeGeometry
        {
            public TessellatedNodeGeometry(IReadOnlyList<Vector2[]> triangles, Rect bounds, bool hasBounds)
            {
                Triangles = triangles ?? Array.Empty<Vector2[]>();
                Bounds = bounds;
                HasBounds = hasBounds;
            }

            public IReadOnlyList<Vector2[]> Triangles { get; }
            public Rect Bounds { get; }
            public bool HasBounds { get; }
        }

        public static bool TryBuildVisualContentBounds(
            IReadOnlyList<PreviewElementGeometry> elements,
            out Rect visualContentBounds)
        {
            visualContentBounds = default;
            if (elements == null || elements.Count == 0)
                return false;

            var hasBounds = false;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            foreach (PreviewElementGeometry element in elements)
            {
                if (element == null || element.BoundsQuality == BoundsQuality.Unknown)
                    continue;

                Rect bounds = element.VisualBounds;
                min = Vector2.Min(min, bounds.min);
                max = Vector2.Max(max, bounds.max);
                hasBounds = true;
            }

            if (!hasBounds)
                return false;

            visualContentBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        public static IReadOnlyList<PreviewElementGeometry> BuildElementBounds(
            SVGParser.SceneInfo sceneInfo,
            IReadOnlyDictionary<string, (string Key, string TargetKey)> keyByNodeId)
        {
            var drawOrderByNode = BuildDrawOrderLookup(sceneInfo.Scene.Root);
            var worldTransformByNode = BuildWorldTransformLookup(sceneInfo);
            var worldGeometryByNode = BuildWorldGeometryLookup(sceneInfo.Scene.Root, worldTransformByNode);
            var elements = new List<PreviewElementGeometry>();

            foreach (var pair in sceneInfo.NodeIDs)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) ||
                    pair.Value == null ||
                    !keyByNodeId.TryGetValue(pair.Key, out var mapping))
                {
                    continue;
                }

                IReadOnlyList<Vector2[]> hitGeometry = BuildHitTriangles(
                    pair.Value,
                    worldGeometryByNode,
                    out Rect visualBounds,
                    out bool hasExactBounds);
                if (!hasExactBounds)
                    hasExactBounds = TryBuildFallbackBounds(pair.Value, worldTransformByNode, out visualBounds);

                Matrix2D resolvedWorldTransform = worldTransformByNode.TryGetValue(pair.Value, out Matrix2D wt)
                    ? wt
                    : pair.Value.Transform;
                // parentWorldTransform = worldTransform * inverse(node.Transform)
                // This is the transform from SVG parent coordinate space to world space,
                // needed to convert world-space deltas/pivots back to SVG parent space for transform attributes.
                Matrix2D parentWorldTransform = resolvedWorldTransform * pair.Value.Transform.Inverse();

                elements.Add(new PreviewElementGeometry
                {
                    Key = mapping.Key,
                    TargetKey = mapping.TargetKey,
                    VisualBounds = visualBounds,
                    DrawOrder = drawOrderByNode.TryGetValue(pair.Value, out int order) ? order : -1,
                    HitGeometry = hitGeometry,
                    BoundsQuality = hasExactBounds
                        ? BoundsQuality.Exact
                        : (visualBounds.width > Mathf.Epsilon || visualBounds.height > Mathf.Epsilon
                            ? BoundsQuality.Fallback
                            : BoundsQuality.Unknown),
                    WorldTransform = resolvedWorldTransform,
                    ParentWorldTransform = parentWorldTransform
                });
            }

            elements.Sort(static (left, right) => left.DrawOrder.CompareTo(right.DrawOrder));
            return elements;
        }

        public static bool TryBuildSceneRootBounds(SVGParser.SceneInfo sceneInfo, out Rect worldBounds)
        {
            worldBounds = default;
            if (sceneInfo.Scene?.Root == null)
                return false;

            var worldTransformByNode = BuildWorldTransformLookup(sceneInfo);
            return TryBuildFallbackBounds(sceneInfo.Scene.Root, worldTransformByNode, out worldBounds);
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

        private static Dictionary<SceneNode, TessellatedNodeGeometry> BuildWorldGeometryLookup(
            SceneNode root,
            IReadOnlyDictionary<SceneNode, Matrix2D> worldTransformByNode)
        {
            var lookup = new Dictionary<SceneNode, TessellatedNodeGeometry>();
            foreach (SceneNode node in VectorUtils.SceneNodes(root))
            {
                if (node?.Shapes == null || node.Shapes.Count == 0)
                    continue;

                IReadOnlyList<Vector2[]> triangles = TessellateNodeToWorldTriangles(node, worldTransformByNode);
                bool hasBounds = TryBuildTriangleBounds(triangles, out Rect bounds);
                lookup[node] = new TessellatedNodeGeometry(triangles, bounds, hasBounds);
            }

            return lookup;
        }

        private static IReadOnlyList<Vector2[]> BuildHitTriangles(
            SceneNode node,
            IReadOnlyDictionary<SceneNode, TessellatedNodeGeometry> worldGeometryByNode,
            out Rect bounds,
            out bool hasBounds)
        {
            bounds = default;
            hasBounds = false;
            if (node == null)
                return Array.Empty<Vector2[]>();

            List<Vector2[]> triangles = null;

            foreach (SceneNode descendant in VectorUtils.SceneNodes(node))
            {
                if (!worldGeometryByNode.TryGetValue(descendant, out TessellatedNodeGeometry geometry) ||
                    geometry.Triangles.Count == 0)
                    continue;

                triangles ??= new List<Vector2[]>();
                AddTriangles(triangles, geometry.Triangles);
                if (geometry.HasBounds)
                {
                    AccumulateBounds(ref hasBounds, ref bounds, geometry.Bounds);
                }
            }

            return triangles ?? (IReadOnlyList<Vector2[]>)Array.Empty<Vector2[]>();
        }

        private static IReadOnlyList<Vector2[]> TessellateNodeToWorldTriangles(
            SceneNode node,
            IReadOnlyDictionary<SceneNode, Matrix2D> worldTransformByNode)
        {
            if (node == null || node.Shapes == null || node.Shapes.Count == 0)
                return Array.Empty<Vector2[]>();

            if (!worldTransformByNode.TryGetValue(node, out Matrix2D worldTransform))
                worldTransform = node.Transform;

            // Tessellate each scene node only once, then reuse the world-space triangles
            // across all ancestor element hit-geometry aggregations.
            var tempNode = new SceneNode
            {
                Shapes = node.Shapes,
                Transform = Matrix2D.identity
            };

            var tempScene = new Scene
            {
                Root = tempNode
            };

            var triangles = new List<Vector2[]>();
            IEnumerable<VectorUtils.Geometry> geometries = VectorUtils.TessellateScene(
                tempScene,
                PreviewBuildOptions.CreateTessellationOptions());

            foreach (var geometry in geometries)
            {
                if (geometry?.Vertices == null || geometry.Indices == null)
                    continue;

                for (var i = 0; i + 2 < geometry.Indices.Length; i += 3)
                {
                    var a = worldTransform.MultiplyPoint(geometry.Vertices[geometry.Indices[i]]);
                    var b = worldTransform.MultiplyPoint(geometry.Vertices[geometry.Indices[i + 1]]);
                    var c = worldTransform.MultiplyPoint(geometry.Vertices[geometry.Indices[i + 2]]);
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

        private static void AccumulateBounds(ref bool hasBounds, ref Rect accumulatedBounds, Rect bounds)
        {
            if (!hasBounds)
            {
                accumulatedBounds = bounds;
                hasBounds = true;
                return;
            }

            accumulatedBounds = Rect.MinMaxRect(
                Mathf.Min(accumulatedBounds.xMin, bounds.xMin),
                Mathf.Min(accumulatedBounds.yMin, bounds.yMin),
                Mathf.Max(accumulatedBounds.xMax, bounds.xMax),
                Mathf.Max(accumulatedBounds.yMax, bounds.yMax));
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

        private static bool TryBuildFallbackBounds(
            SceneNode node,
            IReadOnlyDictionary<SceneNode, Matrix2D> worldTransformByNode,
            out Rect bounds)
        {
            bounds = default;
            if (node == null)
                return false;

            Rect localBounds = VectorUtils.SceneNodeBounds(node);
            if (localBounds.width <= Mathf.Epsilon && localBounds.height <= Mathf.Epsilon)
                return false;

            Matrix2D worldTransform = worldTransformByNode.TryGetValue(node, out Matrix2D resolvedTransform)
                ? resolvedTransform
                : node.Transform;

            Vector2 topLeft = worldTransform.MultiplyPoint(new Vector2(localBounds.xMin, localBounds.yMin));
            Vector2 topRight = worldTransform.MultiplyPoint(new Vector2(localBounds.xMax, localBounds.yMin));
            Vector2 bottomRight = worldTransform.MultiplyPoint(new Vector2(localBounds.xMax, localBounds.yMax));
            Vector2 bottomLeft = worldTransform.MultiplyPoint(new Vector2(localBounds.xMin, localBounds.yMax));

            bounds = Rect.MinMaxRect(
                Mathf.Min(topLeft.x, topRight.x, bottomRight.x, bottomLeft.x),
                Mathf.Min(topLeft.y, topRight.y, bottomRight.y, bottomLeft.y),
                Mathf.Max(topLeft.x, topRight.x, bottomRight.x, bottomLeft.x),
                Mathf.Max(topLeft.y, topRight.y, bottomRight.y, bottomLeft.y));
            return true;
        }
    }
}
