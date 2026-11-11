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
    /// <para>An empty <c>action-text</c> hides the action. The action is a primary compact <see cref="PillButton"/>.</para>
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
            set => title.SetTextOrHide(value);
        }

        [UxmlAttribute("body-text")]
        public string BodyText
        {
            get => body.text;
            set => body.SetTextOrHide(value);
        }

        [UxmlAttribute("action-text")]
        public string ActionText
        {
            get => action.Text;
            set
            {
                action.Text = value ?? string.Empty;
                action.SetDisplayed(!string.IsNullOrEmpty(value));
            }
        }

        public EmptyState()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            icon = UIToolkitExtensions.CreateVisualElement(this, IconClass).IgnorePicking();
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
            var has = icon.SetImageOrNone(background);
            icon.SetDisplayed(has);
            EnableInClassList(HasIconClass, has);
        }

    }
}
