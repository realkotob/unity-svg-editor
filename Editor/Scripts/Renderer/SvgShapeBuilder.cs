using System.Collections.Generic;
using Unity.VectorGraphics;
using SvgEditor.DocumentModel;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Lookup;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Renderer
{
    internal sealed class SvgShapeBuilder
    {
        private readonly SvgPrimitiveShapeBuilder _primitiveShapeBuilder = new();
        private readonly SvgShapeStyleBuilder _shapeStyleBuilder = new();

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

            switch (node.TagName)
            {
                case SvgTagName.GROUP:
                case SvgTagName.SVG:
                    return true;
                case SvgTagName.RECT:
                    return _primitiveShapeBuilder.TryAddRectShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case SvgTagName.CIRCLE:
                    return _primitiveShapeBuilder.TryAddCircleShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case SvgTagName.ELLIPSE:
                    return _primitiveShapeBuilder.TryAddEllipseShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case SvgTagName.LINE:
                    return _primitiveShapeBuilder.TryAddLineShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case SvgTagName.POLYLINE:
                    return _primitiveShapeBuilder.TryAddPolylineShape(documentModel, node, nodesByXmlId, sceneNode, out error, closed: false);
                case SvgTagName.POLYGON:
                    return _primitiveShapeBuilder.TryAddPolylineShape(documentModel, node, nodesByXmlId, sceneNode, out error, closed: true);
                case SvgTagName.PATH:
                    return _primitiveShapeBuilder.TryAddPathShape(documentModel, node, nodesByXmlId, sceneNode, out error);
                case SvgTagName.TEXT:
                case SvgTagName.TSPAN:
                case SvgTagName.TEXT_PATH:
                    return true;
                case SvgTagName.USE:
                    return TryAddUseNode(documentModel, nodesByXmlId, node, sceneNode, out error);
                default:
                    return true;
            }
        }

        private bool TryAddUseNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel useNode,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!SvgNodeLookupUtility.TryResolveUseReference(useNode, nodesByXmlId, out var referencedNode))
            {
                error = $"Direct renderer could not resolve <use> target for '{useNode.LegacyElementKey}'.";
                return false;
            }

            if (!SvgAttributeUtility.TryGetFloat(useNode.RawAttributes, SvgAttributeName.X, out var x))
                x = 0f;
            if (!SvgAttributeUtility.TryGetFloat(useNode.RawAttributes, SvgAttributeName.Y, out var y))
                y = 0f;
            if (!UnityEngine.Mathf.Approximately(x, 0f) || !UnityEngine.Mathf.Approximately(y, 0f))
                sceneNode.Transform = Matrix2D.Translate(new UnityEngine.Vector2(x, y)) * sceneNode.Transform;

            if (!TryBuildReferencedNode(documentModel, nodesByXmlId, referencedNode, out var referencedSceneNode, out error))
                return false;

            if (referencedSceneNode != null)
                sceneNode.Children.Add(referencedSceneNode);

            return true;
        }

        private bool TryBuildReferencedNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
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
                Transform = SvgTransformParser.Parse(node.RawAttributes)
            };

            if (!TryBuildShapes(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (node.Children == null)
                return true;

            for (var index = 0; index < node.Children.Count; index++)
            {
                var childId = node.Children[index];
                if (!documentModel.TryGetNode(childId, out var childNode) || childNode == null)
                    continue;

                if (!TryBuildReferencedNode(documentModel, nodesByXmlId, childNode, out var childSceneNode, out error))
                    return false;

                if (childSceneNode != null)
                    sceneNode.Children.Add(childSceneNode);
            }

            return true;
        }

        private static bool IsHidden(SvgNodeModel node)
        {
            return SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, SvgAttributeName.DISPLAY, out var display) &&
                   string.Equals(display, "none", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
