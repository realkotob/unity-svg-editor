using System.Collections.Generic;
using Unity.VectorGraphics;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Structure.Lookup;
using SvgEditor.Core.Svg.Transforms;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.Core.Preview.Rendering
{
    internal sealed class SvgShapeBuilder
    {
        internal readonly struct RenderBuildContext
        {
            public RenderBuildContext(
                SvgDocumentModel documentModel,
                IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
                SceneNode sceneNode)
            {
                DocumentModel = documentModel;
                NodesByXmlId = nodesByXmlId;
                SceneNode = sceneNode;
            }

            public SvgDocumentModel DocumentModel { get; }
            public IReadOnlyDictionary<string, SvgNodeModel> NodesByXmlId { get; }
            public SceneNode SceneNode { get; }
        }

        private readonly SvgPrimitiveShapeBuilder _primitiveShapeBuilder = new();

        public bool TryBuildShapes(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (node == null || sceneNode == null)
                return true;

            RenderBuildContext context = new(documentModel, nodesByXmlId, sceneNode);

            switch (node.TagName)
            {
                case SvgTagName.GROUP:
                case SvgTagName.SVG:
                    return true;
                case SvgTagName.RECT:
                    return _primitiveShapeBuilder.TryAddRectShape(context, node, out error);
                case SvgTagName.CIRCLE:
                    return _primitiveShapeBuilder.TryAddCircleShape(context, node, out error);
                case SvgTagName.ELLIPSE:
                    return _primitiveShapeBuilder.TryAddEllipseShape(context, node, out error);
                case SvgTagName.LINE:
                    return _primitiveShapeBuilder.TryAddLineShape(context, node, out error);
                case SvgTagName.POLYLINE:
                    return _primitiveShapeBuilder.TryAddPolylineShape(context, node, out error, closed: false);
                case SvgTagName.POLYGON:
                    return _primitiveShapeBuilder.TryAddPolylineShape(context, node, out error, closed: true);
                case SvgTagName.PATH:
                    return _primitiveShapeBuilder.TryAddPathShape(context, node, out error);
                case SvgTagName.TEXT:
                case SvgTagName.TSPAN:
                case SvgTagName.TEXT_PATH:
                    return true;
                case SvgTagName.USE:
                    return TryAddUseNode(context, node, out error);
                default:
                    return true;
            }
        }

        private bool TryAddUseNode(
            RenderBuildContext context,
            SvgNodeModel useNode,
            out string error)
        {
            error = string.Empty;
            if (!NodeLookup.TryResolveUseReference(useNode, context.NodesByXmlId, out var referencedNode))
            {
                error = $"Direct renderer could not resolve <use> target for '{useNode.LegacyElementKey}'.";
                return false;
            }

            if (!AttributeUtility.TryGetFloat(useNode.RawAttributes, SvgAttributeName.X, out var x))
                x = 0f;
            if (!AttributeUtility.TryGetFloat(useNode.RawAttributes, SvgAttributeName.Y, out var y))
                y = 0f;
            if (!UnityEngine.Mathf.Approximately(x, 0f) || !UnityEngine.Mathf.Approximately(y, 0f))
                context.SceneNode.Transform = Matrix2D.Translate(new UnityEngine.Vector2(x, y)) * context.SceneNode.Transform;

            if (!TryBuildReferencedNode(context, referencedNode, out var referencedSceneNode, out error))
                return false;

            if (referencedSceneNode != null)
                context.SceneNode.Children.Add(referencedSceneNode);

            return true;
        }

        private bool TryBuildReferencedNode(
            RenderBuildContext parentContext,
            SvgNodeModel node,
            out SceneNode sceneNode,
            out string error)
        {
            sceneNode = null;
            error = string.Empty;

            if (node == null || IsHidden(node))
                return true;

            sceneNode = new SceneNode
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = TransformParser.Parse(node.RawAttributes)
            };

            RenderBuildContext context = new(parentContext.DocumentModel, parentContext.NodesByXmlId, sceneNode);
            if (!TryBuildShapes(context.DocumentModel, context.NodesByXmlId, node, context.SceneNode, out error))
                return false;

            if (node.Children == null)
                return true;

            for (var index = 0; index < node.Children.Count; index++)
            {
                var childId = node.Children[index];
                if (!parentContext.DocumentModel.TryGetNode(childId, out var childNode) || childNode == null)
                    continue;

                if (!TryBuildReferencedNode(parentContext, childNode, out var childSceneNode, out error))
                    return false;

                if (childSceneNode != null)
                    sceneNode.Children.Add(childSceneNode);
            }

            return true;
        }

        private static bool IsHidden(SvgNodeModel node)
        {
            return AttributeUtility.TryGetAttribute(node?.RawAttributes, SvgAttributeName.DISPLAY, out var display) &&
                   string.Equals(display, "none", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
