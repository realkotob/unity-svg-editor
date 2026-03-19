using System;
using UnityEngine.UIElements;

namespace Core.UI.Extensions
{
    internal static class VisualElementExtensions
    {
        public static T Callback<T>(this T target, EventCallback<GeometryChangedEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<ClickEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<AttachToPanelEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<DetachFromPanelEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<TransitionEndEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<MouseEnterEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<MouseLeaveEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<PointerDownEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<PointerUpEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<PointerCancelEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<PointerCaptureOutEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);
        public static T Callback<T>(this T target, EventCallback<KeyDownEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Register(target, callback, trickleDown);

        public static T RemoveCallback<T>(this T target, EventCallback<PointerDownEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Unregister(target, callback, trickleDown);
        public static T RemoveCallback<T>(this T target, EventCallback<PointerUpEvent> callback, TrickleDown trickleDown = TrickleDown.TrickleDown) where T : VisualElement => Unregister(target, callback, trickleDown);

        public static T AddClass<T>(this T element, string className) where T : VisualElement
        {
            if (element == null || string.IsNullOrWhiteSpace(className))
            {
                return element;
            }

            element.AddToClassList(className);
            return element;
        }

        public static T RemoveClass<T>(this T element, string className) where T : VisualElement
        {
            if (element == null || string.IsNullOrWhiteSpace(className))
            {
                return element;
            }

            element.RemoveFromClassList(className);
            return element;
        }

        public static T EnableClass<T>(this T element, string className, bool enabled) where T : VisualElement
        {
            if (element == null || string.IsNullOrWhiteSpace(className))
            {
                return element;
            }

            element.EnableInClassList(className, enabled);
            return element;
        }

        public static T SetName<T>(this T element, string name) where T : VisualElement
        {
            if (element == null)
            {
                return null;
            }

            element.name = name ?? string.Empty;
            return element;
        }

        public static T SetDisplay<T>(this T element, bool visible) where T : VisualElement
        {
            if (element == null)
            {
                return null;
            }

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return element;
        }

        public static T SetFlexDirection<T>(this T element, FlexDirection flexDirection) where T : VisualElement
        {
            if (element == null)
            {
                return null;
            }

            element.style.flexDirection = flexDirection;
            return element;
        }

        public static T SetAlignItems<T>(this T element, Align align) where T : VisualElement
        {
            if (element == null)
            {
                return null;
            }

            element.style.alignItems = align;
            return element;
        }

        public static T SetFlexWrap<T>(this T element, Wrap wrap) where T : VisualElement
        {
            if (element == null)
            {
                return null;
            }

            element.style.flexWrap = wrap;
            return element;
        }

        public static T SetHeight<T>(this T element, float height) where T : VisualElement
        {
            if (element == null)
            {
                return null;
            }

            element.style.height = height;
            return element;
        }

        public static T SetHeight<T>(this T element, StyleKeyword keyword) where T : VisualElement
        {
            if (element == null)
            {
                return null;
            }

            element.style.height = keyword;
            return element;
        }

        public static T SetText<T>(this T element, string text) where T : TextElement
        {
            if (element == null)
            {
                return null;
            }

            element.text = text ?? string.Empty;
            return element;
        }

        private static T Register<T, TEventType>(T target, EventCallback<TEventType> callback, TrickleDown trickleDown)
            where T : VisualElement
            where TEventType : EventBase<TEventType>, new()
        {
            if (target == null || callback == null)
            {
                return target;
            }

            target.RegisterCallback(callback, trickleDown);
            return target;
        }

        private static T Unregister<T, TEventType>(T target, EventCallback<TEventType> callback, TrickleDown trickleDown)
            where T : VisualElement
            where TEventType : EventBase<TEventType>, new()
        {
            if (target == null || callback == null)
            {
                return target;
            }

            target.UnregisterCallback(callback, trickleDown);
            return target;
        }
    }
}
