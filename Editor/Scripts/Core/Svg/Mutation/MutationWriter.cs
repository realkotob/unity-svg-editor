using System;
using System.Collections.Generic;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Serialization;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Mutation
{
    internal static class MutationWriter
    {
        public static void ApplyAttributePatch(SvgNodeModel node, AttributePatchRequest request)
        {
            Dictionary<string, string> attributes = ModelClone.CloneAttributes(node.RawAttributes);
            ApplyAttribute(attributes, SvgAttributeName.FILL, request.Fill);
            ApplyAttribute(attributes, SvgAttributeName.STROKE, request.Stroke);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_WIDTH, request.StrokeWidth);
            ApplyAttribute(attributes, SvgAttributeName.OPACITY, request.Opacity);
            ApplyAttribute(attributes, SvgAttributeName.FILL_OPACITY, request.FillOpacity);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_OPACITY, request.StrokeOpacity);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_LINECAP, request.StrokeLinecap);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_LINEJOIN, request.StrokeLinejoin);
            ApplyAttribute(attributes, SvgAttributeName.STROKE_DASHARRAY, request.StrokeDasharray);
            ApplyAttribute(attributes, SvgAttributeName.RX, request.CornerRadiusX);
            ApplyAttribute(attributes, SvgAttributeName.RY, request.CornerRadiusY);
            ApplyAttribute(attributes, SvgAttributeName.TRANSFORM, request.Transform);
            ApplyAttribute(attributes, SvgAttributeName.DISPLAY, request.Display);
            ApplyNodeAttributes(node, attributes);
        }

        public static void ApplyPrependTransform(SvgNodeModel node, Func<string, string> buildUpdatedTransform)
        {
            Dictionary<string, string> attributes = ModelClone.CloneAttributes(node.RawAttributes);
            string existingTransform = attributes.TryGetValue(SvgAttributeName.TRANSFORM, out string transform)
                ? transform ?? string.Empty
                : string.Empty;
            attributes[SvgAttributeName.TRANSFORM] = buildUpdatedTransform(existingTransform);
            ApplyNodeAttributes(node, attributes);
        }

        public static Result<string> Serialize(SvgSerializer serializer, SvgDocumentModel updatedDocumentModel)
        {
            Result<string> result = serializer.Serialize(updatedDocumentModel);
            if (result.IsFailure)
            {
                return result;
            }

            updatedDocumentModel.SourceText = result.Value;
            return result;
        }

        public static string PrependTransform(string existingTransform, string transformSegment)
        {
            return string.IsNullOrWhiteSpace(existingTransform)
                ? transformSegment
                : $"{transformSegment} {existingTransform}";
        }

        private static void ApplyNodeAttributes(SvgNodeModel node, Dictionary<string, string> attributes)
        {
            node.RawAttributes = attributes;
            node.References = NodeReferenceBuilder.Build(attributes);
        }

        private static void ApplyAttribute(Dictionary<string, string> attributes, string attributeName, string value)
        {
            if (value == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                attributes.Remove(attributeName);
                return;
            }

            attributes[attributeName] = value.Trim();
        }
    }
}
