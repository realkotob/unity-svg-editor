using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Structure.Lookup;
using SvgEditor.Core.Svg.Transforms;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.Core.Preview.Text
{
    internal static class SnapshotTextBuilder
    {
        public static IReadOnlyList<PreviewTextOverlay> BuildTextOverlays(SvgDocumentModel documentModel)
        {
            List<PreviewTextOverlay> overlays = new();
            if (documentModel?.Root == null)
                return overlays;

            BuildNodeOverlays(documentModel, documentModel.Root, Matrix2D.identity, new TextStyleContext
            {
                ScenePosition = Vector2.zero,
                HasPosition = false,
                FontSize = 16f,
                Color = Color.white,
                TextAnchor = "start",
                WorldTransform = Matrix2D.identity
            }, overlays);
            return overlays;
        }

        public static IReadOnlyList<PreviewElementGeometry> BuildTextElements(
            IReadOnlyList<PreviewTextOverlay> textOverlays,
            int startingDrawOrder)
        {
            List<PreviewElementGeometry> elements = new();
            if (textOverlays == null)
                return elements;

            for (int index = 0; index < textOverlays.Count; index++)
            {
                PreviewTextOverlay overlay = textOverlays[index];
                if (overlay == null || string.IsNullOrWhiteSpace(overlay.Text))
                    continue;

                Rect bounds = overlay.SceneBounds.width > 0f || overlay.SceneBounds.height > 0f
                    ? overlay.SceneBounds
                    : EstimateTextBounds(overlay);
                elements.Add(new PreviewElementGeometry
                {
                    Key = overlay.Key,
                    TargetKey = overlay.TargetKey,
                    VisualBounds = bounds,
                    DrawOrder = startingDrawOrder + index,
                    HitGeometry = BuildRectHitGeometry(bounds),
                    BoundsQuality = BoundsQuality.Fallback,
                    WorldTransform = Matrix2D.identity,
                    ParentWorldTransform = Matrix2D.identity,
                    RotationPivotWorld = bounds.center,
                    RotationPivotParentSpace = bounds.center,
                    IsTextOverlay = true
                });
            }

            return elements;
        }

        private static void BuildNodeOverlays(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            Matrix2D parentWorldTransform,
            TextStyleContext inheritedStyle,
            List<PreviewTextOverlay> overlays)
        {
            if (node == null)
                return;

            var localTransform = TransformParser.Parse(node.RawAttributes);
            var worldTransform = parentWorldTransform * localTransform;
            var currentStyle = ResolveTextStyle(node, inheritedStyle, worldTransform);

            if (node.Kind == SvgNodeCategory.Text &&
                !string.IsNullOrWhiteSpace(node.TextContent) &&
                currentStyle.HasPosition)
            {
                PreviewTextOverlay overlay = new PreviewTextOverlay
                {
                    Key = node.LegacyElementKey,
                    TargetKey = node.LegacyTargetKey,
                    Text = node.TextContent,
                    ScenePosition = currentStyle.ScenePosition,
                    FontSize = currentStyle.FontSize,
                    WidthScale = currentStyle.ScaleX,
                    HeightScale = currentStyle.ScaleY,
                    Color = currentStyle.Color,
                    TextAnchor = currentStyle.TextAnchor
                };
                overlay.SceneBounds = EstimateTextBounds(overlay);
                overlays.Add(overlay);
            }

            for (int index = 0; index < node.Children.Count; index++)
            {
                SvgNodeId childId = node.Children[index];
                if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                BuildNodeOverlays(documentModel, childNode, worldTransform, currentStyle, overlays);
            }
        }

        private static TextStyleContext ResolveTextStyle(
            SvgNodeModel node,
            TextStyleContext inheritedStyle,
            Matrix2D worldTransform)
        {
            var style = inheritedStyle;
            style.WorldTransform = worldTransform;

            if (AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.FONT_SIZE, out var fontSize))
                style.FontSize = Mathf.Max(1f, fontSize);

            if (TryGetColor(node.RawAttributes, SvgAttributeName.FILL, out var color))
                style.Color = color;

            if (AttributeUtility.TryGetAttribute(node.RawAttributes, SvgAttributeName.TEXT_ANCHOR, out var textAnchor))
                style.TextAnchor = textAnchor;

            var hasX = AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.X, out var x);
            var hasY = AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.Y, out var y);
            var hasDx = AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.DX, out var dx);
            var hasDy = AttributeUtility.TryGetFloat(node.RawAttributes, SvgAttributeName.DY, out var dy);

            Vector2 scenePosition = style.ScenePosition;
            bool hasPosition = style.HasPosition;

            if (hasX || hasY)
            {
                float localX = hasX ? x : 0f;
                float localY = hasY ? y : 0f;
                scenePosition = worldTransform.MultiplyPoint(new Vector2(localX, localY));
                hasPosition = true;
            }

            if (hasDx || hasDy)
            {
                Vector2 delta = worldTransform.MultiplyVector(new Vector2(hasDx ? dx : 0f, hasDy ? dy : 0f));
                scenePosition += delta;
                hasPosition = true;
            }

            style.ScenePosition = scenePosition;
            style.HasPosition = hasPosition;
            ResolveScale(worldTransform, out float scaleX, out float scaleY);
            style.ScaleX = scaleX;
            style.ScaleY = scaleY;
            return style;
        }

        private static bool TryGetColor(IReadOnlyDictionary<string, string> attributes, string name, out Color color)
        {
            color = Color.white;
            return AttributeUtility.TryGetAttribute(attributes, name, out var text) &&
                   AttributeUtility.TryParseColor(text, out color);
        }

        private static Rect EstimateTextBounds(PreviewTextOverlay overlay)
        {
            float heightScale = Mathf.Max(0.01f, overlay.HeightScale);
            float widthScale = Mathf.Max(0.01f, overlay.WidthScale);
            float fontSize = Mathf.Max(1f, overlay.FontSize * heightScale);
            float width = Mathf.Max(fontSize * 0.5f, overlay.Text.Length * overlay.FontSize * 0.55f * widthScale);
            float height = fontSize * 1.2f;
            float x = overlay.ScenePosition.x;
            float y = overlay.ScenePosition.y - fontSize;

            if (string.Equals(overlay.TextAnchor, "middle", StringComparison.OrdinalIgnoreCase))
                x -= width * 0.5f;
            else if (string.Equals(overlay.TextAnchor, "end", StringComparison.OrdinalIgnoreCase))
                x -= width;

            return new Rect(x, y, width, height);
        }

        private static void ResolveScale(Matrix2D matrix, out float scaleX, out float scaleY)
        {
            scaleX = new Vector2(matrix.m00, matrix.m10).magnitude;
            scaleY = new Vector2(matrix.m01, matrix.m11).magnitude;

            if (scaleX <= Mathf.Epsilon)
                scaleX = 1f;
            if (scaleY <= Mathf.Epsilon)
                scaleY = 1f;
        }

        private static IReadOnlyList<Vector2[]> BuildRectHitGeometry(Rect bounds)
        {
            Vector2 topLeft = new(bounds.xMin, bounds.yMin);
            Vector2 topRight = new(bounds.xMax, bounds.yMin);
            Vector2 bottomRight = new(bounds.xMax, bounds.yMax);
            Vector2 bottomLeft = new(bounds.xMin, bounds.yMax);
            return new[]
            {
                new[] { topLeft, topRight, bottomRight },
                new[] { topLeft, bottomRight, bottomLeft }
            };
        }

        private struct TextStyleContext
        {
            public Vector2 ScenePosition { get; set; }
            public bool HasPosition { get; set; }
            public float FontSize { get; set; }
            public float ScaleX { get; set; }
            public float ScaleY { get; set; }
            public Color Color { get; set; }
            public string TextAnchor { get; set; }
            public Matrix2D WorldTransform { get; set; }
        }
    }
}
