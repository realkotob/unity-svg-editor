using System;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.DocumentModel;

namespace SvgEditor.Document
{
    internal static class SvgInheritedAttributeResolver
    {
        public static bool TryGetInheritedAttribute(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            string name,
            out string value)
        {
            value = string.Empty;
            var current = node;
            while (current != null)
            {
                if (SvgAttributeUtility.TryGetAttribute(current.RawAttributes, name, out value) &&
                    !string.Equals(value, "inherit", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (documentModel == null || current.Id.IsRoot)
                {
                    break;
                }

                if (!documentModel.TryGetNode(current.ParentId, out current))
                {
                    break;
                }
            }

            value = string.Empty;
            return false;
        }

        public static bool TryGetInheritedFloat(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            string name,
            out float value)
        {
            value = 0f;
            return TryGetInheritedAttribute(documentModel, node, name, out var text) &&
                   SvgAttributeUtility.TryParseFloat(text, out value);
        }

        public static PathEnding ResolvePathEnding(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            string attributeName)
        {
            if (!TryGetInheritedAttribute(documentModel, node, attributeName, out var value))
            {
                return PathEnding.Chop;
            }

            return value switch
            {
                "round" => PathEnding.Round,
                "square" => PathEnding.Square,
                _ => PathEnding.Chop
            };
        }

        public static PathCorner ResolvePathCorner(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            string attributeName)
        {
            if (!TryGetInheritedAttribute(documentModel, node, attributeName, out var value))
            {
                return PathCorner.Tipped;
            }

            return value switch
            {
                "round" => PathCorner.Round,
                "bevel" => PathCorner.Beveled,
                _ => PathCorner.Tipped
            };
        }

        public static FillMode ResolveFillMode(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            return TryGetInheritedAttribute(documentModel, node, "fill-rule", out var fillRule) &&
                   string.Equals(fillRule, "evenodd", StringComparison.OrdinalIgnoreCase)
                ? FillMode.OddEven
                : FillMode.NonZero;
        }

        public static float ResolveFillOpacity(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            return ResolveOpacity(documentModel, node, "fill-opacity");
        }

        public static float ResolveStrokeOpacity(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            return ResolveOpacity(documentModel, node, "stroke-opacity");
        }

        private static float ResolveOpacity(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            string attributeName)
        {
            var value = 1f;
            if (TryGetInheritedFloat(documentModel, node, attributeName, out var resolved))
            {
                value *= Mathf.Clamp01(resolved);
            }

            return Mathf.Clamp01(value);
        }
    }
}
