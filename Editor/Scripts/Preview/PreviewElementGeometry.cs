using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewElementGeometry
    {
        public string Key { get; set; } = string.Empty;
        public string TargetKey { get; set; } = string.Empty;
        public Rect SceneBounds { get; set; }
        public int DrawOrder { get; set; }
        public IReadOnlyList<Vector2[]> HitTriangles { get; set; } = Array.Empty<Vector2[]>();
    }
}
