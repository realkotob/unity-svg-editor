using System;
using Unity.VectorGraphics;
using SvgEditor.Core.Shared;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.PathEditing;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Svg.Structure.Lookup;

namespace SvgEditor.UI.Canvas
{
    internal sealed class PathEditEntryController
    {
        private readonly ToolController _toolController;
        private readonly OverlayController _overlayController;

        public PathEditEntryController(
            ToolController toolController,
            OverlayController overlayController)
        {
            _toolController = toolController;
            _overlayController = overlayController;
        }

        public PathEditEntryResult TryEnter(PathEditEntryRequest request)
        {
            if (request.ClickCount < 2)
            {
                return new PathEditEntryResult(PathEditEntryResultKind.Ignored, string.Empty, null);
            }

            if (!TryResolveEditableShapeNode(request.CurrentDocument, request.ElementKey, out SvgNodeModel pathNode))
            {
                return new PathEditEntryResult(PathEditEntryResultKind.Ignored, string.Empty, null);
            }

            if (!PrimitivePathConversion.TryCreateEditablePathData(pathNode, out PathData pathData, out _))
            {
                return new PathEditEntryResult(PathEditEntryResultKind.Ignored, string.Empty, null);
            }
            if (pathData.HasUnsupportedCommands)
            {
                string status = BuildUnsupportedStatus(pathData);
                ClearActiveSession();
                return new PathEditEntryResult(PathEditEntryResultKind.BlockedUnsupportedPathData, status, null);
            }

            if (pathData.IsMalformed)
            {
                string status = BuildMalformedStatus(pathData);
                ClearActiveSession();
                return new PathEditEntryResult(PathEditEntryResultKind.BlockedMalformedPathData, status, null);
            }

            if (!TryBuildSession(request, pathData, out PathEditSession session, out string error))
            {
                string status = string.IsNullOrWhiteSpace(error)
                    ? "Path edit is unavailable: preview projection is unavailable."
                    : error;
                ClearActiveSession();
                return new PathEditEntryResult(PathEditEntryResultKind.BlockedUnavailable, status, null);
            }

            _toolController.SetActiveTool(ToolKind.PathEdit);
            _overlayController.SetPathEditSession(session);
            return new PathEditEntryResult(PathEditEntryResultKind.Entered, string.Empty, session);
        }

        public PathEditEntryResult TryRebuild(PathEditEntryRequest request, PathEditSession previousSession)
        {
            if (!TryResolveEditableShapeNode(request.CurrentDocument, request.ElementKey, out SvgNodeModel pathNode))
            {
                ClearActiveSession();
                return new PathEditEntryResult(
                    PathEditEntryResultKind.BlockedUnavailable,
                    "Path edit ended: the active path is no longer available after document refresh.",
                    null);
            }

            if (!PrimitivePathConversion.TryCreateEditablePathData(pathNode, out PathData pathData, out _))
            {
                ClearActiveSession();
                return new PathEditEntryResult(
                    PathEditEntryResultKind.BlockedUnavailable,
                    "Path edit ended: the active path no longer has editable path data.",
                    null);
            }

            if (pathData.HasUnsupportedCommands)
            {
                ClearActiveSession();
                return new PathEditEntryResult(
                    PathEditEntryResultKind.BlockedUnsupportedPathData,
                    BuildUnsupportedResyncStatus(pathData),
                    null);
            }

            if (pathData.IsMalformed)
            {
                ClearActiveSession();
                return new PathEditEntryResult(
                    PathEditEntryResultKind.BlockedMalformedPathData,
                    BuildMalformedResyncStatus(pathData),
                    null);
            }

            if (!TryBuildSession(request, pathData, out PathEditSession session, out string error))
            {
                ClearActiveSession();
                return new PathEditEntryResult(
                    PathEditEntryResultKind.BlockedUnavailable,
                    BuildUnavailableResyncStatus(error),
                    null);
            }

            session.RestoreSelectionState(
                previousSession?.Selection.ActiveNode,
                previousSession?.Selection.ActiveHandle);
            _toolController.SetActiveTool(ToolKind.PathEdit);
            _overlayController.SetPathEditSession(session);
            return new PathEditEntryResult(PathEditEntryResultKind.Entered, string.Empty, session);
        }

        private void ClearActiveSession()
        {
            _toolController.SetActiveTool(ToolKind.Move);
            _overlayController.ClearPathEditSession();
        }

        private static bool TryResolveEditableShapeNode(DocumentSession currentDocument, string elementKey, out SvgNodeModel pathNode)
        {
            pathNode = null;
            if (currentDocument?.DocumentModel == null ||
                string.IsNullOrWhiteSpace(elementKey) ||
                !NodeLookup.TryFindNodeByLegacyElementKey(currentDocument.DocumentModel, elementKey, out pathNode) ||
                pathNode == null ||
                !IsEditableShapeTag(pathNode.TagName) ||
                pathNode.RawAttributes == null)
            {
                pathNode = null;
                return false;
            }

            return true;
        }

        private static bool IsEditableShapeTag(string tagName)
        {
            return string.Equals(tagName, SvgTagName.PATH, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.LINE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.RECT, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.CIRCLE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.ELLIPSE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.POLYLINE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.POLYGON, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildUnsupportedStatus(PathData pathData)
        {
            string commands = string.Join(", ", pathData.UnsupportedCommands);
            return $"Path edit is unavailable: unsupported path commands ({commands}). The path remains read-only.";
        }

        private static string BuildMalformedStatus(PathData pathData)
        {
            string parseError = string.IsNullOrWhiteSpace(pathData.ParseError)
                ? "Path data is malformed."
                : pathData.ParseError;
            return $"Path edit is unavailable: malformed path data. {parseError} The path remains read-only.";
        }

        private static string BuildUnsupportedResyncStatus(PathData pathData)
        {
            string commands = string.Join(", ", pathData.UnsupportedCommands);
            return $"Path edit ended: unsupported path commands ({commands}) after document refresh.";
        }

        private static string BuildMalformedResyncStatus(PathData pathData)
        {
            string parseError = string.IsNullOrWhiteSpace(pathData.ParseError)
                ? "Path data is malformed."
                : pathData.ParseError;
            return $"Path edit ended: malformed path data after document refresh. {parseError}";
        }

        private static string BuildUnavailableResyncStatus(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return "Path edit ended: preview projection is unavailable after document refresh.";
            }

            return $"Path edit ended: {error}";
        }

        private static bool TryBuildSession(
            PathEditEntryRequest request,
            PathData pathData,
            out PathEditSession session,
            out string error)
        {
            session = null;
            error = string.Empty;

            if (pathData == null || request.SceneToViewportPoint == null)
            {
                error = "Path edit is unavailable: preview projection is unavailable.";
                return false;
            }

            session = new PathEditSession(request.ElementKey, request.WorldTransform, request.SceneToViewportPoint);
            if (!session.TrySetPathData(pathData, out error))
            {
                session = null;
                return false;
            }

            return true;
        }
    }
}
