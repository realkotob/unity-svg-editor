using System;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewSnapshotBuilder
    {
        private readonly SvgCanvasRenderer _canvasRenderer = new();

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
            return _canvasRenderer.TryBuildPreviewSnapshot(documentModel, preferredViewportRect, out snapshot, out error);
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
