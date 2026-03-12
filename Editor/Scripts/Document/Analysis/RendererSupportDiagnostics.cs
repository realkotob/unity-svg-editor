using System.Collections.Generic;

namespace SvgEditor
{
    internal static class RendererSupportDiagnostics
    {
        public static string BuildConsoleWarning(FeatureScanResult scanResult)
        {
            if (scanResult == null || !scanResult.IsValidXml)
                return string.Empty;

            List<string> fallbackFeatures = new();
            if (scanResult.HasFilter)
                fallbackFeatures.Add("filter");
            if (scanResult.HasImage)
                fallbackFeatures.Add("image");
            if (scanResult.HasStyleTag)
                fallbackFeatures.Add("style");
            if (scanResult.HasTspan)
                fallbackFeatures.Add("tspan-edit");
            if (scanResult.HasTextPath)
                fallbackFeatures.Add("textPath");

            if (fallbackFeatures.Count == 0)
                return string.Empty;

            return $"[UnitySvgEditor] Renderer fallback likely for: {string.Join(", ", fallbackFeatures)}";
        }
    }
}
