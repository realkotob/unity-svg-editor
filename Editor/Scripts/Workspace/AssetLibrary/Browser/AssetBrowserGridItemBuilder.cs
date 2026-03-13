using System.Collections.Generic;
using Core.UI.Foundation.Tooling;
using SvgEditor.Workspace.AssetLibrary.Grid;
using SvgEditor.Workspace.AssetLibrary.Model;
using SvgEditor.Workspace.AssetLibrary.Presentation;

namespace SvgEditor.Workspace.AssetLibrary.Browser
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
