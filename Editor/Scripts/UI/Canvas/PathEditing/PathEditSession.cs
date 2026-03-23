using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.PathEditing;

namespace SvgEditor.UI.Canvas
{
    internal sealed class PathEditSession
    {
        private const int CurveOverlaySampleCount = 12;
        private readonly List<PathSubpathView> _subpaths = new();
        private readonly ReadOnlyCollection<PathSubpathView> _subpathView;
        private readonly Func<Vector2, Vector2?> _sceneToViewportPoint;

        public PathEditSession(string elementKey)
            : this(elementKey, Matrix2D.identity, null)
        {
        }

        public PathEditSession(
            string elementKey,
            Matrix2D worldTransform,
            Func<Vector2, Vector2?> sceneToViewportPoint)
        {
            ElementKey = elementKey ?? string.Empty;
            WorldTransform = worldTransform;
            _sceneToViewportPoint = sceneToViewportPoint;
            _subpathView = _subpaths.AsReadOnly();
        }

        public string ElementKey { get; }
        public Matrix2D WorldTransform { get; }
        public PathSelectionState Selection { get; } = new();
        public IReadOnlyList<PathSubpathView> Subpaths => _subpathView;
        public PathData PathData { get; private set; }

        public void SetGeometry(IEnumerable<PathSubpathView> subpaths)
        {
            _subpaths.Clear();
            Selection.Clear();
            if (subpaths == null)
            {
                return;
            }

            foreach (PathSubpathView subpath in subpaths)
            {
                if (subpath != null)
                {
                    _subpaths.Add(subpath.Clone());
                }
            }
        }

        public bool TryHit(
            Vector2 viewportPoint,
            float nodeRadius,
            float handleRadius,
            float segmentDistance,
            out PathHit hit)
        {
            return PathHitTester.TryHit(Subpaths, viewportPoint, nodeRadius, handleRadius, segmentDistance, out hit);
        }

        public bool TrySetPathData(PathData pathData, out string error)
        {
            error = string.Empty;
            PathNodeRef? activeNode = Selection.ActiveNode;
            PathHandleRef? activeHandle = Selection.ActiveHandle;
            PathData nextPathData = ClonePathData(pathData);
            if (nextPathData == null)
            {
                return false;
            }

            if (_sceneToViewportPoint == null)
            {
                error = "Path edit is unavailable: preview projection is unavailable.";
                return false;
            }

            if (!TryBuildSubpathViews(nextPathData, WorldTransform, _sceneToViewportPoint, out List<PathSubpathView> subpaths))
            {
                error = "Path edit is unavailable: preview projection is unavailable.";
                return false;
            }

            PathData = nextPathData;
            SetGeometry(subpaths);
            RestoreSelection(activeNode, activeHandle);
            return true;
        }

        public PathData ClonePathData()
        {
            return ClonePathData(PathData);
        }

        internal static PathData ClonePathData(PathData source)
        {
            if (source == null)
            {
                return null;
            }

            PathData clone = new();
            for (int index = 0; index < source.UnsupportedCommands.Count; index++)
            {
                clone.AddUnsupportedCommand(source.UnsupportedCommands[index]);
            }

            if (source.IsMalformed)
            {
                clone.MarkMalformed(source.ParseError);
            }

            for (int subpathIndex = 0; subpathIndex < source.Subpaths.Count; subpathIndex++)
            {
                PathSubpath subpath = source.Subpaths[subpathIndex];
                if (subpath == null)
                {
                    clone.Subpaths.Add(null);
                    continue;
                }

                PathSubpath subpathClone = new()
                {
                    Start = subpath.Start,
                    IsClosed = subpath.IsClosed
                };

                for (int nodeIndex = 0; nodeIndex < subpath.Nodes.Count; nodeIndex++)
                {
                    subpathClone.Nodes.Add(subpath.Nodes[nodeIndex]);
                }

                clone.Subpaths.Add(subpathClone);
            }

            return clone;
        }

        internal static bool TryBuildSubpathViews(
            PathData pathData,
            Matrix2D worldTransform,
            Func<Vector2, Vector2?> sceneToViewportPoint,
            out List<PathSubpathView> subpaths)
        {
            subpaths = null;
            if (pathData == null || sceneToViewportPoint == null)
            {
                return false;
            }

            subpaths = new List<PathSubpathView>(pathData.Subpaths.Count);
            for (int subpathIndex = 0; subpathIndex < pathData.Subpaths.Count; subpathIndex++)
            {
                if (!TryBuildSubpathView(pathData.Subpaths[subpathIndex], worldTransform, sceneToViewportPoint, out PathSubpathView view))
                {
                    subpaths = null;
                    return false;
                }

                subpaths.Add(view);
            }

            return true;
        }

