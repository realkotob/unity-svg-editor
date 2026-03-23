using UnityEngine;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Svg.PathEditing;

namespace SvgEditor.UI.Canvas
{
    internal sealed class PathEditInteractionController
    {
        private const float NodeHitRadius = 6f;
        private const float HandleHitRadius = 6f;
        private const float SegmentHitDistance = 4f;

        private readonly ICanvasPointerDragHost _host;
        private readonly SceneProjector _sceneProjector;
        private readonly ToolController _toolController;
        private readonly OverlayController _overlayController;
        private readonly PathMutationService _mutationService = new();
        private PathDragState _dragState;

        public PathEditInteractionController(
            ICanvasPointerDragHost host,
            SceneProjector sceneProjector,
            ToolController toolController,
            OverlayController overlayController)
        {
            _host = host;
            _sceneProjector = sceneProjector;
            _toolController = toolController;
            _overlayController = overlayController;
        }

        public bool IsDragging => _dragState != null && _dragState.IsActive;

        public bool TryHitPathEditTarget(Vector2 viewportPoint, out PathHit hit)
        {
            hit = default;
            return _toolController.ActiveTool == ToolKind.PathEdit &&
                   TryGetSession(out PathEditSession session) &&
                   session.TryHit(
                       viewportPoint,
                       NodeHitRadius,
                       HandleHitRadius,
                       SegmentHitDistance,
                       out hit);
        }

        public bool TryHandlePointerDown(Vector2 viewportPoint)
        {
            if (_toolController.ActiveTool != ToolKind.PathEdit ||
                !TryGetSession(out PathEditSession session))
            {
                return false;
            }

            if (!session.TryHit(
                    viewportPoint,
                    NodeHitRadius,
                    HandleHitRadius,
                    SegmentHitDistance,
                    out PathHit hit))
            {
                session.Selection.Clear();
                _overlayController.SetPathEditSession(session);
                return true;
            }

            switch (hit.Kind)
            {
                case PathHitKind.Handle:
                    session.Selection.SelectHandle(hit.Handle);
                    BeginDrag(session, hit.Handle);
                    _overlayController.SetPathEditSession(session);
                    return true;

                case PathHitKind.Node:
                    session.Selection.SelectNode(hit.Node);
                    BeginDrag(session, hit.Node);
                    _overlayController.SetPathEditSession(session);
                    return true;

                default:
                    session.Selection.Clear();
                    _overlayController.SetPathEditSession(session);
                    return true;
            }
        }

        public bool UpdateDrag(Vector2 viewportPoint)
        {
            if (!IsDragging || !TryGetSession(out PathEditSession session))
            {
                return false;
            }

            if (!TryResolveLocalPoint(session, viewportPoint, out Vector2 localPoint))
            {
                return false;
            }

            PathData workingPathData = PathEditSession.ClonePathData(_dragState.StartPathData);
            if (!TryApplyDrag(workingPathData, _dragState, localPoint) ||
                !session.TrySetPathData(workingPathData, out _))
            {
                return false;
            }

            _dragState.HasChanged = true;
            _overlayController.SetPathEditSession(session);
            TryRefreshTransientPreview(session.PathData);
            return true;
        }

        public bool CommitDrag()
        {
            if (!IsDragging || !TryGetSession(out PathEditSession session))
            {
                return false;
            }

            PathDragState dragState = _dragState;
            bool hasChanged = _dragState.HasChanged;
            if (!hasChanged)
            {
                _dragState = null;
                _overlayController.SetPathEditSession(session);
                return true;
            }

            if (!TryBuildMutation(session.PathData, out MutationResult mutation))
            {
                RestoreSessionState(session, dragState);
                _host.UpdateSourceStatus(BuildCommitFailureStatus(mutation.Error));
                _host.RefreshLivePreview(keepExistingPreviewOnFailure: true);
                _host.RefreshInspector();
                return false;
            }

            _dragState = null;
            _host.ApplyUpdatedSource(mutation.UpdatedSourceText, "Updated <path> path.");
            return true;
        }

