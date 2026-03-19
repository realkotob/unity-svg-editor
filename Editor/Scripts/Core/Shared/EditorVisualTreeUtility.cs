using UnityEngine.UIElements;

namespace SvgEditor.Core.Shared
{
    internal static class EditorVisualTreeUtility
    {
        public static void MoveChildren(VisualElement source, VisualElement target)
        {
            if (source == null || target == null)
            {
                return;
            }

            while (source.childCount > 0)
            {
                VisualElement child = source[0];
                child.RemoveFromHierarchy();
                target.Add(child);
            }
        }

        public static bool ReplaceElement(VisualElement original, VisualElement replacement)
        {
            if (original?.parent == null || replacement == null)
            {
                return false;
            }

            VisualElement parent = original.parent;
            int index = parent.IndexOf(original);
            original.RemoveFromHierarchy();
            parent.Insert(index, replacement);
            return true;
        }
    }
}
