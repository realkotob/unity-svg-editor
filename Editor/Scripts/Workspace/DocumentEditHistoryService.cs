using System.Collections.Generic;
using UnityEditor;

namespace SvgEditor
{
    internal enum HistoryRecordingMode
    {
        Immediate,
        Coalesced
    }

    internal sealed class DocumentEditHistoryService
    {
        private const int MaxHistoryEntries = 100;
        private const double CoalescedHistoryWindowSeconds = 0.75d;

        private readonly LinkedList<string> _undoStack = new();
        private readonly LinkedList<string> _redoStack = new();
        private string _currentSource = string.Empty;
        private double _lastRecordedAt;
        private HistoryRecordingMode _lastRecordingMode = HistoryRecordingMode.Immediate;

        public bool CanUndo => _undoStack.Count != 0;
        public bool CanRedo => _redoStack.Count != 0;

        public void Reset(DocumentSession document)
        {
            Reset(document?.WorkingSourceText);
        }

        public void Reset(string sourceText)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _currentSource = sourceText ?? string.Empty;
            ResetCoalescingState();
        }

        public void RecordChange(string previousSource, string nextSource, HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate)
        {
            string normalizedPrevious = previousSource ?? string.Empty;
            string normalizedNext = nextSource ?? string.Empty;
            if (string.Equals(normalizedPrevious, normalizedNext, System.StringComparison.Ordinal))
            {
                _currentSource = normalizedNext;
                return;
            }

            double recordedAt = EditorApplication.timeSinceStartup;
            bool shouldCoalesce = ShouldCoalesce(recordingMode, recordedAt);
            if (!shouldCoalesce)
            {
                PushHistory(_undoStack, normalizedPrevious);
                _lastRecordedAt = recordedAt;
            }

            _redoStack.Clear();
            _currentSource = normalizedNext;
            _lastRecordingMode = recordingMode;
        }

        public void SyncCurrent(string sourceText)
        {
            _currentSource = sourceText ?? string.Empty;
            ResetCoalescingState();
        }

        public bool TryUndo(string currentSource, out string restoredSource)
        {
            restoredSource = string.Empty;
            if (_undoStack.Count == 0)
            {
                return false;
            }

            PushHistory(_redoStack, currentSource ?? _currentSource);
            restoredSource = PopHistory(_undoStack);
            _currentSource = restoredSource;
            ResetCoalescingState();
            return true;
        }

        public bool TryRedo(string currentSource, out string restoredSource)
        {
            restoredSource = string.Empty;
            if (_redoStack.Count == 0)
            {
                return false;
            }

            PushHistory(_undoStack, currentSource ?? _currentSource);
            restoredSource = PopHistory(_redoStack);
            _currentSource = restoredSource;
            ResetCoalescingState();
            return true;
        }

        private bool ShouldCoalesce(HistoryRecordingMode recordingMode, double recordedAt)
        {
            return recordingMode == HistoryRecordingMode.Coalesced &&
                   _undoStack.Count != 0 &&
                   _lastRecordingMode == HistoryRecordingMode.Coalesced &&
                   recordedAt - _lastRecordedAt <= CoalescedHistoryWindowSeconds;
        }

        private void ResetCoalescingState()
        {
            _lastRecordedAt = 0d;
            _lastRecordingMode = HistoryRecordingMode.Immediate;
        }

        private static void PushHistory(LinkedList<string> history, string source)
        {
            history.AddLast(source);
            if (history.Count > MaxHistoryEntries)
            {
                history.RemoveFirst();
            }
        }

        private static string PopHistory(LinkedList<string> history)
        {
            var node = history.Last;
            if (node == null)
            {
                return string.Empty;
            }

            string source = node.Value;
            history.RemoveLast();
            return source;
        }
    }
}
