using System;
using System.Collections.Generic;
using System.Linq;
using Core.UI.Foundation.Components.Accordion;
using Core.UI.Foundation.Tooling;
using UnityEngine.UIElements;
using SvgEditor.Document;
using SvgEditor.Workspace.AssetLibrary.Grid;
using SvgEditor.Workspace.AssetLibrary.Model;
using SvgEditor.Workspace.AssetLibrary.Presentation;

namespace SvgEditor.Workspace.AssetLibrary.Browser
{
    internal sealed class AssetBrowser
    {
        private const string ALL_CATEGORIES_FILTER_KEY = "__all__";

        private static class ElementName
        {
            public const string REFRESH_BUTTON = "asset-library-refresh-button";
            public const string FILTER_ACCORDION = "asset-library-filter-accordion";
            public const string FILTER_ACCORDION_ITEM = "asset-library-filter-accordion-item";
            public const string FILTER_HOST = "asset-library-filter-host";
            public const string GRID_VIEW = "asset-grid-view";
        }

        private static readonly StringComparer _assetNameComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly Comparison<AssetEntry> _assetEntryComparison =
            static (left, right) =>
            {
                int groupComparison = _assetNameComparer.Compare(left?.GroupKey, right?.GroupKey);
                return groupComparison != 0
                    ? groupComparison
                    : _assetNameComparer.Compare(left?.DisplayName, right?.DisplayName);
            };

        private readonly DocumentRepository _documentRepository;
        private readonly IVectorImageSourceProvider _vectorImageSourceProvider;
        private readonly List<AssetEntry> _allAssetItems = new();
        private readonly List<AssetEntry> _filteredAssetItems = new();
        private readonly List<GridViewItem> _assetGridItems = new();
        private readonly HashSet<string> _filteredAssetPaths = new(StringComparer.Ordinal);

        private Accordion _assetLibraryFilterAccordion;
        private AccordionItem _assetLibraryFilterAccordionItem;
        private VisualElement _assetLibraryFilterHost;
        private FilterBadgeBar _assetLibraryCategoryBar;
        private Button _assetLibraryRefreshButton;
        private AssetGridView _assetGridView;
        private bool _isProgrammaticSelection;
        private string _selectedCategoryKey = ALL_CATEGORIES_FILTER_KEY;
        private Action<string> _loadAsset;
        private Func<string> _getCurrentAssetPath;
        private Func<bool> _canSwitchDocument;

        public AssetBrowser(DocumentRepository documentRepository, IVectorImageSourceProvider vectorImageSourceProvider)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _vectorImageSourceProvider = vectorImageSourceProvider ?? throw new ArgumentNullException(nameof(vectorImageSourceProvider));
        }

        public void Bind(
            VisualElement root,
            Action<string> loadAsset,
            Func<string> getCurrentAssetPath,
            Func<bool> canSwitchDocument)
        {
            Unbind();
            if (root == null)
            {
                return;
            }

            _loadAsset = loadAsset;
            _getCurrentAssetPath = getCurrentAssetPath;
            _canSwitchDocument = canSwitchDocument;
            _assetLibraryRefreshButton = root.Q<Button>(ElementName.REFRESH_BUTTON);
            _assetLibraryFilterAccordion = root.Q<Accordion>(ElementName.FILTER_ACCORDION);
            _assetLibraryFilterAccordionItem = root.Q<AccordionItem>(ElementName.FILTER_ACCORDION_ITEM);
            _assetLibraryFilterHost = root.Q<VisualElement>(ElementName.FILTER_HOST);
            _assetGridView = root.Q<AssetGridView>(ElementName.GRID_VIEW);

            if (_assetGridView == null)
            {
                return;
            }

            InitializeCategoryBar();

            if (_assetLibraryRefreshButton != null)
            {
                _assetLibraryRefreshButton.clicked -= OnRefreshButtonClicked;
                _assetLibraryRefreshButton.clicked += OnRefreshButtonClicked;
            }

            _assetGridView.BindRuntime(OnAssetGridItemSelected, OnAssetGridSelectionChanged);
            _assetGridView.SetEmptyText("No SVG assets found");
            RebuildCategoryFilterBar();
        }

