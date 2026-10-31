/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The context pill at the top left of AR screens: a logo, a divider, a title and an eyebrow subtitle in a dark
    /// pill, such as "Sample Tour" over "STOP 4 OF 12".
    /// </summary>
    /// <remarks>
    /// <para>The pill hugs its content; a long title ends with an ellipsis. With no title and no subtitle it shows
    /// the logo alone, centred, at least 56 px wide, for the HUD before an experience or space is open. Dim it
    /// while a dialog is open over it; a dimmed pill ignores taps and keyboard focus.</para>
    /// <para>Props: <c>title</c>, <c>subtitle</c> (shown in capitals), <c>dimmed</c>, <c>tint-logo</c> (tints the logo
    /// with the text colour, default on, for a white or single-colour logo). Methods: <see cref="SetLogo"/>.
    /// Events: <see cref="Tapped"/> (a click, or submit from the keyboard).</para>
    /// <para>USS classes: <c>contextPill</c> (root), <c>contextPill__logo</c>, <c>contextPill__divider</c>,
    /// <c>contextPill__texts</c>, <c>contextPill__title</c>, <c>contextPill__subtitle</c>.</para>
    /// <para>Modifier classes: <c>contextPill--dimmed</c>, <c>contextPill--noLogo</c>, <c>contextPill--noSubtitle</c>,
    /// <c>contextPill--logoOnly</c> (no title and no subtitle), <c>contextPill--tintLogo</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-border</c>, <c>--surface-accent</c>, <c>--surface-fg</c>,
    /// <c>--surface-muted</c>, <c>--radius-pill</c>, <c>--space-3</c>, <c>--text-label</c>, <c>--text-micro</c>,
    /// <c>--font-display</c>, <c>--font-body-medium</c>, <c>--motion-base</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ContextPill : VisualElement
    {
        public const string RootClass = "contextPill";
        public const string LogoClass = "contextPill__logo";
        public const string DividerClass = "contextPill__divider";
        public const string TextsClass = "contextPill__texts";
        public const string TitleClass = "contextPill__title";
        public const string SubtitleClass = "contextPill__subtitle";
        public const string DimmedClass = "contextPill--dimmed";
        public const string NoLogoClass = "contextPill--noLogo";
        public const string NoSubtitleClass = "contextPill--noSubtitle";
        public const string LogoOnlyClass = "contextPill--logoOnly";
        public const string TintLogoClass = "contextPill--tintLogo";

        /// <summary>
        /// The logo height; the width follows the image's aspect ratio.
        /// </summary>
        public const float LogoHeight = 22f;

        private readonly VisualElement logo;
        private readonly VisualElement divider;
        private readonly VisualElement texts;
        private readonly Label titleLabel;
        private readonly Label subtitleLabel;
        private string subtitle = string.Empty;
        private bool hasLogo;
        private bool dimmed;

        /// <summary>
        /// Raised when the pill is tapped or submitted, unless it is dimmed.
        /// </summary>
        public event Action Tapped;

        public VisualElement Logo => logo;

        [UxmlAttribute("title")]
        public string Title
        {
            get => titleLabel.text;
            set
            {
                titleLabel.text = value ?? string.Empty;
                UpdateParts();
            }
        }

        /// <summary>
        /// The eyebrow under the title, such as "Stop 4 of 12". Shown in capitals; empty hides it.
        /// </summary>
        [UxmlAttribute("subtitle")]
        public string Subtitle
        {
            get => subtitle;
            set
            {
                subtitle = value ?? string.Empty;
                subtitleLabel.text = subtitle.ToUpperInvariant();
                subtitleLabel.style.display = subtitle.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
                EnableInClassList(NoSubtitleClass, subtitle.Length == 0);
                UpdateParts();
            }
        }

        /// <summary>
        /// True when there is no title and no subtitle, so only the logo shows.
        /// </summary>
        public bool IsLogoOnly => ClassListContains(LogoOnlyClass);

        /// <summary>
        /// Fades the pill while a dialog is over it. A dimmed pill ignores taps and focus.
        /// </summary>
        [UxmlAttribute("dimmed")]
        public bool Dimmed
        {
            get => dimmed;
            set
            {
                dimmed = value;
                EnableInClassList(DimmedClass, dimmed);
                pickingMode = dimmed ? PickingMode.Ignore : PickingMode.Position;
                focusable = !dimmed;
                if (dimmed)
                {
                    Blur();
                }
            }
        }

        /// <summary>
        /// Tints the logo with the text colour. Turn off for a full-colour logo.
        /// </summary>
        [UxmlAttribute("tint-logo")]
        public bool TintLogo
        {
            get => ClassListContains(TintLogoClass);
            set => EnableInClassList(TintLogoClass, value);
        }

        public ContextPill()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            this.MakePill();
            tabIndex = 0;

            logo = UIToolkitExtensions.CreateVisualElement(this, LogoClass);
            logo.pickingMode = PickingMode.Ignore;
            divider = UIToolkitExtensions.CreateVisualElement(this, DividerClass);
            divider.pickingMode = PickingMode.Ignore;

            texts = UIToolkitExtensions.CreateVisualElement(this, TextsClass);
            texts.pickingMode = PickingMode.Ignore;
            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            titleLabel.pickingMode = PickingMode.Ignore;
            subtitleLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, SubtitleClass);
            subtitleLabel.pickingMode = PickingMode.Ignore;

            RegisterCallback<ClickEvent>(_ => OnTapped());
            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                OnTapped();
                evt.StopPropagation();
            });

            TintLogo = true;
            Dimmed = false;
            Subtitle = null;
            SetLogo(default);
        }

        public ContextPill(string title, string subtitle = null)
            : this()
        {
            Title = title;
            Subtitle = subtitle;
        }

        /// <summary>
        /// Sets the logo from a texture, sprite or vector image; a default value hides the logo and divider.
        /// </summary>
        public void SetLogo(Background image)
        {
            var aspect = AspectOf(image);
            hasLogo = aspect > 0f;
            logo.style.backgroundImage = hasLogo ? new StyleBackground(image) : new StyleBackground(StyleKeyword.None);
            logo.style.width = hasLogo ? new StyleLength(LogoHeight * aspect) : new StyleLength(StyleKeyword.Null);
            logo.style.display = hasLogo ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(NoLogoClass, !hasLogo);
            UpdateParts();
        }

        // The divider shows only between a logo and some text; the text block hides when it has nothing to show.
        private void UpdateParts()
        {
            if (texts == null)
            {
                return;
            }

            var hasText = titleLabel.text.Length > 0 || subtitle.Length > 0;
            texts.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
            divider.style.display = hasLogo && hasText ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(LogoOnlyClass, hasLogo && !hasText);
        }

        private void OnTapped()
        {
            if (!dimmed)
            {
                Tapped?.Invoke();
            }
        }

        // Width over height of the image, or 0 when there is none.
        private static float AspectOf(Background image)
        {
            if (image.texture != null)
            {
                return image.texture.height > 0 ? (float)image.texture.width / image.texture.height : 1f;
            }

            if (image.sprite != null)
            {
                var rect = image.sprite.rect;
                return rect.height > 0f ? rect.width / rect.height : 1f;
            }

            if (image.vectorImage != null)
            {
                return image.vectorImage.height > 0f ? image.vectorImage.width / image.vectorImage.height : 1f;
            }

            if (image.renderTexture != null)
            {
                return image.renderTexture.height > 0 ? (float)image.renderTexture.width / image.renderTexture.height : 1f;
            }

            return 0f;
        }
    }
}