        public bool CancelDrag()
        {
            if (!IsDragging || !TryGetSession(out PathEditSession session))
            {
                return false;
            }

            PathData restoredPathData = PathEditSession.ClonePathData(_dragState.StartPathData);
            _dragState = null;
            session.TrySetPathData(restoredPathData, out _);
            _overlayController.SetPathEditSession(session);
            _host.RefreshLivePreview(keepExistingPreviewOnFailure: true);
            _host.RefreshInspector();
            return true;
        }

        public bool HandleEscapeKey()
        {
            if (IsDragging)
            {
                return CancelDrag();
            }

            if (_toolController.ActiveTool != ToolKind.PathEdit || !_overlayController.HasPathEditSession)
            {
                return false;
            }

            _overlayController.ClearPathEditSession();
            _toolController.SetActiveTool(ToolKind.Move);
            _host.UpdateCanvasVisualState();
            return true;
        }

        public void AbandonDrag()
        {
            _dragState = null;
        }

        private void BeginDrag(PathEditSession session, PathHandleRef handle)
        {
            _dragState = new PathDragState(
                PathEditSession.ClonePathData(session.PathData),
                handle: handle,
                node: null);
        }

        private void BeginDrag(PathEditSession session, PathNodeRef node)
        {
            _dragState = new PathDragState(
                PathEditSession.ClonePathData(session.PathData),
                handle: null,
                node: node);
        }

        private bool TryGetSession(out PathEditSession session)
        {
            session = _overlayController.CurrentPathEditSession;
            return session != null;
        }

        private bool TryResolveLocalPoint(PathEditSession session, Vector2 viewportPoint, out Vector2 localPoint)
        {
            localPoint = default;
            if (session == null ||
                _host.PreviewSnapshot == null ||
                !_sceneProjector.TryViewportPointToScenePoint(_host.PreviewSnapshot, viewportPoint, out Vector2 scenePoint))
            {
                return false;
            }

            localPoint = session.WorldTransform.Inverse().MultiplyPoint(scenePoint);
            return true;
        }

        private bool TryRefreshTransientPreview(PathData pathData)
        {
            if (!TryBuildMutation(pathData, out MutationResult mutation))
            {
                return false;
            }

            if (!_host.TryRefreshTransientPreview(mutation.UpdatedDocumentModel))
            {
                return false;
            }

            _host.RefreshInspector(mutation.UpdatedDocumentModel);
            return true;
        }

        private bool TryBuildMutation(PathData pathData, out MutationResult mutation)
        {
            mutation = new MutationResult(null, string.Empty, "Path edit mutation is unavailable.");
            return _host.CurrentDocument?.DocumentModel != null &&
                   _overlayController.CurrentPathEditSession != null &&
                   _mutationService.TryApplyPathData(
                       _host.CurrentDocument.DocumentModel,
                       _overlayController.CurrentPathEditSession.ElementKey,
                       pathData,
                       out mutation);
        }

        private void RestoreSessionState(PathEditSession session, PathDragState dragState)
        {
            if (session == null || dragState?.StartPathData == null)
            {
                _dragState = null;
                return;
            }

            PathData restoredPathData = PathEditSession.ClonePathData(dragState.StartPathData);
            session.TrySetPathData(restoredPathData, out _);
            _overlayController.SetPathEditSession(session);
            _dragState = null;
        }

        private static string BuildCommitFailureStatus(string error)
        {
            return string.IsNullOrWhiteSpace(error)
                ? "Path edit commit failed: transient model state is unavailable."
                : $"Path edit commit failed: {error}";
        }

        private static bool TryApplyDrag(PathData pathData, PathDragState dragState, Vector2 localPoint)
        {
            if (pathData == null || dragState == null)
            {
                return false;
            }

            if (dragState.ActiveHandle.HasValue)
            {
                return TryMoveHandle(pathData, dragState.ActiveHandle.Value, localPoint);
            }

            return dragState.ActiveNode.HasValue &&
                   TryMoveNode(pathData, dragState.ActiveNode.Value, localPoint);
        }