        public void Unbind()
        {
            _assetGridView?.UnbindRuntime();
            if (_assetLibraryRefreshButton != null)
            {
                _assetLibraryRefreshButton.clicked -= OnRefreshButtonClicked;
            }

            _assetLibraryFilterAccordion = null;
            _assetLibraryFilterAccordionItem = null;
            _assetLibraryFilterHost?.Clear();
            _assetGridView = null;
            _assetLibraryCategoryBar = null;
            _assetLibraryFilterHost = null;
            _assetLibraryRefreshButton = null;
            _loadAsset = null;
            _getCurrentAssetPath = null;
            _canSwitchDocument = null;
            _isProgrammaticSelection = false;
            _vectorImageSourceProvider.ClearCache();
        }

        public void RefreshAssetList(bool selectFirst)
        {
            _allAssetItems.Clear();
            IReadOnlyList<string> assetPaths = _documentRepository.FindVectorImageAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                string displayName = VectorImagePresentationUtility.BuildDisplayName(assetPath);
                _allAssetItems.Add(new AssetEntry
                {
                    DisplayName = displayName,
                    AssetPath = assetPath,
                    Library = VectorImagePresentationUtility.BuildLibraryName(assetPath),
                    GroupKey = VectorImagePresentationUtility.ResolveGroupKey(displayName),
                    IsDeveloperFixture = VectorImagePresentationUtility.IsDeveloperFixtureAsset(assetPath)
                });
            }

