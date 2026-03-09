namespace UnitySvgEditor.Editor
{
    internal sealed class StructureNode
    {
        public string Key { get; set; } = string.Empty;
        public string TargetKey { get; set; } = string.Empty;
        public string TagName { get; set; } = string.Empty;
        public int Depth { get; set; }
        public string ParentKey { get; set; } = string.Empty;
        public string LayerKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string TreeLabel { get; set; } = string.Empty;

        public bool CanUseAsTarget => !string.IsNullOrWhiteSpace(TargetKey);
    }
}
