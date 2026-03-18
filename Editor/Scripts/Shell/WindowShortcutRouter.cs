using System;
using UnityEditor;
using UnityEngine;
using SvgEditor.Document;
using Core.UI.Extensions;

namespace SvgEditor.Shell
{
    internal sealed class WindowShortcutRouter
    {
        private const double ShortcutDedupSeconds = 0.05d;

        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Func<bool> _tryCancelActiveDrag;
        private readonly Func<bool> _tryUndo;
        private readonly Func<bool> _tryRedo;
        private readonly Action _saveCurrentDocument;

        private double _lastShortcutHandledAt;
        private KeyCode _lastShortcutKey = KeyCode.None;
        private EventModifiers _lastShortcutModifiers;

        public WindowShortcutRouter(
            Func<DocumentSession> currentDocumentAccessor,
            Func<bool> tryCancelActiveDrag,
            Func<bool> tryUndo,
            Func<bool> tryRedo,
            Action saveCurrentDocument)
        {
            _currentDocumentAccessor = currentDocumentAccessor;
            _tryCancelActiveDrag = tryCancelActiveDrag;
            _tryUndo = tryUndo;
            _tryRedo = tryRedo;
            _saveCurrentDocument = saveCurrentDocument;
        }

        public bool TryHandle(KeyCode keyCode, EventModifiers modifiers)
        {
            if (keyCode == KeyCode.Escape && (_tryCancelActiveDrag?.Invoke() ?? false))
            {
                return true;
            }

            if (_currentDocumentAccessor?.Invoke() == null)
            {
                return false;
            }

            EventModifiers normalizedModifiers = NormalizeShortcutModifiers(modifiers);
            if (!HasDocumentShortcutModifiers(normalizedModifiers))
            {
                return false;
            }

            if (IsDuplicateShortcut(keyCode, normalizedModifiers))
            {
                return true;
            }

            bool handled = TryHandleDocumentShortcut(keyCode, normalizedModifiers);

            if (handled)
            {
                RememberHandledShortcut(keyCode, normalizedModifiers);
            }

            return handled;
        }

        private bool TryHandleDocumentShortcut(KeyCode keyCode, EventModifiers normalizedModifiers)
        {
            if (keyCode == KeyCode.Z)
            {
                return (normalizedModifiers & EventModifiers.Shift) != 0
                    ? (_tryRedo?.Invoke() ?? false)
                    : (_tryUndo?.Invoke() ?? false);
            }

            if (keyCode != KeyCode.S)
                return false;

            _saveCurrentDocument?.Invoke();
            return true;
        }

        private bool IsDuplicateShortcut(KeyCode keyCode, EventModifiers modifiers)
        {
            return keyCode == _lastShortcutKey &&
                   modifiers == _lastShortcutModifiers &&
                   EditorApplication.timeSinceStartup - _lastShortcutHandledAt <= ShortcutDedupSeconds;
        }

        private void RememberHandledShortcut(KeyCode keyCode, EventModifiers modifiers)
        {
            _lastShortcutHandledAt = EditorApplication.timeSinceStartup;
            _lastShortcutKey = keyCode;
            _lastShortcutModifiers = modifiers;
        }

        private static EventModifiers NormalizeShortcutModifiers(EventModifiers modifiers)
        {
            return modifiers & (EventModifiers.Command | EventModifiers.Control | EventModifiers.Shift);
        }

        private static bool HasDocumentShortcutModifiers(EventModifiers normalizedModifiers)
        {
            return (normalizedModifiers & (EventModifiers.Command | EventModifiers.Control)) != 0;
        }
    }
}
