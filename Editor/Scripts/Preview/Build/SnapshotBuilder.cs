using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.DocumentModel;
using SvgEditor.Preview;
using SvgEditor.Preview.Geometry;
using SvgEditor.Preview.Text;

using SvgEditor;
using SvgEditor.Renderer;

namespace SvgEditor.Preview.Build
{
    internal sealed class SnapshotBuilder
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

            IReadOnlyList<PreviewTextOverlay> textOverlays = SnapshotTextBuilder.BuildTextOverlays(documentModel);
            IReadOnlyList<PreviewElementGeometry> textElements = SnapshotTextBuilder.BuildTextElements(
                textOverlays,
                snapshot.Elements?.Count ?? 0);

            if (textElements.Count > 0)
            {
                List<PreviewElementGeometry> mergedElements = new();
                if (snapshot.Elements != null)
                    mergedElements.AddRange(snapshot.Elements);

                mergedElements.AddRange(textElements);
                snapshot.Elements = mergedElements;

                if (SnapshotGeometryBuilder.TryBuildVisualContentBounds(snapshot.Elements, out Rect visualContentBounds))
                {
                    snapshot.VisualContentBounds = visualContentBounds;
                    snapshot.ProjectionRect = SceneImportService.ResolveProjectionRect(
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
                Rect visualContentBounds = SnapshotGeometryBuilder.TryBuildVisualContentBounds(elements, out Rect resolvedVisualContentBounds)
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

            if (!SnapshotDocumentPreparation.TryPrepare(
                    sourceText,
                    out PreparedSnapshotDocument preparedDocument,
                    out error))
            {
                return false;
            }

            if (!SceneImportService.TryImportScene(
                    preparedDocument.Document.OuterXml,
                    out SVGParser.SceneInfo sceneInfo,
                    out error))
            {
                return false;
            }

            VectorImage previewVectorImage = null;
            try
            {
                var elements = SnapshotGeometryBuilder.BuildElementBounds(sceneInfo, preparedDocument.KeyByNodeId);
                var fallbackVisualContentBounds = SnapshotGeometryBuilder.TryBuildSceneRootBounds(sceneInfo, out Rect sceneRootBounds)
                    ? sceneRootBounds
                    : VectorUtils.SceneNodeBounds(sceneInfo.Scene.Root);
                var visualContentBounds = SnapshotGeometryBuilder.TryBuildVisualContentBounds(elements, out Rect resolvedVisualContentBounds)
                    ? resolvedVisualContentBounds
                    : fallbackVisualContentBounds;
                Rect documentViewportRect = sceneInfo.SceneViewport;
                Rect projectionRect = SceneImportService.ResolveProjectionRect(
                    documentViewportRect,
                    visualContentBounds,
                    preferredViewportRect);

                previewVectorImage = SceneImportService.BuildPreviewVectorImage(sceneInfo, projectionRect);
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
