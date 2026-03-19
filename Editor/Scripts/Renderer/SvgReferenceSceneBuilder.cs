using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.UI.Canvas;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Structure.Lookup;
using SvgEditor.Core.Svg.Transforms;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Preview.Rendering;

namespace SvgEditor.Renderer
{
    internal sealed class SvgReferenceSceneBuilder
    {
        private readonly SvgShapeBuilder _shapeBuilder;
        private static readonly CanvasDefinitionOverlayKind[] ReferenceKinds =
        {
            CanvasDefinitionOverlayKind.Mask,
            CanvasDefinitionOverlayKind.ClipPath
        };

        public SvgReferenceSceneBuilder(SvgShapeBuilder shapeBuilder)
        {
            _shapeBuilder = shapeBuilder;
        }

        public bool TryAttachReferenceClipper(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            SceneNode sceneNode,
            out string error)
        {
            error = string.Empty;
            if (sceneNode == null || node == null || nodesByXmlId == null)
            {
                return true;
            }

            for (int index = 0; index < ReferenceKinds.Length; index++)
            {
                if (!TryAttachReferenceClipper(documentModel, nodesByXmlId, node, sceneNode, ReferenceKinds[index], out error))
                {
                    return false;
                }
            }

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

            if (!TryResolveReferenceNode(node, nodesByXmlId, kind, strictInvalidReference: false, out var fragmentId, out var referenceNode, out error))
            {
                return string.IsNullOrWhiteSpace(error);
            }

            if (!TryBuildReferenceSceneNode(documentModel, nodesByXmlId, referenceNode, kind, out SceneNode sceneNode, out error))
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

        private bool TryAttachReferenceClipper(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            SceneNode sceneNode,
            CanvasDefinitionOverlayKind kind,
            out string error)
        {
            error = string.Empty;
            if (!TryResolveReferenceNode(node, nodesByXmlId, kind, strictInvalidReference: true, out _, out var referenceNode, out error))
            {
                return string.IsNullOrWhiteSpace(error);
            }

            if (!TryBuildReferenceSceneNode(documentModel, nodesByXmlId, referenceNode, kind, out SceneNode referenceSceneNode, out error))
            {
                return false;
            }

            sceneNode.Clipper = referenceSceneNode;
            return true;
        }

        private static bool TryResolveReferenceNode(
            SvgNodeModel node,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            CanvasDefinitionOverlayKind kind,
            bool strictInvalidReference,
            out string fragmentId,
            out SvgNodeModel referenceNode,
            out string error)
        {
            fragmentId = string.Empty;
            referenceNode = null;
            error = string.Empty;

            string attributeName = kind == CanvasDefinitionOverlayKind.Mask ? SvgAttributeName.MASK : SvgAttributeName.CLIP_PATH;
            string expectedTag = kind == CanvasDefinitionOverlayKind.Mask ? SvgTagName.MASK : SvgTagName.CLIP_PATH;
            if (!AttributeUtility.TryGetAttribute(node?.RawAttributes, attributeName, out var rawValue))
            {
                return false;
            }

            if (!NodeLookup.TryExtractFragmentId(rawValue, out fragmentId) ||
                nodesByXmlId == null ||
                !nodesByXmlId.TryGetValue(fragmentId, out referenceNode) ||
                !string.Equals(referenceNode.TagName, expectedTag, StringComparison.OrdinalIgnoreCase))
            {
                if (strictInvalidReference && kind == CanvasDefinitionOverlayKind.ClipPath)
                {
                    error = $"Direct renderer could not resolve clipPath for '{node?.LegacyElementKey}'.";
                }

                return false;
            }

            return true;
        }

        private bool TryBuildReferenceSceneNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel referenceNode,
            CanvasDefinitionOverlayKind kind,
            out SceneNode sceneNode,
            out string error)
        {
            return kind == CanvasDefinitionOverlayKind.Mask
                ? TryBuildMaskClipNode(documentModel, nodesByXmlId, referenceNode, out sceneNode, out error)
                : TryBuildClipNode(documentModel, nodesByXmlId, referenceNode, out sceneNode, out error);
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
                Transform = TransformParser.Parse(clipNode?.RawAttributes)
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
                    Transform = TransformParser.Parse(childNode.RawAttributes)
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
                Transform = TransformParser.Parse(maskNode?.RawAttributes)
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
                    Transform = TransformParser.Parse(childNode.RawAttributes)
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
            return AttributeUtility.TryGetAttribute(node?.RawAttributes, SvgAttributeName.DISPLAY, out var display) &&
                   string.Equals(display, "none", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldIncludeMaskNode(SvgNodeModel node)
        {
            if (!AttributeUtility.TryGetAttribute(node?.RawAttributes, SvgAttributeName.FILL, out var fillValue))
            {
                return false;
            }

            if (!AttributeUtility.TryParseColor(fillValue, out var color))
            {
                return false;
            }

            var opacity = 1f;
            if (AttributeUtility.TryGetFloat(node?.RawAttributes, SvgAttributeName.FILL_OPACITY, out var resolvedOpacity))
            {
                opacity = Mathf.Clamp01(resolvedOpacity);
            }

            var luminance = (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);
            return opacity > 0.5f && luminance > 0.5f;
        }
    }
}
