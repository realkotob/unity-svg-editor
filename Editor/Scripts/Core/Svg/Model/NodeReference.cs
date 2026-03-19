namespace SvgEditor.Core.Svg.Model
{
    internal sealed class NodeReference
    {
        public string AttributeName { get; set; } = string.Empty;
        public string RawValue { get; set; } = string.Empty;
        public string FragmentId { get; set; } = string.Empty;
    }
}
