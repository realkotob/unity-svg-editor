using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.UI.Canvas
{
    internal readonly struct PathOverlayNode
    {
        public PathOverlayNode(PathNodeRef node, Vector2 position, bool isSelected, bool isActive)
        {
            Node = node;
            Position = position;
            IsSelected = isSelected;
            IsActive = isActive;
        }

        public PathNodeRef Node { get; }
        public Vector2 Position { get; }
        public bool IsSelected { get; }
        public bool IsActive { get; }
    }

    internal readonly struct PathOverlayHandle
    {
        public PathOverlayHandle(PathHandleRef handle, Vector2 position, bool isActive)
        {
            Handle = handle;
            Position = position;
            IsActive = isActive;
        }

        public PathHandleRef Handle { get; }
        public Vector2 Position { get; }
        public bool IsActive { get; }
    }

    internal sealed class PathOverlayVisual
    {
        public IReadOnlyList<CanvasLineSegment> PathSegments { get; set; } = Array.Empty<CanvasLineSegment>();
        public IReadOnlyList<CanvasLineSegment> HandleSegments { get; set; } = Array.Empty<CanvasLineSegment>();
        public IReadOnlyList<PathOverlayNode> Nodes { get; set; } = Array.Empty<PathOverlayNode>();
        public IReadOnlyList<PathOverlayHandle> Handles { get; set; } = Array.Empty<PathOverlayHandle>();
    }

    internal sealed class PathOverlayPresenter
    {
        private readonly List<VisualElement> _nodeElements = new();
        private readonly List<VisualElement> _handleElements = new();

        private VisualElement _overlay;
        private PolylineOverlayElement _pathElement;
        private PolylineOverlayElement _handleElement;

        public void Bind(VisualElement overlay, PolylineOverlayElement pathElement, PolylineOverlayElement handleElement)
        {
            _overlay = overlay;
            _pathElement = pathElement;
            _handleElement = handleElement;
        }

        public void Clear()
        {
            _pathElement?.ClearSegments();
            _handleElement?.ClearSegments();

            for (int index = 0; index < _nodeElements.Count; index++)
            {
                _nodeElements[index]?.RemoveFromHierarchy();
            }

            for (int index = 0; index < _handleElements.Count; index++)
            {
                _handleElements[index]?.RemoveFromHierarchy();
            }

            _nodeElements.Clear();
            _handleElements.Clear();
        }

        public void SetSession(PathEditSession session)
        {
            Apply(BuildVisual(session));
        }

        public static PathOverlayVisual BuildVisual(PathEditSession session)
        {
            if (session == null || session.Subpaths == null || session.Subpaths.Count == 0)
            {
                return new PathOverlayVisual();
            }

            List<CanvasLineSegment> pathSegments = new();
            List<CanvasLineSegment> handleSegments = new();
            List<PathOverlayNode> nodes = new();
            List<PathOverlayHandle> handles = new();

            for (int subpathIndex = 0; subpathIndex < session.Subpaths.Count; subpathIndex++)
            {
                PathSubpathView subpath = session.Subpaths[subpathIndex];
                if (subpath == null)
                {
                    continue;
                }

                pathSegments.AddRange(subpath.Segments);
                for (int nodeIndex = 0; nodeIndex < subpath.Nodes.Count; nodeIndex++)
                {
                    PathNodeView node = subpath.Nodes[nodeIndex];
                    PathNodeRef nodeRef = new(subpathIndex, nodeIndex);
                    bool isSelected = session.Selection.IsSelected(nodeRef);
                    bool isActive = session.Selection.ActiveNode.HasValue && session.Selection.ActiveNode.Value.Equals(nodeRef);
                    nodes.Add(new PathOverlayNode(nodeRef, node.Position, isSelected, isActive));

                    bool shouldShowIncomingHandle = ShouldShowIncomingHandle(session, nodeRef);
                    bool shouldShowOutgoingHandle = ShouldShowOutgoingHandle(session, nodeRef);

                    if (node.HasInHandle && shouldShowIncomingHandle)
                    {
                        PathHandleRef handleRef = new(nodeRef, PathHandleSlot.In);
                        handleSegments.Add(new CanvasLineSegment(node.Position, node.InHandle));
                        handles.Add(new PathOverlayHandle(handleRef, node.InHandle, session.Selection.HasActiveHandle(handleRef)));
                    }

                    if (node.HasOutHandle && shouldShowOutgoingHandle)
                    {
                        PathHandleRef handleRef = new(nodeRef, PathHandleSlot.Out);
                        handleSegments.Add(new CanvasLineSegment(node.Position, node.OutHandle));
                        handles.Add(new PathOverlayHandle(handleRef, node.OutHandle, session.Selection.HasActiveHandle(handleRef)));
                    }
                }
            }

            return new PathOverlayVisual
            {
                PathSegments = pathSegments,
                HandleSegments = handleSegments,
                Nodes = nodes,
                Handles = handles
            };
        }

        private static bool ShouldShowIncomingHandle(PathEditSession session, PathNodeRef nodeRef)
        {
            if (!session.Selection.ActiveNode.HasValue)
            {
                return false;
            }

            PathNodeRef activeNode = session.Selection.ActiveNode.Value;
            return activeNode.SubpathIndex == nodeRef.SubpathIndex &&
                   (activeNode.NodeIndex == nodeRef.NodeIndex ||
                    ResolveNextNodeIndex(session, activeNode) == nodeRef.NodeIndex);
        }

        private static bool ShouldShowOutgoingHandle(PathEditSession session, PathNodeRef nodeRef)
        {
            if (!session.Selection.ActiveNode.HasValue)
            {
                return false;
            }

            PathNodeRef activeNode = session.Selection.ActiveNode.Value;
            return activeNode.SubpathIndex == nodeRef.SubpathIndex &&
                   (activeNode.NodeIndex == nodeRef.NodeIndex ||
                    ResolvePreviousNodeIndex(session, activeNode) == nodeRef.NodeIndex);
        }

        private static int ResolveNextNodeIndex(PathEditSession session, PathNodeRef activeNode)
        {
            PathSubpathView subpath = session.Subpaths[activeNode.SubpathIndex];
            if (subpath == null || subpath.Nodes.Count == 0)
            {
                return -1;
            }

            if (subpath.IsClosed)
            {
                return (activeNode.NodeIndex + 1) % subpath.Nodes.Count;
            }

            int nextIndex = activeNode.NodeIndex + 1;
            return nextIndex < subpath.Nodes.Count ? nextIndex : -1;
        }

        private static int ResolvePreviousNodeIndex(PathEditSession session, PathNodeRef activeNode)
        {
            PathSubpathView subpath = session.Subpaths[activeNode.SubpathIndex];
            if (subpath == null || subpath.Nodes.Count == 0)
            {
                return -1;
            }

            if (subpath.IsClosed)
            {
                return (activeNode.NodeIndex - 1 + subpath.Nodes.Count) % subpath.Nodes.Count;
            }

            return activeNode.NodeIndex > 0
                ? activeNode.NodeIndex - 1
                : -1;
        }

        private void Apply(PathOverlayVisual visual)
        {
            Clear();
            if (_overlay == null || visual == null)
            {
                return;
            }

            _pathElement?.SetSegmentsFromStyle(visual.PathSegments, 1.75f);
            _handleElement?.SetSegmentsFromStyle(visual.HandleSegments);

            for (int index = 0; index < visual.Nodes.Count; index++)
            {
                VisualElement nodeElement = CreateMarker(
                    visual.Nodes[index].Position,
                    8f,
                    round: true);
                nodeElement.AddToClassList(OverlayClassName.PATH_ANCHOR);
                if (visual.Nodes[index].IsActive)
                    nodeElement.AddToClassList(OverlayClassName.PATH_ANCHOR_ACTIVE);

                _overlay.Add(nodeElement);
                _nodeElements.Add(nodeElement);
            }

            for (int index = 0; index < visual.Handles.Count; index++)
            {
                bool isActive = visual.Handles[index].IsActive;
                VisualElement handleElement = CreateMarker(
                    visual.Handles[index].Position,
                    6f,
                    round: false);
                handleElement.AddToClassList(OverlayClassName.BEZIER_HANDLE);
                if (isActive)
                    handleElement.AddToClassList(OverlayClassName.BEZIER_HANDLE_ACTIVE);
                _overlay.Add(handleElement);
                _handleElements.Add(handleElement);
            }
        }

        private static VisualElement CreateMarker(Vector2 center, float size, bool round)
        {
            VisualElement element = new();
            element.pickingMode = PickingMode.Ignore;
            element.style.position = Position.Absolute;
            element.style.left = center.x - (size * 0.5f);
            element.style.top = center.y - (size * 0.5f);
            element.style.width = size;
            element.style.height = size;
            if (round)
            {
                element.style.borderTopLeftRadius = size * 0.5f;
                element.style.borderTopRightRadius = size * 0.5f;
                element.style.borderBottomLeftRadius = size * 0.5f;
                element.style.borderBottomRightRadius = size * 0.5f;
            }
            return element;
        }
    }
}
