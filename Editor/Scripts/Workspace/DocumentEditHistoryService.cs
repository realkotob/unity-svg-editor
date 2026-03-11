using System.Collections.Generic;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentEditHistoryService
    {
        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private string _currentSource = string.Empty;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Reset(DocumentSession document)
        {
            Reset(document?.WorkingSourceText);
        }

        public void Reset(string sourceText)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _currentSource = sourceText ?? string.Empty;
        }

        public void RecordChange(string previousSource, string nextSource)
        {
            string normalizedPrevious = previousSource ?? string.Empty;
            string normalizedNext = nextSource ?? string.Empty;
            if (string.Equals(normalizedPrevious, normalizedNext, System.StringComparison.Ordinal))
            {
                _currentSource = normalizedNext;
                return;
            }

            _undoStack.Push(normalizedPrevious);
            _redoStack.Clear();
            _currentSource = normalizedNext;
        }

        public void SyncCurrent(string sourceText)
        {
            _currentSource = sourceText ?? string.Empty;
        }

        public bool TryUndo(string currentSource, out string restoredSource)
        {
            restoredSource = string.Empty;
            if (_undoStack.Count == 0)
            {
                return false;
            }

            _redoStack.Push(currentSource ?? _currentSource);
            restoredSource = _undoStack.Pop();
            _currentSource = restoredSource;
            return true;
        }

        public bool TryRedo(string currentSource, out string restoredSource)
        {
            restoredSource = string.Empty;
            if (_redoStack.Count == 0)
            {
                return false;
            }

            _undoStack.Push(currentSource ?? _currentSource);
            restoredSource = _redoStack.Pop();
            _currentSource = restoredSource;
            return true;
        }
    }
}
