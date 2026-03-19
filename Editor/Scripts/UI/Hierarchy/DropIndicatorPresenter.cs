using UnityEngine.UIElements;
using Core.UI.Extensions;

namespace SvgEditor.UI.Hierarchy
{
    internal sealed class DropIndicatorPresenter
    {
        private const float IndicatorRight = 8f;
        private VisualElement _indicator;

        public void Bind(TreeView treeView)
        {
            Unbind(treeView);
            if (treeView == null)
            {
                return;
            }

            _indicator = new VisualElement();
            _indicator.AddToClassList("svg-editor__hierarchy-insert-indicator");
            _indicator.style.display = DisplayStyle.None;
            _indicator.pickingMode = PickingMode.Ignore;
            treeView.hierarchy.Add(_indicator);
        }

        public void Unbind(TreeView treeView)
        {
            if (_indicator != null && treeView != null && _indicator.parent == treeView)
            {
                treeView.hierarchy.Remove(_indicator);
            }

            _indicator = null;
        }

        public void Show(float left, float top)
        {
            if (_indicator == null)
            {
                return;
            }

            _indicator.style.display = DisplayStyle.Flex;
            _indicator.style.left = left;
            _indicator.style.right = IndicatorRight;
            _indicator.style.top = top;
        }

        public void Hide()
        {
            if (_indicator != null)
            {
                _indicator.style.display = DisplayStyle.None;
            }
        }
    }
}
