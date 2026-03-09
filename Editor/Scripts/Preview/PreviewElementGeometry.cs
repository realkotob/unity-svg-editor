using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewElementGeometry
    {
        public string Key { get; set; } = string.Empty;
        public string TargetKey { get; set; } = string.Empty;
        public Rect VisualBounds { get; set; }
        public int DrawOrder { get; set; }
        public IReadOnlyList<Vector2[]> HitGeometry { get; set; } = Array.Empty<Vector2[]>();
        public BoundsQuality BoundsQuality { get; set; } = BoundsQuality.Unknown;
        public Matrix2D WorldTransform { get; set; } = Matrix2D.identity;
        public Matrix2D ParentWorldTransform { get; set; } = Matrix2D.identity;
    }
}
