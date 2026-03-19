namespace SvgEditor.Core.Svg.Hierarchy
{
    internal sealed class LayerSummary
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public int ElementCount { get; set; }
    }
}
