using System;
using SvgEditor.Workspace.Canvas;

namespace SvgEditor.Document
{
    internal static class DefinitionProxyUtility
    {
        public static string BuildProxyKey(string sourceElementKey, CanvasDefinitionOverlayKind kind, string referenceId)
        {
            string kindToken = kind == CanvasDefinitionOverlayKind.Mask ? "mask" : "clip";
            return $"__defproxy__::{sourceElementKey}::{kindToken}::{referenceId}";
        }

        public static string BuildProxyLabel(CanvasDefinitionOverlayKind kind, string referenceId)
        {
            string fallback = kind == CanvasDefinitionOverlayKind.Mask ? "mask" : "clip";
            if (string.IsNullOrWhiteSpace(referenceId))
                return fallback;

            return referenceId.Trim().Replace('_', ' ').Replace('-', ' ');
        }

        public static bool TryParseKind(string kindToken, out CanvasDefinitionOverlayKind kind)
        {
            if (string.Equals(kindToken, "mask", StringComparison.OrdinalIgnoreCase))
            {
                kind = CanvasDefinitionOverlayKind.Mask;
                return true;
            }

            if (string.Equals(kindToken, "clip", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kindToken, "clipPath", StringComparison.OrdinalIgnoreCase))
            {
                kind = CanvasDefinitionOverlayKind.ClipPath;
                return true;
            }

            kind = CanvasDefinitionOverlayKind.ClipPath;
            return false;
        }
    }
}
