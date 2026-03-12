using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SvgEditor
{
    internal sealed class SvgAssetPathResolver
    {
        public IReadOnlyList<string> FindEditableSvgAssetPaths(string searchRoot = null)
        {
            string[] searchRoots = string.IsNullOrWhiteSpace(searchRoot)
                ? Array.Empty<string>()
                : new[] { searchRoot };

            string[] guids = searchRoots.Length > 0
                ? AssetDatabase.FindAssets("t:VectorImage", searchRoots)
                : AssetDatabase.FindAssets("t:VectorImage");

            List<string> results = new(guids.Length);
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(assetPath) ||
                    !assetPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                    !IsEditableSvgAssetPath(assetPath))
                {
                    continue;
                }

                results.Add(assetPath);
            }

            results.Sort(StringComparer.Ordinal);
            return results;
        }

        public bool TryResolveAbsolutePath(string assetPath, out string absolutePath, out string error)
        {
            absolutePath = string.Empty;
            error = string.Empty;

            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrWhiteSpace(normalizedAssetPath))
            {
                error = "SVG asset path is empty.";
                return false;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
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
    }
}
