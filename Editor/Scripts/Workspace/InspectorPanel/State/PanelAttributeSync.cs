using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Lookup;
using SvgEditor.Shared;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal static class PanelAttributeSync
    {
        private const float MissingStrokeWidth = 0f;
        private const float MissingDashLength = 0f;
        private const float MissingDashGap = 0f;

        public static void SyncFromAttributes(PanelState state, IReadOnlyDictionary<string, string> attributes, string tagName)
        {
            state.FillEnabled = false;
            state.FillColor = Color.black;
            state.StrokeEnabled = false;
            state.StrokeColor = Color.black;
            state.StrokeWidth = MissingStrokeWidth;
            state.StrokeWidthEnabled = false;
            state.OpacityEnabled = false;
            state.Opacity = 1f;
            state.CornerRadiusEnabled = string.Equals(tagName, SvgTagName.RECT, StringComparison.OrdinalIgnoreCase);
            state.CornerRadius = 0f;
            state.DashLength = MissingDashLength;
            state.DashGap = MissingDashGap;
            state.DasharrayEnabled = false;
            state.TransformEnabled = false;
            state.Transform = string.Empty;
            state.TranslateX = 0f;
            state.TranslateY = 0f;
            state.Rotate = 0f;
            state.ScaleX = 1f;
            state.ScaleY = 1f;
            state.StrokeLinecap = string.Empty;
            state.StrokeLinejoin = string.Empty;

            state.FillEnabled = SvgAttributeUtility.TryGetAttribute(attributes, SvgAttributeName.FILL, out string fillRaw) &&
                                !SvgAttributeUtility.IsDisabledPaintValue(fillRaw);
            if (state.FillEnabled && SvgAttributeUtility.TryParseColor(fillRaw, out Color fillColor))
            {
                state.FillColor = fillColor;
            }

            if (SvgAttributeUtility.TryGetFloat(attributes, SvgAttributeName.FILL_OPACITY, out float fillOpacity))
            {
                state.FillColor = WithCombinedAlpha(state.FillColor, fillOpacity);
            }

            state.StrokeEnabled = SvgAttributeUtility.TryGetAttribute(attributes, SvgAttributeName.STROKE, out string strokeRaw) &&
                                  !SvgAttributeUtility.IsDisabledPaintValue(strokeRaw);
            if (state.StrokeEnabled && SvgAttributeUtility.TryParseColor(strokeRaw, out Color strokeColor))
            {
                state.StrokeColor = strokeColor;
            }

            if (SvgAttributeUtility.TryGetFloat(attributes, SvgAttributeName.STROKE_OPACITY, out float strokeOpacity))
            {
                state.StrokeColor = WithCombinedAlpha(state.StrokeColor, strokeOpacity);
            }

            state.StrokeWidthEnabled = SvgAttributeUtility.TryGetFloat(attributes, SvgAttributeName.STROKE_WIDTH, out float strokeWidth);
            if (state.StrokeWidthEnabled)
            {
                state.StrokeWidth = Mathf.Max(0f, strokeWidth);
            }
            else if (state.StrokeEnabled)
            {
                state.StrokeWidth = 1f;
            }

            state.OpacityEnabled = SvgAttributeUtility.TryGetFloat(attributes, SvgAttributeName.OPACITY, out float opacity);
            if (state.OpacityEnabled)
            {
                state.Opacity = Mathf.Clamp01(opacity);
            }

            if (state.CornerRadiusEnabled)
            {
                if (SvgAttributeUtility.TryGetFloat(attributes, SvgAttributeName.RX, out float radiusX))
                {
                    state.CornerRadius = Mathf.Max(0f, radiusX);
                }
                else if (SvgAttributeUtility.TryGetFloat(attributes, SvgAttributeName.RY, out float radiusY))
                {
                    state.CornerRadius = Mathf.Max(0f, radiusY);
                }
            }

            state.DasharrayEnabled = SvgAttributeUtility.TryGetAttribute(attributes, SvgAttributeName.STROKE_DASHARRAY, out string dashRaw);
            if (state.DasharrayEnabled)
            {
                ParseDasharray(dashRaw, out float dashLength, out float dashGap);
                state.DashLength = dashLength;
                state.DashGap = dashGap;
            }

            state.TransformEnabled = SvgAttributeUtility.TryGetAttribute(attributes, SvgAttributeName.TRANSFORM, out string transformRaw);
            state.Transform = state.TransformEnabled ? transformRaw.Trim() : string.Empty;
            TrySyncTransformHelperFromText(state);

            state.StrokeLinecap = NormalizeStrokeValue(attributes, SvgAttributeName.STROKE_LINECAP, new[] { "butt", "round", "square" });
            state.StrokeLinejoin = NormalizeStrokeValue(attributes, SvgAttributeName.STROKE_LINEJOIN, new[] { "miter", "round", "bevel" });
        }

        public static bool TrySyncTransformHelperFromText(PanelState state)
        {
            if (!TransformStringBuilder.TryParseSimpleTransform(
                    state.Transform,
                    out float translateX,
                    out float translateY,
                    out float rotate,
                    out float scaleX,
                    out float scaleY))
            {
                return false;
            }

            state.TranslateX = translateX;
            state.TranslateY = translateY;
            state.Rotate = rotate;
            state.ScaleX = scaleX;
            state.ScaleY = scaleY;
            return true;
        }

        private static void ParseDasharray(string raw, out float dash, out float gap)
        {
            dash = MissingDashLength;
            gap = MissingDashGap;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            List<string> tokens = raw
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(token => float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                .ToList();

            if (tokens.Count > 0 &&
                float.TryParse(tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedDash))
            {
                dash = Mathf.Max(0f, parsedDash);
            }

            if (tokens.Count > 1 &&
                float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedGap))
            {
                gap = Mathf.Max(0f, parsedGap);
            }
            else
            {
                gap = dash;
            }
        }

        private static string NormalizeStrokeValue(
            IReadOnlyDictionary<string, string> attributes,
            string key,
            IReadOnlyCollection<string> allowed)
        {
            if (!SvgAttributeUtility.TryGetAttribute(attributes, key, out string raw))
            {
                return string.Empty;
            }

            string value = raw.Trim().ToLowerInvariant();
            return allowed.Contains(value) ? value : string.Empty;
        }

        private static Color WithCombinedAlpha(Color color, float opacity)
        {
            color.a = Mathf.Clamp01(color.a * Mathf.Clamp01(opacity));
            return color;
        }
    }
}
