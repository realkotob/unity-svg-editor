using System.Collections.Generic;

namespace SvgEditor.Core.Svg.PathEditing
{
    internal sealed class PathData
    {
        public List<PathSubpath> Subpaths { get; } = new();
        public List<char> UnsupportedCommands { get; } = new();
        public bool IsMalformed { get; private set; }
        public string ParseError { get; private set; } = string.Empty;

        public bool HasUnsupportedCommands => UnsupportedCommands.Count > 0;

        public void AddUnsupportedCommand(char command)
        {
            char normalized = char.ToUpperInvariant(command);
            if (!UnsupportedCommands.Contains(normalized))
            {
                UnsupportedCommands.Add(normalized);
            }
        }

        public void MarkMalformed(string error)
        {
            IsMalformed = true;
            ParseError = string.IsNullOrWhiteSpace(error)
                ? "Path data is malformed."
                : error.Trim();
        }
    }
}