        internal static bool TryBuildSubpathView(
            PathSubpath subpath,
            Matrix2D worldTransform,
            Func<Vector2, Vector2?> sceneToViewportPoint,
            out PathSubpathView view)
        {
            view = null;
            if (subpath == null)
            {
                return false;
            }

            bool isClosedLoop = subpath.IsClosed && subpath.Nodes.Count > 0;
            bool hasExplicitClosingNode = isClosedLoop &&
                                          (subpath.Nodes[^1].Position - subpath.Start).sqrMagnitude <= 0.000001f;
            int nodeCount = isClosedLoop && hasExplicitClosingNode
                ? subpath.Nodes.Count
                : subpath.Nodes.Count + 1;
            Vector2[] positions = new Vector2[nodeCount];
            Vector2[] inHandles = new Vector2[nodeCount];
            Vector2[] outHandles = new Vector2[nodeCount];
            bool[] hasInHandle = new bool[nodeCount];
            bool[] hasOutHandle = new bool[nodeCount];

            if (!TryProjectPoint(subpath.Start, worldTransform, sceneToViewportPoint, out positions[0]))
            {
                return false;
            }

            int projectedNodeCount = isClosedLoop && hasExplicitClosingNode
                ? subpath.Nodes.Count - 1
                : subpath.Nodes.Count;
            for (int nodeIndex = 0; nodeIndex < projectedNodeCount; nodeIndex++)
            {
                if (!TryProjectPoint(subpath.Nodes[nodeIndex].Position, worldTransform, sceneToViewportPoint, out positions[nodeIndex + 1]))
                {
                    return false;
                }
            }

            Vector2 segmentStartPosition = subpath.Start;
            for (int segmentIndex = 0; segmentIndex < subpath.Nodes.Count; segmentIndex++)
            {
                PathNode segmentNode = subpath.Nodes[segmentIndex];
                int incomingNodeIndex = isClosedLoop
                    ? (hasExplicitClosingNode ? (segmentIndex + 1) % nodeCount : segmentIndex + 1)
                    : segmentIndex + 1;
                switch (segmentNode.Command)
                {
                    case 'C':
                    case 'S':
                        if (!TryProjectPoint(segmentNode.Control0, worldTransform, sceneToViewportPoint, out outHandles[segmentIndex]) ||
                            !TryProjectPoint(segmentNode.Control1, worldTransform, sceneToViewportPoint, out inHandles[incomingNodeIndex]))
                        {
                            return false;
                        }

                        hasOutHandle[segmentIndex] = true;
                        hasInHandle[incomingNodeIndex] = true;
                        break;

                    case 'Q':
                    case 'T':
                        Vector2 outgoingQuadraticHandle = segmentStartPosition + ((segmentNode.Control0 - segmentStartPosition) * (2f / 3f));
                        Vector2 incomingQuadraticHandle = segmentNode.Position + ((segmentNode.Control0 - segmentNode.Position) * (2f / 3f));
                        if (!TryProjectPoint(outgoingQuadraticHandle, worldTransform, sceneToViewportPoint, out outHandles[segmentIndex]) ||
                            !TryProjectPoint(incomingQuadraticHandle, worldTransform, sceneToViewportPoint, out inHandles[incomingNodeIndex]))
                        {
                            return false;
                        }
                        hasOutHandle[segmentIndex] = true;
                        hasInHandle[incomingNodeIndex] = true;
                        break;
                }

                segmentStartPosition = segmentNode.Position;
            }

            var nodes = new List<PathNodeView>(nodeCount);
            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                nodes.Add(new PathNodeView(
                    positions[nodeIndex],
                    inHandles[nodeIndex],
                    hasInHandle[nodeIndex],
                    outHandles[nodeIndex],
                    hasOutHandle[nodeIndex]));
            }

            var segments = new List<CanvasLineSegment>(Mathf.Max(0, nodeCount - 1) * CurveOverlaySampleCount + (subpath.IsClosed ? 1 : 0));
            Vector2 localSegmentStart = subpath.Start;
            for (int segmentIndex = 0; segmentIndex < subpath.Nodes.Count; segmentIndex++)
            {
                int viewportEndIndex = isClosedLoop
                    ? (hasExplicitClosingNode ? (segmentIndex + 1) % nodeCount : segmentIndex + 1)
                    : segmentIndex + 1;
                if (!TryAppendOverlaySegments(
                        localSegmentStart,
                        positions[segmentIndex],
                        subpath.Nodes[segmentIndex],
                        positions[viewportEndIndex],
                        worldTransform,
                        sceneToViewportPoint,
                        segments))
                {
                    return false;
                }

                localSegmentStart = subpath.Nodes[segmentIndex].Position;
            }

            if (isClosedLoop && !hasExplicitClosingNode)
            {
                segments.Add(new CanvasLineSegment(positions[nodeCount - 1], positions[0]));
            }

