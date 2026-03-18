namespace SvgEditor.Workspace.AssetLibrary.Grid
{
    internal readonly struct GridLayoutMetrics
    {
        public static readonly GridLayoutMetrics Default = new(76f, 84f, 28f);
        public static readonly GridLayoutMetrics CompactPreview = new(64f, 72f, 24f);

        public GridLayoutMetrics(float cellWidth, float cellHeight, float headerHeight)
        {
            CellWidth = cellWidth;
            CellHeight = cellHeight;
            HeaderHeight = headerHeight;
        }

        public float CellWidth { get; }
        public float CellHeight { get; }
        public float HeaderHeight { get; }
    }
}
