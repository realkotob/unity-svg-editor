using System;
using System.Collections.Generic;
using System.Linq;
using Core.UI.Foundation.Components.Accordion;
using Core.UI.Foundation.Tooling;
using SvgEditor.Workspace.AssetLibrary.Model;

namespace SvgEditor.Workspace.AssetLibrary.Browser
{
    internal static class AssetBrowserCategoryController
    {
        public static List<string> BuildCategoryList(IEnumerable<AssetEntry> items, StringComparer comparer)
        {
            return items
                .Select(static item => item.Library)
                .Where(static category => !string.IsNullOrWhiteSpace(category))
                .Distinct(comparer)
                .OrderBy(static category => category, comparer)
                .ToList();
        }

        public static string NormalizeSelectedCategoryKey(
            string selectedCategoryKey,
            IReadOnlyCollection<string> categories,
            string allCategoriesFilterKey,
            StringComparer comparer)
        {
            if (string.IsNullOrWhiteSpace(selectedCategoryKey) ||
                string.Equals(selectedCategoryKey, allCategoriesFilterKey, StringComparison.Ordinal))
            {
                return allCategoriesFilterKey;
            }

            foreach (string category in categories)
            {
                if (comparer.Equals(category, selectedCategoryKey))
                {
                    return category;
                }
            }

            return allCategoriesFilterKey;
        }

        public static List<FilterBadgeOption> BuildOptions(
            IReadOnlyList<string> categories,
            string selectedCategoryKey,
            string allCategoriesFilterKey,
            StringComparer comparer)
        {
            List<FilterBadgeOption> options = new()
            {
                new()
                {
                    key = allCategoriesFilterKey,
                    label = "All",
                    tooltip = "Show all SVG assets",
                    isSelected = string.Equals(selectedCategoryKey, allCategoriesFilterKey, StringComparison.Ordinal)
                }
            };

            foreach (string category in categories)
            {
                options.Add(new FilterBadgeOption
                {
                    key = category,
                    label = category,
                    tooltip = $"Filter assets in {category}",
                    isSelected = comparer.Equals(selectedCategoryKey, category)
                });
            }

            return options;
        }

        public static int CountSelectedAssets(
            IReadOnlyList<AssetEntry> items,
            string selectedCategoryKey,
            string allCategoriesFilterKey,
            StringComparer comparer)
        {
            if (string.Equals(selectedCategoryKey, allCategoriesFilterKey, StringComparison.Ordinal))
            {
                return items.Count;
            }

            int count = 0;
            foreach (AssetEntry item in items)
            {
                if (comparer.Equals(item.Library, selectedCategoryKey))
                {
                    count++;
                }
            }

            return count;
        }

        public static string BuildAccordionTitle(
            string selectedCategoryKey,
            int assetCount,
            string allCategoriesFilterKey)
        {
            string selectedLabel = string.Equals(selectedCategoryKey, allCategoriesFilterKey, StringComparison.Ordinal)
                ? "All"
                : selectedCategoryKey;
            return assetCount > 0
                ? $"Libraries: {selectedLabel} ({assetCount})"
                : "Libraries";
        }
    }
}
