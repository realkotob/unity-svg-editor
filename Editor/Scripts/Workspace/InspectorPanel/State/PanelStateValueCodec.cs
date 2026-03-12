using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using SvgEditor.Shared;
using SvgEditor.Document;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal static class PanelStateValueCodec
    {
        private const float MissingStrokeWidth = 0f;
        private const float MissingDashLength = 0f;
        private const float MissingDashGap = 0f;

        public static AttributePatchRequest BuildPatchRequest(PanelState state)
        {
            var request = CreatePatchRequest(state);
            if (state.FillEnabled)
            {
                ApplyFill(request, state);
            }

            if (state.StrokeEnabled)
            {
                ApplyStroke(request, state);
            }

            request.StrokeWidth = state.StrokeWidthEnabled ? FormatNumber(Mathf.Max(0f, state.StrokeWidth)) : null;
            request.Opacity = state.OpacityEnabled ? FormatNumber(Mathf.Clamp01(state.Opacity)) : null;
            ApplyCornerRadius(request, state);
            request.StrokeLinecap = state.StrokeLinecap;
            request.StrokeLinejoin = state.StrokeLinejoin;
            request.StrokeDasharray = state.DasharrayEnabled ? BuildDasharrayValue(state) : null;
            request.Transform = state.TransformEnabled ? state.Transform : null;
            return request;
        }

        public static AttributePatchRequest BuildPatchRequest(PanelState state, PanelView.ImmediateApplyField field)
        {
            var request = CreatePatchRequest(state);
            switch (field)
            {
                case PanelView.ImmediateApplyField.Opacity:
                    request.Opacity = FormatNumber(Mathf.Clamp01(state.Opacity));
                    break;
                case PanelView.ImmediateApplyField.CornerRadius:
                    ApplyCornerRadius(request, state);
                    break;
                case PanelView.ImmediateApplyField.FillColor:
                    ApplyFill(request, state);
                    break;
                case PanelView.ImmediateApplyField.StrokeColor:
                    ApplyStroke(request, state);
                    break;
                case PanelView.ImmediateApplyField.StrokeWidth:
                    request.StrokeWidth = FormatNumber(Mathf.Max(0f, state.StrokeWidth));
                    break;
                case PanelView.ImmediateApplyField.StrokeLinecap:
                    request.StrokeLinecap = state.StrokeLinecap;
                    break;
                case PanelView.ImmediateApplyField.StrokeLinejoin:
                    request.StrokeLinejoin = state.StrokeLinejoin;
                    break;
                case PanelView.ImmediateApplyField.StrokeDasharray:
                    request.StrokeDasharray = BuildDasharrayValue(state);
                    break;
            }

            return request;
        }

        public static AttributePatchRequest BuildPatchRequest(PanelState state, PanelView.AttributeAction action)
        {
            var request = CreatePatchRequest(state);
            switch (action)
            {
                case PanelView.AttributeAction.AddFill:
                    ApplyFill(request, state);
                    break;
                case PanelView.AttributeAction.RemoveFill:
                    request.Fill = "none";
                    request.FillOpacity = string.Empty;
                    break;
                case PanelView.AttributeAction.AddStroke:
                    ApplyStroke(request, state);
                    request.StrokeWidth = FormatNumber(Mathf.Max(0f, state.StrokeWidth));
                    break;
                case PanelView.AttributeAction.RemoveStroke:
                    request.Stroke = "none";
                    request.StrokeOpacity = string.Empty;
                    request.StrokeWidth = string.Empty;
                    request.StrokeLinecap = string.Empty;
                    request.StrokeLinejoin = string.Empty;
                    request.StrokeDasharray = string.Empty;
                    break;
            }

            return request;
        }

        public static string BuildTransformFromHelper(PanelState state)
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
            state.CornerRadiusEnabled = string.Equals(tagName, SvgTagName.RECT, System.StringComparison.OrdinalIgnoreCase);
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

            state.FillEnabled = TryGetNonEmpty(attributes, SvgAttributeName.FILL, out var fillRaw) && !IsDisabledPaintValue(fillRaw);
            if (state.FillEnabled && ColorUtility.TryParseHtmlString(fillRaw.Trim(), out var fillColor))
                state.FillColor = fillColor;
            if (TryGetFloat(attributes, SvgAttributeName.FILL_OPACITY, out var fillOpacity))
                state.FillColor = WithCombinedAlpha(state.FillColor, fillOpacity);

            state.StrokeEnabled = TryGetNonEmpty(attributes, SvgAttributeName.STROKE, out var strokeRaw) && !IsDisabledPaintValue(strokeRaw);
            if (state.StrokeEnabled && ColorUtility.TryParseHtmlString(strokeRaw.Trim(), out var strokeColor))
                state.StrokeColor = strokeColor;
            if (TryGetFloat(attributes, SvgAttributeName.STROKE_OPACITY, out var strokeOpacity))
                state.StrokeColor = WithCombinedAlpha(state.StrokeColor, strokeOpacity);

            state.StrokeWidthEnabled = TryGetFloat(attributes, SvgAttributeName.STROKE_WIDTH, out var strokeWidth);
            if (state.StrokeWidthEnabled)
                state.StrokeWidth = Mathf.Max(0f, strokeWidth);
            else if (state.StrokeEnabled)
                state.StrokeWidth = 1f;

            state.OpacityEnabled = TryGetFloat(attributes, SvgAttributeName.OPACITY, out var opacity);
            if (state.OpacityEnabled)
                state.Opacity = Mathf.Clamp01(opacity);

            if (state.CornerRadiusEnabled)
            {
                if (TryGetFloat(attributes, SvgAttributeName.RX, out var radiusX))
                    state.CornerRadius = Mathf.Max(0f, radiusX);
                else if (TryGetFloat(attributes, SvgAttributeName.RY, out var radiusY))
                    state.CornerRadius = Mathf.Max(0f, radiusY);
            }

            state.DasharrayEnabled = TryGetNonEmpty(attributes, SvgAttributeName.STROKE_DASHARRAY, out var dashRaw);
            if (state.DasharrayEnabled)
            {
                ParseDasharray(dashRaw, out var dashLength, out var dashGap);
                state.DashLength = dashLength;
                state.DashGap = dashGap;
            }

            state.TransformEnabled = TryGetNonEmpty(attributes, SvgAttributeName.TRANSFORM, out var transformRaw);
            state.Transform = state.TransformEnabled ? transformRaw.Trim() : string.Empty;
            TrySyncTransformHelperFromText(state);

            state.StrokeLinecap = NormalizeStrokeValue(attributes, SvgAttributeName.STROKE_LINECAP, new[] { "butt", "round", "square" });
            state.StrokeLinejoin = NormalizeStrokeValue(attributes, SvgAttributeName.STROKE_LINEJOIN, new[] { "miter", "round", "bevel" });
        }

        public static bool TrySyncTransformHelperFromText(PanelState state)
        {
            if (!TransformStringBuilder.TryParseSimpleTransform(
                    state.Transform,
                    out var translateX,
                    out var translateY,
                    out var rotate,
                    out var scaleX,
                    out var scaleY))
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

        private static string BuildDasharrayValue(PanelState state)
        {
            var dash = Mathf.Max(0f, state.DashLength);
            var gap = Mathf.Max(0f, state.DashGap);
            return $"{FormatNumber(dash)} {FormatNumber(gap)}";
        }

        private static AttributePatchRequest CreatePatchRequest(PanelState state)
        {
            return new AttributePatchRequest
            {
                TargetKey = state.ResolveSelectedTargetKey()
            };
        }

        private static void ApplyFill(AttributePatchRequest request, PanelState state)
        {
            request.Fill = ColorToRgbHex(state.FillColor);
            request.FillOpacity = FormatAlphaAttribute(state.FillColor.a);
        }

        private static void ApplyStroke(AttributePatchRequest request, PanelState state)
        {
            request.Stroke = ColorToRgbHex(state.StrokeColor);
            request.StrokeOpacity = FormatAlphaAttribute(state.StrokeColor.a);
        }

        private static void ApplyCornerRadius(AttributePatchRequest request, PanelState state)
        {
            if (!state.CornerRadiusEnabled)
                return;

            var radius = FormatNumber(Mathf.Max(0f, state.CornerRadius));
            request.CornerRadiusX = radius;
            request.CornerRadiusY = radius;
        }

        private static void ParseDasharray(string raw, out float dash, out float gap)
        {
            dash = MissingDashLength;
            gap = MissingDashGap;
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

        private static bool IsDisabledPaintValue(string value)
        {
            return string.Equals(value?.Trim(), "none", StringComparison.OrdinalIgnoreCase);
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
