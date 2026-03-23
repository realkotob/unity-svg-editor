namespace SvgEditor.UI.Canvas
{
    internal enum PathEditEntryResultKind
    {
        Ignored,
        Entered,
        BlockedUnsupportedPathData,
        BlockedMalformedPathData,
        BlockedUnavailable
    }

    internal readonly struct PathEditEntryResult
    {
        public PathEditEntryResult(PathEditEntryResultKind kind, string statusMessage, PathEditSession session)
        {
            Kind = kind;
            StatusMessage = statusMessage ?? string.Empty;
            Session = session;
        }

        public PathEditEntryResultKind Kind { get; }
        public string StatusMessage { get; }
        public PathEditSession Session { get; }
    }
}
