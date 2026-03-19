using Core.UI.Extensions;
using UnityEngine.UIElements;

namespace SvgEditor.Core.Shared
{
    internal static class EditorInspectorSectionUtility
    {
        public static bool TryUpgradeToInspectorSection(
            VisualElement original,
            string titleClassName,
            string fallbackTitle,
            InspectorSectionClasses classes,
            bool accent)
        {
            if (original?.parent == null)
            {
                return false;
            }

            Label titleLabel = original.Q<Label>(className: titleClassName);
            InspectorSection section = CreateInspectorSection(
                original,
                titleLabel?.text ?? fallbackTitle,
                classes);

            titleLabel?.RemoveFromHierarchy();
            EditorVisualTreeUtility.MoveChildren(original, section.Body);
            if (!EditorVisualTreeUtility.ReplaceElement(original, section))
            {
                return false;
            }

            section.SetAccent(accent);
            return true;
        }

        private static InspectorSection CreateInspectorSection(
            VisualElement original,
            string title,
            InspectorSectionClasses classes)
        {
            InspectorSection section = new(title, classes)
            {
                name = original.name
            };

            foreach (string className in original.GetClasses())
            {
                section.AddClass(className);
            }

            return section;
        }
    }
}
