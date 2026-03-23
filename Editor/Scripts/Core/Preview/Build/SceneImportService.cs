using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;


namespace SvgEditor.Core.Preview.Build
{
    internal static class SceneImportService
    {
        #region Constants
        private const string PREVIEW_VECTOR_IMAGE_NAME = "VectorEditorPreview";
        private const uint GRADIENT_RESOLUTION = 32u;
        #endregion Constants

        #region Variables
        private static readonly VectorUtils.TessellationOptions DefaultTessellationOptions = new()
        {
            StepDistance = 1f,
            SamplingStepSize = 0.1f,
            MaxCordDeviation = 0.05f,
            MaxTanAngleDeviation = 0.02f
        };

        private static readonly MethodInfo InternalBuildVectorImageMethod = ResolveInternalBuildVectorImageMethod();
        private static readonly FieldInfo VectorImageVerticesField = typeof(VectorImage).GetField("vertices", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo VectorImageIndicesField = typeof(VectorImage).GetField("indices", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        #endregion Variables

        #region Public Methods
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
                DefaultTessellationOptions,
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
                DefaultTessellationOptions,
                nodeOpacity);

            return BuildPreviewVectorImage(
                geometries,
                previewRect,
                () => VectorUtils.BuildVectorImage(geometries, GRADIENT_RESOLUTION));
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
            Result<VectorImage> reflectionResult = BuildVectorImageWithPreviewRect(geometries, previewRect);
            if (reflectionResult.IsFailure)
            {
                return FinalizePreviewVectorImage(fallbackBuilder());
            }

            if (!HasRenderableGeometry(reflectionResult.Value))
            {
                return FinalizePreviewVectorImage(fallbackBuilder());
            }

            return FinalizePreviewVectorImage(reflectionResult.Value);
        }
        #endregion Public Methods

        #region Help Methods
        private static Result<VectorImage> BuildVectorImageWithPreviewRect(
            IEnumerable<VectorUtils.Geometry> geometries,
            Rect previewRect)
        {
            if (InternalBuildVectorImageMethod == null)
                return Result.Failure<VectorImage>("Internal BuildVectorImage method is unavailable.");

            try
            {
                VectorImage vectorImage = InternalBuildVectorImageMethod.Invoke(
                    null,
                    new object[]
                    {
                        geometries,
                        previewRect,
                        GRADIENT_RESOLUTION
                    }) as VectorImage;

                return vectorImage != null
                    ? Result.Success(vectorImage)
                    : Result.Failure<VectorImage>("Internal BuildVectorImage returned null.");
            }
            catch (TargetInvocationException ex)
            {
                return Result.Failure<VectorImage>(ex.InnerException?.Message ?? ex.Message);
            }
            catch (Exception ex)
            {
                return Result.Failure<VectorImage>(ex.Message);
            }
        }

        private static VectorImage FinalizePreviewVectorImage(VectorImage vectorImage)
        {
            if (vectorImage == null)
                return null;

            vectorImage.hideFlags = HideFlags.HideAndDontSave;
            vectorImage.name = PREVIEW_VECTOR_IMAGE_NAME;
            return vectorImage;
        }

        internal static bool HasRenderableGeometry(VectorImage vectorImage)
        {
            if (vectorImage == null)
            {
                return false;
            }

            Array vertices = VectorImageVerticesField?.GetValue(vectorImage) as Array;
            Array indices = VectorImageIndicesField?.GetValue(vectorImage) as Array;
            return vertices != null &&
                   indices != null &&
                   vertices.Length > 0 &&
                   indices.Length > 0;
        }
        #endregion Help Methods
    }
}
