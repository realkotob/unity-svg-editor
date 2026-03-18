using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Preview.Geometry
{
    internal static class SnapshotGeometryBuilder
    {
        public static bool TryBuildVisualContentBounds(
            IReadOnlyList<PreviewElementGeometry> elements,
            out Rect visualContentBounds)
        {
            return GeometryBoundsUtility.TryBuildVisualContentBounds(elements, out visualContentBounds);
        }

        public static IReadOnlyList<PreviewElementGeometry> BuildElementBounds(
            SVGParser.SceneInfo sceneInfo,
            IReadOnlyDictionary<string, (string Key, string TargetKey)> keyByNodeId)
        {
            GeometryWorldContext context = GeometryWorldContextBuilder.Build(sceneInfo);
            List<(SceneNode Node, string Key, string TargetKey)> mappedNodes = new();

            foreach (var pair in sceneInfo.NodeIDs)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) ||
                    pair.Value == null ||
                    !keyByNodeId.TryGetValue(pair.Key, out var mapping))
                {
                    continue;
                }

                mappedNodes.Add((pair.Value, mapping.Key, mapping.TargetKey));
            }

            return BuildElementBounds(mappedNodes, context);
        }

        public static IReadOnlyList<PreviewElementGeometry> BuildElementBounds(
            Scene scene,
            IReadOnlyDictionary<SceneNode, (string Key, string TargetKey)> keyByNode,
            Dictionary<SceneNode, float> nodeOpacity)
        {
            if (scene?.Root == null || keyByNode == null || keyByNode.Count == 0)
                return Array.Empty<PreviewElementGeometry>();

            GeometryWorldContext context = GeometryWorldContextBuilder.Build(scene, nodeOpacity);
            List<(SceneNode Node, string Key, string TargetKey)> mappedNodes = new(keyByNode.Count);

            foreach (KeyValuePair<SceneNode, (string Key, string TargetKey)> pair in keyByNode)
            {
                if (pair.Key == null)
                    continue;

                mappedNodes.Add((pair.Key, pair.Value.Key, pair.Value.TargetKey));
            }

            return BuildElementBounds(mappedNodes, context);
        }

        public static bool TryBuildOverlayGeometry(
            SceneNode overlayRoot,
            Dictionary<SceneNode, float> nodeOpacity,
            out Rect visualBounds,
            out IReadOnlyList<Vector2[]> hitGeometry)
        {
            visualBounds = default;
            hitGeometry = Array.Empty<Vector2[]>();
            if (overlayRoot == null)
                return false;

            Scene scene = new()
            {
                Root = overlayRoot
            };

            GeometryWorldContext context = GeometryWorldContextBuilder.Build(
                scene,
                nodeOpacity ?? new Dictionary<SceneNode, float>());
            hitGeometry = GeometryWorldContextBuilder.BuildHitTriangles(
                overlayRoot,
                context.WorldGeometryByNode,
                out visualBounds,
                out bool hasBounds);
            if (!hasBounds)
            {
                hasBounds = GeometryBoundsUtility.TryBuildFallbackBounds(
                    overlayRoot,
                    context.WorldTransformByNode,
                    out visualBounds);
            }

            return hasBounds || hitGeometry.Count > 0;
        }

        public static bool TryBuildSceneRootBounds(SVGParser.SceneInfo sceneInfo, out Rect worldBounds)
        {
            worldBounds = default;
            if (sceneInfo.Scene?.Root == null)
                return false;

            GeometryWorldContext context = GeometryWorldContextBuilder.Build(sceneInfo);
            return GeometryBoundsUtility.TryBuildFallbackBounds(
                sceneInfo.Scene.Root,
                context.WorldTransformByNode,
                out worldBounds);
        }

        public static bool TryBuildSceneRootBounds(
            Scene scene,
            Dictionary<SceneNode, float> nodeOpacity,
            out Rect worldBounds)
        {
            worldBounds = default;
            if (scene?.Root == null)
                return false;

            GeometryWorldContext context = GeometryWorldContextBuilder.Build(scene, nodeOpacity);
            return GeometryBoundsUtility.TryBuildFallbackBounds(
                scene.Root,
                context.WorldTransformByNode,
                out worldBounds);
        }

        private static IReadOnlyList<PreviewElementGeometry> BuildElementBounds(
            IReadOnlyList<(SceneNode Node, string Key, string TargetKey)> mappedNodes,
            GeometryWorldContext context)
        {
            List<PreviewElementGeometry> elements = new();

            for (int index = 0; index < mappedNodes.Count; index++)
            {
                var mappedNode = mappedNodes[index];
                if (mappedNode.Node == null)
                    continue;

                elements.Add(BuildElementGeometry(mappedNode.Node, mappedNode.Key, mappedNode.TargetKey, context));
            }

            elements.Sort(static (left, right) => left.DrawOrder.CompareTo(right.DrawOrder));
            return elements;
        }

        private static PreviewElementGeometry BuildElementGeometry(
            SceneNode node,
            string key,
            string targetKey,
            GeometryWorldContext context)
        {
            IReadOnlyList<Vector2[]> hitGeometry = GeometryWorldContextBuilder.BuildHitTriangles(
                node,
                context.WorldGeometryByNode,
                out Rect visualBounds,
                out bool hasExactBounds);
            if (!hasExactBounds)
            {
                hasExactBounds = GeometryBoundsUtility.TryBuildFallbackBounds(
                    node,
                    context.WorldTransformByNode,
                    out visualBounds);
            }

            Matrix2D resolvedWorldTransform = context.WorldTransformByNode.TryGetValue(node, out Matrix2D worldTransform)
                ? worldTransform
                : node.Transform;
            Matrix2D parentWorldTransform = resolvedWorldTransform * node.Transform.Inverse();
            GeometryBoundsUtility.ResolveRotationPivot(
                node,
                resolvedWorldTransform,
                visualBounds,
                out Vector2 rotationPivotWorld,
                out Vector2 rotationPivotParentSpace);

            return new PreviewElementGeometry
            {
                Key = key,
                TargetKey = targetKey,
                VisualBounds = visualBounds,
                DrawOrder = context.DrawOrderByNode.TryGetValue(node, out int order) ? order : -1,
                HitGeometry = hitGeometry,
                BoundsQuality = hasExactBounds
                    ? BoundsQuality.Exact
                    : (visualBounds.width > Mathf.Epsilon || visualBounds.height > Mathf.Epsilon
                        ? BoundsQuality.Fallback
                        : BoundsQuality.Unknown),
                WorldTransform = resolvedWorldTransform,
                ParentWorldTransform = parentWorldTransform,
                RotationPivotWorld = rotationPivotWorld,
                RotationPivotParentSpace = rotationPivotParentSpace
            };
        }
    }
}
