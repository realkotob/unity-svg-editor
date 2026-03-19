using System.Collections.Generic;
using SvgEditor.UI.AssetLibrary.Grid;
using SvgEditor.UI.AssetLibrary.Model;
using SvgEditor.UI.AssetLibrary.Presentation;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.UI.AssetLibrary.Browser
{
    internal static class AssetBrowserGridItemBuilder
    {
        public static void Populate(
            IReadOnlyList<AssetEntry> filteredAssetItems,
            IVectorImageSourceProvider vectorImageSourceProvider,
            IList<GridViewItem> assetGridItems,
            ISet<string> filteredAssetPaths)
        {
            assetGridItems.Clear();
            filteredAssetPaths.Clear();

            foreach (AssetEntry item in filteredAssetItems)
            {
                filteredAssetPaths.Add(item.AssetPath);
                assetGridItems.Add(new GridViewItem
                {
                    Id = item.AssetPath,
                    Label = item.DisplayName,
                    SortKey = item.DisplayName,
                    GroupKey = item.GroupKey,
                    PreviewSource = PreviewImageSource.FromVectorImage(vectorImageSourceProvider.Load(item.AssetPath)),
                    UserData = item.AssetPath
                });
            }
        }
    }
}
