using System;
using UnityEditor;
using UnityEngine;
using SvgEditor.Core.Svg.Source;
using Core.UI.Extensions;

namespace SvgEditor.UI.Shell
{
    internal sealed class WindowShortcutRouter
    {
        private const double ShortcutDedupSeconds = 0.05d;

        private struct ShortcutStamp
        {
            public double handledAt;
            public KeyCode keyCode;
            public EventModifiers modifiers;

            public bool Matches(KeyCode candidateKeyCode, EventModifiers candidateModifiers, double currentTime)
            {
                return keyCode == candidateKeyCode &&
                       modifiers == candidateModifiers &&
                       currentTime - handledAt <= ShortcutDedupSeconds;
            }

            public void Remember(KeyCode handledKeyCode, EventModifiers handledModifiers, double handledTime)
            {
                handledAt = handledTime;
                keyCode = handledKeyCode;
                modifiers = handledModifiers;
            }
        }

        private readonly Func<DocumentSession> _currentDocumentAccessor;
        private readonly Func<bool> _tryCancelActiveDrag;
        private readonly Func<bool> _tryUndo;
        private readonly Func<bool> _tryRedo;
        private readonly Action _saveCurrentDocument;

        private ShortcutStamp _lastHandledShortcut;

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

        public bool TryHandleShortcut(KeyCode keyCode, EventModifiers modifiers)
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
            if (!IsDocumentShortcut(normalizedModifiers))
            {
                return false;
            }

            if (WasHandledRecently(keyCode, normalizedModifiers))
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

        private bool WasHandledRecently(KeyCode keyCode, EventModifiers modifiers)
        {
            return _lastHandledShortcut.Matches(keyCode, modifiers, EditorApplication.timeSinceStartup);
        }

        private void RememberHandledShortcut(KeyCode keyCode, EventModifiers modifiers)
        {
            _lastHandledShortcut.Remember(keyCode, modifiers, EditorApplication.timeSinceStartup);
        }

        private static EventModifiers NormalizeShortcutModifiers(EventModifiers modifiers)
        {
            return modifiers & (EventModifiers.Command | EventModifiers.Control | EventModifiers.Shift);
        }

        private static bool IsDocumentShortcut(EventModifiers normalizedModifiers)
        {
            return (normalizedModifiers & (EventModifiers.Command | EventModifiers.Control)) != 0;
        }
    }
}
