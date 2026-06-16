/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public class PillInputField : VisualElement
    {
        private const float FocusActivationDragThresholdPx = 8f;

        public const string RootClass = "pillInputField";
        public const string LabelClass = "pillInputField__label";
        public const string InputClass = "pillInputField__input";
        public const string InputTextContentClass = "pillInputField__inputTextContent";
        public const string PasswordModeClass = "pillInputField--passwordMode";
        public const string MultilineClass = "pillInputField--multiline";

        private readonly Label labelField;
        private readonly TextField textField;
        private bool isPasswordField;
        private TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default;
        private bool isPointerDown;
        private bool isPointerDragging;
        private int activePointerId = -1;
        private Vector2 pointerDownPosition;

        public event Action<string> OnValueChanged;
        public event Action<string> OnValidation;

        public PillInputField()
        {
            AddToClassList(RootClass);

            labelField = UIToolkitExtensions.CreateVisualElement<Label>(this, LabelClass);
            labelField.style.display = DisplayStyle.None;

            textField = UIToolkitExtensions.CreateVisualElement<TextField>(this, InputClass);
            textField.label = string.Empty;
            textField.textEdition.keyboardType = keyboardType;
            textField.AddToClassList(InputTextContentClass);

            textField.RegisterCallback<PointerDownEvent>(OnTextFieldPointerDown, TrickleDown.TrickleDown);
            textField.RegisterCallback<PointerMoveEvent>(OnTextFieldPointerMove, TrickleDown.TrickleDown);
            textField.RegisterCallback<PointerUpEvent>(OnTextFieldPointerReleased, TrickleDown.TrickleDown);
            textField.RegisterCallback<PointerCancelEvent>(OnTextFieldPointerReleased, TrickleDown.TrickleDown);
            textField.RegisterCallback<FocusOutEvent>(OnTextFieldBlur);
            textField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChanged);
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
            get => textField.value;
            set
            {
                textField.SetValueWithoutNotify(value ?? string.Empty);
                UpdatePasswordMaskState();
            }
        }

        public TouchScreenKeyboardType KeyboardType
        {
            get => keyboardType;
            set
            {
                keyboardType = value;
                textField.textEdition.keyboardType = keyboardType;
            }
        }

        public new void Focus()
        {
            textField.Focus();
        }

        public new void Blur()
        {
            textField.Blur();
        }

        public void SetPlaceholder(string placeholder)
        {
            textField.textEdition.placeholder = placeholder ?? string.Empty;
        }

        public void SetPasswordMode(bool isPassword)
        {
            isPasswordField = isPassword;
            UpdatePasswordMaskState();
        }

        public void SetMaxLength(int maxLength)
        {
            textField.maxLength = maxLength;
        }

        public void SetMultiline(bool multiline)
        {
            textField.multiline = multiline;
            EnableInClassList(MultilineClass, multiline);
        }

        public void SetBackgroundColor(Color color)
        {
            style.backgroundColor = color;
        }

        public void SetTextColor(Color color)
        {
            textField.style.color = color;
        }

        public void SetFontSize(float fontSize)
        {
            textField.style.fontSize = fontSize;
        }

        public void Validate()
        {
            OnValidation?.Invoke(Value);
        }

        private void OnTextFieldPointerDown(PointerDownEvent evt)
        {
            if (!Application.isMobilePlatform)
            {
                return;
            }

            isPointerDown = true;
            isPointerDragging = false;
            activePointerId = evt.pointerId;
            pointerDownPosition = evt.position;

            textField.focusController?.IgnoreEvent(evt);
        }

        private void OnTextFieldPointerMove(PointerMoveEvent evt)
        {
            if (!Application.isMobilePlatform)
            {
                return;
            }

            if (!isPointerDown || evt.pointerId != activePointerId || isPointerDragging)
            {
                return;
            }

            if (Vector2.Distance(evt.position, pointerDownPosition) < FocusActivationDragThresholdPx)
            {
                return;
            }

            isPointerDragging = true;
            if (textField.focusController?.focusedElement == textField)
            {
                textField.Blur();
            }
        }

        private void OnTextFieldPointerReleased(IPointerEvent evt)
        {
            if (!Application.isMobilePlatform)
            {
                return;
            }

            if (!isPointerDown || evt.pointerId != activePointerId)
            {
                return;
            }

            bool shouldFocus = !isPointerDragging;
            ResetPointerTracking();

            if (shouldFocus)
            {
                textField.Focus();
            }
        }

        private void ResetPointerTracking()
        {
            isPointerDown = false;
            isPointerDragging = false;
            activePointerId = -1;
            pointerDownPosition = Vector2.zero;
        }

        private void OnTextFieldBlur(FocusOutEvent evt)
        {
            ResetPointerTracking();
            Validate();
        }

        private void OnTextFieldChanged(ChangeEvent<string> evt)
        {
            OnValueChanged?.Invoke(Value);
        }

        private void UpdatePasswordMaskState()
        {
            textField.isPasswordField = isPasswordField;
            EnableInClassList(PasswordModeClass, isPasswordField);
        }
    }
}
