using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SvgEditor.Tests.Editor
{
    public sealed class RefreshIconStyleTests
    {
        private const string IconStyleSheetAssetPath = "unity-svg-editor/Editor/UI/USS/Shared/Icon.uss";

        [Test]
        public void IconStyleSheet_DefinesRefreshIconClass()
        {
            string styleSheetPath = Path.Combine(Application.dataPath, IconStyleSheetAssetPath);

            Assert.That(File.Exists(styleSheetPath), Is.True, $"Missing stylesheet at '{styleSheetPath}'.");

            string styleSheet = File.ReadAllText(styleSheetPath);

            Assert.That(styleSheet, Does.Contain("--icon-refresh: resource(\"Icons/lucide/refresh-ccw\");"));
            Assert.That(styleSheet, Does.Contain(".icon-refresh { background-image: var(--icon-refresh); }"));
        }
    }
}
