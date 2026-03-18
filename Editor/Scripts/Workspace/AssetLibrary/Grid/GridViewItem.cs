using SvgEditor.Shared;

namespace SvgEditor.Workspace.AssetLibrary.Grid
{
    internal sealed class GridViewItem
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string SortKey { get; set; } = string.Empty;
        public string GroupKey { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;
        public bool ActionIsDestructive { get; set; }
        public bool ShowStatusMark { get; set; }
        public object UserData { get; set; }
        public PreviewImageSource PreviewSource { get; set; } = PreviewImageSource.None;
    }
}
