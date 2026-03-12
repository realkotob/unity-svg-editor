using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgModelSceneBuilder
    {
        private readonly SvgShapeBuilder _shapeBuilder = new();
        private readonly SvgReferenceSceneBuilder _referenceSceneBuilder;

        public SvgModelSceneBuilder()
        {
            _referenceSceneBuilder = new SvgReferenceSceneBuilder(_shapeBuilder);
        }

        public bool TryBuildReferenceOverlayScenes(
            SvgDocumentModel documentModel,
            string elementKey,
            out IReadOnlyList<CanvasDefinitionOverlayScene> overlays,
            out string error)
        {
            overlays = Array.Empty<CanvasDefinitionOverlayScene>();
            error = string.Empty;

            if (documentModel?.Root == null || string.IsNullOrWhiteSpace(elementKey))
                return true;

            if (!SvgNodeLookupUtility.TryFindNodeByLegacyElementKey(documentModel, elementKey, out var node) || node == null)
                return true;

            var nodesByXmlId = SvgNodeLookupUtility.BuildNodeLookupByXmlId(documentModel);
            var resolved = new List<CanvasDefinitionOverlayScene>();

            if (!_referenceSceneBuilder.TryBuildReferenceOverlayScene(documentModel, nodesByXmlId, node, CanvasDefinitionOverlayKind.Mask, out CanvasDefinitionOverlayScene maskOverlay, out error))
                return false;
            if (maskOverlay != null)
                resolved.Add(maskOverlay);

            if (!_referenceSceneBuilder.TryBuildReferenceOverlayScene(documentModel, nodesByXmlId, node, CanvasDefinitionOverlayKind.ClipPath, out CanvasDefinitionOverlayScene clipOverlay, out error))
                return false;
            if (clipOverlay != null)
                resolved.Add(clipOverlay);

            overlays = resolved;
            return true;
        }

        public bool TryBuild(
            SvgDocumentModel documentModel,
            out SvgModelSceneBuildResult result,
            out string error)
        {
            result = new SvgModelSceneBuildResult();
            error = string.Empty;

            if (documentModel?.Root == null)
            {
                error = "Document model root is unavailable.";
                return false;
            }

            SceneNode rootSceneNode = new()
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = Matrix2D.identity
            };

            result.Scene = new Scene
            {
                Root = rootSceneNode
            };
            result.DocumentViewportRect = ResolveDocumentViewport(documentModel);
            result.PreserveAspectRatioMode = SvgPreserveAspectRatioMode.Parse(documentModel.PreserveAspectRatio);

            var nodesByXmlId = SvgNodeLookupUtility.BuildNodeLookupByXmlId(documentModel);

            if (!TryBuildChildren(documentModel, nodesByXmlId, documentModel.Root, rootSceneNode, result, out error))
                return false;

            return true;
        }

        private bool TryBuildChildren(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel parentNode,
            SceneNode parentSceneNode,
            SvgModelSceneBuildResult result,
            out string error)
        {
            error = string.Empty;
            if (parentNode?.Children == null)
                return true;

            for (int index = 0; index < parentNode.Children.Count; index++)
            {
                SvgNodeId childId = parentNode.Children[index];
                if (!documentModel.TryGetNode(childId, out SvgNodeModel childNode) || childNode == null)
                    continue;

                if (!TryBuildNode(documentModel, nodesByXmlId, childNode, out SceneNode sceneNode, result, out error))
                    return false;

                if (sceneNode != null)
                    parentSceneNode.Children.Add(sceneNode);
            }

            return true;
        }

        private bool TryBuildNode(
            SvgDocumentModel documentModel,
            IReadOnlyDictionary<string, SvgNodeModel> nodesByXmlId,
            SvgNodeModel node,
            out SceneNode sceneNode,
            SvgModelSceneBuildResult result,
            out string error)
        {
            sceneNode = null;
            error = string.Empty;

            if (node == null || node.IsDefinitionNode || node.Kind == SvgNodeKind.Definitions || IsHidden(node))
                return true;

            sceneNode = new SceneNode
            {
                Children = new List<SceneNode>(),
                Shapes = new List<Shape>(),
                Transform = SvgTransformParser.Parse(node.RawAttributes)
            };

            if (!_referenceSceneBuilder.TryAttachMask(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!_referenceSceneBuilder.TryAttachClipper(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!_shapeBuilder.TryBuildShapes(documentModel, nodesByXmlId, node, sceneNode, out error))
                return false;

            if (!TryBuildChildren(documentModel, nodesByXmlId, node, sceneNode, result, out error))
                return false;

            if (SvgAttributeUtility.TryGetOpacity(node.RawAttributes, out var opacity) && !Mathf.Approximately(opacity, 1f))
                result.NodeOpacities[sceneNode] = opacity;

            if (!node.Id.IsRoot)
                result.NodeMappings[sceneNode] = (node.LegacyElementKey, node.LegacyTargetKey);

            return true;
        }

        private static Rect ResolveDocumentViewport(SvgDocumentModel documentModel)
        {
            if (TryParseViewBox(documentModel?.ViewBox, out Rect viewBox))
                return viewBox;

            float width = ParseLength(documentModel?.Width);
            float height = ParseLength(documentModel?.Height);
            return width > Mathf.Epsilon && height > Mathf.Epsilon
                ? new Rect(0f, 0f, width, height)
                : default;
        }

        private static bool TryParseViewBox(string viewBoxText, out Rect viewBox)
        {
            viewBox = default;
            if (string.IsNullOrWhiteSpace(viewBoxText))
                return false;

            string[] tokens = viewBoxText.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 4 ||
                !SvgAttributeUtility.TryParseFloat(tokens[0], out var minX) ||
                !SvgAttributeUtility.TryParseFloat(tokens[1], out var minY) ||
                !SvgAttributeUtility.TryParseFloat(tokens[2], out var width) ||
                !SvgAttributeUtility.TryParseFloat(tokens[3], out var height))
            {
                return false;
            }

            viewBox = new Rect(minX, minY, width, height);
            return true;
        }

        private static float ParseLength(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0f;

            int length = 0;
            while (length < value.Length && ("+-0123456789.eE".IndexOf(value[length]) >= 0))
                length++;

            return length > 0 && SvgAttributeUtility.TryParseFloat(value.Substring(0, length), out var parsed)
                ? parsed
                : 0f;
        }


        private static bool TryParseDasharray(
            SvgDocumentModel documentModel,
            SvgNodeModel node,
            out float[] pattern)
        {
            pattern = null;
            if (!SvgInheritedAttributeResolver.TryGetInheritedAttribute(documentModel, node, "stroke-dasharray", out var dasharray) ||
                string.IsNullOrWhiteSpace(dasharray))
            {
                return false;
            }

            var tokens = dasharray.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var values = new List<float>(tokens.Length);
            for (var index = 0; index < tokens.Length; index++)
            {
                if (!SvgAttributeUtility.TryParseFloat(tokens[index], out var value))
                    return false;

                values.Add(Mathf.Max(0f, value));
            }

            pattern = values.Count > 0 ? values.ToArray() : null;
            return pattern != null;
        }

        private static bool TryParseOffset(string offsetText, out float offset)
        {
            offset = 0f;
            if (string.IsNullOrWhiteSpace(offsetText))
                return false;

            var normalized = offsetText.Trim();
            if (normalized.EndsWith("%", StringComparison.Ordinal))
            {
                return SvgAttributeUtility.TryParseFloat(normalized[..^1], out var percent) &&
                       TryNormalizeStop(percent / 100f, out offset);
            }

            return SvgAttributeUtility.TryParseFloat(normalized, out var raw) &&
                   TryNormalizeStop(raw, out offset);
        }

        private static bool TryNormalizeStop(float value, out float offset)
        {
            offset = Mathf.Clamp01(value);
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsHidden(SvgNodeModel node)
        {
            return SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, "display", out var display) &&
                   string.Equals(display, "none", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldIncludeMaskNode(SvgNodeModel node)
        {
            if (!SvgAttributeUtility.TryGetAttribute(node?.RawAttributes, "fill", out var fillValue))
                return false;

            if (!SvgAttributeUtility.TryParseColor(fillValue, out var color))
                return false;

            var opacity = 1f;
            if (SvgAttributeUtility.TryGetFloat(node?.RawAttributes, "fill-opacity", out var resolvedOpacity))
                opacity = Mathf.Clamp01(resolvedOpacity);
            var luminance = (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);
            return opacity > 0.5f && luminance > 0.5f;
        }

        private static FillMode ResolveFillMode(SvgDocumentModel documentModel, SvgNodeModel node)
        {
            return SvgInheritedAttributeResolver.ResolveFillMode(documentModel, node);
        }

        private static bool TryGetFloat(IReadOnlyDictionary<string, string> attributes, string name, out float value)
        {
            return SvgAttributeUtility.TryGetFloat(attributes, name, out value);
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return SvgAttributeUtility.TryParseFloat(text, out value);
        }

    }
}
