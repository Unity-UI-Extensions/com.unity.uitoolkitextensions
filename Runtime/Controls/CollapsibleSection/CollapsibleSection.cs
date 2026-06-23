/// Credit SimonDarksideJ

using System;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A collapsible accordion section. The header toggles an expanded/collapsed body that
    /// animates via USS max-height transitions driven by the <see cref="ExpandedClass"/> modifier.
    /// </summary>
    [UxmlElement]
    public partial class CollapsibleSection : VisualElement
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

        [UxmlAttribute("expanded")]
        public bool IsExpanded
        {
            get => isExpanded;
            set => SetExpanded(value);
        }

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => titleLabel.text;
            set => titleLabel.text = value;
        }

        [UxmlAttribute("body-text")]
        public string BodyText
        {
            get
            {
                var existing = bodyContent.Q<Label>(className: BodyTextClass);
                return existing != null ? existing.text : string.Empty;
            }
            set => SetBodyText(value);
        }

        public CollapsibleSection()
        {
            AddToClassList(RootClass);

            var header = UIToolkitExtensions.CreateVisualElement(HeaderClass);
            header.pickingMode = PickingMode.Position;
            hierarchy.Add(header);

            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(header, TitleClass);
            titleLabel.pickingMode = PickingMode.Ignore;

            var chevron = UIToolkitExtensions.CreateVisualElement(header, ChevronClass);
            chevron.pickingMode = PickingMode.Ignore;

            var body = UIToolkitExtensions.CreateVisualElement(BodyClass);
            hierarchy.Add(body);

            bodyContent = UIToolkitExtensions.CreateVisualElement(body, BodyContentClass);

            header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
        }

        public override VisualElement contentContainer => bodyContent;

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