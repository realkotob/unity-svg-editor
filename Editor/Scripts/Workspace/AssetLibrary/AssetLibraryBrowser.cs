using System;
using System.Collections.Generic;
using System.Linq;
using Core.UI.Foundation;
using Core.UI.Foundation.Tooling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class AssetLibraryBrowser
    {
        private static readonly StringComparer AssetNameComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly Comparison<AssetLibraryEntry> FixtureEntryComparison =
            static (left, right) => AssetNameComparer.Compare(left?.DisplayName, right?.DisplayName);
        private static readonly Comparison<AssetLibraryEntry> AssetEntryComparison =
            static (left, right) =>
            {
                int groupComparison = AssetNameComparer.Compare(left?.GroupKey, right?.GroupKey);
                return groupComparison != 0
                    ? groupComparison
                    : AssetNameComparer.Compare(left?.DisplayName, right?.DisplayName);
            };

        private readonly DocumentRepository _documentRepository;
        private readonly List<AssetLibraryEntry> _allAssetItems = new();
        private readonly List<AssetLibraryEntry> _fixtureAssetItems = new();
        private readonly List<AssetLibraryEntry> _filteredAssetItems = new();
        private readonly List<GridViewItem> _fixtureGridItems = new();
        private readonly List<GridViewItem> _assetGridItems = new();
        private readonly AssetVectorImageCache _assetVectorCache = new();
        private readonly HashSet<string> _fixtureAssetPaths = new(StringComparer.Ordinal);
        private readonly HashSet<string> _filteredAssetPaths = new(StringComparer.Ordinal);

        private VisualElement _fixtureLibrarySection;
        private Button _assetLibraryRefreshButton;
        private AssetLibraryGridView _fixtureGridView;
        private AssetLibraryGridView _assetGridView;
        private bool _isProgrammaticSelection;
        private Action<string> _loadAsset;
        private Func<string> _getCurrentAssetPath;
        private Func<bool> _canSwitchDocument;

        public AssetLibraryBrowser(DocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
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
            _fixtureLibrarySection = root.Q<VisualElement>("fixture-library-section");
            _fixtureGridView = root.Q<AssetLibraryGridView>("fixture-grid-view");
            _assetGridView = root.Q<AssetLibraryGridView>("asset-grid-view");

            if (_assetGridView == null || _fixtureGridView == null)
            {
                return;
            }

            if (_assetLibraryRefreshButton != null)
            {
                _assetLibraryRefreshButton.clicked -= OnRefreshButtonClicked;
                _assetLibraryRefreshButton.clicked += OnRefreshButtonClicked;
            }

            _fixtureGridView.BindRuntime(OnFixtureGridItemSelected, OnFixtureGridSelectionChanged);
            _fixtureGridView.SetEmptyText("No fixture SVGs");
            _assetGridView.BindRuntime(OnAssetGridItemSelected, OnAssetGridSelectionChanged);
            _assetGridView.SetEmptyText("No SVG assets found");
        }

        public void Unbind()
        {
            _fixtureGridView?.UnbindRuntime();
            _assetGridView?.UnbindRuntime();
            if (_assetLibraryRefreshButton != null)
            {
                _assetLibraryRefreshButton.clicked -= OnRefreshButtonClicked;
            }

            _fixtureGridView = null;
            _assetGridView = null;
            _fixtureLibrarySection = null;
            _assetLibraryRefreshButton = null;
            _loadAsset = null;
            _getCurrentAssetPath = null;
            _canSwitchDocument = null;
            _isProgrammaticSelection = false;
            _assetVectorCache.Clear();
        }

        public void RefreshAssetList(bool selectFirst)
        {
            _allAssetItems.Clear();
            _fixtureAssetItems.Clear();
            _fixtureAssetPaths.Clear();
            IReadOnlyList<string> assetPaths = _documentRepository.FindVectorImageAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                bool isFixture = VectorImageAssetPresentationUtility.IsDeveloperFixtureAsset(assetPath);
                string displayName = VectorImageAssetPresentationUtility.BuildDisplayName(assetPath);
                var entry = new AssetLibraryEntry
                {
                    DisplayName = displayName,
                    AssetPath = assetPath,
                    Library = VectorImageAssetPresentationUtility.BuildLibraryName(assetPath),
                    GroupKey = VectorImageAssetPresentationUtility.ResolveGroupKey(displayName),
                    IsDeveloperFixture = isFixture
                };

                if (isFixture)
                {
                    _fixtureAssetItems.Add(entry);
                    _fixtureAssetPaths.Add(assetPath);
                    continue;
                }

                _allAssetItems.Add(entry);
            }

            _fixtureAssetItems.Sort(FixtureEntryComparison);
            _allAssetItems.Sort(AssetEntryComparison);

            ApplyAssetFilter(selectFirst);
        }

        public void SetSelectionByAssetPath(string assetPath)
        {
            if (_assetGridView == null || _fixtureGridView == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                _fixtureGridView.ClearSelection();
                _assetGridView.ClearSelection();
                return;
            }

            _isProgrammaticSelection = true;
            bool isFixture = _fixtureAssetPaths.Contains(assetPath);
            if (isFixture)
            {
                _assetGridView.ClearSelection();
                _fixtureGridView.SelectById(assetPath);
            }
            else
            {
                _fixtureGridView.ClearSelection();
                _assetGridView.SelectById(assetPath);
            }
            _isProgrammaticSelection = false;
        }

        private void ApplyAssetFilter(bool selectFirst)
        {
            _filteredAssetItems.Clear();
            _filteredAssetItems.AddRange(_allAssetItems);
            RebuildFilteredAssetPathLookup();

            RebuildFixtureGridItems();
            RebuildAssetGridItems();
            _fixtureGridView?.SetItems(_fixtureGridItems);
            _assetGridView?.SetItems(_assetGridItems);
            if (_fixtureLibrarySection != null)
            {
                _fixtureLibrarySection.style.display = DisplayStyle.Flex;
            }

            var currentAssetPath = _getCurrentAssetPath?.Invoke();
            bool hasCurrentSelection = !string.IsNullOrWhiteSpace(currentAssetPath) &&
                                       (_filteredAssetPaths.Contains(currentAssetPath) ||
                                        _fixtureAssetPaths.Contains(currentAssetPath));
            if (hasCurrentSelection)
            {
                SetSelectionByAssetPath(currentAssetPath);
            }

            if (selectFirst && !hasCurrentSelection)
            {
                string firstAssetPath = _filteredAssetItems.Count > 0
                    ? _filteredAssetItems[0].AssetPath
                    : _fixtureAssetItems.Count > 0 ? _fixtureAssetItems[0].AssetPath : null;
                if (string.IsNullOrWhiteSpace(firstAssetPath))
                {
                    return;
                }

                _loadAsset?.Invoke(firstAssetPath);
                SetSelectionByAssetPath(firstAssetPath);
            }
        }

        private void RebuildFixtureGridItems()
        {
            _fixtureGridItems.Clear();

            foreach (var item in _fixtureAssetItems)
            {
                _fixtureGridItems.Add(new GridViewItem
                {
                    Id = item.AssetPath,
                    Label = item.DisplayName,
                    SortKey = item.DisplayName,
                    GroupKey = string.Empty,
                    PreviewSource = PreviewImageSource.FromVectorImage(_assetVectorCache.GetOrLoad(item.AssetPath)),
                    UserData = item.AssetPath
                });
            }
        }

        private void RebuildAssetGridItems()
        {
            _assetGridItems.Clear();

            foreach (var item in _filteredAssetItems)
            {
                _assetGridItems.Add(new GridViewItem
                {
                    Id = item.AssetPath,
                    Label = item.DisplayName,
                    SortKey = item.DisplayName,
                    GroupKey = item.GroupKey,
                    PreviewSource = PreviewImageSource.FromVectorImage(_assetVectorCache.GetOrLoad(item.AssetPath)),
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

            ClearFixtureSelection();
            TryLoadSelectedAsset(selectedItem);
        }

        private void OnFixtureGridSelectionChanged(List<GridViewItem> selectedItems)
        {
            if (_isProgrammaticSelection)
            {
                return;
            }

            ClearAssetSelection();
            TryLoadSelectedAsset(selectedItems?.FirstOrDefault());
        }

        private void OnFixtureGridItemSelected(GridViewItem selectedItem)
        {
            if (_isProgrammaticSelection)
            {
                return;
            }

            ClearAssetSelection();
            TryLoadSelectedAsset(selectedItem);
        }

        private void TryLoadSelectedAsset(GridViewItem selectedItem)
        {
            var selectedAssetPath = selectedItem?.UserData as string;
            if (string.IsNullOrWhiteSpace(selectedAssetPath))
            {
                return;
            }

            var currentAssetPath = _getCurrentAssetPath?.Invoke();
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

        private void ClearFixtureSelection()
        {
            _isProgrammaticSelection = true;
            _fixtureGridView?.ClearSelection();
            _isProgrammaticSelection = false;
        }

        private void ClearAssetSelection()
        {
            _isProgrammaticSelection = true;
            _assetGridView?.ClearSelection();
            _isProgrammaticSelection = false;
        }

        private void RebuildFilteredAssetPathLookup()
        {
            _filteredAssetPaths.Clear();
            foreach (var item in _filteredAssetItems)
            {
                _filteredAssetPaths.Add(item.AssetPath);
            }
        }
    }
}
