using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewSnapshot : IDisposable
    {
        public VectorImage PreviewVectorImage { get; set; }
        public Rect SceneViewport { get; set; }
        public Rect SceneBounds { get; set; }
        public IReadOnlyList<PreviewElementGeometry> Elements { get; set; } = Array.Empty<PreviewElementGeometry>();

        public Rect EffectiveViewport =>
            SceneViewport.width > 0f && SceneViewport.height > 0f
                ? SceneViewport
                : SceneBounds;

        public void Dispose()
        {
            if (PreviewVectorImage != null)
            {
                UnityEngine.Object.DestroyImmediate(PreviewVectorImage);
                PreviewVectorImage = null;
            }
        }
    }
}
