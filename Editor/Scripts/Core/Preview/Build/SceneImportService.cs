using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.Core.Preview.Build
{
    internal static class SceneImportService
    {
        private const string PREVIEW_VECTOR_IMAGE_NAME = "VectorEditorPreview";
        private static readonly MethodInfo InternalBuildVectorImageMethod = ResolveInternalBuildVectorImageMethod();
        private static bool _loggedInternalBuildVectorImageFallback;

        public static Result<SVGParser.SceneInfo> ImportScene(string sourceText)
        {
            Result<SVGParser.SceneInfo> result;
            try
            {
                using var reader = new StringReader(sourceText);
                result = Result.Success(SVGParser.ImportSVG(reader, ViewportOptions.PreserveViewport));
            }
            catch (Exception ex)
            {
                result = Result.Failure<SVGParser.SceneInfo>($"Preview parse failed. {ex.Message}");
            }

            return result.Ensure(sceneInfo => sceneInfo.Scene?.Root != null, "Preview scene root was not created.");
        }

        public static bool TryImportScene(string sourceText, out SVGParser.SceneInfo sceneInfo, out string error)
        {
            Result<SVGParser.SceneInfo> result = ImportScene(sourceText);
            sceneInfo = result.GetValueOrDefault();
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        public static Rect ResolveProjectionRect(
            Rect documentViewportRect,
            Rect sceneBounds,
            Rect preferredViewportRect)
        {
            return documentViewportRect.width > 0f && documentViewportRect.height > 0f
                ? documentViewportRect
                : (preferredViewportRect.width > 0f && preferredViewportRect.height > 0f
                    ? preferredViewportRect
                    : sceneBounds);
        }

        public static VectorImage BuildPreviewVectorImage(SVGParser.SceneInfo sceneInfo, Rect previewRect)
        {
            IEnumerable<VectorUtils.Geometry> geometries = VectorUtils.TessellateScene(
                sceneInfo.Scene,
                PreviewBuildOptions.CreateTessellationOptions(),
                sceneInfo.NodeOpacity);

            return BuildPreviewVectorImage(
                geometries,
                previewRect,
                () => VectorUtils.BuildVectorImage(sceneInfo));
        }

        public static VectorImage BuildPreviewVectorImage(Scene scene, Dictionary<SceneNode, float> nodeOpacity, Rect previewRect)
        {
            IEnumerable<VectorUtils.Geometry> geometries = VectorUtils.TessellateScene(
                scene,
                PreviewBuildOptions.CreateTessellationOptions(),
                nodeOpacity);

            return BuildPreviewVectorImage(
                geometries,
                previewRect,
                () => VectorUtils.BuildVectorImage(geometries, PreviewBuildOptions.GRADIENT_RESOLUTION));
        }

        private static MethodInfo ResolveInternalBuildVectorImageMethod()
        {
            return typeof(VectorUtils).GetMethod(
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
        }

        private static VectorImage BuildPreviewVectorImage(
            IEnumerable<VectorUtils.Geometry> geometries,
            Rect previewRect,
            Func<VectorImage> fallbackBuilder)
        {
            string reflectionError = string.Empty;
            VectorImage vectorImage = TryBuildVectorImageWithPreviewRect(geometries, previewRect, out reflectionError);
            if (vectorImage == null)
            {
                LogReflectionFallback(reflectionError);
                vectorImage = fallbackBuilder();
            }

            return FinalizePreviewVectorImage(vectorImage);
        }

        private static VectorImage TryBuildVectorImageWithPreviewRect(
            IEnumerable<VectorUtils.Geometry> geometries,
            Rect previewRect,
            out string error)
        {
            error = string.Empty;
            if (InternalBuildVectorImageMethod == null)
                return null;

            try
            {
                VectorImage vectorImage = InternalBuildVectorImageMethod.Invoke(
                    null,
                    new object[]
                    {
                        geometries,
                        previewRect,
                        PreviewBuildOptions.GRADIENT_RESOLUTION
                    }) as VectorImage;

                if (vectorImage == null)
                    error = "Internal BuildVectorImage returned null.";

                return vectorImage;
            }
            catch (TargetInvocationException ex)
            {
                error = ex.InnerException?.Message ?? ex.Message;
                return null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static void LogReflectionFallback(string error)
        {
            if (string.IsNullOrWhiteSpace(error) || _loggedInternalBuildVectorImageFallback)
                return;

            _loggedInternalBuildVectorImageFallback = true;
            Debug.LogWarning($"SceneImportService falling back to the public VectorImage builder: {error}");
        }

        private static VectorImage FinalizePreviewVectorImage(VectorImage vectorImage)
        {
            if (vectorImage == null)
                return null;

            vectorImage.hideFlags = HideFlags.HideAndDontSave;
            vectorImage.name = PREVIEW_VECTOR_IMAGE_NAME;
            return vectorImage;
        }
    }
}
