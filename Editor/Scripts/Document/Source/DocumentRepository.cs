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

        public IReadOnlyList<string> FindVectorImageAssetPaths(string searchRoot = null)
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

                if (!IsEditableSvgAssetPath(assetPath))
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

            if (!TryResolveAbsolutePath(assetPath, out var absolutePath, out error))
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

            if (!TryResolveAbsolutePath(document.AssetPath, out string absolutePath, out error))
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

            if (!SvgSafeMaskArtifactSanitizer.TrySanitize(sourceText, out sourceText, out _, out error))
                return false;

            return ValidateXml(sourceText, out error);
        }

        #endregion Public Methods

        #region Help Methods

        private static bool TryResolveAbsolutePath(string assetPath, out string absolutePath, out string error)
        {
            absolutePath = string.Empty;
            error = string.Empty;

            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrWhiteSpace(normalizedAssetPath))
            {
                error = "SVG asset path is empty.";
                return false;
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (IsProjectAssetPath(normalizedAssetPath))
            {
                absolutePath = Path.GetFullPath(Path.Combine(projectRoot, normalizedAssetPath));
                return true;
            }

            if (!normalizedAssetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                error = $"Unsupported SVG asset location: {assetPath}";
                return false;
            }

            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(normalizedAssetPath);
            if (packageInfo == null)
            {
                error = $"Unable to resolve package asset path: {assetPath}";
                return false;
            }

            if (!IsEditablePackageSource(packageInfo.source))
            {
                error = $"SVG package assets are only editable for embedded/local packages: {assetPath}";
                return false;
            }

            string packageRoot = NormalizeAssetPath(packageInfo.assetPath);
            if (string.IsNullOrWhiteSpace(packageRoot))
            {
                error = $"Unable to resolve package root for asset: {assetPath}";
                return false;
            }

            string relativePath = normalizedAssetPath.StartsWith(packageRoot + "/", StringComparison.OrdinalIgnoreCase)
                ? normalizedAssetPath.Substring(packageRoot.Length + 1)
                : Path.GetFileName(normalizedAssetPath);

            absolutePath = Path.GetFullPath(Path.Combine(packageInfo.resolvedPath, relativePath));
            return true;
        }

        private static bool IsEditableSvgAssetPath(string assetPath)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (IsProjectAssetPath(normalizedAssetPath))
            {
                return true;
            }

            if (!normalizedAssetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(normalizedAssetPath);
            return packageInfo != null && IsEditablePackageSource(packageInfo.source);
        }

        private static bool IsProjectAssetPath(string assetPath)
        {
            return string.Equals(assetPath, "Assets", StringComparison.OrdinalIgnoreCase) ||
                   assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEditablePackageSource(UnityEditor.PackageManager.PackageSource source)
        {
            return source == UnityEditor.PackageManager.PackageSource.Embedded ||
                   source == UnityEditor.PackageManager.PackageSource.Local;
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath?.Replace('\\', '/').Trim();
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
