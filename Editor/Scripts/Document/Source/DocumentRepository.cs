using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Document.Structure.Xml;

namespace SvgEditor.Document
{
    internal sealed class DocumentRepository
    {
        private static readonly UTF8Encoding _utf8WithoutBom = new(false);
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

            string sourceText;
            try
            {
                sourceText = File.ReadAllText(absolutePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                error = $"Failed to read SVG source: {ex.Message}";
                return false;
            }

            if (!SvgSafeMaskArtifactSanitizer.TrySanitize(sourceText, out sourceText, out _, out error))
                return false;

            var vectorImageAsset = AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
            document = new DocumentSession
            {
                AssetPath = assetPath,
                AbsolutePath = absolutePath,
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
                File.WriteAllText(absolutePath, sourceTextToPersist, _utf8WithoutBom);
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
