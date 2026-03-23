using System;
using System.Collections.Generic;
using UnityEngine;

namespace SvgEditor.UI.Canvas
{
    internal readonly struct PathSegmentRef : IEquatable<PathSegmentRef>
    {
        public PathSegmentRef(int subpathIndex, int segmentIndex)
        {
            SubpathIndex = subpathIndex;
            SegmentIndex = segmentIndex;
        }

        public int SubpathIndex { get; }
        public int SegmentIndex { get; }

        public bool Equals(PathSegmentRef other)
        {
            return SubpathIndex == other.SubpathIndex && SegmentIndex == other.SegmentIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is PathSegmentRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SubpathIndex * 397) ^ SegmentIndex;
            }
        }
    }

    internal enum PathHitKind
    {
        None,
        Node,
        Handle,
        Segment
    }

    internal readonly struct PathHit
    {
        private PathHit(
            PathHitKind kind,
            PathNodeRef node,
            PathHandleRef handle,
            PathSegmentRef segment,
            float distanceSquared)
        {
            Kind = kind;
            Node = node;
            Handle = handle;
            Segment = segment;
            DistanceSquared = distanceSquared;
        }

        public PathHitKind Kind { get; }
        public PathNodeRef Node { get; }
        public PathHandleRef Handle { get; }
        public PathSegmentRef Segment { get; }
        public float DistanceSquared { get; }

        public static PathHit NodeHit(PathNodeRef node, float distanceSquared)
        {
            return new PathHit(PathHitKind.Node, node, default, default, distanceSquared);
        }

        public static PathHit HandleHit(PathHandleRef handle, float distanceSquared)
        {
            return new PathHit(PathHitKind.Handle, handle.Node, handle, default, distanceSquared);
        }

        public static PathHit SegmentHit(PathSegmentRef segment, float distanceSquared)
        {
            return new PathHit(PathHitKind.Segment, default, default, segment, distanceSquared);
        }
    }

    internal static class PathHitTester
    {
        public static bool TryHit(
            IReadOnlyList<PathSubpathView> subpaths,
            Vector2 viewportPoint,
            float nodeRadius,
            float handleRadius,
            float segmentDistance,
            out PathHit hit)
        {
            if (TryHitHandles(subpaths, viewportPoint, handleRadius, out hit))
            {
                return true;
            }

            if (TryHitNodes(subpaths, viewportPoint, nodeRadius, out hit))
            {
                return true;
            }

            if (TryHitSegments(subpaths, viewportPoint, segmentDistance, out hit))
            {
                return true;
            }

            hit = default;
            return false;
        }

        private static bool TryHitHandles(
            IReadOnlyList<PathSubpathView> subpaths,
            Vector2 viewportPoint,
            float radius,
            out PathHit hit)
        {
            float bestDistanceSquared = radius * radius;
            PathHandleRef bestHandle = default;
            bool found = false;

            if (subpaths != null)
            {
                for (int subpathIndex = 0; subpathIndex < subpaths.Count; subpathIndex++)
                {
                    PathSubpathView subpath = subpaths[subpathIndex];
                    if (subpath == null)
                    {
                        continue;
                    }

                    for (int nodeIndex = 0; nodeIndex < subpath.Nodes.Count; nodeIndex++)
                    {
                        PathNodeView node = subpath.Nodes[nodeIndex];
                        PathNodeRef nodeRef = new(subpathIndex, nodeIndex);
                        TryMeasureHandle(node.HasInHandle, node.InHandle, new PathHandleRef(nodeRef, PathHandleSlot.In), viewportPoint, ref bestDistanceSquared, ref bestHandle, ref found);
                        TryMeasureHandle(node.HasOutHandle, node.OutHandle, new PathHandleRef(nodeRef, PathHandleSlot.Out), viewportPoint, ref bestDistanceSquared, ref bestHandle, ref found);
                    }
                }
            }

            hit = found
                ? PathHit.HandleHit(bestHandle, bestDistanceSquared)
                : default;
            return found;
        }

        private static bool TryHitNodes(
            IReadOnlyList<PathSubpathView> subpaths,
            Vector2 viewportPoint,
            float radius,
            out PathHit hit)
        {
            float bestDistanceSquared = radius * radius;
            PathNodeRef bestNode = default;
            bool found = false;

            if (subpaths != null)
            {
                for (int subpathIndex = 0; subpathIndex < subpaths.Count; subpathIndex++)
                {
                    PathSubpathView subpath = subpaths[subpathIndex];
                    if (subpath == null)
                    {
                        continue;
                    }

                    for (int nodeIndex = 0; nodeIndex < subpath.Nodes.Count; nodeIndex++)
                    {
                        float distanceSquared = (subpath.Nodes[nodeIndex].Position - viewportPoint).sqrMagnitude;
                        if (!ShouldReplace(distanceSquared, bestDistanceSquared, found))
                        {
                            continue;
                        }

                        bestDistanceSquared = distanceSquared;
                        bestNode = new PathNodeRef(subpathIndex, nodeIndex);
                        found = true;
                    }
                }
            }

            hit = found
                ? PathHit.NodeHit(bestNode, bestDistanceSquared)
                : default;
            return found;
        }

        private static bool TryHitSegments(
            IReadOnlyList<PathSubpathView> subpaths,
            Vector2 viewportPoint,
            float distance,
            out PathHit hit)
        {
            float bestDistanceSquared = distance * distance;
            PathSegmentRef bestSegment = default;
            bool found = false;

            if (subpaths != null)
            {
                for (int subpathIndex = 0; subpathIndex < subpaths.Count; subpathIndex++)
                {
                    PathSubpathView subpath = subpaths[subpathIndex];
                    if (subpath == null)
                    {
                        continue;
                    }

                    for (int segmentIndex = 0; segmentIndex < subpath.Segments.Count; segmentIndex++)
                    {
                        float distanceSquared = DistanceToSegmentSquared(viewportPoint, subpath.Segments[segmentIndex]);
                        if (!ShouldReplace(distanceSquared, bestDistanceSquared, found))
                        {
                            continue;
                        }

                        bestDistanceSquared = distanceSquared;
                        bestSegment = new PathSegmentRef(subpathIndex, segmentIndex);
                        found = true;
                    }
                }
            }

            hit = found
                ? PathHit.SegmentHit(bestSegment, bestDistanceSquared)
                : default;
            return found;
        }

        private static bool TryMeasureHandle(
            bool hasHandle,
            Vector2 handlePoint,
            PathHandleRef handle,
            Vector2 viewportPoint,
            ref float bestDistanceSquared,
            ref PathHandleRef bestHandle,
            ref bool found)
        {
            if (!hasHandle)
            {
                return false;
            }

            float distanceSquared = (handlePoint - viewportPoint).sqrMagnitude;
            if (!ShouldReplace(distanceSquared, bestDistanceSquared, found))
            {
                return false;
            }

            bestDistanceSquared = distanceSquared;
            bestHandle = handle;
            found = true;
            return true;
        }

        private static bool ShouldReplace(float distanceSquared, float bestDistanceSquared, bool found)
        {
            return !found
                ? distanceSquared <= bestDistanceSquared
                : distanceSquared < bestDistanceSquared;
        }

        private static float DistanceToSegmentSquared(Vector2 point, CanvasLineSegment segment)
        {
            Vector2 direction = segment.End - segment.Start;
            float lengthSquared = direction.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return (point - segment.Start).sqrMagnitude;
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - segment.Start, direction) / lengthSquared);
            Vector2 closest = segment.Start + (direction * t);
            return (point - closest).sqrMagnitude;
        }
    }
}
