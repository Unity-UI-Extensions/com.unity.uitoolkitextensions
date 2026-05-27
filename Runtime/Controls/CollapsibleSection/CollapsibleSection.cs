/// Credit SimonDarksideJ

using System;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A collapsible accordion section. The header toggles an expanded/collapsed body that
    /// animates via USS max-height transitions driven by the <see cref="ExpandedClass"/> modifier.
    /// </summary>
    public class CollapsibleSection : VisualElement
    {
        public const string RootClass = "collapsibleSection";
        public const string HeaderClass = "collapsibleSection__header";
        public const string TitleClass = "collapsibleSection__title";
        public const string ChevronClass = "collapsibleSection__chevron";
        public const string BodyClass = "collapsibleSection__body";
        public const string BodyContentClass = "collapsibleSection__bodyContent";
        public const string BodyTextClass = "collapsibleSection__bodyText";
        public const string ExpandedClass = "collapsibleSection--expanded";

        private readonly Label titleLabel;
        private readonly VisualElement bodyContent;
        private bool isExpanded;

        public event Action<bool> OnExpandedChanged;

        public bool IsExpanded
        {
            get => isExpanded;
            set => SetExpanded(value);
        }

        public string TitleText
        {
            get => titleLabel.text;
            set => titleLabel.text = value;
        }

        public CollapsibleSection()
        {
            AddToClassList(RootClass);

            var header = UIToolkitExtensions.CreateVisualElement(this, HeaderClass);
            header.pickingMode = PickingMode.Position;

            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(header, TitleClass);
            titleLabel.pickingMode = PickingMode.Ignore;

            var chevron = UIToolkitExtensions.CreateVisualElement(header, ChevronClass);
            chevron.pickingMode = PickingMode.Ignore;

            var body = UIToolkitExtensions.CreateVisualElement(this, BodyClass);
            bodyContent = UIToolkitExtensions.CreateVisualElement(body, BodyContentClass);

            header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
        }

        public void AddBodyContent(VisualElement element)
        {
            bodyContent.Add(element);
        }

        public Label SetBodyText(string text)
        {
            var existing = bodyContent.Q<Label>(className: BodyTextClass);
            existing?.RemoveFromHierarchy();

            var label = UIToolkitExtensions.CreateVisualElement<Label>(bodyContent, BodyTextClass);
            label.text = text;
            return label;
        }

        public void Toggle() => SetExpanded(!isExpanded);

        private void OnHeaderPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
            Toggle();
        }

        private void SetExpanded(bool expanded)
        {
            if (isExpanded == expanded)
            {
                return;
            }

            isExpanded = expanded;
            EnableInClassList(ExpandedClass, isExpanded);
            OnExpandedChanged?.Invoke(isExpanded);
        }
    }
}
