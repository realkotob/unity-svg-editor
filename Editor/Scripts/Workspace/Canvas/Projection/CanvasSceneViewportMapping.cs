using UnityEngine;

namespace SvgEditor.Workspace.Canvas
{
    internal readonly struct CanvasSceneViewportMapping
    {
        public CanvasSceneViewportMapping(Rect sceneRect, CanvasViewportLayoutUtility.FrameContentLayout layout, Vector2 scale)
        {
            SceneRect = sceneRect;
            Layout = layout;
            Scale = scale;
        }

        public Rect SceneRect { get; }
        public CanvasViewportLayoutUtility.FrameContentLayout Layout { get; }
        public Vector2 Scale { get; }
    }
}
