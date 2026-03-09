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

            if (!ValidateXml(document.WorkingSourceText, out error))
            {
                return false;
            }

            try
            {
                File.WriteAllText(document.AbsolutePath, document.WorkingSourceText, _utf8WithoutBom);
                AssetDatabase.ImportAsset(document.AssetPath, ImportAssetOptions.ForceUpdate);
                document.OriginalSourceText = document.WorkingSourceText;
                document.VectorImageAsset = AssetDatabase.LoadAssetAtPath<VectorImage>(document.AssetPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        #endregion Public Methods

        #region Help Methods

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        #endregion Help Methods
    }
}
