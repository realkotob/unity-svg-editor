using UnityEngine;

namespace SvgEditor.UI.Canvas
{
    internal readonly struct PathNodeView
    {
        public PathNodeView(
            Vector2 position,
            Vector2 inHandle,
            bool hasInHandle,
            Vector2 outHandle,
            bool hasOutHandle)
        {
            Position = position;
            InHandle = inHandle;
            HasInHandle = hasInHandle;
            OutHandle = outHandle;
            HasOutHandle = hasOutHandle;
        }

        public Vector2 Position { get; }
        public Vector2 InHandle { get; }
        public bool HasInHandle { get; }
        public Vector2 OutHandle { get; }
        public bool HasOutHandle { get; }
    }
}
