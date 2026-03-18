using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal static class VisualElementDashedBorderUtility
    {
        private static readonly Dictionary<VisualElement, Action<MeshGenerationContext>> DashedBorderHandlers = new();
        private static readonly Dictionary<VisualElement, EventCallback<DetachFromPanelEvent>> DashedBorderCleanupHandlers = new();

        public static T SetDashedBorder<T>(this T element, bool enable) where T : VisualElement
        {
            if (enable)
            {
                if (!DashedBorderHandlers.ContainsKey(element))
                {
                    void handler(MeshGenerationContext context) => RenderDashedBorder(element, context);
                    EventCallback<DetachFromPanelEvent> cleanup = _ => element.SetDashedBorder(false);

                    DashedBorderHandlers[element] = handler;
                    DashedBorderCleanupHandlers[element] = cleanup;

                    element.generateVisualContent += handler;
                    element.RegisterCallback(cleanup);
                }
            }
            else if (DashedBorderHandlers.TryGetValue(element, out Action<MeshGenerationContext> handler))
            {
                element.generateVisualContent -= handler;
                DashedBorderHandlers.Remove(element);

                if (DashedBorderCleanupHandlers.TryGetValue(element, out EventCallback<DetachFromPanelEvent> cleanup))
                {
                    element.UnregisterCallback(cleanup);
                    DashedBorderCleanupHandlers.Remove(element);
                }
            }

            element.MarkDirtyRepaint();
            return element;
        }

        private static void RenderDashedBorder(VisualElement element, MeshGenerationContext context)
        {
            float width = element.layout.width;
            float height = element.layout.height;
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            const float dash = 6f;
            const float gap = 4f;
            const float inset = 0.5f;

            Rect rect = new(inset, inset, width - (inset * 2f), height - (inset * 2f));
            float radius = Mathf.Min(
                Mathf.Min(element.resolvedStyle.borderTopLeftRadius, element.resolvedStyle.borderTopRightRadius),
                Mathf.Min(element.resolvedStyle.borderBottomLeftRadius, element.resolvedStyle.borderBottomRightRadius));
            radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);

            Painter2D painter = context.painter2D;
            painter.strokeColor = element.resolvedStyle.borderTopColor;
            painter.lineWidth = 1f;

            List<Vector2> points = BuildRoundedRectPoints(rect, radius, 8);
            painter.BeginPath();
            DrawDashedPolyline(painter, points, dash, gap);
            painter.Stroke();
        }

        private static List<Vector2> BuildRoundedRectPoints(Rect rect, float radius, int arcSegments)
        {
            List<Vector2> points = new();

            if (radius <= 0f)
            {
                points.Add(new Vector2(rect.xMin, rect.yMin));
                points.Add(new Vector2(rect.xMax, rect.yMin));
                points.Add(new Vector2(rect.xMax, rect.yMax));
                points.Add(new Vector2(rect.xMin, rect.yMax));
                points.Add(new Vector2(rect.xMin, rect.yMin));
                return points;
            }

            AddArc(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, arcSegments);
            points.Add(new Vector2(rect.xMax - radius, rect.yMin));
            AddArc(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, arcSegments, true);
            points.Add(new Vector2(rect.xMax, rect.yMax - radius));
            AddArc(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, arcSegments, true);
            points.Add(new Vector2(rect.xMin + radius, rect.yMax));
            AddArc(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, arcSegments, true);
            points.Add(points[0]);
            return points;
        }

        private static void AddArc(List<Vector2> points, Vector2 center, float radius, float startDeg, float endDeg, int segments, bool skipFirst = false)
        {
            int startIndex = skipFirst ? 1 : 0;
            for (int index = startIndex; index <= segments; index++)
            {
                float angle = Mathf.Lerp(startDeg, endDeg, (float)index / segments) * Mathf.Deg2Rad;
                points.Add(center + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius));
            }
        }

        private static void DrawDashedPolyline(Painter2D painter, List<Vector2> points, float dash, float gap)
        {
            if (points.Count < 2)
            {
                return;
            }

            bool drawing = true;
            float remaining = dash;

            for (int index = 0; index < points.Count - 1; index++)
            {
                Vector2 start = points[index];
                Vector2 end = points[index + 1];
                Vector2 segment = end - start;
                float segmentLength = segment.magnitude;
                if (segmentLength <= 0f)
                {
                    continue;
                }

                Vector2 direction = segment / segmentLength;
                float position = 0f;
                while (position < segmentLength)
                {
                    float step = Mathf.Min(remaining, segmentLength - position);
                    if (drawing && step > 0f)
                    {
                        painter.MoveTo(start + (direction * position));
                        painter.LineTo(start + (direction * (position + step)));
                    }

                    position += step;
                    remaining -= step;
                    if (remaining <= 0.001f)
                    {
                        drawing = !drawing;
                        remaining = drawing ? dash : gap;
                    }
                }
            }
        }
    }
}
