using System;
using System.Collections.Generic;
namespace SvgEditor
{
    internal static class FeatureScanner
    {
        #region Public Methods

        public static FeatureScanResult Scan(string sourceText)
        {
            var result = new FeatureScanResult
            {
                IsValidXml = false
            };

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                result.ValidationMessage = "SVG source is empty.";
                return result;
            }

            if (!SvgDocumentXmlUtility.TryGetRootElement(sourceText, out _, out var root, out var error))
            {
                result.ValidationMessage = error;
                return result;
            }

            result.IsValidXml = true;
            foreach (var node in SvgDocumentXmlUtility.EnumerateElementsDepthFirst(root))
            {
                result.ElementCount++;
                var localName = node.LocalName;
                if (string.Equals(localName, SvgTagName.PATH, StringComparison.OrdinalIgnoreCase))
                {
                    result.PathElementCount++;
                }

                switch (localName)
                {
                    case SvgTagName.LINEAR_GRADIENT:
                        result.HasLinearGradient = true;
                        break;
                    case SvgTagName.RADIAL_GRADIENT:
                        result.HasRadialGradient = true;
                        break;
                    case SvgTagName.CLIP_PATH:
                        result.HasClipPath = true;
                        break;
                    case SvgTagName.MASK:
                        result.HasMask = true;
                        break;
                    case SvgTagName.FILTER:
                        result.HasFilter = true;
                        break;
                    case SvgTagName.TEXT:
                        result.HasText = true;
                        break;
                    case SvgTagName.TSPAN:
                        result.HasTspan = true;
                        break;
                    case SvgTagName.TEXT_PATH:
                        result.HasTextPath = true;
                        break;
                    case SvgTagName.IMAGE:
                        result.HasImage = true;
                        break;
                    case SvgTagName.USE:
                        result.HasUse = true;
                        break;
                    case SvgTagName.STYLE:
                        result.HasStyleTag = true;
                        break;
                }

                if (!result.HasTransformAttribute && node.Attributes != null)
                {
                    for (var i = 0; i < node.Attributes.Count; i++)
                    {
                        var attribute = node.Attributes[i];
                        if (attribute == null)
                        {
                            continue;
                        }

                        if (string.Equals(attribute.LocalName, SvgAttributeName.TRANSFORM, StringComparison.OrdinalIgnoreCase))
                        {
                            result.HasTransformAttribute = true;
                            break;
                        }
                    }
                }

            }

            return result;
        }

        #endregion Public Methods
    }
}
