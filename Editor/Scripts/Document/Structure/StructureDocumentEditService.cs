using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal static class StructureDocumentEditService
    {
        public static bool TryReorderElementWithinSameParent(
            string sourceText,
            string elementKey,
            int targetChildIndex,
            out string reorderedSource,
            out string error)
        {
            reorderedSource = sourceText ?? string.Empty;
            error = string.Empty;

            if (!ValidateEditRequest(sourceText, elementKey, out error))
                return false;

            if (!SvgDocumentXmlUtility.TryResolveElementDocument(
                    sourceText,
                    elementKey,
                    out XmlDocument document,
                    out _,
                    out XmlElement movedElement,
                    out error))
            {
                return false;
            }

            if (movedElement.ParentNode is not XmlElement parentElement)
            {
                error = "Moved element does not have an XML element parent.";
                return false;
            }

            var siblingElements = SvgDocumentXmlUtility.GetElementChildren(parentElement);
            var sourceIndex = siblingElements.FindIndex(child => ReferenceEquals(child, movedElement));
            if (sourceIndex < 0)
            {
                error = "Could not resolve the moved element index.";
                return false;
            }

            var clampedTargetIndex = Math.Clamp(targetChildIndex, 0, siblingElements.Count);
            if (sourceIndex < clampedTargetIndex)
                clampedTargetIndex--;

            if (clampedTargetIndex == sourceIndex)
                return true;

            var moveWhitespace = DetachLeadingWhitespace(movedElement);
            parentElement.RemoveChild(movedElement);

            var remainingElements = SvgDocumentXmlUtility.GetElementChildren(parentElement);
            InsertReorderedElement(parentElement, movedElement, moveWhitespace, remainingElements, clampedTargetIndex);

            reorderedSource = document.OuterXml;
            return true;
        }

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

        private static void InsertReorderedElement(
            XmlElement parentElement,
            XmlElement movedElement,
            XmlNode moveWhitespace,
            List<XmlElement> remainingElements,
            int targetIndex)
        {
            if (targetIndex < remainingElements.Count)
            {
                var referenceElement = remainingElements[targetIndex];
                if (moveWhitespace != null)
                    parentElement.InsertBefore(moveWhitespace, referenceElement);

                parentElement.InsertBefore(movedElement, referenceElement);
                return;
            }

            var trailingWhitespace = GetTrailingWhitespaceNode(parentElement);
            if (moveWhitespace != null)
            {
                if (trailingWhitespace != null)
                    parentElement.InsertBefore(moveWhitespace, trailingWhitespace);
                else
                    parentElement.AppendChild(moveWhitespace);
            }

            if (trailingWhitespace != null)
                parentElement.InsertBefore(movedElement, trailingWhitespace);
            else
                parentElement.AppendChild(movedElement);
        }

        private static void PrependTransform(XmlElement element, string transformSegment)
        {
            var existingTransform = element.GetAttribute("transform")?.Trim();
            var combinedTransform = string.IsNullOrWhiteSpace(existingTransform)
                ? transformSegment
                : $"{transformSegment} {existingTransform}";

            element.SetAttribute("transform", combinedTransform);
        }

        private static XmlNode DetachLeadingWhitespace(XmlElement element)
        {
            var previousSibling = element?.PreviousSibling;
            if (!IsWhitespaceNode(previousSibling) || previousSibling?.ParentNode == null)
                return null;

            previousSibling.ParentNode.RemoveChild(previousSibling);
            return previousSibling;
        }

        private static XmlNode GetTrailingWhitespaceNode(XmlElement parentElement)
        {
            if (parentElement == null)
                return null;

            for (var i = parentElement.ChildNodes.Count - 1; i >= 0; i--)
            {
                var node = parentElement.ChildNodes[i];
                if (IsWhitespaceNode(node))
                    return node;
            }

            return null;
        }

        private static bool IsWhitespaceNode(XmlNode node)
        {
            if (node == null)
                return false;

            return node.NodeType is XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace
                || (node.NodeType == XmlNodeType.Text && string.IsNullOrWhiteSpace(node.Value));
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
