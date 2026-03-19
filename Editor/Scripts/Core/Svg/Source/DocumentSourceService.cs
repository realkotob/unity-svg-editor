using System;
using System.Xml;
using SvgEditor.Core.Svg.Analysis;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Serialization;
using SvgEditor.Core.Svg.Structure.Xml;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Source
{
    internal sealed class DocumentSourceService
    {
        private readonly SvgLoader _loader = new();
        private readonly SvgSerializer _serializer = new();
        private const string TextEditingBlockReason =
            "Model-based editing is disabled for SVG text content (text, tspan, textPath).";

        public Result<Unit> ValidateXml(string sourceText)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return Result.Failure<Unit>("SVG source is empty.");
            }

            return XmlUtility.TryLoadDocument(sourceText, out _, out string error)
                ? Result.Success(Unit.Default)
                : Result.Failure<Unit>(error);
        }

        public bool TryValidateXml(string sourceText, out string error)
        {
            Result<Unit> result = ValidateXml(sourceText);
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
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

            Result<SvgDocumentModel> load = _loader.Load(sourceText);
            if (load.IsFailure)
            {
                document.DocumentModelLoadError = load.Error ?? string.Empty;
                return;
            }

            document.DocumentModel = load.Value;

            FeatureScanResult featureScan = FeatureScanner.Scan(sourceText);
            if (featureScan.HasText || featureScan.HasTspan || featureScan.HasTextPath)
            {
                document.ModelEditingBlockReason = TextEditingBlockReason;
            }
        }

        public Result<string> ResolvePersistedSource(DocumentSession document)
        {
            if (document == null)
            {
                return Result.Failure<string>("Document is null.");
            }

            string sourceText = document.WorkingSourceText ?? string.Empty;
            if (document.CanUseDocumentModelForEditing)
            {
                Result<string> serialized = _serializer.Serialize(document.DocumentModel);
                if (serialized.IsFailure)
                {
                    return serialized;
                }

                sourceText = serialized.Value;
                sourceText = RestoreXmlDeclaration(document.WorkingSourceText, sourceText);
            }

            if (!MaskArtifactSanitizer.TrySanitize(sourceText, out sourceText, out _, out string error))
            {
                return Result.Failure<string>(error);
            }

            Result<Unit> validation = ValidateXml(sourceText);
            return validation.IsSuccess
                ? Result.Success(sourceText)
                : Result.Failure<string>(validation.Error);
        }

        public bool TryResolvePersistedSource(DocumentSession document, out string sourceText, out string error)
        {
            Result<string> result = ResolvePersistedSource(document);
            sourceText = result.GetValueOrDefault(string.Empty);
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        private static string RestoreXmlDeclaration(string originalSourceText, string serializedSourceText)
        {
            if (string.IsNullOrWhiteSpace(originalSourceText) || string.IsNullOrWhiteSpace(serializedSourceText))
            {
                return serializedSourceText ?? string.Empty;
            }

            if (!XmlUtility.TryLoadDocument(originalSourceText, out XmlDocument document, out _))
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
