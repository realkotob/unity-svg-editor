using System.Collections.Generic;
using UnityEngine.UIElements;
using Core.UI.Extensions;

namespace SvgEditor.Shared
{
    internal abstract class PreviewCollectionRenderer<TView, TItem>
        where TView : VisualElement
    {
        private List<TItem> _previewItems;

        public void ApplyPreview(
            VisualElement owner,
            TView view,
            List<TItem> items,
            string previewEnabledClassName)
        {
            if (owner == null || view == null || items == null)
            {
                return;
            }

            items.Clear();
            items.AddRange(GetPreviewItems());
            ApplyPreviewItems(view, items);
            owner.EnableClass(previewEnabledClassName, true);
            AfterApplyPreview(view, items);
        }

        public void ClearPreview(
            VisualElement owner,
            TView view,
            List<TItem> items,
            string previewEnabledClassName)
        {
            if (owner == null || view == null || items == null)
            {
                return;
            }

            items.Clear();
            ClearPreviewItems(view, items);
            owner.EnableClass(previewEnabledClassName, false);
            AfterClearPreview(view, items);
            _previewItems = null;
        }

        protected virtual void AfterApplyPreview(TView view, List<TItem> items)
        {
        }

        protected virtual void AfterClearPreview(TView view, List<TItem> items)
        {
        }

        protected abstract List<TItem> CreatePreviewItems();
        protected abstract void ApplyPreviewItems(TView view, List<TItem> items);
        protected abstract void ClearPreviewItems(TView view, List<TItem> items);

        private IReadOnlyList<TItem> GetPreviewItems()
        {
            _previewItems ??= CreatePreviewItems();
            return _previewItems;
        }
    }
}
