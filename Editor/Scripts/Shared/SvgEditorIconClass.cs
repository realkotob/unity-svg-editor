using Core.UI.Foundation;

namespace UnitySvgEditor.Editor
{
    internal enum IconKind
    {
        Square,
        Circle,
        FileText,
        Minus,
        Pen,
        Folder,
        File
    }

    internal static class SvgEditorIconClass
    {
        public const string HIERARCHY_EXPANDER = IconClass.CHEVRON_RIGHT;
        public const string HIERARCHY_SQUARE = IconClass.SQUARE;
        public const string HIERARCHY_CIRCLE = IconClass.CIRCLE;
        public const string HIERARCHY_FILE_TEXT = IconClass.FILE_TEXT;
        public const string HIERARCHY_MINUS = IconClass.MINUS;
        public const string HIERARCHY_PEN = IconClass.PEN;
        public const string HIERARCHY_FOLDER = IconClass.FOLDER;
        public const string HIERARCHY_FILE = IconClass.FILE;
        public const string POSITION_ALIGN_LEFT = IconClass.ALIGN_HORIZONTAL_LEFT;
        public const string POSITION_ALIGN_CENTER = IconClass.ALIGN_HORIZONTAL_CENTER;
        public const string POSITION_ALIGN_RIGHT = IconClass.ALIGN_HORIZONTAL_RIGHT;
        public const string POSITION_ALIGN_TOP = IconClass.ALIGN_VERTICAL_TOP;
        public const string POSITION_ALIGN_MIDDLE = IconClass.ALIGN_VERTICAL_CENTER;
        public const string POSITION_ALIGN_BOTTOM = IconClass.ALIGN_VERTICAL_BOTTOM;
        public const string POSITION_ROTATE_CLOCKWISE_90 = "icon-refresh-ccw";
        public const string POSITION_FLIP_HORIZONTAL = IconClass.FLIP_HORIZONTAL;
        public const string POSITION_FLIP_VERTICAL = IconClass.FLIP_VERTICAL;

        internal static string ResolveHierarchyIcon(IconKind iconKind)
        {
            return iconKind switch
            {
                IconKind.Square => HIERARCHY_SQUARE,
                IconKind.Circle => HIERARCHY_CIRCLE,
                IconKind.FileText => HIERARCHY_FILE_TEXT,
                IconKind.Minus => HIERARCHY_MINUS,
                IconKind.Pen => HIERARCHY_PEN,
                IconKind.Folder => HIERARCHY_FOLDER,
                _ => HIERARCHY_FILE
            };
        }
    }
}
