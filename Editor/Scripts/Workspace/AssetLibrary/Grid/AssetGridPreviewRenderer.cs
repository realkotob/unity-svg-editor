using System.Collections.Generic;
using Core.UI.Foundation;
using Core.UI.Foundation.Tooling;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Shared;
using SvgEditor.Workspace.AssetLibrary.Presentation;

namespace SvgEditor.Workspace.AssetLibrary.Grid
{
    internal sealed class AssetGridPreviewRenderer : PreviewCollectionRenderer<VirtualizedGridView, GridViewItem>
    {
        private static readonly string[] PREVIEW_RESOURCE_PATHS =
        {
            SvgEditorIconClass.RESOURCE_FILE_TEXT,
            SvgEditorIconClass.RESOURCE_PEN,
            SvgEditorIconClass.RESOURCE_CIRCLE
        };

        protected override void ApplyPreviewItems(VirtualizedGridView gridView, List<GridViewItem> gridItems)
        {
            gridView.EmptyText = "No SVG assets found";
            gridView.SetItems(gridItems);
        }

        protected override void ClearPreviewItems(VirtualizedGridView gridView, List<GridViewItem> gridItems)
        {
            gridView.SetItems(gridItems);
        }

        protected override List<GridViewItem> CreatePreviewItems()
        {
            List<GridViewItem> previewGridItems = new();
            foreach (string resourcePath in PREVIEW_RESOURCE_PATHS)
            {
                previewGridItems.Add(CreatePreviewGridItem(resourcePath));
            }

            return previewGridItems;
        }

        private static GridViewItem CreatePreviewGridItem(string resourcePath)
        {
            string displayName = resourcePath[(resourcePath.LastIndexOf('/') + 1)..];
            return new GridViewItem
            {
                Id = resourcePath,
                Label = displayName,
                SortKey = displayName,
                GroupKey = VectorImagePresentationUtility.ResolveGroupKey(displayName),
                PreviewSource = PreviewImageSource.FromVectorImage(Resources.Load<VectorImage>(resourcePath)),
                UserData = resourcePath
            };
        }
    }
}
