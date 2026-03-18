using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal interface IVectorImageSourceProvider
    {
        VectorImage Load(string assetPath);
        bool Contains(VectorImage vectorImage);
        string GetAssetPath(VectorImage vectorImage);
        bool IsValidFolder(string assetPath);
        IReadOnlyList<string> FindAssetPaths(string searchFilter, IReadOnlyList<string> searchRoots = null);
        void ClearCache();
    }
}
