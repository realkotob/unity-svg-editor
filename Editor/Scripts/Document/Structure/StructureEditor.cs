using System.Xml;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class StructureEditor
    {
        public bool TryBuildSnapshot(string sourceText, out StructureOutline snapshot, out string error)
        {
            return StructureOutlineBuilder.TryBuildSnapshot(sourceText, out snapshot, out error);
        }

        public bool TrySetElementTransform(
            string sourceText,
            string elementKey,
            string transformValue,
            out string updatedSource,
            out string error)
        {
            return StructureDocumentEditService.TrySetElementTransform(
                sourceText,
                elementKey,
                transformValue,
                out updatedSource,
                out error);
        }

        public bool TryPrependElementTranslation(
            string sourceText,
            string elementKey,
            Vector2 translation,
            out string updatedSource,
            out string error)
        {
            return StructureDocumentEditService.TryPrependElementTranslation(
                sourceText,
                elementKey,
                translation,
                out updatedSource,
                out error);
        }

        public bool TryPrependElementScale(
            string sourceText,
            string elementKey,
            Vector2 scale,
            Vector2 pivot,
            out string updatedSource,
            out string error)
        {
            return StructureDocumentEditService.TryPrependElementScale(
                sourceText,
                elementKey,
                scale,
                pivot,
                out updatedSource,
                out error);
        }

        internal static bool TryLoadDocument(string sourceText, out XmlDocument document, out string error)
        {
            return SvgDocumentXmlUtility.TryLoadDocument(sourceText, out document, out error);
        }

        internal static System.Collections.Generic.List<XmlElement> GetElementChildren(XmlElement parent)
        {
            return SvgDocumentXmlUtility.GetElementChildren(parent);
        }

        internal static bool TryFindElementByKey(XmlElement root, XmlElement current, string elementKey, out XmlElement result)
        {
            return SvgDocumentXmlUtility.TryFindElementByKey(root, current, elementKey, out result);
        }

        internal static string BuildElementKey(XmlElement element, XmlElement root)
        {
            return SvgDocumentXmlUtility.BuildElementKey(element, root);
        }

        internal static int GetElementIndex(XmlElement element)
        {
            return SvgDocumentXmlUtility.GetElementIndex(element);
        }

        internal static bool TryGetId(XmlElement element, out string id)
        {
            return SvgDocumentXmlUtility.TryGetId(element, out id);
        }
    }
}
