using System;
using System.Collections.Generic;
using System.Xml;
using SvgEditor.Core.Svg.Structure.Xml;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.Core.Preview.Build
{
    internal static class PreviewDocumentPreparation
    {
        private const string SyntheticIdPrefix = "__unity_svg_editor_preview__";

        public static Result<PreparedPreviewDocument> Prepare(string sourceText)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return Result.Failure<PreparedPreviewDocument>("SVG source is empty.");
            }

            if (!XmlUtility.TryGetRootElement(sourceText, out XmlDocument document, out XmlElement root, out string error))
            {
                return Result.Failure<PreparedPreviewDocument>(error);
            }

            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            CollectExistingIds(root, usedIds);

            var keyByNodeId = new Dictionary<string, (string Key, string TargetKey)>(StringComparer.Ordinal);
            var syntheticIdCounter = 0;
            RegisterElementMappings(root);

            return Result.Success(new PreparedPreviewDocument
            {
                Document = document,
                Root = root,
                PreserveAspectRatioMode = ResolvePreserveAspectRatioMode(root),
                KeyByNodeId = keyByNodeId
            });

            void RegisterElementMappings(XmlElement element)
            {
                if (element == null)
                {
                    return;
                }

                bool hasStableId = XmlUtility.TryGetId(element, out string stableId);
                if (ShouldRegisterPreviewElement(element, root, hasStableId))
                {
                    string key = XmlUtility.BuildElementKey(element, root);
                    string targetKey = key;
                    string nodeId = stableId;
                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        nodeId = CreateSyntheticId(usedIds, ref syntheticIdCounter);
                        element.SetAttribute(SvgAttributeName.ID, nodeId);
                    }

                    keyByNodeId[nodeId] = (key, targetKey);
                }

                foreach (XmlElement child in XmlUtility.GetElementChildren(element))
                {
                    RegisterElementMappings(child);
                }
            }
        }

        public static bool TryPrepare(
            string sourceText,
            out PreparedPreviewDocument preparedDocument,
            out string error)
        {
            Result<PreparedPreviewDocument> result = Prepare(sourceText);
            preparedDocument = result.GetValueOrDefault();
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        private static SvgPreserveAspectRatioMode ResolvePreserveAspectRatioMode(XmlElement root)
        {
            return SvgPreserveAspectRatioMode.Parse(root?.GetAttribute(SvgAttributeName.PRESERVE_ASPECT_RATIO));
        }

        private static void CollectExistingIds(XmlElement root, ISet<string> usedIds)
        {
            if (root == null)
                return;

            if (XmlUtility.TryGetId(root, out string id))
                usedIds.Add(id);

            foreach (XmlElement child in XmlUtility.GetElementChildren(root))
                CollectExistingIds(child, usedIds);
        }

        private static bool ShouldRegisterPreviewElement(XmlElement element, XmlElement root, bool hasStableId)
        {
            if (ReferenceEquals(element, root))
                return false;

            if (hasStableId)
                return true;

            return true;
        }

        private static string CreateSyntheticId(ISet<string> usedIds, ref int syntheticIdCounter)
        {
            string syntheticId;
            do
            {
                syntheticId = $"{SyntheticIdPrefix}{syntheticIdCounter++}";
            }
            while (usedIds.Contains(syntheticId));

            usedIds.Add(syntheticId);
            return syntheticId;
        }
    }
}
