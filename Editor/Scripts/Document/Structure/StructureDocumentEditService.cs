using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal static class StructureDocumentEditService
    {
        public static bool TrySetElementTransform(
            string sourceText,
            string elementKey,
            string transformValue,
            out string updatedSource,
            out string error)
        {
            updatedSource = sourceText ?? string.Empty;
            error = string.Empty;

            if (!ValidateEditRequest(sourceText, elementKey, out error))
                return false;

            if (!SvgDocumentXmlUtility.TryResolveElementDocument(
                    sourceText,
                    elementKey,
                    out XmlDocument document,
                    out _,
                    out XmlElement element,
                    out error))
            {
                return false;
            }

            SetOrRemoveAttribute(element, "transform", transformValue);
            updatedSource = document.OuterXml;
            return true;
        }

        public static bool TryPrependElementTranslation(
            string sourceText,
            string elementKey,
            Vector2 translation,
            out string updatedSource,
            out string error)
        {
            updatedSource = sourceText ?? string.Empty;
            error = string.Empty;

            if (!ValidateEditRequest(sourceText, elementKey, out error))
                return false;

            if (Mathf.Approximately(translation.x, 0f) && Mathf.Approximately(translation.y, 0f))
                return true;

            if (!SvgDocumentXmlUtility.TryResolveElementDocument(
                    sourceText,
                    elementKey,
                    out XmlDocument document,
                    out _,
                    out XmlElement element,
                    out error))
            {
                return false;
            }

            var translate = TransformStringBuilder.BuildTranslate(translation, FormatNumber);
            PrependTransform(element, translate);
            updatedSource = document.OuterXml;
            return true;
        }

        public static bool TryPrependElementScale(
            string sourceText,
            string elementKey,
            Vector2 scale,
            Vector2 pivot,
            out string updatedSource,
            out string error)
        {
            updatedSource = sourceText ?? string.Empty;
            error = string.Empty;

            if (!ValidateEditRequest(sourceText, elementKey, out error))
                return false;

            if ((Mathf.Approximately(scale.x, 1f) && Mathf.Approximately(scale.y, 1f)) ||
                scale.x <= Mathf.Epsilon ||
                scale.y <= Mathf.Epsilon)
            {
                return true;
            }

            if (!SvgDocumentXmlUtility.TryResolveElementDocument(
                    sourceText,
                    elementKey,
                    out XmlDocument document,
                    out _,
                    out XmlElement element,
                    out error))
            {
                return false;
            }

            var scaleTransform = TransformStringBuilder.BuildScaleAround(scale, pivot, FormatNumber);
            PrependTransform(element, scaleTransform);
            updatedSource = document.OuterXml;
            return true;
        }

        private static bool ValidateEditRequest(string sourceText, string elementKey, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(elementKey))
            {
                error = "Element key is empty.";
                return false;
            }

            return true;
        }

        private static void PrependTransform(XmlElement element, string transformSegment)
        {
            var existingTransform = element.GetAttribute("transform")?.Trim();
            var combinedTransform = string.IsNullOrWhiteSpace(existingTransform)
                ? transformSegment
                : $"{transformSegment} {existingTransform}";

            element.SetAttribute("transform", combinedTransform);
        }

        private static void SetOrRemoveAttribute(XmlElement element, string attributeName, string value)
        {
            if (element == null || string.IsNullOrWhiteSpace(attributeName))
                return;

            if (string.IsNullOrWhiteSpace(value))
            {
                element.RemoveAttribute(attributeName);
                return;
            }

            element.SetAttribute(attributeName, value.Trim());
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
