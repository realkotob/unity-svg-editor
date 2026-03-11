using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgModelSceneBuildResult
    {
        public Scene Scene { get; set; } = new();
        public Dictionary<SceneNode, (string Key, string TargetKey)> NodeMappings { get; } = new();
        public Dictionary<SceneNode, float> NodeOpacities { get; } = new();
        public Rect DocumentViewportRect { get; set; }
        public SvgPreserveAspectRatioMode PreserveAspectRatioMode { get; set; } = SvgPreserveAspectRatioMode.Meet;
    }
}
