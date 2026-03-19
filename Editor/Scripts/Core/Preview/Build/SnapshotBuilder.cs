using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Serialization;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Preview.Geometry;
using SvgEditor.Core.Preview.Rendering;
using SvgEditor.Core.Preview.Text;
using SvgEditor.Renderer;

namespace SvgEditor.Core.Preview.Build
{
    internal sealed class SnapshotBuilder
    {
        #region Variables
        private readonly SvgSerializer _serializer = new();
        private readonly SvgModelSceneBuilder _sceneBuilder = new();
        #endregion Variables

        #region Public Methods
        public bool TryBuildSnapshot(
            SvgDocumentModel documentModel,
            Rect preferredViewportRect,
            out PreviewSnapshot snapshot,
            out string error)
        {
            Result<PreviewSnapshot> result = BuildSnapshot(documentModel, preferredViewportRect);
            snapshot = result.GetValueOrDefault(new PreviewSnapshot());
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }
        #endregion Public Methods

        #region Help Methods
        private Result<PreviewSnapshot> BuildSnapshot(SvgDocumentModel documentModel, Rect preferredViewportRect)
        {
            if (documentModel?.Root == null)
            {
                return Result.Failure<PreviewSnapshot>("Document model root is unavailable.");
            }

            Result<PreviewSnapshot> result = TryBuildScene(documentModel)
                .Bind(sceneBuildResult => BuildSceneSnapshot(sceneBuildResult, preferredViewportRect));

            if (result.IsFailure)
            {
                result = SerializeDocumentModel(documentModel)
                    .Bind(sourceText => BuildImportedSnapshot(sourceText, preferredViewportRect));
            }

            return result.Map(snapshot => AppendTextOverlays(snapshot, documentModel, preferredViewportRect));
        }

        private Result<SvgModelSceneBuildResult> TryBuildScene(SvgDocumentModel documentModel)
        {
            return _sceneBuilder.TryBuild(documentModel, out SvgModelSceneBuildResult sceneBuildResult, out string error)
                ? Result.Success(sceneBuildResult)
                : Result.Failure<SvgModelSceneBuildResult>(error);
        }

        private Result<string> SerializeDocumentModel(SvgDocumentModel documentModel)
        {
            return _serializer.TrySerialize(documentModel, out string sourceText, out string error)
                ? Result.Success(sourceText)
                : Result.Failure<string>(error);
        }

        private static Result<PreviewSnapshot> BuildSceneSnapshot(
            SvgModelSceneBuildResult sceneBuildResult,
            Rect preferredViewportRect)
        {
            VectorImage previewVectorImage = null;
            try
            {
                IReadOnlyList<PreviewElementGeometry> elements = SnapshotGeometryBuilder.BuildElementBounds(
                    sceneBuildResult.Scene,
                    sceneBuildResult.NodeMappings,
                    sceneBuildResult.NodeOpacities);
                Rect fallbackVisualContentBounds = SnapshotGeometryBuilder.TryBuildSceneRootBounds(
                    sceneBuildResult.Scene,
                    sceneBuildResult.NodeOpacities,
                    out Rect sceneRootBounds)
                    ? sceneRootBounds
                    : default;
                Rect visualContentBounds = GeometryBoundsUtility.TryBuildVisualContentBounds(elements, out Rect resolvedVisualContentBounds)
                    ? resolvedVisualContentBounds
                    : fallbackVisualContentBounds;
                Rect projectionRect = SceneImportService.ResolveProjectionRect(
                    sceneBuildResult.DocumentViewportRect,
                    visualContentBounds,
                    preferredViewportRect);

                previewVectorImage = SceneImportService.BuildPreviewVectorImage(
                    sceneBuildResult.Scene,
                    sceneBuildResult.NodeOpacities,
                    projectionRect);

                PreviewSnapshot snapshot = new()
                {
                    PreviewVectorImage = previewVectorImage,
                    DocumentViewportRect = sceneBuildResult.DocumentViewportRect,
                    ProjectionRect = projectionRect,
                    VisualContentBounds = visualContentBounds,
                    PreserveAspectRatioMode = sceneBuildResult.PreserveAspectRatioMode,
                    Elements = elements
                };
                return previewVectorImage != null
                    ? Result.Success(snapshot)
                    : Result.Failure<PreviewSnapshot>("Preview vector image was not created.");
            }
            catch (Exception ex)
            {
                if (previewVectorImage != null)
                    UnityEngine.Object.Destroy(previewVectorImage);

                return Result.Failure<PreviewSnapshot>($"Preview build failed: {ex.Message}");
            }
        }

