using System.Collections.Generic;
using Core.UI.Extensions;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal sealed class InspectorSectionClasses
    {
        public string rootClass = string.Empty;
        public string accentClass = string.Empty;
        public string headerClass = string.Empty;
        public string titleClass = string.Empty;
        public string actionsClass = string.Empty;
        public string bodyClass = string.Empty;
    }

    internal sealed class InspectorSection : VisualElement
    {
        public static class ClassName
        {
            public const string BASE = "tooling-inspector-section";
            public const string ACCENT = BASE + "--accent";
            public const string HEADER = BASE + "__header";
            public const string TITLE = BASE + "__title";
            public const string ACTIONS = BASE + "__actions";
            public const string BODY = BASE + "__body";
        }

        private readonly InspectorSectionClasses _classes;

        public InspectorSection(string title, InspectorSectionClasses classes = null)
        {
            _classes = classes ?? new InspectorSectionClasses();

            this.AddClass(ClassName.BASE)
                .SetFlexDirection(FlexDirection.Column);
            AddOptionalClass(this, _classes.rootClass);

            Header = new VisualElement()
                .AddClass(ClassName.HEADER)
                .SetFlexDirection(FlexDirection.Row)
                .SetAlignItems(Align.Center);
            AddOptionalClass(Header, _classes.headerClass);
            Add(Header);

            TitleLabel = new Label(title ?? string.Empty).AddClass(ClassName.TITLE);
            AddOptionalClass(TitleLabel, _classes.titleClass);
            Header.Add(TitleLabel);

            Actions = new VisualElement()
                .AddClass(ClassName.ACTIONS)
                .SetFlexDirection(FlexDirection.Row)
                .SetAlignItems(Align.Center);
            Actions.style.marginLeft = StyleKeyword.Auto;
            Actions.SetDisplay(false);
            AddOptionalClass(Actions, _classes.actionsClass);
            Header.Add(Actions);

            Body = new VisualElement()
                .AddClass(ClassName.BODY)
                .SetFlexDirection(FlexDirection.Column);
            AddOptionalClass(Body, _classes.bodyClass);
            Add(Body);
        }

        public VisualElement Header { get; }
        public Label TitleLabel { get; }
        public VisualElement Actions { get; }
        public VisualElement Body { get; }

        public void SetAccent(bool accent)
        {
            this.EnableClass(ClassName.ACCENT, accent);
            if (!string.IsNullOrWhiteSpace(_classes.accentClass))
            {
                this.EnableClass(_classes.accentClass, accent);
            }
        }

        public void SetActions(IEnumerable<VisualElement> actions)
        {
            Actions.Clear();

            bool hasAction = false;
            if (actions != null)
            {
                foreach (VisualElement action in actions)
                {
                    if (action == null)
                    {
                        continue;
                    }

                    Actions.Add(action);
                    hasAction = true;
                }
            }

            Actions.SetDisplay(hasAction);
        }

        private static void AddOptionalClass(VisualElement element, string className)
        {
            if (!string.IsNullOrWhiteSpace(className))
            {
                element.AddClass(className);
            }
        }
    }
}
