using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewElementHitTester
    {
        public bool TryHitTest(
            IReadOnlyList<PreviewElementGeometry> elements,
            Vector2 scenePoint,
            float sceneHitRadius,
            out PreviewElementGeometry hitElement)
        {
            hitElement = null;
            if (elements == null || elements.Count == 0)
            {
                return false;
            }

            foreach (var element in elements.OrderByDescending(item => item.DrawOrder))
            {
                if (!ExpandRect(element.VisualBounds, sceneHitRadius).Contains(scenePoint) ||
                    element.HitGeometry == null ||
                    element.HitGeometry.Count == 0)
                {
                    continue;
                }

                if (!element.HitGeometry.Any(triangle => IsPointInTriangle(scenePoint, triangle[0], triangle[1], triangle[2])))
                {
                    continue;
                }

                hitElement = element;
                return true;
            }

            var boundsFallback = elements
                .Where(item => ExpandRect(item.VisualBounds, sceneHitRadius).Contains(scenePoint))
                .OrderBy(item => item.VisualBounds.width * item.VisualBounds.height)
                .ThenByDescending(item => item.DrawOrder)
                .FirstOrDefault();
            if (boundsFallback != null)
            {
                hitElement = boundsFallback;
                return true;
            }

            return false;
        }

        public bool TryHitTest(
            IReadOnlyList<PreviewElementGeometry> elements,
            Vector2 scenePoint,
            out PreviewElementGeometry hitElement)
        {
            return TryHitTest(elements, scenePoint, 0f, out hitElement);
        }

        private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
            {
                return ((p1.x - p3.x) * (p2.y - p3.y)) - ((p2.x - p3.x) * (p1.y - p3.y));
            }

            var d1 = Sign(point, a, b);
            var d2 = Sign(point, b, c);
            var d3 = Sign(point, c, a);
            var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static Rect ExpandRect(Rect rect, float radius)
        {
            if (radius <= Mathf.Epsilon)
                return rect;

            return Rect.MinMaxRect(
                rect.xMin - radius,
                rect.yMin - radius,
                rect.xMax + radius,
                rect.yMax + radius);
        }
    }
}
