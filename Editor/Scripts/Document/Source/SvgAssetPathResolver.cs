using System;
using System.Collections.Generic;
using Core.UI.Foundation;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SvgEditor.Document
{
    internal sealed class SvgAssetPathResolver
    {
        private const string ASSETS_ROOT = "Assets";
        private const string PACKAGES_ROOT = "Packages/";

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
                    !IsSvgAssetPath(assetPath) ||
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

            if (!IsSvgAssetPath(normalizedAssetPath))
            {
                error = $"Asset is not an editable SVG file: {assetPath}";
                return false;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (IsProjectAssetPath(normalizedAssetPath))
            {
                absolutePath = Path.GetFullPath(Path.Combine(projectRoot, normalizedAssetPath));
                return true;
            }

            if (!normalizedAssetPath.StartsWith(PACKAGES_ROOT, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Unsupported SVG asset location: {assetPath}";
                return false;
            }

            if (!TryResolveEditablePackageInfo(normalizedAssetPath, out UnityEditor.PackageManager.PackageInfo packageInfo, out error))
                return false;

            string packageRoot = NormalizeAssetPath(packageInfo.assetPath);
            if (string.IsNullOrWhiteSpace(packageRoot))
            {
                error = $"Unable to resolve package root for asset: {assetPath}";
                return false;
            }

            if (!normalizedAssetPath.StartsWith(packageRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                error = $"Unable to resolve package-relative SVG path: {assetPath}";
                return false;
            }

            string relativePath = normalizedAssetPath.Substring(packageRoot.Length + 1);

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

            return normalizedAssetPath.StartsWith(PACKAGES_ROOT, StringComparison.OrdinalIgnoreCase) &&
                   TryResolveEditablePackageInfo(normalizedAssetPath, out _, out _);
        }

        private static bool IsProjectAssetPath(string assetPath)
        {
            return string.Equals(assetPath, ASSETS_ROOT, StringComparison.OrdinalIgnoreCase) ||
                   assetPath.StartsWith(ASSETS_ROOT + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEditablePackageSource(UnityEditor.PackageManager.PackageSource source)
        {
            return source == UnityEditor.PackageManager.PackageSource.Embedded ||
                   source == UnityEditor.PackageManager.PackageSource.Local;
        }

        private static bool IsSvgAssetPath(string assetPath)
        {
            return !string.IsNullOrWhiteSpace(assetPath) &&
                   assetPath.EndsWith(AssetFileExtension.SVG, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryResolveEditablePackageInfo(
            string normalizedAssetPath,
            out UnityEditor.PackageManager.PackageInfo packageInfo,
            out string error)
        {
            packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(normalizedAssetPath);
            if (packageInfo == null)
            {
                error = $"Unable to resolve package asset path: {normalizedAssetPath}";
                return false;
            }

            if (!IsEditablePackageSource(packageInfo.source))
            {
                error = $"SVG package assets are only editable for embedded/local packages: {normalizedAssetPath}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath?.Replace('\\', '/').Trim();
        }
    }
}
