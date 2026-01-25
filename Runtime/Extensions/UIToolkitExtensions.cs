// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Extension methods and utilities for Unity UI Toolkit.
    /// </summary>
    public static class UIToolkitExtensions
    {
        /// <summary>
        /// Creates a VisualElement with the specified CSS class names.
        /// </summary>
        /// <param name="classNames">CSS class names to add to the element</param>
        /// <returns>A new VisualElement with the specified classes</returns>
        public static VisualElement CreateVisualElement(params string[] classNames)
        {
            return CreateVisualElement<VisualElement>(classNames);
        }

        /// <summary>
        /// Creates a VisualElement with the specified CSS class names and adds it to a parent.
        /// </summary>
        /// <param name="parent">The parent element to add the new element to</param>
        /// <param name="classNames">CSS class names to add to the element</param>
        /// <returns>A new VisualElement with the specified classes, added to the parent</returns>
        public static VisualElement CreateVisualElement(VisualElement parent, params string[] classNames)
        {
            return CreateVisualElement<VisualElement>(parent, classNames);
        }

        /// <summary>
        /// Creates a typed VisualElement with the specified CSS class names.
        /// </summary>
        /// <typeparam name="T">The type of VisualElement to create</typeparam>
        /// <param name="classNames">CSS class names to add to the element</param>
        /// <returns>A new VisualElement of type T with the specified classes</returns>
        public static T CreateVisualElement<T>(params string[] classNames) where T : VisualElement, new()
        {
            var ele = new T();
            foreach (var className in classNames)
            {
                ele.AddToClassList(className);
            }

            return ele;
        }

        /// <summary>
        /// Creates a typed VisualElement with the specified CSS class names and adds it to a parent.
        /// </summary>
        /// <typeparam name="T">The type of VisualElement to create</typeparam>
        /// <param name="parent">The parent element to add the new element to</param>
        /// <param name="classNames">CSS class names to add to the element</param>
        /// <returns>A new VisualElement of type T with the specified classes, added to the parent</returns>
        public static T CreateVisualElement<T>(VisualElement parent, params string[] classNames) where T : VisualElement, new()
        {
            var ele = CreateVisualElement<T>(classNames);
            parent.Add(ele);
            return ele;
        }

        /// <summary>
        /// Creates a hyperlink-styled label with underlined text.
        /// </summary>
        /// <param name="parent">The parent element to add the label to</param>
        /// <param name="text">The text content for the label</param>
        /// <returns>A Label configured as a hyperlink</returns>
        public static Label CreateHyperlinkLabel(VisualElement parent, string text)
        {
            var label = CreateVisualElement<Label>("link-text");
            label.text = $"<u>{text}</u>";
            label.enableRichText = true;
            parent.Add(label);
            return label;
        }

        /// <summary>
        /// Creates a text input field with placeholder text and auto-selection behavior.
        /// </summary>
        /// <param name="parent">The parent element to add the text field to</param>
        /// <param name="placeholder">The placeholder text for the field</param>
        /// <returns>A TextField configured with the specified placeholder and auto-selection</returns>
        public static TextField CreateTextInputField(VisualElement parent, string placeholder)
        {
            var textField = CreateVisualElement<TextField>("text-field");
            textField.value = placeholder;
            textField.selectAllOnFocus = true;
            textField.RegisterCallback<FocusInEvent>(evt => textField.SelectAll());
            parent.Add(textField);
            return textField;
        }

        /// <summary>
        /// Validates that a TextField contains a valid email address and updates its visual style accordingly.
        /// </summary>
        /// <param name="inputText">The TextField to validate</param>
        /// <returns>True if the text field contains a valid email address</returns>
        public static bool ValidateTextFieldEmailAddress(this TextField inputText)
        {
            // Regex pattern for validating email
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            var isValidEmail = System.Text.RegularExpressions.Regex.IsMatch(inputText.value, emailPattern);
            if (isValidEmail)
            {
                inputText.style.color = Color.black;
            }
            else
            {
                inputText.style.color = Color.red;
            }
            return isValidEmail;
        }

        /// <summary>
        /// Creates a button with the specified text and click action.
        /// </summary>
        /// <param name="parent">The parent element to add the button to</param>
        /// <param name="text">The text content for the button</param>
        /// <param name="onClickAction">The action to perform when the button is clicked</param>
        /// <param name="isInitiallyVisible">Whether the button is visible initially</param>
        /// <param name="className">Optional CSS class name to apply to the button</param>
        /// <returns>A new Button element</returns>
        public static Button CreateButton(VisualElement parent, string text, UnityEvent onClickAction, bool isInitiallyVisible = true, string className = null)
        {
            Button button;
            if (string.IsNullOrEmpty(className))
            {
                button = CreateVisualElement<Button>(parent);
            }
            else
            {
                button = CreateVisualElement<Button>(parent, className);
            }
            button.text = text;
            button.clicked += () => onClickAction?.Invoke();
            button.style.display = isInitiallyVisible ? DisplayStyle.Flex : DisplayStyle.None;
            return button;
        }

        /// <summary>
        /// Creates a button with the specified text and click action.
        /// </summary>
        /// <param name="parent">The parent element to add the button to</param>
        /// <param name="text">The text content for the button</param>
        /// <param name="onClickAction">The action to perform when the button is clicked</param>
        /// <param name="isInitiallyVisible">Whether the button is visible initially</param>
        /// <param name="className">Optional CSS class name to apply to the button</param>
        /// <returns>A new Button element</returns>
        public static Button CreateButton(VisualElement parent, string text, Action onClickAction, bool isInitiallyVisible = true, string className = null)
        {
            Button button;
            if (string.IsNullOrEmpty(className))
            {
                button = CreateVisualElement<Button>(parent);
            }
            else
            {
                button = CreateVisualElement<Button>(parent, className);
            }
            button.text = text;
            button.clicked += () => onClickAction?.Invoke();
            button.style.display = isInitiallyVisible ? DisplayStyle.Flex : DisplayStyle.None;
            return button;
        }
    }
}