using System.Collections.Generic;
using UnityEngine;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Preview.Geometry
{
    internal sealed class ElementHitTester
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

            PreviewElementGeometry boundsFallback = null;
            float boundsFallbackArea = float.MaxValue;
            int boundsFallbackDrawOrder = int.MinValue;
            bool boundsFallbackIsText = false;
            PreviewElementGeometry textHit = null;

            for (int index = 0; index < elements.Count; index++)
            {
                var element = elements[index];
                Rect expandedBounds = ExpandRect(element.VisualBounds, sceneHitRadius);
                if (!expandedBounds.Contains(scenePoint))
                {
                    continue;
                }

                float elementArea = element.VisualBounds.width * element.VisualBounds.height;
                if (boundsFallback == null ||
                    (element.IsTextOverlay && !boundsFallbackIsText) ||
                    (element.IsTextOverlay == boundsFallbackIsText &&
                     (elementArea < boundsFallbackArea ||
                      (elementArea == boundsFallbackArea && element.DrawOrder > boundsFallbackDrawOrder))))
                {
                    boundsFallback = element;
                    boundsFallbackArea = elementArea;
                    boundsFallbackDrawOrder = element.DrawOrder;
                    boundsFallbackIsText = element.IsTextOverlay;
                }

                if (element.IsTextOverlay)
                {
                    if (textHit == null || element.DrawOrder > textHit.DrawOrder)
                        textHit = element;
                    continue;
                }

                if (element.HitGeometry == null ||
                    element.HitGeometry.Count == 0)
                {
                    continue;
                }

                if (TryHitGeometry(element, scenePoint, hitElement, out PreviewElementGeometry candidate))
                {
                    hitElement = candidate;
                }
            }

            if (textHit != null)
            {
                hitElement = textHit;
                return true;
            }

            if (hitElement != null)
            {
                return true;
            }

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

        private static bool TryHitGeometry(
            PreviewElementGeometry element,
            Vector2 scenePoint,
            PreviewElementGeometry currentHit,
            out PreviewElementGeometry hitElement)
        {
            hitElement = currentHit;
            for (int triangleIndex = 0; triangleIndex < element.HitGeometry.Count; triangleIndex++)
            {
                Vector2[] triangle = element.HitGeometry[triangleIndex];
                if (!IsPointInTriangle(scenePoint, triangle[0], triangle[1], triangle[2]))
                {
                    continue;
                }

                if (currentHit == null || element.DrawOrder > currentHit.DrawOrder)
                {
                    hitElement = element;
                }

                return true;
            }

            return false;
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
