using Unity.VectorGraphics;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class PathEditSessionSyncController
    {
        private readonly ICanvasPointerDragHost _host;
        private readonly SceneProjector _sceneProjector;
        private readonly ToolController _toolController;
        private readonly OverlayController _overlayController;
        private readonly PathEditEntryController _entryController;

        public PathEditSessionSyncController(
            ICanvasPointerDragHost host,
            SceneProjector sceneProjector,
            ToolController toolController,
            OverlayController overlayController)
        {
            _host = host;
            _sceneProjector = sceneProjector;
            _toolController = toolController;
            _overlayController = overlayController;
            _entryController = new PathEditEntryController(toolController, overlayController);
        }

        public string ResyncActiveSession(bool previewIsCurrent)
        {
            if (_toolController.ActiveTool != ToolKind.PathEdit || !_overlayController.HasPathEditSession)
            {
                return string.Empty;
            }

            PathEditSession currentSession = _overlayController.CurrentPathEditSession;
            if (currentSession == null)
            {
                return string.Empty;
            }

            if (!previewIsCurrent)
            {
                return ApplyFailureResult(new PathEditEntryResult(
                    PathEditEntryResultKind.BlockedUnavailable,
                    "Path edit ended: preview is unavailable after document refresh.",
                    null));
            }

            PreviewElementGeometry previewElement = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, currentSession.ElementKey);
            if (previewElement == null)
            {
                return ApplyFailureResult(new PathEditEntryResult(
                    PathEditEntryResultKind.BlockedUnavailable,
                    "Path edit ended: preview geometry is unavailable after document refresh.",
                    null));
            }

            PathEditEntryResult result = _entryController.TryRebuild(
                new PathEditEntryRequest(
                    clickCount: 2,
                    currentDocument: _host.CurrentDocument,
                    elementKey: currentSession.ElementKey,
                    worldTransform: previewElement.WorldTransform,
                    sceneToViewportPoint: scenePoint =>
                        _sceneProjector.TryScenePointToViewportPoint(_host.PreviewSnapshot, scenePoint, out var viewportPoint)
                            ? viewportPoint
                            : null),
                currentSession);

            if (result.Kind != PathEditEntryResultKind.Entered)
            {
                return ApplyFailureResult(result);
            }

            return string.Empty;
        }

        private string ApplyFailureResult(PathEditEntryResult result)
        {
            if (result.Kind == PathEditEntryResultKind.Entered)
            {
                return string.Empty;
            }

            _toolController.SetActiveTool(ToolKind.Move);
            _overlayController.ClearPathEditSession();
            _host.UpdateSourceStatus(result.StatusMessage);
            return result.StatusMessage;
        }
    }
}
