// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.Text.RegularExpressions;
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
        private const string ControlStylesAssetPath = "Packages/com.unity.uitoolkitextensions/Runtime/Styles/UIToolkitExtensionsControlStyles.uss";
        private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        private static StyleSheet controlStyles;

        /// <summary>
        /// The shared stylesheet with the default styles for all controls.
        /// It loads through the AssetDatabase, so it is null in player builds.
        /// </summary>
        public static StyleSheet ControlStyles
        {
            get
            {
#if UNITY_EDITOR
                if (controlStyles == null)
                {
                    controlStyles = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(ControlStylesAssetPath);
                }
#endif
                return controlStyles;
            }
        }

        /// <summary>
        /// Attaches the shared stylesheet to an element, so controls render correctly in UI Builder
        /// and in Play Mode without a manual stylesheet reference.
        /// </summary>
        /// <remarks>
        /// This does nothing in player builds. For builds, the starter templates and the theme from
        /// the "GameObject/UI Toolkit/Extensions" menu reference the stylesheet. Code-first UI must
        /// add the stylesheet to its panel theme or root element.
        /// </remarks>
        public static void ApplyControlStyles(this VisualElement element) => element.AttachOnce(ControlStyles);

        /// <summary>
        /// Creates a VisualElement with the given USS classes.
        /// </summary>
        public static VisualElement CreateVisualElement(params string[] classNames)
        {
            return CreateVisualElement<VisualElement>(classNames);
        }

        /// <summary>
        /// Creates a VisualElement with the given USS classes and adds it to a parent.
        /// </summary>
        public static VisualElement CreateVisualElement(VisualElement parent, params string[] classNames)
        {
            return CreateVisualElement<VisualElement>(parent, classNames);
        }

        /// <summary>
        /// Creates an element of type T with the given USS classes.
        /// </summary>
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
        /// Creates an element of type T with the given USS classes and adds it to a parent.
        /// </summary>
        public static T CreateVisualElement<T>(VisualElement parent, params string[] classNames) where T : VisualElement, new()
        {
            var ele = CreateVisualElement<T>(classNames);
            parent.Add(ele);
            return ele;
        }

        /// <summary>
        /// Creates a label with underlined text and the "link-text" class.
        /// </summary>
        public static Label CreateHyperlinkLabel(VisualElement parent, string text)
        {
            var label = CreateVisualElement<Label>(parent, "link-text");
            label.text = $"<u>{text}</u>";
            label.enableRichText = true;
            return label;
        }

        /// <summary>
        /// Creates a text field that shows the placeholder as its value and selects all text on focus.
        /// </summary>
        public static TextField CreateTextInputField(VisualElement parent, string placeholder)
        {
            var textField = CreateVisualElement<TextField>(parent, "text-field");
            textField.value = placeholder;
            textField.selectAllOnFocus = true;
            textField.RegisterCallback<FocusInEvent>(evt => textField.SelectAll());
            return textField;
        }

        /// <summary>
        /// Checks that a TextField holds an email address. Colours the text black when valid, red when not.
        /// </summary>
        public static bool ValidateTextFieldEmailAddress(this TextField inputText)
        {
            var isValidEmail = Regex.IsMatch(inputText.value, EmailPattern);
            inputText.style.color = isValidEmail ? Color.black : Color.red;
            return isValidEmail;
        }

        /// <summary>
        /// Creates a button that invokes a UnityEvent on click.
        /// </summary>
        public static Button CreateButton(VisualElement parent, string text, UnityEvent onClickAction, bool isInitiallyVisible = true, string className = null)
        {
            return CreateButton(parent, text, () => onClickAction?.Invoke(), isInitiallyVisible, className);
        }

        /// <summary>
        /// Creates a button that invokes an Action on click.
        /// </summary>
        public static Button CreateButton(VisualElement parent, string text, Action onClickAction, bool isInitiallyVisible = true, string className = null)
        {
            var button = string.IsNullOrEmpty(className)
                ? CreateVisualElement<Button>(parent)
                : CreateVisualElement<Button>(parent, className);
            button.text = text;
            button.clicked += onClickAction;
            button.SetDisplayed(isInitiallyVisible);
            return button;
        }

        internal static void AttachOnce(this VisualElement element, StyleSheet sheet)
        {
            if (sheet != null && !element.styleSheets.Contains(sheet))
            {
                element.styleSheets.Add(sheet);
            }
        }

        internal static void SetDisplayed(this VisualElement element, bool shown)
        {
            element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Sets the text and hides the element when the text is null or empty.
        /// </summary>
        internal static void SetTextOrHide(this TextElement element, string text)
        {
            element.text = text ?? string.Empty;
            element.SetDisplayed(!string.IsNullOrEmpty(text));
        }

        /// <summary>
        /// Makes pointer events pass through the element. Returns the element for chaining.
        /// </summary>
        internal static T IgnorePicking<T>(this T element) where T : VisualElement
        {
            element.pickingMode = PickingMode.Ignore;
            return element;
        }

        /// <summary>
        /// Calls the action on a click and on a keyboard or gamepad submit.
        /// </summary>
        internal static void RegisterActivate(this VisualElement element, Action action)
        {
            element.RegisterCallback<ClickEvent>(_ => action());
            element.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                action();
                evt.StopPropagation();
            });
        }

        /// <summary>
        /// The longest transition duration of the element, in milliseconds.
        /// </summary>
        internal static float LongestTransitionMs(this VisualElement element)
        {
            var longest = 0f;
            foreach (var duration in element.resolvedStyle.transitionDuration)
            {
                longest = Mathf.Max(longest, duration.unit == TimeUnit.Second ? duration.value * 1000f : duration.value);
            }

            return longest;
        }

        internal static Color ParseColorOr(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
        }

        internal static bool HasContent(this Background background)
        {
            return background.texture != null || background.sprite != null || background.vectorImage != null || background.renderTexture != null;
        }

        /// <summary>
        /// Shows the image as the element background, or no background when the image is empty.
        /// </summary>
        /// <returns>True when the image has content.</returns>
        internal static bool SetImageOrNone(this VisualElement element, Background image)
        {
            var has = image.HasContent();
            element.style.backgroundImage = has ? new StyleBackground(image) : new StyleBackground(StyleKeyword.None);
            return has;
        }
    }
}
