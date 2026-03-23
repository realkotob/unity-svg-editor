using System.Collections.Generic;
using UnityEngine;

namespace SvgEditor.Core.Svg.PathEditing
{
    internal sealed class PathSubpath
    {
        public PathSubpath()
        {
        }

        public PathSubpath(Vector2 start, IEnumerable<PathNode> nodes, bool isClosed = false)
        {
            Start = start;
            if (nodes != null)
            {
                Nodes.AddRange(nodes);
            }

            IsClosed = isClosed;
        }

        public Vector2 Start { get; set; }
        public List<PathNode> Nodes { get; } = new();
        public bool IsClosed { get; set; }
    }
}
