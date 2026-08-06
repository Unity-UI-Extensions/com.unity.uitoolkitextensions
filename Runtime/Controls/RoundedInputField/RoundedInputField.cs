/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Simpler implementation of the PillINputField for a minimalist implementation.
    /// </summary>
    [UxmlElement]
    public partial class RoundedInputField : VisualElement
    {
        public const string RootClass = "roundedInputField";
        public const string TextFieldClass = "roundedInputField__textField";
        public const string InputClass = "roundedInputField__input";
        public const string PlaceholderClass = "roundedInputField__placeholder";

        private readonly TextField textField;
        private string placeholderText = string.Empty;
        private Label placeholderLabel;
        private bool isPassword;

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

            var input = textField.Q(className: TextField.inputUssClassName);
            if (input != null)
            {
                input.AddToClassList(InputClass);
            }

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
            get => isPassword;
            set
            {
                isPassword = value;
                textField.isPasswordField = value;
            }
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
            bool showPlaceholder = string.IsNullOrEmpty(textField.value) && !string.IsNullOrEmpty(placeholderText);
            placeholderLabel.style.display = showPlaceholder ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}