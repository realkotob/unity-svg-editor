using System.Collections.Generic;
using UnityEditor;

namespace UnitySvgEditor.Editor
{
    internal sealed class AssetVectorImageCache
    {
        private readonly Dictionary<string, UnityEngine.UIElements.VectorImage> _cache = new(System.StringComparer.Ordinal);

        public UnityEngine.UIElements.VectorImage GetOrLoad(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            if (_cache.TryGetValue(assetPath, out var cached))
                return cached;

            var loaded = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VectorImage>(assetPath);
            _cache[assetPath] = loaded;
            return loaded;
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
