using Core.UI.Foundation;

namespace SvgEditor.Shared
{
    internal enum IconKind
    {
        Square,
        Circle,
        FileText,
        Minus,
        Pen,
        Mask,
        Folder,
        File
    }

    internal static class SvgEditorIconClass
    {
        public const string RESOURCE_CIRCLE = "Icons/lucide/circle";
        public const string RESOURCE_FILE_TEXT = "Icons/lucide/file-text";
        public const string RESOURCE_MOVE = "Icons/lucide/move";
        public const string RESOURCE_PEN = "Icons/lucide/pen";

        internal static string ResolveHierarchyIcon(IconKind iconKind)
        {
            return iconKind switch
            {
                IconKind.Square => IconClass.SQUARE,
                IconKind.Circle => IconClass.CIRCLE,
                IconKind.FileText => IconClass.FILE_TEXT,
                IconKind.Minus => IconClass.MINUS,
                IconKind.Pen => IconClass.PEN,
                IconKind.Mask => IconClass.MASK,
                IconKind.Folder => IconClass.FOLDER,
                _ => IconClass.FILE
            };
        }
    }
}
