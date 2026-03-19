namespace SvgEditor.UI.AssetLibrary.Model
{
    internal sealed class AssetEntry
    {
        public string DisplayName { get; set; } = string.Empty;
        public string AssetPath { get; set; } = string.Empty;
        public string Library { get; set; } = string.Empty;
        public string GroupKey { get; set; } = string.Empty;
        public bool IsDeveloperFixture { get; set; }
    }
}
