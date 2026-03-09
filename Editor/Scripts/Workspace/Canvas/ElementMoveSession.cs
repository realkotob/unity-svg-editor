using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class ElementMoveSession
    {
        private string _activeElementKey = string.Empty;
        private Vector2 _startViewportPoint;
        private Rect _startSelectionViewportRect;
        private Rect _currentSelectionViewportRect;
        private Rect _startElementSceneRect;
        private string _previewSourceText = string.Empty;

        private string _committedElementKey = string.Empty;
        private Rect _committedSelectionViewportRect;
        private bool _hasCommittedSelectionViewportRect;

        public bool IsActive => !string.IsNullOrWhiteSpace(_activeElementKey);
        public string ActiveElementKey => _activeElementKey;
        public Rect CurrentSelectionViewportRect => _currentSelectionViewportRect;
        public Rect StartElementSceneRect => _startElementSceneRect;
        public string PreviewSourceText => _previewSourceText;

        public void Begin(string elementKey, Vector2 startViewportPoint, Rect startSelectionViewportRect, Rect startElementSceneRect)
        {
            _activeElementKey = elementKey ?? string.Empty;
            _startViewportPoint = startViewportPoint;
            _startSelectionViewportRect = startSelectionViewportRect;
            _currentSelectionViewportRect = startSelectionViewportRect;
            _startElementSceneRect = startElementSceneRect;
            _previewSourceText = string.Empty;
        }

        public Vector2 Update(Vector2 currentViewportPoint)
        {
            var viewportDelta = currentViewportPoint - _startViewportPoint;
            _currentSelectionViewportRect = new Rect(
                _startSelectionViewportRect.position + viewportDelta,
                _startSelectionViewportRect.size);
            return viewportDelta;
        }

        public void SetPreviewSource(string previewSourceText)
        {
            _previewSourceText = previewSourceText ?? string.Empty;
        }

        public void CommitSelectionRect()
        {
            if (string.IsNullOrWhiteSpace(_activeElementKey))
            {
                return;
            }

            _committedElementKey = _activeElementKey;
            _committedSelectionViewportRect = _currentSelectionViewportRect;
            _hasCommittedSelectionViewportRect = true;
        }

        public bool TryGetCommittedSelectionViewportRect(string elementKey, out Rect viewportRect)
        {
            viewportRect = default;
            if (!_hasCommittedSelectionViewportRect ||
                string.IsNullOrWhiteSpace(elementKey) ||
                !string.Equals(_committedElementKey, elementKey, System.StringComparison.Ordinal))
            {
                return false;
            }

            viewportRect = _committedSelectionViewportRect;
            return true;
        }

        public void ClearCommittedSelection()
        {
            _committedElementKey = string.Empty;
            _committedSelectionViewportRect = default;
            _hasCommittedSelectionViewportRect = false;
        }

        public void End()
        {
            _activeElementKey = string.Empty;
            _previewSourceText = string.Empty;
        }
    }
}
