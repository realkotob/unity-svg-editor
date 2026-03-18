using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.DocumentModel;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Preview;
using SvgEditor.Preview.Geometry;
using SvgEditor.Renderer;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class DefinitionOverlayBuilder
    {
        private const float EdgeQuantizationStep = 0.5f;

        private readonly SvgModelSceneBuilder _sceneBuilder = new();

        public bool TryBuild(
            SvgDocumentModel documentModel,
            string selectedElementKey,
            PreviewSnapshot previewSnapshot,
            SceneProjector sceneProjector,
            out IReadOnlyList<CanvasDefinitionOverlayVisual> overlays,
            out string error)
        {
            overlays = Array.Empty<CanvasDefinitionOverlayVisual>();
            error = string.Empty;

            if (documentModel?.Root == null ||
                previewSnapshot == null ||
                sceneProjector == null ||
                string.IsNullOrWhiteSpace(selectedElementKey))
            {
                return true;
            }

            if (!_sceneBuilder.TryBuildReferenceOverlayScenes(documentModel, selectedElementKey, out IReadOnlyList<CanvasDefinitionOverlayScene> overlayScenes, out error))
                return false;

            if (overlayScenes == null || overlayScenes.Count == 0)
                return true;

            PreviewElementGeometry sourceGeometry = FindPreviewElement(previewSnapshot, selectedElementKey);
            Matrix2D sourceWorldTransform = sourceGeometry?.WorldTransform ?? Matrix2D.identity;
            List<CanvasDefinitionOverlayVisual> resolved = new();
            for (int index = 0; index < overlayScenes.Count; index++)
            {
                CanvasDefinitionOverlayScene overlayScene = overlayScenes[index];
                if (overlayScene?.RootNode == null ||
                    !SnapshotGeometryBuilder.TryBuildOverlayGeometry(
                        overlayScene.RootNode,
                        overlayScene.NodeOpacities,
                        out Rect sceneBounds,
                        out IReadOnlyList<Vector2[]> hitGeometry))
                {
                    continue;
                }

                if (!sourceWorldTransform.Equals(Matrix2D.identity))
                {
                    sceneBounds = TransformRect(sourceWorldTransform, sceneBounds);
                    hitGeometry = TransformTriangles(sourceWorldTransform, hitGeometry);
                }

                if (!sceneProjector.TrySceneRectToViewportRect(previewSnapshot, sceneBounds, out Rect viewportBounds))
                    continue;

                IReadOnlyList<CanvasLineSegment> viewportSegments = BuildViewportSegments(hitGeometry, previewSnapshot, sceneProjector);
                resolved.Add(new CanvasDefinitionOverlayVisual
                {
                    Kind = overlayScene.Kind,
                    ReferenceId = overlayScene.ReferenceId,
                    ProxyElementKey = DefinitionProxyUtility.BuildProxyKey(selectedElementKey, overlayScene.Kind, overlayScene.ReferenceId),
                    DefinitionElementKey = overlayScene.DefinitionElementKey,
                    SceneBounds = sceneBounds,
                    ParentWorldTransform = sourceWorldTransform,
                    ViewportBounds = viewportBounds,
                    OutlineSegments = viewportSegments
                });
            }

            overlays = resolved;
            return true;
        }

        private static PreviewElementGeometry FindPreviewElement(PreviewSnapshot previewSnapshot, string elementKey)
        {
            if (previewSnapshot?.Elements == null || string.IsNullOrWhiteSpace(elementKey))
                return null;

            for (int index = 0; index < previewSnapshot.Elements.Count; index++)
            {
                PreviewElementGeometry element = previewSnapshot.Elements[index];
                if (element != null && string.Equals(element.Key, elementKey, StringComparison.Ordinal))
                    return element;
            }

            return null;
        }

        private static Rect TransformRect(Matrix2D transform, Rect rect)
        {
            Vector2 topLeft = transform.MultiplyPoint(new Vector2(rect.xMin, rect.yMin));
            Vector2 topRight = transform.MultiplyPoint(new Vector2(rect.xMax, rect.yMin));
            Vector2 bottomRight = transform.MultiplyPoint(new Vector2(rect.xMax, rect.yMax));
            Vector2 bottomLeft = transform.MultiplyPoint(new Vector2(rect.xMin, rect.yMax));

            return Rect.MinMaxRect(
                Mathf.Min(topLeft.x, topRight.x, bottomRight.x, bottomLeft.x),
                Mathf.Min(topLeft.y, topRight.y, bottomRight.y, bottomLeft.y),
                Mathf.Max(topLeft.x, topRight.x, bottomRight.x, bottomLeft.x),
                Mathf.Max(topLeft.y, topRight.y, bottomRight.y, bottomLeft.y));
        }

        private static IReadOnlyList<Vector2[]> TransformTriangles(Matrix2D transform, IReadOnlyList<Vector2[]> triangles)
        {
            if (triangles == null || triangles.Count == 0)
                return Array.Empty<Vector2[]>();

            List<Vector2[]> transformed = new(triangles.Count);
            for (int triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                Vector2[] triangle = triangles[triangleIndex];
                if (triangle == null || triangle.Length < 3)
                    continue;

                transformed.Add(new[]
                {
                    transform.MultiplyPoint(triangle[0]),
                    transform.MultiplyPoint(triangle[1]),
                    transform.MultiplyPoint(triangle[2])
                });
            }

            return transformed;
        }

        private static IReadOnlyList<CanvasLineSegment> BuildViewportSegments(
            IReadOnlyList<Vector2[]> hitGeometry,
            PreviewSnapshot previewSnapshot,
            SceneProjector sceneProjector)
        {
            if (hitGeometry == null || hitGeometry.Count == 0)
                return Array.Empty<CanvasLineSegment>();

            Dictionary<EdgeKey, EdgeAccumulator> edges = new();
            for (int triangleIndex = 0; triangleIndex < hitGeometry.Count; triangleIndex++)
            {
                Vector2[] triangle = hitGeometry[triangleIndex];
                if (triangle == null || triangle.Length < 3)
                    continue;

                AddEdge(edges, triangle[0], triangle[1]);
                AddEdge(edges, triangle[1], triangle[2]);
                AddEdge(edges, triangle[2], triangle[0]);
            }

            List<CanvasLineSegment> segments = new();
            foreach (EdgeAccumulator edge in edges.Values)
            {
                if (edge.Count != 1 ||
                    !sceneProjector.TryScenePointToViewportPoint(previewSnapshot, edge.Start, out Vector2 viewportStart) ||
                    !sceneProjector.TryScenePointToViewportPoint(previewSnapshot, edge.End, out Vector2 viewportEnd))
                {
                    continue;
                }

                segments.Add(new CanvasLineSegment(viewportStart, viewportEnd));
            }

            return segments;
        }

        private static void AddEdge(Dictionary<EdgeKey, EdgeAccumulator> edges, Vector2 start, Vector2 end)
        {
            if ((end - start).sqrMagnitude <= Mathf.Epsilon)
                return;

            EdgeKey key = EdgeKey.Create(start, end);
            if (!edges.TryGetValue(key, out EdgeAccumulator accumulator))
            {
                accumulator = new EdgeAccumulator(start, end);
                edges.Add(key, accumulator);
            }

            accumulator.Count++;
        }

        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            private EdgeKey(int ax, int ay, int bx, int by)
            {
                Ax = ax;
                Ay = ay;
                Bx = bx;
                By = by;
            }

            private int Ax { get; }
            private int Ay { get; }
            private int Bx { get; }
            private int By { get; }

            public static EdgeKey Create(Vector2 start, Vector2 end)
            {
                int ax = Quantize(start.x);
                int ay = Quantize(start.y);
                int bx = Quantize(end.x);
                int by = Quantize(end.y);

                bool swap = ax > bx || (ax == bx && ay > by);
                return swap
                    ? new EdgeKey(bx, by, ax, ay)
                    : new EdgeKey(ax, ay, bx, by);
            }

            public bool Equals(EdgeKey other)
            {
                return Ax == other.Ax && Ay == other.Ay && Bx == other.Bx && By == other.By;
            }

            public override bool Equals(object obj)
            {
                return obj is EdgeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Ax;
                    hash = (hash * 397) ^ Ay;
                    hash = (hash * 397) ^ Bx;
                    hash = (hash * 397) ^ By;
                    return hash;
                }
            }

            private static int Quantize(float value)
            {
                return Mathf.RoundToInt(value / EdgeQuantizationStep);
            }
        }

        private sealed class EdgeAccumulator
        {
            public EdgeAccumulator(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public int Count { get; set; }
        }
    }
}
