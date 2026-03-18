using System;
using System.Collections.Generic;
using UnityEngine;
using Core.UI.Extensions;

namespace SvgEditor.Preview.Geometry
{
    internal readonly struct TessellatedNodeGeometry
    {
        public TessellatedNodeGeometry(IReadOnlyList<Vector2[]> triangles, Rect bounds, bool hasBounds)
        {
            Triangles = triangles ?? Array.Empty<Vector2[]>();
            Bounds = bounds;
            HasBounds = hasBounds;
        }

        public IReadOnlyList<Vector2[]> Triangles { get; }
        public Rect Bounds { get; }
        public bool HasBounds { get; }
    }
}
