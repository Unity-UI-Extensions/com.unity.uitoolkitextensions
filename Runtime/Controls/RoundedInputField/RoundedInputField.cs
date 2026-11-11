/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum RoundedInputFieldVariant
    {
        /// <summary>
        /// The look from the stylesheet, without tokens.
        /// </summary>
        Default,

        /// <summary>
        /// Styled from ThemeTokens (<c>roundedInputField--themed</c>): 50 px tall, a 1.5 px <c>--surface-input-border</c>,
        /// <c>--radius-input</c> corners, <c>--surface-input</c> fill and <c>--surface-fg</c> text, on deep and light grounds.
        /// </summary>
        Themed,
    }

    /// <summary>
    /// A text input field with rounded corners and a simpler appearance than <see cref="PillInputField"/>.
    /// </summary>
    [UxmlElement]
    public partial class RoundedInputField : VisualElement
    {
        public const string RootClass = "roundedInputField";
        public const string TextFieldClass = "roundedInputField__textField";
        public const string InputClass = "roundedInputField__input";
        public const string PlaceholderClass = "roundedInputField__placeholder";
        public const string InvalidClass = "roundedInputField--invalid";
        public const string ThemedClass = "roundedInputField--themed";
        public const string FocusedClass = "roundedInputField--focused";

        private readonly TextField textField;
        private string placeholderText = string.Empty;
        private readonly Label placeholderLabel;

        public event Action<string> ValueChanged;

        public RoundedInputField()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.position = Position.Relative;

            placeholderLabel = new Label();
            placeholderLabel.AddToClassList(PlaceholderClass);
            placeholderLabel.pickingMode = PickingMode.Ignore;
            placeholderLabel.style.position = Position.Absolute;

            textField = new TextField
            {
                label = string.Empty,
                isDelayed = false,
            };

            textField.AddToClassList(TextFieldClass);
            textField.labelElement.style.display = DisplayStyle.None;

            textField.Q(className: TextField.inputUssClassName)?.AddToClassList(InputClass);

            textField.RegisterCallback<FocusInEvent>(_ => AddToClassList(FocusedClass));
            textField.RegisterCallback<FocusOutEvent>(_ => RemoveFromClassList(FocusedClass));

            textField.RegisterValueChangedCallback(evt =>
            {
                UpdatePlaceholderVisibility();
                ValueChanged?.Invoke(evt.newValue);
            });

            container.Add(placeholderLabel);
            container.Add(textField);
            Add(container);

            UpdatePlaceholderVisibility();
        }

        [UxmlAttribute("value")]
        public string Value
        {
            get => textField.value;
            set
            {
                textField.value = value;
                UpdatePlaceholderVisibility();
            }
        }

        public void SetValueWithoutNotify(string value)
        {
            textField.SetValueWithoutNotify(value);
            UpdatePlaceholderVisibility();
        }

        [UxmlAttribute("placeholder")]
        public string Placeholder
        {
            get => placeholderText;
            set
            {
                placeholderText = value;
                placeholderLabel.text = value;
                UpdatePlaceholderVisibility();
            }
        }

        [UxmlAttribute("is-password")]
        public bool IsPassword
        {
            get => textField.isPasswordField;
            set => textField.isPasswordField = value;
        }

        [UxmlAttribute("variant")]
        public RoundedInputFieldVariant Variant
        {
            get => ClassListContains(ThemedClass) ? RoundedInputFieldVariant.Themed : RoundedInputFieldVariant.Default;
            set
            {
                EnableInClassList(ThemedClass, value == RoundedInputFieldVariant.Themed);
                if (value == RoundedInputFieldVariant.Themed)
                {
                    this.UseThemeTokens();
                }
            }
        }

        /// <summary>
        /// Marks the field as failing validation (<c>roundedInputField--invalid</c>): a 1.5 px border in
        /// <c>--c-warn</c>. The error text is the owner's to show.
        /// </summary>
        [UxmlAttribute("invalid")]
        public bool Invalid
        {
            get => ClassListContains(InvalidClass);
            set => EnableInClassList(InvalidClass, value);
        }

        [UxmlAttribute("multiline")]
        public bool Multiline
        {
            get => textField.multiline;
            set => textField.multiline = value;
        }

        [UxmlAttribute("max-length")]
        public int MaxLength
        {
            get => textField.maxLength;
            set => textField.maxLength = value;
        }

        public void SetBackgroundColor(Color color)
        {
            textField.style.backgroundColor = color;
        }

        public void SetTextColor(Color color)
        {
            textField.style.color = color;
        }

        public void SetPlaceholderColor(Color color)
        {
            placeholderLabel.style.color = color;
        }

        public void SetFontSize(float fontSize)
        {
            textField.style.fontSize = fontSize;
            placeholderLabel.style.fontSize = fontSize;
        }

        public new void Focus()
        {
            textField.Focus();
        }

        private void UpdatePlaceholderVisibility()
        {
            placeholderLabel.SetDisplayed(string.IsNullOrEmpty(textField.value) && !string.IsNullOrEmpty(placeholderText));
        }
    }
}