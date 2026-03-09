using System.Collections.Generic;
using Core.UI.Foundation;
using Core.UI.Foundation.Tooling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class AssetLibraryGridPreviewRenderer
    {
        private static readonly string[] PREVIEW_RESOURCE_PATHS =
        {
            "Icons/file-text",
            "Icons/pen",
            "Icons/circle"
        };

        private List<GridViewItem> _previewGridItems;

        public void ApplyPreview(
            VisualElement owner,
            VirtualizedGridView gridView,
            List<GridViewItem> gridItems,
            string previewEnabledClassName)
        {
            if (owner == null || gridView == null || gridItems == null)
            {
                return;
            }

            gridItems.Clear();
            gridItems.AddRange(GetPreviewGridItems());
            gridView.EmptyText = "No SVG assets found";
            gridView.SetItems(gridItems);
            owner.EnableClass(previewEnabledClassName, true);
        }

        public void ClearPreview(
            VisualElement owner,
            VirtualizedGridView gridView,
            List<GridViewItem> gridItems,
            string previewEnabledClassName)
        {
            if (owner == null || gridView == null || gridItems == null)
            {
                return;
            }

            gridItems.Clear();
            gridView.SetItems(gridItems);
            owner.EnableClass(previewEnabledClassName, false);
            _previewGridItems = null;
        }

        private IReadOnlyList<GridViewItem> GetPreviewGridItems()
        {
            _previewGridItems ??= CreatePreviewGridItems();
            return _previewGridItems;
        }

        private static List<GridViewItem> CreatePreviewGridItems()
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
                GroupKey = VectorImageAssetPresentationUtility.ResolveGroupKey(displayName),
                PreviewSource = PreviewImageSource.FromVectorImage(Resources.Load<VectorImage>(resourcePath)),
                UserData = resourcePath
            };
        }
    }
}
