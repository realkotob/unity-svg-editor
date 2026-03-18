using Core.UI.Extensions;
using UnityEngine.UIElements;

namespace SvgEditor
{
    public enum SeparatorOrientation
    {
        Horizontal,
        Vertical
    }

    [UxmlElement(libraryPath = LibraryPath.COMPONENT_PATH)]
    public partial class Separator : VisualElement
    {
        public static class ClassName
        {
            public const string BASE = "separator";

            private const string MODIFIER_PREFIX = BASE + "--";

            public const string HORIZONTAL = MODIFIER_PREFIX + "horizontal";
            public const string VERTICAL = MODIFIER_PREFIX + "vertical";
        }

        private SeparatorOrientation? _orientation;

        [UxmlAttribute]
        public SeparatorOrientation Orientation
        {
            get => _orientation ?? SeparatorOrientation.Horizontal;
            set
            {
                if (_orientation.HasValue && _orientation.Value == value)
                {
                    return;
                }

                if (_orientation.HasValue)
                {
                    RemoveFromClassList(ToOrientationClassName(_orientation.Value));
                }

                _orientation = value;
                AddToClassList(ToOrientationClassName(value));
            }
        }

        public Separator()
            : this(SeparatorOrientation.Horizontal)
        {
        }

        public Separator(SeparatorOrientation orientation)
        {
            this.AddClass(ClassName.BASE);
            Orientation = orientation;
        }

        private static string ToOrientationClassName(SeparatorOrientation orientation) => orientation switch
        {
            SeparatorOrientation.Horizontal => ClassName.HORIZONTAL,
            SeparatorOrientation.Vertical => ClassName.VERTICAL,
            _ => string.Empty
        };
    }
}
