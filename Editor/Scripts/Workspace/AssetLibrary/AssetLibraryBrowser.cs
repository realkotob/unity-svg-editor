using System;
using System.Collections.Generic;
using System.Linq;
using Core.UI.Foundation.Tooling;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class AssetLibraryBrowser
    {
        private const string AllCategoriesFilterKey = "__all__";

        private static readonly StringComparer AssetNameComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly Comparison<AssetLibraryEntry> AssetEntryComparison =
            static (left, right) =>
            {
                int groupComparison = AssetNameComparer.Compare(left?.GroupKey, right?.GroupKey);
                return groupComparison != 0
                    ? groupComparison
                    : AssetNameComparer.Compare(left?.DisplayName, right?.DisplayName);
            };

        private readonly DocumentRepository _documentRepository;
        private readonly IVectorImageSourceProvider _vectorImageSourceProvider;
        private readonly List<AssetLibraryEntry> _allAssetItems = new();
        private readonly List<AssetLibraryEntry> _filteredAssetItems = new();
        private readonly List<GridViewItem> _assetGridItems = new();
        private readonly HashSet<string> _filteredAssetPaths = new(StringComparer.Ordinal);

        private VisualElement _assetLibraryFilterHost;
        private FilterBadgeBar _assetLibraryCategoryBar;
        private Button _assetLibraryRefreshButton;
        private AssetLibraryGridView _assetGridView;
        private bool _isProgrammaticSelection;
        private string _selectedCategoryKey = AllCategoriesFilterKey;
        private Action<string> _loadAsset;
        private Func<string> _getCurrentAssetPath;
        private Func<bool> _canSwitchDocument;

        public AssetLibraryBrowser(DocumentRepository documentRepository, IVectorImageSourceProvider vectorImageSourceProvider)
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
            _assetLibraryRefreshButton = root.Q<Button>("asset-library-refresh-button");
            _assetLibraryFilterHost = root.Q<VisualElement>("asset-library-filter-host");
            _assetGridView = root.Q<AssetLibraryGridView>("asset-grid-view");

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
                string displayName = VectorImageAssetPresentationUtility.BuildDisplayName(assetPath);
                _allAssetItems.Add(new AssetLibraryEntry
                {
                    DisplayName = displayName,
                    AssetPath = assetPath,
                    Library = VectorImageAssetPresentationUtility.BuildLibraryName(assetPath),
                    GroupKey = VectorImageAssetPresentationUtility.ResolveGroupKey(displayName),
                    IsDeveloperFixture = VectorImageAssetPresentationUtility.IsDeveloperFixtureAsset(assetPath)
                });
            }

            _allAssetItems.Sort(AssetEntryComparison);
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
            List<string> categories = _allAssetItems
                .Select(static item => item.Library)
                .Where(static category => !string.IsNullOrWhiteSpace(category))
                .Distinct(AssetNameComparer)
                .OrderBy(static category => category, AssetNameComparer)
                .ToList();

            if (!string.Equals(_selectedCategoryKey, AllCategoriesFilterKey, StringComparison.Ordinal) &&
                !categories.Contains(_selectedCategoryKey, AssetNameComparer))
            {
                _selectedCategoryKey = AllCategoriesFilterKey;
            }

            if (_assetLibraryFilterHost != null)
            {
                _assetLibraryFilterHost.style.display = categories.Count > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_assetLibraryCategoryBar == null)
            {
                return;
            }

            List<FilterBadgeOption> options = new()
            {
                new()
                {
                    key = AllCategoriesFilterKey,
                    label = "All",
                    tooltip = "Show all SVG assets",
                    isSelected = string.Equals(_selectedCategoryKey, AllCategoriesFilterKey, StringComparison.Ordinal)
                }
            };

            foreach (string category in categories)
            {
                options.Add(new FilterBadgeOption
                {
                    key = category,
                    label = category,
                    tooltip = $"Filter assets in {category}",
                    isSelected = AssetNameComparer.Equals(_selectedCategoryKey, category)
                });
            }

            _assetLibraryCategoryBar.Bind(options, _selectedCategoryKey, OnCategorySelected);
        }

        private void OnCategorySelected(string selectedKey)
        {
            string nextCategoryKey = string.IsNullOrWhiteSpace(selectedKey)
                ? AllCategoriesFilterKey
                : selectedKey;
            if (string.Equals(_selectedCategoryKey, nextCategoryKey, StringComparison.Ordinal))
            {
                return;
            }

            _selectedCategoryKey = nextCategoryKey;
            ApplyAssetFilter(selectFirst: false);
        }

        private void ApplyAssetFilter(bool selectFirst)
        {
            _filteredAssetItems.Clear();

            bool showAllCategories = string.Equals(_selectedCategoryKey, AllCategoriesFilterKey, StringComparison.Ordinal);
            foreach (AssetLibraryEntry item in _allAssetItems)
            {
                if (showAllCategories || AssetNameComparer.Equals(item.Library, _selectedCategoryKey))
                {
                    _filteredAssetItems.Add(item);
                }
            }

            RebuildFilteredAssetPathLookup();
            RebuildAssetGridItems();
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

        private void RebuildAssetGridItems()
        {
            _assetGridItems.Clear();

            foreach (AssetLibraryEntry item in _filteredAssetItems)
            {
                _assetGridItems.Add(new GridViewItem
                {
                    Id = item.AssetPath,
                    Label = item.DisplayName,
                    SortKey = item.DisplayName,
                    GroupKey = item.GroupKey,
                    PreviewSource = PreviewImageSource.FromVectorImage(_vectorImageSourceProvider.Load(item.AssetPath)),
                    UserData = item.AssetPath
                });
            }
        }

        private void OnRefreshButtonClicked()
        {
            RefreshAssetList(selectFirst: false);
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

        private void RebuildFilteredAssetPathLookup()
        {
            _filteredAssetPaths.Clear();
            foreach (AssetLibraryEntry item in _filteredAssetItems)
            {
                _filteredAssetPaths.Add(item.AssetPath);
            }
        }
    }
}
