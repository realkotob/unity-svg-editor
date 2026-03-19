using System.Globalization;
using UnityEngine;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Svg.Transforms;
using SvgEditor.UI.Inspector.State;
using Core.UI.Extensions;

namespace SvgEditor.UI.Inspector
{
    internal static class PanelPatchBuilder
    {
        public static AttributePatchRequest BuildPatchRequest(PanelState state)
        {
            AttributePatchRequest request = CreatePatchRequest(state);
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

        public static AttributePatchRequest BuildPatchRequest(PanelState state, ImmediateApplyField field)
        {
            AttributePatchRequest request = CreatePatchRequest(state);
            switch (field)
            {
                case ImmediateApplyField.Opacity:
                    request.Opacity = FormatNumber(Mathf.Clamp01(state.Opacity));
                    break;
                case ImmediateApplyField.CornerRadius:
                    ApplyCornerRadius(request, state);
                    break;
                case ImmediateApplyField.FillColor:
                    ApplyFill(request, state);
                    break;
                case ImmediateApplyField.StrokeColor:
                    ApplyStroke(request, state);
                    break;
                case ImmediateApplyField.StrokeWidth:
                    request.StrokeWidth = FormatNumber(Mathf.Max(0f, state.StrokeWidth));
                    break;
                case ImmediateApplyField.StrokeLinecap:
                    request.StrokeLinecap = state.StrokeLinecap;
                    break;
                case ImmediateApplyField.StrokeLinejoin:
                    request.StrokeLinejoin = state.StrokeLinejoin;
                    break;
                case ImmediateApplyField.StrokeDasharray:
                    request.StrokeDasharray = BuildDasharrayValue(state);
                    break;
            }

            return request;
        }

        public static AttributePatchRequest BuildPatchRequest(PanelState state, AttributeAction action)
        {
            AttributePatchRequest request = CreatePatchRequest(state);
            switch (action)
            {
                case AttributeAction.AddFill:
                    ApplyFill(request, state);
                    break;
                case AttributeAction.RemoveFill:
                    request.Fill = "none";
                    request.FillOpacity = string.Empty;
                    break;
                case AttributeAction.AddStroke:
                    ApplyStroke(request, state);
                    request.StrokeWidth = FormatNumber(Mathf.Max(0f, state.StrokeWidth));
                    break;
                case AttributeAction.RemoveStroke:
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
            state.Transform = TransformBuilder.BuildTransform(
                state.TranslateX,
                state.TranslateY,
                state.Rotate,
                state.ScaleX,
                state.ScaleY,
                FormatNumber);
            if (!string.IsNullOrWhiteSpace(state.Transform))
            {
                state.TransformEnabled = true;
            }

            return state.Transform;
        }

        private static string BuildDasharrayValue(PanelState state)
        {
            float dash = Mathf.Max(0f, state.DashLength);
            float gap = Mathf.Max(0f, state.DashGap);
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
            {
                return;
            }

            string radius = FormatNumber(Mathf.Max(0f, state.CornerRadius));
            request.CornerRadiusX = radius;
            request.CornerRadiusY = radius;
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string ColorToRgbHex(Color color)
        {
            Color normalized = new(color.r, color.g, color.b, 1f);
            return $"#{ColorUtility.ToHtmlStringRGB(normalized)}";
        }

        private static string FormatAlphaAttribute(float alpha)
        {
            float normalized = Mathf.Clamp01(alpha);
            return Mathf.Approximately(normalized, 1f) ? string.Empty : FormatNumber(normalized);
        }
    }
}
