using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using SvgEditor.Core.Shared;

namespace SvgEditor.UI.AssetLibrary.Browser
{
    internal sealed class AssetDatabaseVectorImageSourceProvider : IVectorImageSourceProvider
    {
        private readonly Dictionary<string, VectorImage> _cache = new(StringComparer.Ordinal);

        public VectorImage Load(string assetPath)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrWhiteSpace(normalizedAssetPath))
            {
                return null;
            }

            if (_cache.TryGetValue(normalizedAssetPath, out VectorImage cachedVectorImage))
            {
                return cachedVectorImage;
            }

            VectorImage vectorImage = AssetDatabase.LoadAssetAtPath<VectorImage>(normalizedAssetPath);
            _cache[normalizedAssetPath] = vectorImage;
            return vectorImage;
        }

        public bool Contains(VectorImage vectorImage)
        {
            return vectorImage != null && AssetDatabase.Contains(vectorImage);
        }

        public string GetAssetPath(VectorImage vectorImage)
        {
            return NormalizeAssetPath(vectorImage == null ? string.Empty : AssetDatabase.GetAssetPath(vectorImage));
        }

        public bool IsValidFolder(string assetPath)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            return !string.IsNullOrWhiteSpace(normalizedAssetPath) && AssetDatabase.IsValidFolder(normalizedAssetPath);
        }

        public IReadOnlyList<string> FindAssetPaths(string searchFilter, IReadOnlyList<string> searchRoots = null)
        {
            string[] roots = ToSearchRoots(searchRoots);
            string[] guids = roots.Length > 0
                ? AssetDatabase.FindAssets(searchFilter, roots)
                : AssetDatabase.FindAssets(searchFilter);

            List<string> assetPaths = new(guids.Length);
            foreach (string guid in guids)
            {
                string assetPath = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(guid));
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    assetPaths.Add(assetPath);
                }
            }

            return assetPaths;
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath?.Replace('\\', '/').Trim() ?? string.Empty;
        }

        private static string[] ToSearchRoots(IReadOnlyList<string> searchRoots)
        {
            if (searchRoots == null || searchRoots.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<string> roots = new(searchRoots.Count);
            for (int index = 0; index < searchRoots.Count; index++)
            {
                string root = NormalizeAssetPath(searchRoots[index]);
                if (!string.IsNullOrWhiteSpace(root))
                {
                    roots.Add(root);
                }
            }

            return roots.Count > 0 ? roots.ToArray() : Array.Empty<string>();
        }
    }
}
