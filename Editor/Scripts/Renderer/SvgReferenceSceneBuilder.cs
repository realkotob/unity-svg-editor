using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Workspace.Canvas;
using SvgEditor.DocumentModel;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;
using SvgEditor.Document.Structure.Lookup;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Renderer
{
    internal sealed class SvgReferenceSceneBuilder
    {
        private readonly SvgShapeBuilder _shapeBuilder;

        public SvgReferenceSceneBuilder(SvgShapeBuilder shapeBuilder)
        {
            _shapeBuilder = shapeBuilder;
        }

        public bool TryAttachMask(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, SvgAttributeName.MASK, out var maskValue))
            {
                return true;
            }

            if (!SvgNodeLookupUtility.TryExtractFragmentId(maskValue, out var fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out var maskNode) ||
                !string.Equals(maskNode.TagName, SvgTagName.MASK, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!TryBuildMaskClipNode(documentModel, nodesByXmlId, maskNode, out var maskClipNode, out error))
            {
                return false;
            }

            sceneNode.Clipper = maskClipNode;
            return true;
        }

        public bool TryAttachClipper(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (!SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, SvgAttributeName.CLIP_PATH, out var clipPathValue))
            {
                return true;
            }

            if (!SvgNodeLookupUtility.TryExtractFragmentId(clipPathValue, out var fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out var clipNode) ||
                !string.Equals(clipNode.TagName, SvgTagName.CLIP_PATH, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Direct renderer could not resolve clipPath for '{node?.LegacyElementKey}'.";
                return false;
            }

            if (!TryBuildClipNode(documentModel, nodesByXmlId, clipNode, out var clipSceneNode, out error))
            {
                return false;
            }

            sceneNode.Clipper = clipSceneNode;
            return true;
        }

        public bool TryBuildReferenceOverlayScene(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            CanvasDefinitionOverlayKind kind,
            out CanvasDefinitionOverlayScene overlay,
            out string error)
        {
            overlay = null;
            error = string.Empty;
            if (node == null || nodesByXmlId == null)
            {
                return true;
            }

            var attributeName = kind == CanvasDefinitionOverlayKind.Mask ? SvgAttributeName.MASK : SvgAttributeName.CLIP_PATH;
            var expectedTag = kind == CanvasDefinitionOverlayKind.Mask ? SvgTagName.MASK : SvgTagName.CLIP_PATH;
            if (!SvgAttributeUtility.TryGetAttribute(node.RawAttributes, attributeName, out var rawValue) ||
                !SvgNodeLookupUtility.TryExtractFragmentId(rawValue, out var fragmentId) ||
                !nodesByXmlId.TryGetValue(fragmentId, out var referenceNode) ||
                !string.Equals(referenceNode.TagName, expectedTag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            SceneNode sceneNode;
            var built = kind == CanvasDefinitionOverlayKind.Mask
                ? TryBuildMaskClipNode(documentModel, nodesByXmlId, referenceNode, out sceneNode, out error)
                : TryBuildClipNode(documentModel, nodesByXmlId, referenceNode, out sceneNode, out error);
            if (!built)
            {
                return false;
            }

            if (sceneNode == null)
            {
                return true;
            }

            overlay = new CanvasDefinitionOverlayScene
            {
                Kind = kind,
                ReferenceId = fragmentId,
                DefinitionElementKey = referenceNode.LegacyElementKey,
                RootNode = sceneNode
            };
            return true;
        }

        private bool TryBuildClipNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel clipNode,
            out SceneNode sceneNode,
            out string error)
        {
            sceneNode = new SceneNode
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = SvgTransformParser.Parse(clipNode?.RawAttributes)
            };
            error = string.Empty;

            if (clipNode == null)
            {
                error = "Clip node was null.";
                return false;
            }

            for (var index = 0; index < clipNode.Children.Count; index++)
            {
                var childId = clipNode.Children[index];
                if (!documentModel.TryGetNode(childId, out var childNode) || childNode == null || IsHidden(childNode))
                {
                    continue;
                }

                var clipChildNode = new SceneNode
                {
                    Children = new List<SceneNode>(),
                    Shapes = new List<Shape>(),
                    Transform = SvgTransformParser.Parse(childNode.RawAttributes)
                };

                if (!_shapeBuilder.TryBuildShapes(documentModel, nodesByXmlId, childNode, clipChildNode, out error))
                {
                    return false;
                }

                if (clipChildNode.Shapes.Count > 0 || clipChildNode.Children.Count > 0)
                {
                    sceneNode.Children.Add(clipChildNode);
                }
            }

            return true;
        }

        private bool TryBuildMaskClipNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel maskNode,
            out SceneNode sceneNode,
            out string error)
        {
            sceneNode = new SceneNode
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = SvgTransformParser.Parse(maskNode?.RawAttributes)
            };
            error = string.Empty;

            if (maskNode == null)
            {
                error = "Mask node was null.";
                return false;
            }

            for (var index = 0; index < maskNode.Children.Count; index++)
            {
                var childId = maskNode.Children[index];
                if (!documentModel.TryGetNode(childId, out var childNode) ||
                    childNode == null ||
                    IsHidden(childNode) ||
                    !ShouldIncludeMaskNode(childNode))
                {
                    continue;
                }

                var maskChildNode = new SceneNode
                {
                    Children = new List<SceneNode>(),
                    Shapes = new List<Shape>(),
                    Transform = SvgTransformParser.Parse(childNode.RawAttributes)
                };

                if (!_shapeBuilder.TryBuildShapes(documentModel, nodesByXmlId, childNode, maskChildNode, out error))
                {
                    return false;
                }

                if (maskChildNode.Shapes.Count > 0 || maskChildNode.Children.Count > 0)
                {
                    sceneNode.Children.Add(maskChildNode);
                }
            }

            return sceneNode.Children.Count > 0;
        }

        private static bool IsHidden(SvgNodeModel node)
        {
            return SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, SvgAttributeName.DISPLAY, out var display) &&
                   string.Equals(display, "none", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldIncludeMaskNode(SvgNodeModel node)
        {
            if (!SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, SvgAttributeName.FILL, out var fillValue))
            {
                return false;
            }

            if (!SvgAttributeUtility.TryParseColor(fillValue, out var color))
            {
                return false;
            }

            var opacity = 1f;
            if (SvgAttributeUtility.TryGetFloat(node?.RawAttributes, SvgAttributeName.FILL_OPACITY, out var resolvedOpacity))
            {
                opacity = Mathf.Clamp01(resolvedOpacity);
            }

            var luminance = (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);
            return opacity > 0.5f && luminance > 0.5f;
        }
    }
}
