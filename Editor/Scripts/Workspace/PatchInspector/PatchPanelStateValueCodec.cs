using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal static class PatchPanelStateValueCodec
    {
        public static AttributePatchRequest BuildPatchRequest(PatchPanelState state)
        {
            return new AttributePatchRequest
            {
                TargetKey = state.ResolveSelectedTargetKey(),
                Fill = state.FillEnabled ? ColorToRgbHex(state.FillColor) : null,
                Stroke = state.StrokeEnabled ? ColorToRgbHex(state.StrokeColor) : null,
                StrokeWidth = state.StrokeWidthEnabled ? FormatNumber(Mathf.Max(0f, state.StrokeWidth)) : null,
                Opacity = state.OpacityEnabled ? FormatNumber(Mathf.Clamp01(state.Opacity)) : null,
                FillOpacity = state.FillEnabled ? FormatAlphaAttribute(state.FillColor.a) : null,
                StrokeOpacity = state.StrokeEnabled ? FormatAlphaAttribute(state.StrokeColor.a) : null,
                StrokeLinecap = state.StrokeLinecap,
                StrokeLinejoin = state.StrokeLinejoin,
                StrokeDasharray = state.DasharrayEnabled ? BuildDasharrayValue(state) : null,
                Transform = state.TransformEnabled ? state.Transform : null
            };
        }

        public static string BuildTransformFromHelper(PatchPanelState state)
        {
            state.Transform = TransformStringBuilder.BuildTransform(
                state.TranslateX,
                state.TranslateY,
                state.Rotate,
                state.ScaleX,
                state.ScaleY,
                FormatNumber);
            if (!string.IsNullOrWhiteSpace(state.Transform))
                state.TransformEnabled = true;

            return state.Transform;
        }

        public static void SyncFromAttributes(PatchPanelState state, IReadOnlyDictionary<string, string> attributes)
        {
            state.FillEnabled = TryGetNonEmpty(attributes, "fill", out var fillRaw);
            if (state.FillEnabled && ColorUtility.TryParseHtmlString(fillRaw.Trim(), out var fillColor))
                state.FillColor = fillColor;
            if (TryGetFloat(attributes, "fill-opacity", out var fillOpacity))
                state.FillColor = WithCombinedAlpha(state.FillColor, fillOpacity);

            state.StrokeEnabled = TryGetNonEmpty(attributes, "stroke", out var strokeRaw);
            if (state.StrokeEnabled && ColorUtility.TryParseHtmlString(strokeRaw.Trim(), out var strokeColor))
                state.StrokeColor = strokeColor;
            if (TryGetFloat(attributes, "stroke-opacity", out var strokeOpacity))
                state.StrokeColor = WithCombinedAlpha(state.StrokeColor, strokeOpacity);

            state.StrokeWidthEnabled = TryGetFloat(attributes, "stroke-width", out var strokeWidth);
            if (state.StrokeWidthEnabled)
                state.StrokeWidth = Mathf.Max(0f, strokeWidth);

            state.OpacityEnabled = TryGetFloat(attributes, "opacity", out var opacity);
            if (state.OpacityEnabled)
                state.Opacity = Mathf.Clamp01(opacity);

            state.DasharrayEnabled = TryGetNonEmpty(attributes, "stroke-dasharray", out var dashRaw);
            if (state.DasharrayEnabled)
            {
                ParseDasharray(dashRaw, out var dashLength, out var dashGap);
                state.DashLength = dashLength;
                state.DashGap = dashGap;
            }

            state.TransformEnabled = TryGetNonEmpty(attributes, "transform", out var transformRaw);
            state.Transform = state.TransformEnabled ? transformRaw.Trim() : string.Empty;

            state.StrokeLinecap = NormalizeStrokeValue(attributes, "stroke-linecap", new[] { "butt", "round", "square" });
            state.StrokeLinejoin = NormalizeStrokeValue(attributes, "stroke-linejoin", new[] { "miter", "round", "bevel" });
        }

        private static string BuildDasharrayValue(PatchPanelState state)
        {
            var dash = Mathf.Max(0f, state.DashLength);
            var gap = Mathf.Max(0f, state.DashGap);
            return $"{FormatNumber(dash)} {FormatNumber(gap)}";
        }

        private static void ParseDasharray(string raw, out float dash, out float gap)
        {
            dash = 4f;
            gap = 2f;
            if (string.IsNullOrWhiteSpace(raw))
                return;

            var tokens = raw
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(token => float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                .ToList();

            if (tokens.Count > 0 &&
                float.TryParse(tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDash))
            {
                dash = Mathf.Max(0f, parsedDash);
            }

            if (tokens.Count > 1 &&
                float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedGap))
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
            if (!TryGetNonEmpty(attributes, key, out var raw))
                return string.Empty;

            var value = raw.Trim().ToLowerInvariant();
            return allowed.Contains(value) ? value : string.Empty;
        }

        private static bool TryGetFloat(IReadOnlyDictionary<string, string> attributes, string key, out float value)
        {
            value = 0f;
            return TryGetNonEmpty(attributes, key, out var raw) &&
                   float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetNonEmpty(IReadOnlyDictionary<string, string> attributes, string key, out string value)
        {
            value = string.Empty;
            if (attributes == null || string.IsNullOrWhiteSpace(key))
                return false;

            if (!attributes.TryGetValue(key, out var found) || string.IsNullOrWhiteSpace(found))
                return false;

            value = found;
            return true;
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string ColorToRgbHex(Color color)
        {
            var normalized = new Color(color.r, color.g, color.b, 1f);
            return $"#{ColorUtility.ToHtmlStringRGB(normalized)}";
        }

        private static string FormatAlphaAttribute(float alpha)
        {
            var normalized = Mathf.Clamp01(alpha);
            return Mathf.Approximately(normalized, 1f) ? string.Empty : FormatNumber(normalized);
        }

        private static Color WithCombinedAlpha(Color color, float opacity)
        {
            color.a = Mathf.Clamp01(color.a * Mathf.Clamp01(opacity));
            return color;
        }
    }
}
