using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewTextOverlay
    {
        public string Key { get; set; } = string.Empty;
        public string TargetKey { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public Vector2 ScenePosition { get; set; }
        public float FontSize { get; set; } = 16f;
        public Color Color { get; set; } = Color.white;
        public string TextAnchor { get; set; } = "start";
        public Rect SceneBounds { get; set; }
    }
}
