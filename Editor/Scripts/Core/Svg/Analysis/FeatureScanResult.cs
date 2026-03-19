namespace SvgEditor.Core.Svg.Analysis
{
    internal sealed class FeatureScanResult
    {
        public int ElementCount { get; set; }
        public int PathElementCount { get; set; }
        public bool HasLinearGradient { get; set; }
        public bool HasRadialGradient { get; set; }
        public bool HasClipPath { get; set; }
        public bool HasMask { get; set; }
        public bool HasFilter { get; set; }
        public bool HasText { get; set; }
        public bool HasTspan { get; set; }
        public bool HasTextPath { get; set; }
        public bool HasImage { get; set; }
        public bool HasUse { get; set; }
        public bool HasStyleTag { get; set; }
        public bool HasTransformAttribute { get; set; }
        public bool IsValidXml { get; set; }
        public string ValidationMessage { get; set; } = string.Empty;
    }
}
