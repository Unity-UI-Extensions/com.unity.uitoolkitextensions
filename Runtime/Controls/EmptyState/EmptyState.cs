/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// What a list or browser shows when it has nothing to show: an icon, a title, a short body and
    /// at most one action.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>icon</c>, <c>title-text</c>, <c>body-text</c>, <c>action-text</c> (empty hides the action).
    /// Event: <see cref="ActionClicked"/>.</para>
    /// <para>USS classes: <c>emptyState</c> (root), <c>emptyState__icon</c>, <c>emptyState__title</c>,
    /// <c>emptyState__body</c>, <c>emptyState__action</c> (a primary compact <see cref="PillButton"/>).
    /// Modifier: <c>emptyState--hasIcon</c>.</para>
    /// <para>Tokens read: <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--space-2..6</c>, <c>--text-subtitle</c>,
    /// <c>--text-label</c>, <c>--font-display</c>, <c>--font-body</c>, plus the PillButton primary tokens.</para>
    /// </remarks>
    [UxmlElement]
    public partial class EmptyState : VisualElement
    {
        public const string RootClass = "emptyState";
        public const string IconClass = "emptyState__icon";
        public const string TitleClass = "emptyState__title";
        public const string BodyClass = "emptyState__body";
        public const string ActionClass = "emptyState__action";
        public const string HasIconClass = "emptyState--hasIcon";

        private readonly VisualElement icon;
        private readonly Label title;
        private readonly Label body;
        private readonly PillButton action;
        private Texture2D iconTexture;

        public event Action ActionClicked;

        public PillButton ActionButton => action;

        [UxmlAttribute("icon")]
        public Texture2D Icon
        {
            get => iconTexture;
            set
            {
                iconTexture = value;
                SetIcon(value != null ? Background.FromTexture2D(value) : default);
            }
        }

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => SetText(title, value);
        }

        [UxmlAttribute("body-text")]
        public string BodyText
        {
            get => body.text;
            set => SetText(body, value);
        }

        [UxmlAttribute("action-text")]
        public string ActionText
        {
            get => action.Text;
            set
            {
                action.Text = value ?? string.Empty;
                action.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public EmptyState()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            icon = UIToolkitExtensions.CreateVisualElement(this, IconClass);
            icon.pickingMode = PickingMode.Ignore;
            title = UIToolkitExtensions.CreateVisualElement<Label>(this, TitleClass);
            body = UIToolkitExtensions.CreateVisualElement<Label>(this, BodyClass);

            action = new PillButton { Variant = PillButtonVariant.Primary, Compact = true };
            action.AddToClassList(ActionClass);
            action.Clicked += () => ActionClicked?.Invoke();
            Add(action);

            SetIcon(default);
            TitleText = null;
            BodyText = null;
            ActionText = null;
        }

        public EmptyState(string titleText, string bodyText = null, string actionText = null)
            : this()
        {
            TitleText = titleText;
            BodyText = bodyText;
            ActionText = actionText;
        }

        /// <summary>
        /// Sets the icon from a texture, sprite or vector image; a default value hides it.
        /// </summary>
        public void SetIcon(Background background)
        {
            var has = background.texture != null || background.sprite != null || background.vectorImage != null || background.renderTexture != null;
            icon.style.backgroundImage = has ? new StyleBackground(background) : new StyleBackground(StyleKeyword.None);
            icon.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(HasIconClass, has);
        }

        private static void SetText(Label label, string text)
        {
            label.text = text ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
