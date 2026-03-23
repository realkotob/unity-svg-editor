using System.Collections.Generic;
using System.Collections.ObjectModel;
using SvgEditor.Core.Shared;

namespace SvgEditor.UI.Canvas
{
    internal sealed class PathSubpathView
    {
        private readonly PathNodeView[] _nodes;
        private readonly CanvasLineSegment[] _segments;
        private readonly ReadOnlyCollection<PathNodeView> _nodeView;
        private readonly ReadOnlyCollection<CanvasLineSegment> _segmentView;

        public PathSubpathView(
            IEnumerable<PathNodeView> nodes,
            IEnumerable<CanvasLineSegment> segments,
            bool isClosed)
        {
            _nodes = CopyNodes(nodes);
            _segments = CopySegments(segments);
            _nodeView = System.Array.AsReadOnly(_nodes);
            _segmentView = System.Array.AsReadOnly(_segments);
            IsClosed = isClosed;
        }

        public IReadOnlyList<PathNodeView> Nodes => _nodeView;
        public IReadOnlyList<CanvasLineSegment> Segments => _segmentView;
        public bool IsClosed { get; }

        public PathSubpathView Clone()
        {
            return new PathSubpathView(_nodes, _segments, IsClosed);
        }

        private static PathNodeView[] CopyNodes(IEnumerable<PathNodeView> nodes)
        {
            if (nodes == null)
            {
                return System.Array.Empty<PathNodeView>();
            }

            return nodes is PathNodeView[] array
                ? (PathNodeView[])array.Clone()
                : new List<PathNodeView>(nodes).ToArray();
        }

        private static CanvasLineSegment[] CopySegments(IEnumerable<CanvasLineSegment> segments)
        {
            if (segments == null)
            {
                return System.Array.Empty<CanvasLineSegment>();
            }

            return segments is CanvasLineSegment[] array
                ? (CanvasLineSegment[])array.Clone()
                : new List<CanvasLineSegment>(segments).ToArray();
        }
    }
}
