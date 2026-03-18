using Unity.VectorGraphics;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.Preview.Build
{
    internal static class BuildOptions
    {
        public const uint GRADIENT_RESOLUTION = 32u;

        public static VectorUtils.TessellationOptions CreateTessellationOptions()
        {
            return new VectorUtils.TessellationOptions
            {
                StepDistance = 1f,
                SamplingStepSize = 0.1f,
                MaxCordDeviation = 0.05f,
                MaxTanAngleDeviation = 0.02f
            };
        }
    }
}
