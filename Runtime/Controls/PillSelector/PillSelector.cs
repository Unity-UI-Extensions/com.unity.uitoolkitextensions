/// Credit SimonDarksideJ

using System;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A dropdown-style selector styled as a pill: optional label above, clickable display label with chevron icon.
    /// </summary>
    public class PillSelector : VisualElement
    {
        public const string RootClass = "pillSelector";
        public const string LabelClass = "pillSelector__label";
        public const string ContainerClass = "pillSelector__container";
        public const string DisplayLabel = "pillSelector__clickableLabel";
        public const string DisplayIcon = "pillSelector__icon";

        private readonly Label labelField;
        private readonly Label selectedValueLabel;

        public event Action Clicked;

        public PillSelector()
        {
            AddToClassList(RootClass);

            labelField = UIToolkitExtensions.CreateVisualElement<Label>(this, LabelClass);
            labelField.style.display = DisplayStyle.None;

            var container = UIToolkitExtensions.CreateVisualElement(this, ContainerClass);

            selectedValueLabel = UIToolkitExtensions.CreateVisualElement<Label>(container, DisplayLabel);
            selectedValueLabel.text = "Select an option";
            selectedValueLabel.RegisterCallback<ClickEvent>(_ => OnClicked());

            UIToolkitExtensions.CreateVisualElement(container, DisplayIcon);
        }

        private void OnClicked()
        {
            Clicked?.Invoke();
        }

        public string Label
        {
            get => labelField.text;
            set
            {
                labelField.text = value;
                labelField.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public string Value
        {
            get => selectedValueLabel.text;
            set => selectedValueLabel.text = value;
        }

        public void SetFontSize(float fontSize)
        {
            selectedValueLabel.style.fontSize = fontSize;
        }
    }
}
