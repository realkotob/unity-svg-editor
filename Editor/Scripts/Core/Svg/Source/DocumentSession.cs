using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Model;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Source
{
    internal sealed class DocumentSession
    {
        public string AssetPath { get; set; } = string.Empty;
        public string AbsolutePath { get; set; } = string.Empty;
        public Encoding SourceEncoding { get; set; } = new UTF8Encoding(false);
        public VectorImage VectorImageAsset { get; set; }
        public string OriginalSourceText { get; set; } = string.Empty;
        public string WorkingSourceText { get; set; } = string.Empty;
        public SvgDocumentModel DocumentModel { get; set; }
        public string DocumentModelLoadError { get; set; } = string.Empty;
        public string ModelEditingBlockReason { get; set; } = string.Empty;

        public bool IsDirty => !string.Equals(OriginalSourceText, WorkingSourceText, StringComparison.Ordinal);
        public bool HasModelEditingBlock => !string.IsNullOrWhiteSpace(ModelEditingBlockReason);

        public bool CanUseDocumentModelForEditing =>
            DocumentModel != null &&
            string.IsNullOrWhiteSpace(DocumentModelLoadError) &&
            !HasModelEditingBlock &&
            string.Equals(DocumentModel.SourceText, WorkingSourceText, StringComparison.Ordinal);

        public string ResolveModelEditingFailureReason()
        {
            if (DocumentModel == null)
            {
                return "Document model is unavailable.";
            }

            if (!string.IsNullOrWhiteSpace(DocumentModelLoadError))
            {
                return DocumentModelLoadError;
            }

            if (!string.IsNullOrWhiteSpace(ModelEditingBlockReason))
            {
                return ModelEditingBlockReason;
            }

            if (!string.Equals(DocumentModel.SourceText, WorkingSourceText, StringComparison.Ordinal))
            {
                return "Document model is out of sync with the working source.";
            }

            return string.Empty;
        }
    }
}
