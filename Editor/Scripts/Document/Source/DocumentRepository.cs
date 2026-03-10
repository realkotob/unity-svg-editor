using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentRepository
    {
        private static readonly UTF8Encoding _utf8WithoutBom = new(false);
        private readonly SvgDocumentModelLoader _documentModelLoader = new();
        private readonly SvgDocumentModelSerializer _documentModelSerializer = new();

        #region Public Methods

        public IReadOnlyList<string> FindVectorImageAssetPaths(string searchRoot = "Assets")
        {
            string[] searchRoots = string.IsNullOrWhiteSpace(searchRoot)
                ? Array.Empty<string>()
                : new[] { searchRoot };

            string[] guids = searchRoots.Length > 0
                ? AssetDatabase.FindAssets("t:VectorImage", searchRoots)
                : AssetDatabase.FindAssets("t:VectorImage");

            List<string> results = new(guids.Length);
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                if (!assetPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                results.Add(assetPath);
            }

            results.Sort(StringComparer.Ordinal);
            return results;
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

            var absolutePath = ToAbsolutePath(assetPath);
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

            var vectorImageAsset = AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
            document = new DocumentSession
            {
                AssetPath = assetPath,
                AbsolutePath = absolutePath,
                VectorImageAsset = vectorImageAsset,
                OriginalSourceText = sourceText,
                WorkingSourceText = sourceText
            };
            RefreshDocumentModelSnapshot(document, sourceText);

            return true;
        }

        public bool ValidateXml(string sourceText, out string error)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                error = "SVG source is empty.";
                return false;
            }

            return SvgDocumentXmlUtility.TryLoadDocument(sourceText, out _, out error);
        }

        public bool Save(DocumentSession document, out string error)
        {
            error = string.Empty;
            if (document == null)
            {
                error = "Document is null.";
                return false;
            }

            if (!TryResolveSourceTextToPersist(document, out string sourceTextToPersist, out error))
            {
                return false;
            }

            try
            {
                File.WriteAllText(document.AbsolutePath, sourceTextToPersist, _utf8WithoutBom);
                AssetDatabase.ImportAsset(document.AssetPath, ImportAssetOptions.ForceUpdate);
                document.WorkingSourceText = sourceTextToPersist;
                document.OriginalSourceText = sourceTextToPersist;
                document.VectorImageAsset = AssetDatabase.LoadAssetAtPath<VectorImage>(document.AssetPath);
                RefreshDocumentModelSnapshot(document, sourceTextToPersist);
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
            if (document == null)
                return;

            RefreshDocumentModelSnapshot(document, document.WorkingSourceText);
        }

        internal bool TryResolveSourceTextToPersist(DocumentSession document, out string sourceText, out string error)
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
                    return false;
            }

            return ValidateXml(sourceText, out error);
        }

        #endregion Public Methods

        #region Help Methods

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private void RefreshDocumentModelSnapshot(DocumentSession document, string sourceText)
        {
            if (document == null)
                return;

            document.DocumentModel = null;
            document.DocumentModelLoadError = string.Empty;

            if (!_documentModelLoader.TryLoad(sourceText, out SvgDocumentModel documentModel, out string error))
            {
                document.DocumentModelLoadError = error;
                return;
            }

            document.DocumentModel = documentModel;
        }

        #endregion Help Methods
    }
}