        private static Result<PreviewSnapshot> BuildImportedSnapshot(string sourceText, Rect preferredViewportRect)
        {
            return PreviewDocumentPreparation
                .Prepare(sourceText)
                .Bind(preparedDocument => SceneImportService
                    .ImportScene(preparedDocument.Document.OuterXml)
                    .Bind(sceneInfo => BuildImportedSnapshot(sceneInfo, preparedDocument, preferredViewportRect)));
        }

        private static Result<PreviewSnapshot> BuildImportedSnapshot(
            SVGParser.SceneInfo sceneInfo,
            PreparedPreviewDocument preparedDocument,
            Rect preferredViewportRect)
        {
            VectorImage previewVectorImage = null;
            try
            {
                var elements = SnapshotGeometryBuilder.BuildElementBounds(sceneInfo, preparedDocument.KeyByNodeId);
                var fallbackVisualContentBounds = SnapshotGeometryBuilder.TryBuildSceneRootBounds(sceneInfo, out Rect sceneRootBounds)
                    ? sceneRootBounds
                    : VectorUtils.SceneNodeBounds(sceneInfo.Scene.Root);
                var visualContentBounds = GeometryBoundsUtility.TryBuildVisualContentBounds(elements, out Rect resolvedVisualContentBounds)
                    ? resolvedVisualContentBounds
                    : fallbackVisualContentBounds;
                Rect documentViewportRect = sceneInfo.SceneViewport;
                Rect projectionRect = SceneImportService.ResolveProjectionRect(
                    documentViewportRect,
                    visualContentBounds,
                    preferredViewportRect);

                previewVectorImage = SceneImportService.BuildPreviewVectorImage(sceneInfo, projectionRect);
                PreviewSnapshot previewSnapshot = new()
                {
                    PreviewVectorImage = previewVectorImage,
                    DocumentViewportRect = documentViewportRect,
                    ProjectionRect = projectionRect,
                    VisualContentBounds = visualContentBounds,
                    PreserveAspectRatioMode = preparedDocument.PreserveAspectRatioMode,
                    Elements = elements
                };
                return previewVectorImage != null
                    ? Result.Success(previewSnapshot)
                    : Result.Failure<PreviewSnapshot>("Preview vector image was not created.");
            }
            catch (Exception ex)
            {
                if (previewVectorImage != null)
                    UnityEngine.Object.Destroy(previewVectorImage);

                return Result.Failure<PreviewSnapshot>($"Preview build failed: {ex.Message}");
            }
        }

        private static PreviewSnapshot AppendTextOverlays(
            PreviewSnapshot snapshot,
            SvgDocumentModel documentModel,
            Rect preferredViewportRect)
        {
            IReadOnlyList<PreviewTextOverlay> textOverlays = SnapshotTextBuilder.BuildTextOverlays(documentModel);
            IReadOnlyList<PreviewElementGeometry> textElements = SnapshotTextBuilder.BuildTextElements(
                textOverlays,
                snapshot.Elements?.Count ?? 0);

            if (textElements.Count > 0)
            {
                List<PreviewElementGeometry> mergedElements = new();
                if (snapshot.Elements != null)
                {
                    mergedElements.AddRange(snapshot.Elements);
                }

                mergedElements.AddRange(textElements);
                snapshot.Elements = mergedElements;

                if (GeometryBoundsUtility.TryBuildVisualContentBounds(snapshot.Elements, out Rect visualContentBounds))
                {
                    snapshot.VisualContentBounds = visualContentBounds;
                    snapshot.ProjectionRect = SceneImportService.ResolveProjectionRect(
                        snapshot.DocumentViewportRect,
                        visualContentBounds,
                        preferredViewportRect);
                }
            }

            snapshot.TextOverlays = textOverlays;
            return snapshot;
        }
        #endregion Help Methods
    }
}
