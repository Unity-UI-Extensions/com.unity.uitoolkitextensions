/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum AvatarSize
    {
        /// <summary>
        /// 40 px.
        /// </summary>
        Small,

        /// <summary>
        /// 48 px.
        /// </summary>
        Medium,

        /// <summary>
        /// 56 px.
        /// </summary>
        Large,
    }

    /// <summary>
    /// A round portrait: an image when there is one, otherwise the initials of <see cref="DisplayName"/>.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>display-name</c> (initials are derived from it), <c>initials</c> (overrides the derived ones),
    /// <c>size</c>, <c>show-ring</c>. Method: <see cref="SetImage"/>.</para>
    /// <para>USS classes: <c>avatarPortrait</c> (root), <c>avatarPortrait__image</c>, <c>avatarPortrait__initials</c>.</para>
    /// <para>Modifier classes: <c>avatarPortrait--small</c>, <c>avatarPortrait--medium</c>, <c>avatarPortrait--large</c>, <c>avatarPortrait--hasImage</c>,
    /// <c>avatarPortrait--ring</c>.</para>
    /// <para>Tokens read: <c>--surface-tile</c>, <c>--surface-accent</c>, <c>--c-primary</c>, <c>--radius-pill</c>,
    /// <c>--text-label</c>, <c>--text-body</c>, <c>--text-subtitle</c>, <c>--font-body-semibold</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class AvatarPortrait : VisualElement
    {
        public const string RootClass = "avatarPortrait";
        public const string ImageClass = "avatarPortrait__image";
        public const string InitialsClass = "avatarPortrait__initials";
        public const string HasImageClass = "avatarPortrait--hasImage";
        public const string RingClass = "avatarPortrait--ring";

        private readonly VisualElement image;
        private readonly Label initialsLabel;
        private string displayName = string.Empty;
        private string initials;
        private AvatarSize size = AvatarSize.Medium;

        [UxmlAttribute("display-name")]
        public string DisplayName
        {
            get => displayName;
            set
            {
                displayName = value ?? string.Empty;
                tooltip = displayName;
                UpdateInitials();
            }
        }

        /// <summary>
        /// Explicit initials; empty derives them from <see cref="DisplayName"/>.
        /// </summary>
        [UxmlAttribute("initials")]
        public string Initials
        {
            get => initialsLabel.text;
            set
            {
                initials = value;
                UpdateInitials();
            }
        }

        [UxmlAttribute("size")]
        public AvatarSize Size
        {
            get => size;
            set
            {
                size = value;
                EnableInClassList($"{RootClass}--small", size == AvatarSize.Small);
                EnableInClassList($"{RootClass}--medium", size == AvatarSize.Medium);
                EnableInClassList($"{RootClass}--large", size == AvatarSize.Large);
            }
        }

        /// <summary>
        /// A 2 px primary ring around the portrait.
        /// </summary>
        [UxmlAttribute("show-ring")]
        public bool ShowRing
        {
            get => ClassListContains(RingClass);
            set => EnableInClassList(RingClass, value);
        }

        public AvatarPortrait()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            initialsLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, InitialsClass);
            initialsLabel.pickingMode = PickingMode.Ignore;
            image = UIToolkitExtensions.CreateVisualElement(this, ImageClass);
            image.pickingMode = PickingMode.Ignore;

            Size = AvatarSize.Medium;
            ShowRing = true;
            SetImage(default);
        }

        public AvatarPortrait(string name, AvatarSize size = AvatarSize.Medium)
            : this()
        {
            DisplayName = name;
            Size = size;
        }

        /// <summary>
        /// The portrait from a texture, sprite or vector image; a default value shows the initials.
        /// </summary>
        public void SetImage(Background portrait)
        {
            var has = portrait.texture != null || portrait.sprite != null || portrait.vectorImage != null || portrait.renderTexture != null;
            image.style.backgroundImage = has ? new StyleBackground(portrait) : new StyleBackground(StyleKeyword.None);
            image.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            initialsLabel.style.display = has ? DisplayStyle.None : DisplayStyle.Flex;
            EnableInClassList(HasImageClass, has);
        }

        /// <summary>
        /// The first letters of the first and last words: "Mara Okafor" is "MO", "Mara" is "M".
        /// </summary>
        public static string InitialsOf(string name)
        {
            var words = (name ?? string.Empty).Split(new[] { ' ', '\t', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return string.Empty;
            }

            var first = char.ToUpperInvariant(words[0][0]);
            return words.Length == 1 ? first.ToString() : $"{first}{char.ToUpperInvariant(words[^1][0])}";
        }

        private void UpdateInitials()
        {
            initialsLabel.text = string.IsNullOrEmpty(initials) ? InitialsOf(displayName) : initials;
        }
    }
}