            view = new PathSubpathView(nodes, segments, subpath.IsClosed);
            return true;
        }

        internal static bool TryProjectPoint(
            Vector2 localPoint,
            Matrix2D worldTransform,
            Func<Vector2, Vector2?> sceneToViewportPoint,
            out Vector2 viewportPoint)
        {
            Vector2 scenePoint = worldTransform.MultiplyPoint(localPoint);
            Vector2? resolvedViewportPoint = sceneToViewportPoint(scenePoint);
            if (!resolvedViewportPoint.HasValue)
            {
                viewportPoint = default;
                return false;
            }

            viewportPoint = resolvedViewportPoint.Value;
            return true;
        }

        private static bool TryAppendOverlaySegments(
            Vector2 localStart,
            Vector2 viewportStart,
            PathNode segmentNode,
            Vector2 viewportEnd,
            Matrix2D worldTransform,
            Func<Vector2, Vector2?> sceneToViewportPoint,
            List<CanvasLineSegment> segments)
        {
            switch (segmentNode.Command)
            {
                case 'C':
                case 'S':
                    return TryAppendSampledCurve(
                        CurveOverlaySampleCount,
                        t => EvaluateCubic(localStart, segmentNode.Control0, segmentNode.Control1, segmentNode.Position, t),
                        viewportStart,
                        viewportEnd,
                        worldTransform,
                        sceneToViewportPoint,
                        segments);

                case 'Q':
                case 'T':
                    return TryAppendSampledCurve(
                        CurveOverlaySampleCount,
                        t => EvaluateQuadratic(localStart, segmentNode.Control0, segmentNode.Position, t),
                        viewportStart,
                        viewportEnd,
                        worldTransform,
                        sceneToViewportPoint,
                        segments);

                default:
                    segments.Add(new CanvasLineSegment(viewportStart, viewportEnd));
                    return true;
            }
        }

        private static bool TryAppendSampledCurve(
            int sampleCount,
            Func<float, Vector2> evaluatePoint,
            Vector2 viewportStart,
            Vector2 viewportEnd,
            Matrix2D worldTransform,
            Func<Vector2, Vector2?> sceneToViewportPoint,
            List<CanvasLineSegment> segments)
        {
            Vector2 previousViewportPoint = viewportStart;
            for (int sampleIndex = 1; sampleIndex <= sampleCount; sampleIndex++)
            {
                Vector2 nextViewportPoint = sampleIndex == sampleCount
                    ? viewportEnd
                    : default;

                if (sampleIndex < sampleCount &&
                    !TryProjectPoint(
                        evaluatePoint(sampleIndex / (float)sampleCount),
                        worldTransform,
                        sceneToViewportPoint,
                        out nextViewportPoint))
                {
                    return false;
                }

                segments.Add(new CanvasLineSegment(previousViewportPoint, nextViewportPoint));
                previousViewportPoint = nextViewportPoint;
            }

            return true;
        }

        private static Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float oneMinusT = 1f - t;
            return (oneMinusT * oneMinusT * oneMinusT * p0) +
                   (3f * oneMinusT * oneMinusT * t * p1) +
                   (3f * oneMinusT * t * t * p2) +
                   (t * t * t * p3);
        }

        private static Vector2 EvaluateQuadratic(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float oneMinusT = 1f - t;
            return (oneMinusT * oneMinusT * p0) +
                   (2f * oneMinusT * t * p1) +
                   (t * t * p2);
        }

        private void RestoreSelection(PathNodeRef? activeNode, PathHandleRef? activeHandle)
        {
            if (activeHandle.HasValue && IsValidHandle(activeHandle.Value))
            {
                Selection.SelectHandle(activeHandle.Value);
                return;
            }

            if (activeNode.HasValue && IsValidNode(activeNode.Value))
            {
                Selection.SelectNode(activeNode.Value);
            }
        }

        internal void RestoreSelectionState(PathNodeRef? activeNode, PathHandleRef? activeHandle)
        {
            RestoreSelection(activeNode, activeHandle);
        }

        private bool IsValidHandle(PathHandleRef handle)
        {
            return IsValidNode(handle.Node) &&
                   (handle.Slot == PathHandleSlot.In
                       ? Subpaths[handle.Node.SubpathIndex].Nodes[handle.Node.NodeIndex].HasInHandle
                       : Subpaths[handle.Node.SubpathIndex].Nodes[handle.Node.NodeIndex].HasOutHandle);
        }

        private bool IsValidNode(PathNodeRef node)
        {
            return node.SubpathIndex >= 0 &&
                   node.SubpathIndex < Subpaths.Count &&
                   Subpaths[node.SubpathIndex] != null &&
                   node.NodeIndex >= 0 &&
                   node.NodeIndex < Subpaths[node.SubpathIndex].Nodes.Count;
        }
    }
}
