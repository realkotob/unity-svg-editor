using System;
using System.Collections.Generic;
namespace UnitySvgEditor.Editor
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
                if (string.Equals(localName, "path", StringComparison.OrdinalIgnoreCase))
                {
                    result.PathElementCount++;
                }

                switch (localName)
                {
                    case "linearGradient":
                        result.HasLinearGradient = true;
                        break;
                    case "radialGradient":
                        result.HasRadialGradient = true;
                        break;
                    case "clipPath":
                        result.HasClipPath = true;
                        break;
                    case "mask":
                        result.HasMask = true;
                        break;
                    case "filter":
                        result.HasFilter = true;
                        break;
                    case "text":
                        result.HasText = true;
                        break;
                    case "tspan":
                        result.HasTspan = true;
                        break;
                    case "textPath":
                        result.HasTextPath = true;
                        break;
                    case "image":
                        result.HasImage = true;
                        break;
                    case "use":
                        result.HasUse = true;
                        break;
                    case "style":
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

                        if (string.Equals(attribute.LocalName, "transform", StringComparison.OrdinalIgnoreCase))
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
