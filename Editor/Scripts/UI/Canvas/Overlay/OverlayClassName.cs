using SvgEditor;
using SvgEditor.Core.Preview;
using Core.UI.Extensions;

namespace SvgEditor.UI.Canvas
{
    internal static class OverlayClassName
    {
        private const string Prefix = "svg-editor__";

        public const string OVERLAY = Prefix + "canvas-overlay";
        public const string OVERLAY_INTERACTIVE_HIT = Prefix + "canvas-overlay--interactive-hit";
        public const string FRAME_LABEL = Prefix + "canvas-frame-label";
        public const string ALIGNMENT_GUIDE = Prefix + "alignment-guide";
        public const string ALIGNMENT_GUIDE_VERTICAL = ALIGNMENT_GUIDE + "--vertical";
        public const string ALIGNMENT_GUIDE_HORIZONTAL = ALIGNMENT_GUIDE + "--horizontal";
        public const string HOVER_BOX = Prefix + "hover-box";

        public const string SELECTION_BOX = Prefix + "selection-box";
        public const string SELECTION_BOX_FRAME = SELECTION_BOX + "--frame";
        public const string SELECTION_SIZE_BADGE = Prefix + "selection-size-badge";
        public const string SELECTION_HANDLE = Prefix + "selection-handle";
        public const string SELECTION_HANDLE_TOP_LEFT = SELECTION_HANDLE + "--top-left";
        public const string SELECTION_HANDLE_TOP = SELECTION_HANDLE + "--top";
        public const string SELECTION_HANDLE_TOP_RIGHT = SELECTION_HANDLE + "--top-right";
        public const string SELECTION_HANDLE_RIGHT = SELECTION_HANDLE + "--right";
        public const string SELECTION_HANDLE_BOTTOM_RIGHT = SELECTION_HANDLE + "--bottom-right";
        public const string SELECTION_HANDLE_BOTTOM = SELECTION_HANDLE + "--bottom";
        public const string SELECTION_HANDLE_BOTTOM_LEFT = SELECTION_HANDLE + "--bottom-left";
        public const string SELECTION_HANDLE_LEFT = SELECTION_HANDLE + "--left";

        public const string EDGE_ZONE = Prefix + "edge-zone";
        public const string EDGE_ZONE_VERTICAL = EDGE_ZONE + "--vertical";
        public const string EDGE_ZONE_HORIZONTAL = EDGE_ZONE + "--horizontal";

        public const string ROTATION_ZONE = Prefix + "rotation-zone";

        public const string DEFINITION_BOUNDS = Prefix + "definition-overlay-bounds";
        public const string DEFINITION_BOUNDS_MASK = DEFINITION_BOUNDS + "--mask";
        public const string DEFINITION_BOUNDS_CLIP = DEFINITION_BOUNDS + "--clip";
        public const string PATH_LINE = "path-line";
        public const string BEZIER_HANDLE_LINE = "bezier-handle-line";

        public const string PATH_ANCHOR = "path-anchor";
        public const string PATH_ANCHOR_ACTIVE = PATH_ANCHOR + "--active";
        public const string BEZIER_HANDLE = "bezier-handle";
        public const string BEZIER_HANDLE_ACTIVE = BEZIER_HANDLE + "--active";
    }
}
