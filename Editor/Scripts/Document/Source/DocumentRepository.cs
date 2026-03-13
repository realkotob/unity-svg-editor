using System;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Document.Structure.Xml;

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

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "SVG asset path is empty.";
                return false;
            }

            if (!_assetPathResolver.TryResolveAbsolutePath(assetPath, out string absolutePath, out error))
            {
                return false;
            }

            if (!File.Exists(absolutePath))
            {
                error = $"SVG file does not exist: {absolutePath}";
                return false;
            }

            if (!SvgSourceEncodingUtility.TryReadAllText(absolutePath, out string sourceText, out var sourceEncoding, out error))
            {
                return false;
            }

            if (!SvgSafeMaskArtifactSanitizer.TrySanitize(sourceText, out sourceText, out _, out error))
                return false;

            var vectorImageAsset = AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
            document = new DocumentSession
            {
                AssetPath = assetPath,
                AbsolutePath = absolutePath,
                SourceEncoding = sourceEncoding,
                VectorImageAsset = vectorImageAsset,
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

            if (!_assetPathResolver.TryResolveAbsolutePath(document.AssetPath, out string absolutePath, out error))
            {
                return false;
            }

            try
            {
                if (!SvgSourceEncodingUtility.TryWriteAllText(absolutePath, sourceTextToPersist, document.SourceEncoding, out error))
                {
                    return false;
                }

                AssetDatabase.ImportAsset(document.AssetPath, ImportAssetOptions.ForceUpdate);
                document.AbsolutePath = absolutePath;
                document.WorkingSourceText = sourceTextToPersist;
                document.OriginalSourceText = sourceTextToPersist;
                document.VectorImageAsset = AssetDatabase.LoadAssetAtPath<VectorImage>(document.AssetPath);
                _documentSourceService.RefreshDocumentModelSnapshot(document, sourceTextToPersist);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
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
    }
}
