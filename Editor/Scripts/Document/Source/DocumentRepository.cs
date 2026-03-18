using System;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Document.Structure.Xml;
using Core.UI.Extensions;

namespace SvgEditor.Document
{
    internal sealed class DocumentRepository
    {
        private readonly SvgAssetPathResolver _assetPathResolver = new();
        private readonly SvgDocumentSourceService _documentSourceService = new();

        #region Public Methods

        public IReadOnlyList<string> FindVectorImageAssetPaths(string searchRoot = null)
        {
            return _assetPathResolver.FindEditableSvgAssetPaths(searchRoot);
        }

        public bool TryLoad(string assetPath, out DocumentSession document, out string error)
        {
            document = null;
            error = string.Empty;

            if (!TryResolveDocumentPath(assetPath, out string normalizedAssetPath, out string absolutePath, out error))
                return false;

            if (!TryReadDocumentSource(absolutePath, out string sourceText, out var sourceEncoding, out error))
                return false;

            document = new DocumentSession
            {
                AssetPath = normalizedAssetPath,
                AbsolutePath = absolutePath,
                SourceEncoding = sourceEncoding,
                VectorImageAsset = LoadVectorImageAsset(normalizedAssetPath),
                OriginalSourceText = sourceText,
                WorkingSourceText = sourceText
            };
            _documentSourceService.RefreshDocumentModelSnapshot(document, sourceText);

            return true;
        }

        public bool ValidateXml(string sourceText, out string error)
        {
            return _documentSourceService.ValidateXml(sourceText, out error);
        }

        public bool Save(DocumentSession document, out string error)
        {
            error = string.Empty;
            if (document == null)
            {
                error = "Document is null.";
                return false;
            }

            if (!_documentSourceService.TryResolvePersistedSource(document, out string sourceTextToPersist, out error))
            {
                return false;
            }

            if (!TryResolveDocumentPath(document.AssetPath, out string normalizedAssetPath, out string absolutePath, out error))
                return false;

            try
            {
                if (!SvgSourceEncodingUtility.TryWriteAllText(absolutePath, sourceTextToPersist, document.SourceEncoding, out error))
                {
                    return false;
                }

                AssetDatabase.ImportAsset(normalizedAssetPath, ImportAssetOptions.ForceUpdate);
                document.AssetPath = normalizedAssetPath;
                document.AbsolutePath = absolutePath;
                document.WorkingSourceText = sourceTextToPersist;
                document.OriginalSourceText = sourceTextToPersist;
                document.VectorImageAsset = LoadVectorImageAsset(normalizedAssetPath);
                _documentSourceService.RefreshDocumentModelSnapshot(document, sourceTextToPersist);
                return true;
            }
            catch (Exception ex)
            {
                error = $"SVG save failed: {ex.Message}";
                return false;
            }
        }

        public void RefreshDocumentModel(DocumentSession document)
        {
            _documentSourceService.RefreshDocumentModel(document);
        }

        #endregion Public Methods

        #region Internal Methods

        internal bool TryResolveSourceTextToPersist(DocumentSession document, out string sourceText, out string error)
        {
            return _documentSourceService.TryResolvePersistedSource(document, out sourceText, out error);
        }

        #endregion Internal Methods

        private bool TryResolveDocumentPath(string assetPath, out string normalizedAssetPath, out string absolutePath, out string error)
        {
            normalizedAssetPath = assetPath?.Replace('\\', '/').Trim();
            absolutePath = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedAssetPath))
            {
                error = "SVG asset path is empty.";
                return false;
            }

            return _assetPathResolver.TryResolveAbsolutePath(normalizedAssetPath, out absolutePath, out error);
        }

        private static bool TryReadDocumentSource(string absolutePath, out string sourceText, out System.Text.Encoding sourceEncoding, out string error)
        {
            sourceText = string.Empty;
            sourceEncoding = null;
            error = string.Empty;

            if (!File.Exists(absolutePath))
            {
                error = $"SVG file does not exist: {absolutePath}";
                return false;
            }

            if (!SvgSourceEncodingUtility.TryReadAllText(absolutePath, out sourceText, out sourceEncoding, out error))
                return false;

            return SvgSafeMaskArtifactSanitizer.TrySanitize(sourceText, out sourceText, out _, out error);
        }

        private static VectorImage LoadVectorImageAsset(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
        }
    }
}
