using System;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Svg.Source;

namespace SvgEditor.UI.Canvas
{
    internal readonly struct PathEditEntryRequest
    {
        public PathEditEntryRequest(
            int clickCount,
            DocumentSession currentDocument,
            string elementKey,
            Matrix2D worldTransform,
            Func<Vector2, Vector2?> sceneToViewportPoint)
        {
            ClickCount = clickCount;
            CurrentDocument = currentDocument;
            ElementKey = elementKey ?? string.Empty;
            WorldTransform = worldTransform;
            SceneToViewportPoint = sceneToViewportPoint;
        }

        public int ClickCount { get; }
        public DocumentSession CurrentDocument { get; }
        public string ElementKey { get; }
        public Matrix2D WorldTransform { get; }
        public Func<Vector2, Vector2?> SceneToViewportPoint { get; }
    }
}
