using System;
using System.Collections.Generic;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.Core.Svg.Serialization;
using Core.UI.Extensions;
using UnityEngine;

namespace SvgEditor.Core.Svg.Mutation
{
    internal sealed class PathMutationService
    {
        private readonly SvgSerializer _serializer = new();

        public bool TryApplyPathData(
            SvgDocumentModel documentModel,
            string targetKey,
            PathData pathData,
            out MutationResult result)
        {
            Result<MutationResult> mutation = ApplyPathData(documentModel, targetKey, pathData);
            result = mutation.IsSuccess
                ? mutation.Value
                : new MutationResult(null, string.Empty, mutation.Error);
            return mutation.IsSuccess;
        }

        private Result<MutationResult> ApplyPathData(
            SvgDocumentModel documentModel,
            string targetKey,
            PathData pathData)
        {
            if (documentModel?.Root == null)
            {
                return Result.Failure<MutationResult>("Document model is unavailable.");
            }

            if (pathData == null)
            {
                return Result.Failure<MutationResult>("Path data is unavailable.");
            }

            SvgDocumentModel updatedDocumentModel = ModelClone.Create(documentModel);
            if (!MutationLookup.TryResolveTargetNode(updatedDocumentModel, targetKey, out SvgNodeModel targetNode))
            {
                return Result.Failure<MutationResult>($"Could not find target '{targetKey}'.");
            }

            string normalizedTagName = targetNode.TagName?.Trim() ?? string.Empty;
            bool isPath = string.Equals(normalizedTagName, SvgTagName.PATH, StringComparison.OrdinalIgnoreCase);
            bool isLine = string.Equals(normalizedTagName, SvgTagName.LINE, StringComparison.OrdinalIgnoreCase);
            bool isRect = string.Equals(normalizedTagName, SvgTagName.RECT, StringComparison.OrdinalIgnoreCase);
            bool isCircle = string.Equals(normalizedTagName, SvgTagName.CIRCLE, StringComparison.OrdinalIgnoreCase);
            bool isEllipse = string.Equals(normalizedTagName, SvgTagName.ELLIPSE, StringComparison.OrdinalIgnoreCase);
            bool isPolyline = string.Equals(normalizedTagName, SvgTagName.POLYLINE, StringComparison.OrdinalIgnoreCase);
            bool isPolygon = string.Equals(normalizedTagName, SvgTagName.POLYGON, StringComparison.OrdinalIgnoreCase);
            if (!isPath && !isLine && !isRect && !isCircle && !isEllipse && !isPolyline && !isPolygon)
            {
                return Result.Failure<MutationResult>($"Target '{targetKey}' is not an editable path element.");
            }

            Dictionary<string, string> attributes = ModelClone.CloneAttributes(targetNode.RawAttributes);
            if (isLine && PrimitivePathConversion.TrySerializeAsLine(pathData, out Vector2 lineStart, out Vector2 lineEnd))
            {
                attributes.Remove(SvgAttributeName.D);
                attributes.Remove(SvgAttributeName.POINTS);
                attributes[SvgAttributeName.X1] = FormatFloat(lineStart.x);
                attributes[SvgAttributeName.Y1] = FormatFloat(lineStart.y);
                attributes[SvgAttributeName.X2] = FormatFloat(lineEnd.x);
                attributes[SvgAttributeName.Y2] = FormatFloat(lineEnd.y);
                MutationWriter.ApplyAttributes(targetNode, attributes);
                targetNode.DisplayTagName = normalizedTagName;

                return MutationWriter.Serialize(_serializer, updatedDocumentModel)
                    .Map(updatedSourceText => new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty));
            }

            if (isRect && PrimitivePathConversion.TrySerializeAsRect(pathData, out Rect rect))
            {
                attributes.Remove(SvgAttributeName.D);
                attributes.Remove(SvgAttributeName.POINTS);
                attributes[SvgAttributeName.X] = FormatFloat(rect.xMin);
                attributes[SvgAttributeName.Y] = FormatFloat(rect.yMin);
                attributes[SvgAttributeName.WIDTH] = FormatFloat(rect.width);
                attributes[SvgAttributeName.HEIGHT] = FormatFloat(rect.height);
                MutationWriter.ApplyAttributes(targetNode, attributes);
                targetNode.DisplayTagName = normalizedTagName;

                return MutationWriter.Serialize(_serializer, updatedDocumentModel)
                    .Map(updatedSourceText => new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty));
            }

            if ((isPolyline || isPolygon) &&
                PrimitivePathConversion.TrySerializeAsPoints(pathData, isPolygon, out string pointsText))
            {
                attributes.Remove(SvgAttributeName.D);
                attributes[SvgAttributeName.POINTS] = pointsText;
                MutationWriter.ApplyAttributes(targetNode, attributes);
                targetNode.DisplayTagName = normalizedTagName;

                return MutationWriter.Serialize(_serializer, updatedDocumentModel)
                    .Map(updatedSourceText => new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty));
            }

            Result<string> serializedPathData = PathDataSerializer.SerializeResult(pathData);
            if (serializedPathData.IsFailure)
            {
                return Result.Failure<MutationResult>(serializedPathData.Error);
            }

            RemovePrimitiveGeometryAttributes(attributes);
            attributes[SvgAttributeName.D] = serializedPathData.Value;
            MutationWriter.ApplyAttributes(targetNode, attributes);
            targetNode.TagName = SvgTagName.PATH;
            targetNode.DisplayTagName = SvgTagName.PATH;

            return MutationWriter.Serialize(_serializer, updatedDocumentModel)
                .Map(updatedSourceText => new MutationResult(updatedDocumentModel, updatedSourceText, string.Empty));
        }

        private static string FormatFloat(float value)
        {
            if (Math.Abs(value) < 0.0000005f)
            {
                value = 0f;
            }

            return value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void RemovePrimitiveGeometryAttributes(Dictionary<string, string> attributes)
        {
            attributes.Remove(SvgAttributeName.POINTS);
            attributes.Remove(SvgAttributeName.X);
            attributes.Remove(SvgAttributeName.Y);
            attributes.Remove(SvgAttributeName.WIDTH);
            attributes.Remove(SvgAttributeName.HEIGHT);
            attributes.Remove(SvgAttributeName.RX);
            attributes.Remove(SvgAttributeName.RY);
            attributes.Remove(SvgAttributeName.CX);
            attributes.Remove(SvgAttributeName.CY);
            attributes.Remove(SvgAttributeName.R);
            attributes.Remove(SvgAttributeName.X1);
            attributes.Remove(SvgAttributeName.Y1);
            attributes.Remove(SvgAttributeName.X2);
            attributes.Remove(SvgAttributeName.Y2);
        }
    }
}
