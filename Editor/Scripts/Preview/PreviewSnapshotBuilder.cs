using System;
using Unity.VectorGraphics;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class PreviewSnapshotBuilder
    {
        public bool TryBuildSnapshot(string sourceText, out PreviewSnapshot snapshot, out string error)
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
                var sceneBounds = VectorUtils.SceneNodeBounds(sceneInfo.Scene.Root);
                var previewRect = PreviewSnapshotSceneImportService.ResolvePreviewRect(sceneInfo, sceneBounds);

                previewVectorImage = PreviewSnapshotSceneImportService.BuildPreviewVectorImage(sceneInfo, previewRect);
                snapshot = new PreviewSnapshot
                {
                    PreviewVectorImage = previewVectorImage,
                    SceneViewport = sceneInfo.SceneViewport,
                    SceneBounds = sceneBounds,
                    Elements = PreviewSnapshotGeometryBuilder.BuildElementBounds(sceneInfo, preparedDocument.KeyByNodeId)
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
