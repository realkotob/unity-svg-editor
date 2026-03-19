using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Core.UI.Extensions;
using SvgEditor.Core.Shared;

namespace SvgEditor.Core.Svg.Source
{
    internal sealed class AssetPathResolver
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

        public Result<string> ResolveAbsolutePath(string assetPath)
        {
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrWhiteSpace(normalizedAssetPath))
            {
                return Result.Failure<string>("SVG asset path is empty.");
            }

            if (!IsSvgAssetPath(normalizedAssetPath))
            {
                return Result.Failure<string>($"Asset is not an editable SVG file: {assetPath}");
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (IsProjectAssetPath(normalizedAssetPath))
            {
                return Result.Success(Path.GetFullPath(Path.Combine(projectRoot, normalizedAssetPath)));
            }

            if (!normalizedAssetPath.StartsWith(PACKAGES_ROOT, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string>($"Unsupported SVG asset location: {assetPath}");
            }

            if (!TryResolveEditablePackageInfo(normalizedAssetPath, out UnityEditor.PackageManager.PackageInfo packageInfo, out string error))
                return Result.Failure<string>(error);

            string packageRoot = NormalizeAssetPath(packageInfo.assetPath);
            if (string.IsNullOrWhiteSpace(packageRoot))
            {
                return Result.Failure<string>($"Unable to resolve package root for asset: {assetPath}");
            }

            if (!normalizedAssetPath.StartsWith(packageRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string>($"Unable to resolve package-relative SVG path: {assetPath}");
            }

            string relativePath = normalizedAssetPath.Substring(packageRoot.Length + 1);
            return Result.Success(Path.GetFullPath(Path.Combine(packageInfo.resolvedPath, relativePath)));
        }

        public bool TryResolveAbsolutePath(string assetPath, out string absolutePath, out string error)
        {
            Result<string> result = ResolveAbsolutePath(assetPath);
            absolutePath = result.GetValueOrDefault(string.Empty);
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
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
                   assetPath.EndsWith(SvgEditorAssetFileExtension.SVG, StringComparison.OrdinalIgnoreCase);
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
