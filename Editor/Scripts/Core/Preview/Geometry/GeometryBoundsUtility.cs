using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace SvgEditor.Core.Preview.Geometry
{
    internal static class GeometryBoundsUtility
    {
        public static bool TryBuildVisualContentBounds(
            IReadOnlyList<PreviewElementGeometry> elements,
            out Rect visualContentBounds)
        {
            visualContentBounds = default;
            if (elements == null || elements.Count == 0)
                return false;

            bool hasBounds = false;
            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);

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

        public static void AccumulateBounds(ref bool hasBounds, ref Rect accumulatedBounds, Rect bounds)
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

        public static bool TryBuildTriangleBounds(IReadOnlyList<Vector2[]> triangles, out Rect bounds)
        {
            bounds = default;
            if (triangles == null || triangles.Count == 0)
                return false;

            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);

            foreach (Vector2[] triangle in triangles)
            {
                if (triangle == null || triangle.Length < 3)
                    continue;

                for (int index = 0; index < triangle.Length; index++)
                {
                    min = Vector2.Min(min, triangle[index]);
                    max = Vector2.Max(max, triangle[index]);
                }
            }

            if (min.x == float.MaxValue || min.y == float.MaxValue)
                return false;

            bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        public static bool TryBuildFallbackBounds(
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

        public static void ResolveRotationPivot(
            SceneNode node,
            Matrix2D resolvedWorldTransform,
            Rect fallbackWorldBounds,
            out Vector2 rotationPivotWorld,
            out Vector2 rotationPivotParentSpace)
        {
            rotationPivotWorld = Vector2.zero;
            rotationPivotParentSpace = Vector2.zero;
            if (node == null)
                return;

            // Keep the rotation pivot aligned with the visible selection bounds center.
            // Using a geometry-derived local center can drift for grouped / uneven content,
            // which makes the pivot look slightly off from the selection box midpoint.
            rotationPivotWorld = fallbackWorldBounds.center;
            rotationPivotParentSpace = (resolvedWorldTransform * node.Transform.Inverse()).Inverse().MultiplyPoint(rotationPivotWorld);
        }
    }
}