            _allAssetItems.Sort(_assetEntryComparison);
            RebuildCategoryFilterBar();
            ApplyAssetFilter(selectFirst);
        }

        public void SetSelectionByAssetPath(string assetPath)
        {
            if (_assetGridView == null)
            {
                return;
            }

            _isProgrammaticSelection = true;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                _assetGridView.ClearSelection();
            }
            else
            {
                _assetGridView.SelectById(assetPath);
            }

            _isProgrammaticSelection = false;
        }

        private void InitializeCategoryBar()
        {
            if (_assetLibraryFilterHost == null)
            {
                return;
            }

            _assetLibraryFilterHost.Clear();
            _assetLibraryCategoryBar = new FilterBadgeBar(new FilterBadgeBarClasses
            {
                rootClass = "project-tab__filter-bar",
                buttonClass = "browse-tab__variant-tab",
                activeButtonClass = "browse-tab__variant-tab--active"
            });
            _assetLibraryFilterHost.Add(_assetLibraryCategoryBar);
        }

        private void RebuildCategoryFilterBar()
        {
            List<string> categories = AssetBrowserCategoryController.BuildCategoryList(_allAssetItems, _assetNameComparer);
            _selectedCategoryKey = AssetBrowserCategoryController.NormalizeSelectedCategoryKey(
                _selectedCategoryKey,
                categories,
                ALL_CATEGORIES_FILTER_KEY,
                _assetNameComparer);

            if (_assetLibraryFilterAccordion != null)
            {
                _assetLibraryFilterAccordion.style.display = categories.Count > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            UpdateCategoryFilterAccordionTitle(
                AssetBrowserCategoryController.CountSelectedAssets(
                    _allAssetItems,
                    _selectedCategoryKey,
                    ALL_CATEGORIES_FILTER_KEY,
                    _assetNameComparer));

            if (_assetLibraryCategoryBar == null)
            {
                return;
            }

            List<FilterBadgeOption> options = AssetBrowserCategoryController.BuildOptions(
                categories,
                _selectedCategoryKey,
                ALL_CATEGORIES_FILTER_KEY,
                _assetNameComparer);
            _assetLibraryCategoryBar.Bind(options, _selectedCategoryKey, OnCategorySelected);
        }

        private void OnCategorySelected(string selectedKey)
        {
            string nextCategoryKey = string.IsNullOrWhiteSpace(selectedKey)
                ? ALL_CATEGORIES_FILTER_KEY
                : selectedKey;
            if (string.Equals(_selectedCategoryKey, nextCategoryKey, StringComparison.Ordinal))
            {
                return;
            }

            _selectedCategoryKey = nextCategoryKey;
            RebuildCategoryFilterBar();
            ApplyAssetFilter(selectFirst: false);
        }

        private void ApplyAssetFilter(bool selectFirst)
        {
            _filteredAssetItems.Clear();

            bool showAllCategories = string.Equals(_selectedCategoryKey, ALL_CATEGORIES_FILTER_KEY, StringComparison.Ordinal);
            foreach (AssetEntry item in _allAssetItems)
            {
                if (showAllCategories || _assetNameComparer.Equals(item.Library, _selectedCategoryKey))
                {
                    _filteredAssetItems.Add(item);
                }
            }

            AssetBrowserGridItemBuilder.Populate(
                _filteredAssetItems,
                _vectorImageSourceProvider,
                _assetGridItems,
                _filteredAssetPaths);
            UpdateCategoryFilterAccordionTitle(_assetGridItems.Count);
            _assetGridView?.SetItems(_assetGridItems);

            string currentAssetPath = _getCurrentAssetPath?.Invoke();
            bool hasCurrentSelection = !string.IsNullOrWhiteSpace(currentAssetPath) &&
                                       _filteredAssetPaths.Contains(currentAssetPath);
            if (hasCurrentSelection)
            {
                SetSelectionByAssetPath(currentAssetPath);
            }
            else
            {
                SetSelectionByAssetPath(null);
            }

            if (selectFirst && !hasCurrentSelection)
            {
                string firstAssetPath = _filteredAssetItems.Count > 0
                    ? _filteredAssetItems[0].AssetPath
                    : null;
                if (string.IsNullOrWhiteSpace(firstAssetPath))
                {
                    return;
                }

                _loadAsset?.Invoke(firstAssetPath);
                SetSelectionByAssetPath(firstAssetPath);
            }
        }

        private void OnRefreshButtonClicked()
        {
            RefreshAssetList(selectFirst: false);
        }

        private void UpdateCategoryFilterAccordionTitle(int assetCount)
        {
            if (_assetLibraryFilterAccordionItem == null)
            {
                return;
            }

            _assetLibraryFilterAccordionItem.Title = AssetBrowserCategoryController.BuildAccordionTitle(
                _selectedCategoryKey,
                assetCount,
                ALL_CATEGORIES_FILTER_KEY);
        }

        private void OnAssetGridSelectionChanged(List<GridViewItem> selectedItems)
        {
            if (_isProgrammaticSelection)
            {
                return;
            }

            TryLoadSelectedAsset(selectedItems?.FirstOrDefault());
        }

        private void OnAssetGridItemSelected(GridViewItem selectedItem)
        {
            if (_isProgrammaticSelection)
            {
                return;
            }

            TryLoadSelectedAsset(selectedItem);
        }

        private void TryLoadSelectedAsset(GridViewItem selectedItem)
        {
            string selectedAssetPath = selectedItem?.UserData as string;
            if (string.IsNullOrWhiteSpace(selectedAssetPath))
            {
                return;
            }

            string currentAssetPath = _getCurrentAssetPath?.Invoke();
            if (!string.IsNullOrWhiteSpace(currentAssetPath) &&
                string.Equals(currentAssetPath, selectedAssetPath, StringComparison.Ordinal))
            {
                return;
            }

            if (_canSwitchDocument != null && !_canSwitchDocument())
            {
                SetSelectionByAssetPath(currentAssetPath);
                return;
            }

            _loadAsset?.Invoke(selectedAssetPath);
        }
    }
}
