/// Credit SimonDarksideJ

using System;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A selector styled as a pill, with an optional label above and a clickable display label with a chevron icon.
    /// Use it to open a secondary window or pane of selection options.
    /// </summary>
    [UxmlElement]
    public partial class PillSelector : VisualElement
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
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            labelField = UIToolkitExtensions.CreateVisualElement<Label>(this, LabelClass);
            labelField.style.display = DisplayStyle.None;

            var container = UIToolkitExtensions.CreateVisualElement(this, ContainerClass);

            selectedValueLabel = UIToolkitExtensions.CreateVisualElement<Label>(container, DisplayLabel);
            selectedValueLabel.text = "Select an option";
            selectedValueLabel.RegisterCallback<ClickEvent>(_ => Clicked?.Invoke());

            UIToolkitExtensions.CreateVisualElement(container, DisplayIcon);
        }

        [UxmlAttribute("label")]
        public string Label
        {
            get => labelField.text;
            set
            {
                labelField.text = value;
                labelField.SetDisplayed(!string.IsNullOrEmpty(value));
            }
        }

        [UxmlAttribute("value")]
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