        private static bool TryMoveNode(PathData pathData, PathNodeRef nodeRef, Vector2 newPosition)
        {
            if (!TryGetSubpath(pathData, nodeRef.SubpathIndex, out PathSubpath subpath))
            {
                return false;
            }

            Vector2 currentPosition = GetAnchorPosition(subpath, nodeRef.NodeIndex);
            Vector2 delta = newPosition - currentPosition;

            if (TryGetIncomingSegmentIndex(subpath, nodeRef.NodeIndex, out int previousSegmentIndex))
            {
                subpath.Nodes[previousSegmentIndex] = TranslateIncomingHandle(subpath.Nodes[previousSegmentIndex], delta);
                subpath.Nodes[previousSegmentIndex] = WithPosition(subpath.Nodes[previousSegmentIndex], newPosition);
            }

            if (nodeRef.NodeIndex == 0)
            {
                subpath.Start = newPosition;
            }

            if (nodeRef.NodeIndex < subpath.Nodes.Count)
            {
                PathNode outgoingSegment = TranslateOutgoingHandle(subpath.Nodes[nodeRef.NodeIndex], delta);
                subpath.Nodes[nodeRef.NodeIndex] = NormalizeAxisLockedSegment(outgoingSegment);
            }

            return true;
        }

        private static bool TryMoveHandle(PathData pathData, PathHandleRef handleRef, Vector2 newPosition)
        {
            if (!TryGetSubpath(pathData, handleRef.Node.SubpathIndex, out PathSubpath subpath))
            {
                return false;
            }

            int nodeIndex = handleRef.Node.NodeIndex;
            if (handleRef.Slot == PathHandleSlot.In)
            {
                if (!TryGetIncomingSegmentIndex(subpath, nodeIndex, out int incomingSegmentIndex))
                {
                    return false;
                }

                subpath.Nodes[incomingSegmentIndex] = MoveIncomingHandle(subpath, incomingSegmentIndex, newPosition);
                if (nodeIndex < subpath.Nodes.Count)
                {
                    subpath.Nodes[nodeIndex] = NormalizeBrokenSmoothSegment(subpath.Nodes[nodeIndex]);
                }

                return true;
            }

            if (nodeIndex < 0 || nodeIndex >= subpath.Nodes.Count)
            {
                return false;
            }

            subpath.Nodes[nodeIndex] = MoveOutgoingHandle(subpath, nodeIndex, newPosition);
            return true;
        }

        private static PathNode MoveIncomingHandle(PathSubpath subpath, int segmentIndex, Vector2 newPosition)
        {
            PathNode node = subpath.Nodes[segmentIndex];
            Vector2 startPosition = GetAnchorPosition(subpath, segmentIndex);
            return char.ToUpperInvariant(node.Command) switch
            {
                'C' => new PathNode('C', node.Position, node.Control0, newPosition, node.HandleMode),
                'S' => new PathNode('C', node.Position, node.Control0, newPosition, PathHandleMode.Free),
                'Q' or 'T' => PromoteQuadraticSegmentToFreeCubic(startPosition, node, incomingHandle: newPosition),
                _ => node
            };
        }

        private static PathNode NormalizeBrokenSmoothSegment(PathNode node)
        {
            return char.ToUpperInvariant(node.Command) switch
            {
                'S' => new PathNode('C', node.Position, node.Control0, node.Control1, PathHandleMode.Free),
                'T' => new PathNode('Q', node.Position, node.Control0, default, PathHandleMode.Free),
                _ => node
            };
        }

        private static PathNode MoveOutgoingHandle(PathSubpath subpath, int segmentIndex, Vector2 newPosition)
        {
            PathNode node = subpath.Nodes[segmentIndex];
            Vector2 startPosition = GetAnchorPosition(subpath, segmentIndex);
            return char.ToUpperInvariant(node.Command) switch
            {
                'C' => new PathNode('C', node.Position, newPosition, node.Control1, node.HandleMode),
                'S' => new PathNode('C', node.Position, newPosition, node.Control1, PathHandleMode.Free),
                'Q' or 'T' => PromoteQuadraticSegmentToFreeCubic(startPosition, node, outgoingHandle: newPosition),
                _ => node
            };
        }

