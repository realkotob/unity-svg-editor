using System;
using System.Collections.Generic;
using System.Xml;

namespace UnitySvgEditor.Editor
{
    internal static class PreviewSnapshotDocumentPreparation
    {
        private const string SyntheticIdPrefix = "__unity_svg_editor_preview__";

        public static bool TryPrepare(
            string sourceText,
            out PreviewSnapshotPreparedDocument preparedDocument,
            out string error)
        {
            preparedDocument = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            if (!SvgDocumentXmlUtility.TryGetRootElement(sourceText, out XmlDocument document, out XmlElement root, out error))
                return false;

            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            CollectExistingIds(root, usedIds);

            var keyByNodeId = new Dictionary<string, (string Key, string TargetKey)>(StringComparer.Ordinal);
            var syntheticIdCounter = 0;
            RegisterElementMappings(root, root, keyByNodeId, usedIds, ref syntheticIdCounter);

            preparedDocument = new PreviewSnapshotPreparedDocument
            {
                Document = document,
                Root = root,
                KeyByNodeId = keyByNodeId
            };
            return true;
        }

        private static void CollectExistingIds(XmlElement root, ISet<string> usedIds)
        {
            if (root == null)
                return;

            if (SvgDocumentXmlUtility.TryGetId(root, out string id))
                usedIds.Add(id);

            foreach (XmlElement child in SvgDocumentXmlUtility.GetElementChildren(root))
                CollectExistingIds(child, usedIds);
        }

        private static void RegisterElementMappings(
            XmlElement element,
            XmlElement root,
            IDictionary<string, (string Key, string TargetKey)> keyByNodeId,
            ISet<string> usedIds,
            ref int syntheticIdCounter)
        {
            if (element == null || root == null)
                return;

            var key = SvgDocumentXmlUtility.BuildElementKey(element, root);
            var hasStableId = SvgDocumentXmlUtility.TryGetId(element, out string stableId);
            var targetKey = hasStableId ? stableId : string.Empty;
            var nodeId = stableId;
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                nodeId = CreateSyntheticId(usedIds, ref syntheticIdCounter);
                element.SetAttribute("id", nodeId);
            }

            keyByNodeId[nodeId] = (key, targetKey);

            foreach (XmlElement child in SvgDocumentXmlUtility.GetElementChildren(element))
                RegisterElementMappings(child, root, keyByNodeId, usedIds, ref syntheticIdCounter);
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
