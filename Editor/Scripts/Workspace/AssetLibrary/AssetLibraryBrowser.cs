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
        private readonly DocumentRepository _documentRepository;
        private readonly List<AssetLibraryEntry> _allAssetItems = new();
        private readonly List<AssetLibraryEntry> _filteredAssetItems = new();
        private readonly List<GridViewItem> _assetGridItems = new();
        private readonly AssetVectorImageCache _assetVectorCache = new();

        private VisualElement _assetLibraryFilterBarHost;
        private FilterBadgeBar _assetLibraryFilterBar;
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
            _assetGridView = root.Q<AssetLibraryGridView>("asset-grid-view");

            if (_assetGridView == null)
            {
                return;
            }

            if (_assetLibraryFilterBarHost != null)
            {
                _assetLibraryFilterBar = new FilterBadgeBar();
                _assetLibraryFilterBar.Bind(Array.Empty<FilterBadgeOption>(), string.Empty, null);
                _assetLibraryFilterBarHost.Add(_assetLibraryFilterBar);
            }

            _assetGridView.BindRuntime(OnAssetGridItemSelected, OnAssetGridSelectionChanged);
            _assetGridView.SetEmptyText("No SVG assets found");
        }

        public void Unbind()
        {
            _assetGridView?.UnbindRuntime();
            _assetGridView = null;
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
            IReadOnlyList<string> assetPaths = _documentRepository.FindVectorImageAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                _allAssetItems.Add(new AssetLibraryEntry
                {
                    DisplayName = VectorImageAssetPresentationUtility.BuildDisplayName(assetPath),
                    AssetPath = assetPath,
                    Library = VectorImageAssetPresentationUtility.BuildLibraryName(assetPath)
                });
            }

            RebuildAssetLibraryFilters();
            ApplyAssetFilter(selectFirst);
        }

        public void SetSelectionByAssetPath(string assetPath)
        {
            if (_assetGridView == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                _assetGridView.ClearSelection();
                return;
            }

            _isProgrammaticSelection = true;
            _assetGridView.SelectById(assetPath);
            _isProgrammaticSelection = false;
        }

        private void ApplyAssetFilter(bool selectFirst)
        {
            _filteredAssetItems.Clear();
            foreach (var item in _allAssetItems)
            {
                _filteredAssetItems.Add(item);
            }

            RebuildAssetGridItems();
            _assetGridView?.SetItems(_assetGridItems);

            var currentAssetPath = _getCurrentAssetPath?.Invoke();
            var hasCurrentSelection = !string.IsNullOrWhiteSpace(currentAssetPath) &&
                                      _filteredAssetItems.Any(item => string.Equals(item.AssetPath, currentAssetPath, StringComparison.Ordinal));
            if (hasCurrentSelection)
            {
                SetSelectionByAssetPath(currentAssetPath);
            }

            if (selectFirst && !hasCurrentSelection && _filteredAssetItems.Count > 0)
            {
                var firstAssetPath = _filteredAssetItems[0].AssetPath;
                _loadAsset?.Invoke(firstAssetPath);
                SetSelectionByAssetPath(firstAssetPath);
            }
        }

        private void RebuildAssetGridItems()
        {
            _assetGridItems.Clear();

            foreach (var item in _filteredAssetItems.OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                _assetGridItems.Add(new GridViewItem
                {
                    Id = item.AssetPath,
                    Label = item.DisplayName,
                    SortKey = item.DisplayName,
                    GroupKey = VectorImageAssetPresentationUtility.ResolveGroupKey(item.DisplayName),
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
    }
}
