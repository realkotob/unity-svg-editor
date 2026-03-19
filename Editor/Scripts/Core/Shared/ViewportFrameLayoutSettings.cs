namespace SvgEditor.Core.Shared
{
    internal readonly struct ViewportFrameLayoutSettings
    {
        public ViewportFrameLayoutSettings(float margin, float padding, float headerHeight)
        {
            Margin = margin;
            Padding = padding;
            HeaderHeight = headerHeight;
        }

        public float Margin { get; }
        public float Padding { get; }
        public float HeaderHeight { get; }
    }
}
