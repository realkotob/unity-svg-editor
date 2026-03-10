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
        private readonly DocumentRepository _documentRepository;
        private readonly List<AssetLibraryEntry> _allAssetItems = new();
        private readonly List<AssetLibraryEntry> _fixtureAssetItems = new();
        private readonly List<AssetLibraryEntry> _filteredAssetItems = new();
        private readonly List<GridViewItem> _fixtureGridItems = new();
        private readonly List<GridViewItem> _assetGridItems = new();
        private readonly AssetVectorImageCache _assetVectorCache = new();

        private VisualElement _assetLibraryFilterBarHost;
        private VisualElement _fixtureLibrarySection;
        private FilterBadgeBar _assetLibraryFilterBar;
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
            _assetLibraryFilterBarHost = root.Q<VisualElement>("asset-library-filter-bar");
            _fixtureLibrarySection = root.Q<VisualElement>("fixture-library-section");
            _fixtureGridView = root.Q<AssetLibraryGridView>("fixture-grid-view");
            _assetGridView = root.Q<AssetLibraryGridView>("asset-grid-view");

            if (_assetGridView == null || _fixtureGridView == null)
            {
                return;
            }

            if (_assetLibraryFilterBarHost != null)
            {
                _assetLibraryFilterBar = new FilterBadgeBar();
                _assetLibraryFilterBar.Bind(Array.Empty<FilterBadgeOption>(), string.Empty, null);
                _assetLibraryFilterBarHost.Add(_assetLibraryFilterBar);
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
            _fixtureGridView = null;
            _assetGridView = null;
            _fixtureLibrarySection = null;
            _assetLibraryFilterBar?.RemoveFromHierarchy();
            _assetLibraryFilterBar = null;
            _assetLibraryFilterBarHost = null;
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
                    continue;
                }

                _allAssetItems.Add(entry);
            }

            RebuildAssetLibraryFilters();
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
            bool isFixture = _fixtureAssetItems.Any(item => string.Equals(item.AssetPath, assetPath, StringComparison.Ordinal));
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
            foreach (var item in _allAssetItems)
            {
                _filteredAssetItems.Add(item);
            }

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
                                       (_filteredAssetItems.Any(item => string.Equals(item.AssetPath, currentAssetPath, StringComparison.Ordinal)) ||
                                        _fixtureAssetItems.Any(item => string.Equals(item.AssetPath, currentAssetPath, StringComparison.Ordinal)));
            if (hasCurrentSelection)
            {
                SetSelectionByAssetPath(currentAssetPath);
            }

            if (selectFirst && !hasCurrentSelection)
            {
                string firstAssetPath = _filteredAssetItems.Count > 0
                    ? _filteredAssetItems[0].AssetPath
                    : _fixtureAssetItems.FirstOrDefault()?.AssetPath;
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

            foreach (var item in _fixtureAssetItems.OrderBy(candidate => candidate.DisplayName, AssetNameComparer))
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

            foreach (var item in _filteredAssetItems
                         .OrderBy(candidate => candidate.GroupKey, AssetNameComparer)
                         .ThenBy(candidate => candidate.DisplayName, AssetNameComparer))
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

        private void RebuildAssetLibraryFilters()
        {
            if (_assetLibraryFilterBar == null)
            {
                return;
            }

            Button refreshButton = new RefreshActionButton(
                () => RefreshAssetList(selectFirst: false),
                "Rescan local assets");
            _assetLibraryFilterBar.SetActions(refreshButton);
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
    }
}
