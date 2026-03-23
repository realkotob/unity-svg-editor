using UnityEngine;

namespace SvgEditor.Core.Svg.PathEditing
{
    internal readonly struct PathNode
    {
        public PathNode(
            char command,
            Vector2 position,
            Vector2 control0 = default,
            Vector2 control1 = default,
            PathHandleMode handleMode = PathHandleMode.None)
        {
            Command = char.ToUpperInvariant(command);
            Position = position;
            Control0 = control0;
            Control1 = control1;
            HandleMode = handleMode;
        }

        public char Command { get; }
        public Vector2 Position { get; }
        public Vector2 Control0 { get; }
        public Vector2 Control1 { get; }
        public PathHandleMode HandleMode { get; }
    }
}
