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
