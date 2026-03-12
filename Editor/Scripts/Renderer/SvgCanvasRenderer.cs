using System.Collections.Generic;
using UnityEngine;
using SvgEditor.DocumentModel;

using SvgEditor;
using SvgEditor.Preview;
using SvgEditor.Preview.Build;
using SvgEditor.RenderModel;

namespace SvgEditor.Renderer
{
    internal sealed class SvgCanvasRenderer
    {
        private readonly SnapshotBuilder _previewSnapshotBuilder = new();

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
            Rect preferredViewportRect,
            out PreviewSnapshot snapshot,
            out string error)
        {
            return _previewSnapshotBuilder.TryBuildSnapshot(
                documentModel,
                preferredViewportRect,
                out snapshot,
                out error);
        }
    }
}
