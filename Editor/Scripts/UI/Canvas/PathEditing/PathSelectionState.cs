using System;
using System.Collections.Generic;

namespace SvgEditor.UI.Canvas
{
    internal enum PathHandleSlot
    {
        In,
        Out
    }

    internal readonly struct PathNodeRef : IEquatable<PathNodeRef>, IComparable<PathNodeRef>
    {
        public PathNodeRef(int subpathIndex, int nodeIndex)
        {
            SubpathIndex = subpathIndex;
            NodeIndex = nodeIndex;
        }

        public int SubpathIndex { get; }
        public int NodeIndex { get; }

        public int CompareTo(PathNodeRef other)
        {
            int subpathCompare = SubpathIndex.CompareTo(other.SubpathIndex);
            return subpathCompare != 0
                ? subpathCompare
                : NodeIndex.CompareTo(other.NodeIndex);
        }

        public bool Equals(PathNodeRef other)
        {
            return SubpathIndex == other.SubpathIndex && NodeIndex == other.NodeIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is PathNodeRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SubpathIndex * 397) ^ NodeIndex;
            }
        }
    }

    internal readonly struct PathHandleRef : IEquatable<PathHandleRef>
    {
        public PathHandleRef(PathNodeRef node, PathHandleSlot slot)
        {
            Node = node;
            Slot = slot;
        }

        public PathNodeRef Node { get; }
        public PathHandleSlot Slot { get; }

        public bool Equals(PathHandleRef other)
        {
            return Node.Equals(other.Node) && Slot == other.Slot;
        }

        public override bool Equals(object obj)
        {
            return obj is PathHandleRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Node.GetHashCode() * 397) ^ (int)Slot;
            }
        }
    }

    internal sealed class PathSelectionState
    {
        private readonly List<PathNodeRef> _nodes = new();

        public IReadOnlyList<PathNodeRef> Nodes => _nodes;
        public PathNodeRef? ActiveNode { get; private set; }
        public PathHandleRef? ActiveHandle { get; private set; }

        public void Clear()
        {
            _nodes.Clear();
            ActiveNode = null;
            ActiveHandle = null;
        }

        public void SelectNode(PathNodeRef node, bool additive = false)
        {
            if (!additive)
            {
                _nodes.Clear();
            }

            InsertNode(node);
            ActiveNode = node;
            ActiveHandle = null;
        }

        public void SelectHandle(PathHandleRef handle, bool additive = false)
        {
            if (!additive)
            {
                _nodes.Clear();
            }

            InsertNode(handle.Node);
            ActiveNode = handle.Node;
            ActiveHandle = handle;
        }

        public bool IsSelected(PathNodeRef node)
        {
            return _nodes.BinarySearch(node) >= 0;
        }

        public bool HasActiveHandle(PathHandleRef handle)
        {
            return ActiveHandle.HasValue && ActiveHandle.Value.Equals(handle);
        }

        private void InsertNode(PathNodeRef node)
        {
            int index = _nodes.BinarySearch(node);
            if (index >= 0)
            {
                return;
            }

            _nodes.Insert(~index, node);
        }
    }
}
