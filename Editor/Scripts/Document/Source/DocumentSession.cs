using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentSession
    {
        public string AssetPath { get; set; } = string.Empty;
        public string AbsolutePath { get; set; } = string.Empty;
        public VectorImage VectorImageAsset { get; set; }
        public string OriginalSourceText { get; set; } = string.Empty;
        public string WorkingSourceText { get; set; } = string.Empty;
        public SvgDocumentModel DocumentModel { get; set; }
        public string DocumentModelLoadError { get; set; } = string.Empty;

        public bool IsDirty => !string.Equals(OriginalSourceText, WorkingSourceText, StringComparison.Ordinal);
    }
}
