using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace SvgEditor.Core.Preview.Geometry
{
    internal static class SnapshotGeometryBuilder
    {
        #region Types
        private readonly struct MappedPreviewNode
        {
            public MappedPreviewNode(SceneNode node, string key, string targetKey)
            {
                Node = node;
                Key = key;
                TargetKey = targetKey;
            }

            public SceneNode Node { get; }
            public string Key { get; }
            public string TargetKey { get; }
        }
        #endregion Types

        #region Public Methods
        public static IReadOnlyList<PreviewElementGeometry> BuildElementBounds(
            SVGParser.SceneInfo sceneInfo,
            IReadOnlyDictionary<string, (string Key, string TargetKey)> keyByNodeId)
        {
            if (sceneInfo.Scene?.Root == null || keyByNodeId == null || keyByNodeId.Count == 0)
                return Array.Empty<PreviewElementGeometry>();

            GeometryWorldContext context = GeometryWorldContextBuilder.Build(sceneInfo);
            return BuildElementBounds(BuildMappedNodes(sceneInfo, keyByNodeId), context);
        }

        public static IReadOnlyList<PreviewElementGeometry> BuildElementBounds(
            Scene scene,
            IReadOnlyDictionary<SceneNode, (string Key, string TargetKey)> keyByNode,
            Dictionary<SceneNode, float> nodeOpacity)
        {
            if (scene?.Root == null || keyByNode == null || keyByNode.Count == 0)
                return Array.Empty<PreviewElementGeometry>();

            GeometryWorldContext context = GeometryWorldContextBuilder.Build(scene, nodeOpacity);
            return BuildElementBounds(BuildMappedNodes(keyByNode), context);
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
            return TryBuildSceneRootBounds(sceneInfo.Scene.Root, context, out worldBounds);
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
            return TryBuildSceneRootBounds(scene.Root, context, out worldBounds);
        }
        #endregion Public Methods

        #region Help Methods
        private static List<MappedPreviewNode> BuildMappedNodes(
            SVGParser.SceneInfo sceneInfo,
            IReadOnlyDictionary<string, (string Key, string TargetKey)> keyByNodeId)
        {
            List<MappedPreviewNode> mappedNodes = new();

            foreach (KeyValuePair<string, SceneNode> pair in sceneInfo.NodeIDs)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) ||
                    pair.Value == null ||
                    !keyByNodeId.TryGetValue(pair.Key, out (string Key, string TargetKey) mapping))
                {
                    continue;
                }

                mappedNodes.Add(new MappedPreviewNode(pair.Value, mapping.Key, mapping.TargetKey));
            }

            return mappedNodes;
        }

        private static List<MappedPreviewNode> BuildMappedNodes(
            IReadOnlyDictionary<SceneNode, (string Key, string TargetKey)> keyByNode)
        {
            List<MappedPreviewNode> mappedNodes = new(keyByNode.Count);

            foreach (KeyValuePair<SceneNode, (string Key, string TargetKey)> pair in keyByNode)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                mappedNodes.Add(new MappedPreviewNode(pair.Key, pair.Value.Key, pair.Value.TargetKey));
            }

            return mappedNodes;
        }

        private static IReadOnlyList<PreviewElementGeometry> BuildElementBounds(
            IReadOnlyList<MappedPreviewNode> mappedNodes,
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
            IReadOnlyList<Vector2[]> hitGeometry = BuildHitGeometry(node, context, out Rect visualBounds, out bool hasExactBounds);

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

        private static IReadOnlyList<Vector2[]> BuildHitGeometry(
            SceneNode node,
            GeometryWorldContext context,
            out Rect visualBounds,
            out bool hasExactBounds)
        {
            IReadOnlyList<Vector2[]> hitGeometry = GeometryWorldContextBuilder.BuildHitTriangles(
                node,
                context.WorldGeometryByNode,
                out visualBounds,
                out hasExactBounds);

            if (!hasExactBounds)
            {
                hasExactBounds = GeometryBoundsUtility.TryBuildFallbackBounds(
                    node,
                    context.WorldTransformByNode,
                    out visualBounds);
            }

            return hitGeometry;
        }

        private static bool TryBuildSceneRootBounds(
            SceneNode root,
            GeometryWorldContext context,
            out Rect worldBounds)
        {
            return GeometryBoundsUtility.TryBuildFallbackBounds(
                root,
                context.WorldTransformByNode,
                out worldBounds);
        }
        #endregion Help Methods
    }
}
