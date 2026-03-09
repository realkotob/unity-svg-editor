using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal static class PreviewSnapshotSceneImportService
    {
        private static readonly MethodInfo InternalBuildVectorImageMethod = typeof(VectorUtils).GetMethod(
            "BuildVectorImage",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(IEnumerable<VectorUtils.Geometry>),
                typeof(Rect),
                typeof(uint)
            },
            null);

        public static bool TryImportScene(string sourceText, out SVGParser.SceneInfo sceneInfo, out string error)
        {
            sceneInfo = default;
            error = string.Empty;

            try
            {
                using var reader = new StringReader(sourceText);
                sceneInfo = SVGParser.ImportSVG(reader, ViewportOptions.PreserveViewport);
            }
            catch (Exception ex)
            {
                error = $"Preview parse failed: {ex.Message}";
                return false;
            }

            if (sceneInfo.Scene?.Root != null)
                return true;

            error = "Preview scene root was not created.";
            return false;
        }

        public static Rect ResolvePreviewRect(SVGParser.SceneInfo sceneInfo, Rect sceneBounds)
        {
            return sceneInfo.SceneViewport.width > 0f && sceneInfo.SceneViewport.height > 0f
                ? sceneInfo.SceneViewport
                : sceneBounds;
        }

        public static VectorImage BuildPreviewVectorImage(SVGParser.SceneInfo sceneInfo, Rect previewRect)
        {
            if (InternalBuildVectorImageMethod == null)
                return FinalizePreviewVectorImage(VectorUtils.BuildVectorImage(sceneInfo));

            IEnumerable<VectorUtils.Geometry> geometries = VectorUtils.TessellateScene(
                sceneInfo.Scene,
                PreviewBuildOptions.CreateTessellationOptions(),
                sceneInfo.NodeOpacity);

            VectorImage vectorImage = InternalBuildVectorImageMethod.Invoke(
                null,
                new object[]
                {
                    geometries,
                    previewRect,
                    PreviewBuildOptions.GRADIENT_RESOLUTION
                }) as VectorImage;

            return FinalizePreviewVectorImage(vectorImage ?? VectorUtils.BuildVectorImage(sceneInfo));
        }

        private static VectorImage FinalizePreviewVectorImage(VectorImage vectorImage)
        {
            if (vectorImage == null)
                return null;

            vectorImage.hideFlags = HideFlags.HideAndDontSave;
            vectorImage.name = "VectorEditorPreview";
            return vectorImage;
        }
    }
}
