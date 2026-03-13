using System;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal static class CallbackBindingUtility
    {
        public static void ToggleButtonClicked(Button button, Action callback, bool register)
        {
            if (button == null || callback == null)
            {
                return;
            }

            button.clicked -= callback;
            if (register)
            {
                button.clicked += callback;
            }
        }

        public static void ToggleValueChangedCallback<TValue>(
            BaseField<TValue> field,
            EventCallback<ChangeEvent<TValue>> callback,
            bool register)
        {
            if (field == null || callback == null)
            {
                return;
            }

            field.UnregisterValueChangedCallback(callback);
            if (register)
            {
                field.RegisterValueChangedCallback(callback);
            }
        }

        public static void ToggleCallback<TEventType>(
            CallbackEventHandler handler,
            EventCallback<TEventType> callback,
            bool register,
            TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new()
        {
            if (handler == null || callback == null)
            {
                return;
            }

            handler.UnregisterCallback(callback, trickleDown);
            if (register)
            {
                handler.RegisterCallback(callback, trickleDown);
            }
        }
    }
}