        private static PathNode PromoteQuadraticSegmentToFreeCubic(
            Vector2 startPosition,
            PathNode node,
            Vector2? outgoingHandle = null,
            Vector2? incomingHandle = null)
        {
            Vector2 quadraticControl = node.Control0;
            Vector2 cubicOutgoingHandle = startPosition + ((quadraticControl - startPosition) * (2f / 3f));
            Vector2 cubicIncomingHandle = node.Position + ((quadraticControl - node.Position) * (2f / 3f));

            return new PathNode(
                'C',
                node.Position,
                outgoingHandle ?? cubicOutgoingHandle,
                incomingHandle ?? cubicIncomingHandle,
                PathHandleMode.Free);
        }

        private static PathNode TranslateIncomingHandle(PathNode node, Vector2 delta)
        {
            return char.ToUpperInvariant(node.Command) switch
            {
                'C' => new PathNode('C', node.Position, node.Control0, node.Control1 + delta, node.HandleMode),
                'S' => new PathNode('S', node.Position, node.Control0, node.Control1 + delta, node.HandleMode),
                'Q' => new PathNode('Q', node.Position, node.Control0 + delta, default, node.HandleMode),
                'T' => new PathNode('T', node.Position, node.Control0 + delta, default, node.HandleMode),
                _ => node
            };
        }

        private static PathNode TranslateOutgoingHandle(PathNode node, Vector2 delta)
        {
            return char.ToUpperInvariant(node.Command) switch
            {
                'C' => new PathNode('C', node.Position, node.Control0 + delta, node.Control1, node.HandleMode),
                'S' => new PathNode('S', node.Position, node.Control0 + delta, node.Control1, node.HandleMode),
                'Q' => new PathNode('Q', node.Position, node.Control0 + delta, default, node.HandleMode),
                'T' => new PathNode('T', node.Position, node.Control0 + delta, default, node.HandleMode),
                _ => node
            };
        }

        private static PathNode NormalizeAxisLockedSegment(PathNode node)
        {
            return char.ToUpperInvariant(node.Command) switch
            {
                'H' or 'V' => new PathNode('L', node.Position, node.Control0, node.Control1, node.HandleMode),
                _ => node
            };
        }

        private static PathNode WithPosition(PathNode node, Vector2 position)
        {
            char command = char.ToUpperInvariant(node.Command) switch
            {
                'H' or 'V' => 'L',
                _ => char.ToUpperInvariant(node.Command)
            };

            return new PathNode(command, position, node.Control0, node.Control1, node.HandleMode);
        }

        private static bool TryGetSubpath(PathData pathData, int subpathIndex, out PathSubpath subpath)
        {
            subpath = null;
            if (pathData == null || subpathIndex < 0 || subpathIndex >= pathData.Subpaths.Count)
            {
                return false;
            }

            subpath = pathData.Subpaths[subpathIndex];
            return subpath != null;
        }

        private static bool TryGetIncomingSegmentIndex(PathSubpath subpath, int nodeIndex, out int segmentIndex)
        {
            segmentIndex = -1;
            if (subpath == null || subpath.Nodes.Count == 0)
            {
                return false;
            }

            if (nodeIndex > 0)
            {
                segmentIndex = nodeIndex - 1;
                return segmentIndex < subpath.Nodes.Count;
            }

            if (!subpath.IsClosed)
            {
                return false;
            }

            segmentIndex = subpath.Nodes.Count - 1;
            return true;
        }

        private static Vector2 GetAnchorPosition(PathSubpath subpath, int nodeIndex)
        {
            return nodeIndex == 0
                ? subpath.Start
                : subpath.Nodes[nodeIndex - 1].Position;
        }

        private sealed class PathDragState
        {
            public PathDragState(PathData startPathData, PathHandleRef? handle, PathNodeRef? node)
            {
                StartPathData = startPathData;
                ActiveHandle = handle;
                ActiveNode = node;
            }

            public PathData StartPathData { get; }
            public PathHandleRef? ActiveHandle { get; }
            public PathNodeRef? ActiveNode { get; }
            public bool HasChanged { get; set; }
            public bool IsActive => StartPathData != null && (ActiveHandle.HasValue || ActiveNode.HasValue);
        }
    }
}
