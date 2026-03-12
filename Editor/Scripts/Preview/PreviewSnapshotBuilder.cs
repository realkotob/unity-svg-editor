using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.DocumentModel;

using SvgEditor;
using SvgEditor.Renderer;

namespace SvgEditor.Preview
{
    internal sealed class PreviewSnapshotBuilder
    {
        private readonly SvgDocumentModelSerializer _serializer = new();
        private readonly SvgModelSceneBuilder _sceneBuilder = new();

        public bool TryBuildSnapshot(
            string sourceText,
            Rect preferredViewportRect,
            out PreviewSnapshot snapshot,
            out string error)
        {
            return TryBuildImportedSnapshot(sourceText, preferredViewportRect, out snapshot, out error);
        }

        public bool TryBuildSnapshot(
            SvgDocumentModel documentModel,
            Rect preferredViewportRect,
            out PreviewSnapshot snapshot,
            out string error)
        {
            snapshot = new PreviewSnapshot();
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model root is unavailable.";
                return false;
            }

            if (!_sceneBuilder.TryBuild(documentModel, out SvgModelSceneBuildResult sceneBuildResult, out error))
            {
                if (!_serializer.TrySerialize(documentModel, out string sourceText, out error))
                    return false;

                if (!TryBuildImportedSnapshot(sourceText, preferredViewportRect, out snapshot, out error))
                    return false;
            }
            else if (!TryBuildSceneSnapshot(sceneBuildResult, preferredViewportRect, out snapshot, out error))
            {
                return false;
            }

            IReadOnlyList<PreviewTextOverlay> textOverlays = PreviewSnapshotTextBuilder.BuildTextOverlays(documentModel);
            IReadOnlyList<PreviewElementGeometry> textElements = PreviewSnapshotTextBuilder.BuildTextElements(
                textOverlays,
                snapshot.Elements?.Count ?? 0);

            if (textElements.Count > 0)
            {
                List<PreviewElementGeometry> mergedElements = new();
                if (snapshot.Elements != null)
                    mergedElements.AddRange(snapshot.Elements);

                mergedElements.AddRange(textElements);
                snapshot.Elements = mergedElements;

                if (PreviewSnapshotGeometryBuilder.TryBuildVisualContentBounds(snapshot.Elements, out Rect visualContentBounds))
                {
                    snapshot.VisualContentBounds = visualContentBounds;
                    snapshot.ProjectionRect = PreviewSnapshotSceneImportService.ResolveProjectionRect(
                        snapshot.DocumentViewportRect,
                        visualContentBounds,
                        preferredViewportRect);
                }
            }

            snapshot.TextOverlays = textOverlays;
            return true;
        }

        private static bool TryBuildSceneSnapshot(
            SvgModelSceneBuildResult sceneBuildResult,
            Rect preferredViewportRect,
            out PreviewSnapshot snapshot,
            out string error)
        {
            snapshot = new PreviewSnapshot();
            error = string.Empty;

            VectorImage previewVectorImage = null;
            try
            {
                IReadOnlyList<PreviewElementGeometry> elements = PreviewSnapshotGeometryBuilder.BuildElementBounds(
                    sceneBuildResult.Scene,
                    sceneBuildResult.NodeMappings,
                    sceneBuildResult.NodeOpacities);
                Rect fallbackVisualContentBounds = PreviewSnapshotGeometryBuilder.TryBuildSceneRootBounds(
                    sceneBuildResult.Scene,
                    sceneBuildResult.NodeOpacities,
                    out Rect sceneRootBounds)
                    ? sceneRootBounds
                    : default;
                Rect visualContentBounds = PreviewSnapshotGeometryBuilder.TryBuildVisualContentBounds(elements, out Rect resolvedVisualContentBounds)
                    ? resolvedVisualContentBounds
                    : fallbackVisualContentBounds;
                Rect projectionRect = PreviewSnapshotSceneImportService.ResolveProjectionRect(
                    sceneBuildResult.DocumentViewportRect,
                    visualContentBounds,
                    preferredViewportRect);

                previewVectorImage = PreviewSnapshotSceneImportService.BuildPreviewVectorImage(
                    sceneBuildResult.Scene,
                    sceneBuildResult.NodeOpacities,
                    projectionRect);
                snapshot = new PreviewSnapshot
                {
                    PreviewVectorImage = previewVectorImage,
                    DocumentViewportRect = sceneBuildResult.DocumentViewportRect,
                    ProjectionRect = projectionRect,
                    VisualContentBounds = visualContentBounds,
                    PreserveAspectRatioMode = sceneBuildResult.PreserveAspectRatioMode,
                    Elements = elements
                };
                return previewVectorImage != null;
            }
            catch (Exception ex)
            {
                if (previewVectorImage != null)
                    UnityEngine.Object.Destroy(previewVectorImage);

                error = $"Preview build failed: {ex.Message}";
                return false;
            }
        }

        internal static bool TryBuildImportedSnapshot(
            string sourceText,
            Rect preferredViewportRect,
            out PreviewSnapshot snapshot,
            out string error)
        {
            snapshot = new PreviewSnapshot();
            error = string.Empty;

            if (!PreviewSnapshotDocumentPreparation.TryPrepare(
                    sourceText,
                    out PreviewSnapshotPreparedDocument preparedDocument,
                    out error))
            {
                return false;
            }

            if (!PreviewSnapshotSceneImportService.TryImportScene(
                    preparedDocument.Document.OuterXml,
                    out SVGParser.SceneInfo sceneInfo,
                    out error))
            {
                return false;
            }

            VectorImage previewVectorImage = null;
            try
            {
                var elements = PreviewSnapshotGeometryBuilder.BuildElementBounds(sceneInfo, preparedDocument.KeyByNodeId);
                var fallbackVisualContentBounds = PreviewSnapshotGeometryBuilder.TryBuildSceneRootBounds(sceneInfo, out Rect sceneRootBounds)
                    ? sceneRootBounds
                    : VectorUtils.SceneNodeBounds(sceneInfo.Scene.Root);
                var visualContentBounds = PreviewSnapshotGeometryBuilder.TryBuildVisualContentBounds(elements, out Rect resolvedVisualContentBounds)
                    ? resolvedVisualContentBounds
                    : fallbackVisualContentBounds;
                Rect documentViewportRect = sceneInfo.SceneViewport;
                Rect projectionRect = PreviewSnapshotSceneImportService.ResolveProjectionRect(
                    documentViewportRect,
                    visualContentBounds,
                    preferredViewportRect);

                previewVectorImage = PreviewSnapshotSceneImportService.BuildPreviewVectorImage(sceneInfo, projectionRect);
                snapshot = new PreviewSnapshot
                {
                    PreviewVectorImage = previewVectorImage,
                    DocumentViewportRect = documentViewportRect,
                    ProjectionRect = projectionRect,
                    VisualContentBounds = visualContentBounds,
                    PreserveAspectRatioMode = preparedDocument.PreserveAspectRatioMode,
                    Elements = elements
                };
                return true;
            }
            catch (Exception ex)
            {
                if (previewVectorImage != null)
                    UnityEngine.Object.Destroy(previewVectorImage);

                error = $"Preview build failed: {ex.Message}";
                return false;
            }
        }
    }
}
