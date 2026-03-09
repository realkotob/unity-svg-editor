using System;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewSnapshotBuilder
    {
        public bool TryBuildSnapshot(
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
                var fallbackVisualContentBounds = VectorUtils.SceneNodeBounds(sceneInfo.Scene.Root);
                var visualContentBounds = PreviewSnapshotGeometryBuilder.TryBuildVisualContentBounds(elements, out Rect resolvedVisualContentBounds)
                    ? resolvedVisualContentBounds
                    : fallbackVisualContentBounds;
                var documentViewportRect = PreviewSnapshotSceneImportService.ResolvePreviewRect(
                    sceneInfo,
                    visualContentBounds,
                    preferredViewportRect);

                previewVectorImage = PreviewSnapshotSceneImportService.BuildPreviewVectorImage(sceneInfo, documentViewportRect);
                snapshot = new PreviewSnapshot
                {
                    PreviewVectorImage = previewVectorImage,
                    DocumentViewportRect = documentViewportRect,
                    VisualContentBounds = visualContentBounds,
                    Elements = elements
                };
                return true;
            }
            catch (Exception ex)
            {
                if (previewVectorImage != null)
                    UnityEngine.Object.DestroyImmediate(previewVectorImage);

                error = $"Preview build failed: {ex.Message}";
                return false;
            }
        }
    }
}
