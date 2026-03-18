using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal static class EditorFoundationIconUtility
    {
        private const string ToggleCheckmarkClassName = "unity-toggle__checkmark";

        public static bool ApplyToggleVectorImage(VisualElement root, string toggleName, string resourcePath)
        {
            if (root == null || string.IsNullOrWhiteSpace(toggleName) || string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            Toggle toggle = root.Q<Toggle>(toggleName);
            if (toggle == null)
            {
                return false;
            }

            VectorImage icon = Resources.Load<VectorImage>(resourcePath);
            if (icon == null)
            {
                return false;
            }

            VisualElement checkmark = toggle.Q(className: ToggleCheckmarkClassName);
            if (checkmark == null)
            {
                return false;
            }

            checkmark.style.backgroundImage = new StyleBackground(icon);
            return true;
        }
    }
}
