using System;

namespace UnitySvgEditor.Editor
{
    internal sealed class SvgDocumentSourceService
    {
        private readonly SvgDocumentModelLoader _documentModelLoader = new();
        private readonly SvgDocumentModelSerializer _documentModelSerializer = new();

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

            if (!_documentModelLoader.TryLoad(sourceText, out SvgDocumentModel documentModel, out string error))
            {
                document.DocumentModelLoadError = error;
                return;
            }

            document.DocumentModel = documentModel;
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

            if (document.DocumentModel != null &&
                string.IsNullOrWhiteSpace(document.DocumentModelLoadError) &&
                string.Equals(document.DocumentModel.SourceText, document.WorkingSourceText, StringComparison.Ordinal))
            {
                if (!_documentModelSerializer.TrySerialize(document.DocumentModel, out sourceText, out error))
                {
                    return false;
                }
            }

            if (!SvgSafeMaskArtifactSanitizer.TrySanitize(sourceText, out sourceText, out _, out error))
            {
                return false;
            }

            return ValidateXml(sourceText, out error);
        }
    }
}
