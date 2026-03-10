using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgCanvasRenderer
    {
        private readonly SvgDocumentModelSerializer _serializer = new();

        public bool TryBuildRenderDocument(
            SvgDocumentModel documentModel,
            out SvgRenderDocument renderDocument,
            out string error)
        {
            renderDocument = new SvgRenderDocument();
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model root is unavailable.";
                return false;
            }

            Dictionary<SvgNodeId, SvgRenderNode> nodes = new();
            List<SvgNodeId> drawOrder = new();

            for (var index = 0; index < documentModel.NodeOrder.Count; index++)
            {
                SvgNodeId nodeId = documentModel.NodeOrder[index];
                if (!documentModel.TryGetNode(nodeId, out SvgNodeModel node) || node == null)
                    continue;

                drawOrder.Add(nodeId);
                nodes[nodeId] = new SvgRenderNode
                {
                    NodeId = node.Id,
                    ParentNodeId = node.ParentId,
                    ElementKey = node.LegacyElementKey,
                    TargetKey = node.LegacyTargetKey,
                    TagName = node.TagName,
                    Depth = node.Depth,
                    DrawOrder = drawOrder.Count - 1,
                    IsDefinitionNode = node.IsDefinitionNode,
                    Children = new List<SvgNodeId>(node.Children)
                };
            }

            renderDocument = new SvgRenderDocument
            {
                RootNodeId = documentModel.RootId,
                Nodes = nodes,
                DrawOrder = drawOrder
            };
            return true;
        }

        public bool TryBuildPreviewSnapshot(
            SvgDocumentModel documentModel,
            UnityEngine.Rect preferredViewportRect,
            out PreviewSnapshot snapshot,
            out string error)
        {
            snapshot = new PreviewSnapshot();
            error = string.Empty;

            if (!TryBuildRenderDocument(documentModel, out _, out error))
                return false;

            if (!_serializer.TrySerialize(documentModel, out string sourceText, out error))
                return false;

            return PreviewSnapshotBuilder.TryBuildImportedSnapshot(
                sourceText,
                preferredViewportRect,
                out snapshot,
                out error);
        }
    }
}
