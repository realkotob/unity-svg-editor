using System;
using System.Xml;
using SvgEditor.DocumentModel;
using SvgEditor.Document.Structure.Xml;
using Core.UI.Extensions;

namespace SvgEditor.Document
{
    internal sealed class SvgDocumentSourceService
    {
        private readonly SvgDocumentModelLoader _documentModelLoader = new();
        private readonly SvgDocumentModelSerializer _documentModelSerializer = new();
        private const string TextEditingBlockReason =
            "Model-based editing is disabled for SVG text content (text, tspan, textPath).";

        public bool ValidateXml(string sourceText, out string error)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            return SvgDocumentXmlUtility.TryLoadDocument(sourceText, out _, out error);
        }

        public void RefreshDocumentModel(DocumentSession document)
        {
            if (document == null)
            {
                return;
            }

            RefreshDocumentModelSnapshot(document, document.WorkingSourceText);
        }

        public void RefreshDocumentModelSnapshot(DocumentSession document, string sourceText)
        {
            if (document == null)
            {
                return;
            }

            document.DocumentModel = null;
            document.DocumentModelLoadError = string.Empty;
            document.ModelEditingBlockReason = string.Empty;

            if (!_documentModelLoader.TryLoad(sourceText, out SvgDocumentModel documentModel, out string error))
            {
                document.DocumentModelLoadError = error;
                return;
            }

            document.DocumentModel = documentModel;

            FeatureScanResult featureScan = FeatureScanner.Scan(sourceText);
            if (featureScan.HasText || featureScan.HasTspan || featureScan.HasTextPath)
            {
                document.ModelEditingBlockReason = TextEditingBlockReason;
            }
        }

        public bool TryResolvePersistedSource(DocumentSession document, out string sourceText, out string error)
        {
            sourceText = document?.WorkingSourceText ?? string.Empty;
            error = string.Empty;

            if (document == null)
            {
                error = "Document is null.";
                return false;
            }

            if (document.CanUseDocumentModelForEditing)
            {
                if (!_documentModelSerializer.TrySerialize(document.DocumentModel, out sourceText, out error))
                {
                    return false;
                }

                sourceText = RestoreXmlDeclaration(document.WorkingSourceText, sourceText);
            }

            if (!SvgSafeMaskArtifactSanitizer.TrySanitize(sourceText, out sourceText, out _, out error))
            {
                return false;
            }

            return ValidateXml(sourceText, out error);
        }

        private static string RestoreXmlDeclaration(string originalSourceText, string serializedSourceText)
        {
            if (string.IsNullOrWhiteSpace(originalSourceText) || string.IsNullOrWhiteSpace(serializedSourceText))
            {
                return serializedSourceText ?? string.Empty;
            }

            if (!SvgDocumentXmlUtility.TryLoadDocument(originalSourceText, out XmlDocument document, out _))
            {
                return serializedSourceText;
            }

            if (document.FirstChild is not XmlDeclaration declaration)
            {
                return serializedSourceText;
            }

            return declaration.OuterXml + serializedSourceText;
        }
    }
}
