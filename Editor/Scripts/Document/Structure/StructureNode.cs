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
        public string MaskReferenceId { get; set; } = string.Empty;
        public string ClipPathReferenceId { get; set; } = string.Empty;
        public bool IsDefinitionProxy { get; set; }
        public string SourceElementKey { get; set; } = string.Empty;
        public string DefinitionElementKey { get; set; } = string.Empty;
        public string DefinitionReferenceId { get; set; } = string.Empty;
        public CanvasDefinitionOverlayKind DefinitionProxyKind { get; set; }

        public bool CanUseAsTarget => !string.IsNullOrWhiteSpace(TargetKey);
        public bool HasMaskReference => !string.IsNullOrWhiteSpace(MaskReferenceId);
        public bool HasClipPathReference => !string.IsNullOrWhiteSpace(ClipPathReferenceId);
    }
}
