using System;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class StructureHierarchyViewController
    {
        private readonly StructureHierarchyInteractionController _interactionController;

        private TreeView _treeView;
        private IStructureHierarchyHost _host;

        public StructureHierarchyViewController(StructureHierarchyInteractionController interactionController)
        {
            _interactionController = interactionController;
        }

        public void Bind(TreeView treeView, IStructureHierarchyHost host)
        {
            _treeView = treeView;
            _host = host;
        }

        public void Unbind()
        {
            _host = null;
            _treeView = null;
        }

        public VisualElement CreateHierarchyItemElement()
        {
            var row = new VisualElement();
            row.AddToClassList("svg-editor__hierarchy-item");
            row.RegisterCallback<PointerDownEvent>(_interactionController.OnHierarchyRowPointerDown, TrickleDown.TrickleDown);
            row.RegisterCallback<ClickEvent>(OnHierarchyRowClicked);

            var expander = new VisualElement
            {
                name = "hierarchy-expander"
            };
            expander.AddToClassList("svg-editor__hierarchy-expander");
            expander.RegisterCallback<PointerDownEvent>(OnHierarchyExpanderPointerDown, TrickleDown.TrickleDown);
            expander.RegisterCallback<ClickEvent>(OnHierarchyExpanderClicked);
            row.Add(expander);

            var icon = new VisualElement
            {
                name = "hierarchy-icon"
            };
            icon.AddToClassList("svg-editor__hierarchy-icon");
            row.Add(icon);

            var text = new Label
            {
                name = "hierarchy-text"
            };
            text.AddToClassList("svg-editor__hierarchy-text");
            row.Add(text);
            return row;
        }

        public void BindHierarchyItem(VisualElement element, int index)
        {
            if (_treeView == null || _host == null)
                return;

            var expander = element.Q<VisualElement>("hierarchy-expander");
            var iconElement = element.Q<VisualElement>("hierarchy-icon");
            var textLabel = element.Q<Label>("hierarchy-text");
            if (iconElement == null || textLabel == null || expander == null)
                return;

            if (!TryGetHierarchyItem(index, out StructureNode item))
            {
                StructureHierarchyTreeUtility.ApplyHierarchyIconVariant(iconElement, "file");
                textLabel.text = string.Empty;
                expander.style.display = DisplayStyle.Flex;
                expander.userData = null;
                expander.EnableInClassList("svg-editor__hierarchy-expander--placeholder", true);
                element.tooltip = string.Empty;
                return;
            }

            var tagName = (item.TagName ?? string.Empty).ToLowerInvariant();
            StructureHierarchyTreeUtility.ApplyHierarchyIconVariant(
                iconElement,
                StructureHierarchyTreeUtility.ResolveHierarchyIconKind(tagName));
            textLabel.text = StructureHierarchyTreeUtility.BuildHierarchyLabel(item);
            element.style.paddingLeft = 6f + (item.Depth * 20f);
            element.userData = item.Key;
            element.tooltip = item.CanUseAsTarget
                ? $"#{item.TargetKey} <{item.TagName}>"
                : $"<{item.TagName}>";

            var hasChildren = StructureHierarchyTreeUtility.TryFindHierarchyItem(item.Key, _host.HierarchyItems, out TreeViewItemData<StructureNode> treeItem) &&
                              treeItem.hasChildren;
            expander.style.display = DisplayStyle.Flex;
            expander.userData = hasChildren ? item.Key : null;
            expander.EnableInClassList("svg-editor__hierarchy-expander--placeholder", !hasChildren);
            expander.EnableInClassList(
                "svg-editor__hierarchy-expander--expanded",
                hasChildren &&
                StructureHierarchyTreeUtility.TryFindHierarchyItemId(item.Key, _host.HierarchyItems, out int treeItemId) &&
                _treeView.IsExpanded(treeItemId));

            var isGroup = string.Equals(tagName, "g", StringComparison.Ordinal);
            var isText = string.Equals(tagName, "text", StringComparison.Ordinal);
            element.EnableInClassList("svg-editor__hierarchy-item--group", isGroup);
            element.EnableInClassList("svg-editor__hierarchy-item--text", isText);
        }

        public void UnbindHierarchyItem(VisualElement element, int index)
        {
            if (element == null)
                return;

            element.tooltip = string.Empty;
            element.userData = null;
            element.style.paddingLeft = StyleKeyword.Null;
            element.EnableInClassList("svg-editor__hierarchy-item--group", false);
            element.EnableInClassList("svg-editor__hierarchy-item--text", false);

            if (element.Q<VisualElement>("hierarchy-expander") is { } expander)
            {
                expander.style.display = DisplayStyle.Flex;
                expander.userData = null;
                expander.EnableInClassList("svg-editor__hierarchy-expander--placeholder", true);
                expander.EnableInClassList("svg-editor__hierarchy-expander--expanded", false);
            }

            if (element.Q<VisualElement>("hierarchy-icon") is { } iconElement)
                StructureHierarchyTreeUtility.ApplyHierarchyIconVariant(iconElement, "file");

            if (element.Q<Label>("hierarchy-text") is { } textLabel)
                textLabel.text = string.Empty;
        }

        public void ToggleHierarchyItemExpansion(string elementKey)
        {
            if (_treeView == null ||
                _host == null ||
                !StructureHierarchyTreeUtility.TryFindHierarchyItemId(elementKey, _host.HierarchyItems, out int treeItemId))
            {
                return;
            }

            if (_treeView.IsExpanded(treeItemId))
                _treeView.CollapseItem(treeItemId, false);
            else
                _treeView.ExpandItem(treeItemId, false);

            _treeView.Rebuild();
        }

        private bool TryGetHierarchyItem(int index, out StructureNode item)
        {
            try
            {
                item = _treeView.GetItemDataForIndex<StructureNode>(index);
                return true;
            }
            catch
            {
                item = null;
                return false;
            }
        }

        private void OnHierarchyExpanderPointerDown(PointerDownEvent evt)
        {
            _interactionController.CancelPendingPress();
            evt.StopPropagation();
        }

        private void OnHierarchyExpanderClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not VisualElement expander || expander.userData is not string elementKey)
                return;

            ToggleHierarchyItemExpansion(elementKey);
            evt.StopPropagation();
        }

        private void OnHierarchyRowClicked(ClickEvent evt)
        {
            if (_interactionController.TryConsumeSuppressedRowClick())
            {
                evt.StopPropagation();
                return;
            }

            if (evt.currentTarget is not VisualElement row || row.userData is not string elementKey)
                return;
            if (row.Q<VisualElement>("hierarchy-expander")?.userData is not string)
                return;

            ToggleHierarchyItemExpansion(elementKey);
        }
    }
}
