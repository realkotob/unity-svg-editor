using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Core.Shared
{
    internal static class EditorThemeUtility
    {
        private static readonly PropertyInfo ThemeStyleSheetProperty = typeof(VisualElement).GetProperty(
            "themeStyleSheet",
            BindingFlags.Instance | BindingFlags.Public);

        public static bool ApplyThemeStyleSheet(VisualElement root, string resourcePath)
        {
            if (root == null || string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            ThemeStyleSheet theme = Resources.Load<ThemeStyleSheet>(resourcePath);
            return ApplyThemeStyleSheet(root, theme);
        }

        public static bool ApplyThemeStyleSheet(VisualElement root, ThemeStyleSheet theme)
        {
            if (root == null || theme == null)
            {
                return false;
            }

            if (theme is StyleSheet styleSheet && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            if (ThemeStyleSheetProperty == null || !ThemeStyleSheetProperty.CanWrite)
            {
                return false;
            }

            ThemeStyleSheetProperty.SetValue(root, theme);
            return true;
        }
    }
}
