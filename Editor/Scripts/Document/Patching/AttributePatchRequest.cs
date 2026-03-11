namespace UnitySvgEditor.Editor
{
    internal sealed class AttributePatchRequest
    {
        public string TargetKey { get; set; } = SvgDocumentTargets.RootTargetKey;
        public string Fill { get; set; }
        public string Stroke { get; set; }
        public string StrokeWidth { get; set; }
        public string Opacity { get; set; }
        public string FillOpacity { get; set; }
        public string StrokeOpacity { get; set; }
        public string StrokeLinecap { get; set; }
        public string StrokeLinejoin { get; set; }
        public string StrokeDasharray { get; set; }
        public string CornerRadiusX { get; set; }
        public string CornerRadiusY { get; set; }
        public string Transform { get; set; }
        public string Display { get; set; }
    }
}
