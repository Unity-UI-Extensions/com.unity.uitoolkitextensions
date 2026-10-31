/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One badge (FS10_8): a star disc, filled with the primary gradient when unlocked and muted with an outline star
    /// when locked, then the name and a status such as "Unlocked", "8 of 12 stops" or "Locked".
    /// </summary>
    /// <remarks>
    /// <para>The title wraps between words; a single word wider than the tile, such as "Conversationalist" in a
    /// three-column grid, shrinks its font to fit (down to 9 px) rather than breaking mid-word.</para>
    /// <para>Badge art set with <see cref="SetIcon"/> replaces the star. It fills the disc untinted, scaled and cropped,
    /// clipped round, inside a 1.5 px ring; the primary gradient is not drawn behind it. A locked badge shows its art at
    /// 45% opacity.</para>
    /// <para>Props: <c>title-text</c>, <c>status</c>, <c>unlocked</c>, <c>size</c> (disc diameter, default 64; the
    /// <see cref="AchievementCard"/> uses 96). Method: <see cref="SetIcon"/>. Event: <see cref="Clicked"/>. Strings are
    /// display text.</para>
    /// <para>USS classes: <c>badgeTile</c> (root), <c>badgeTile__disc</c>, <c>badgeTile__icon</c>,
    /// <c>badgeTile__title</c>, <c>badgeTile__status</c>. Modifiers: <c>badgeTile--unlocked</c>,
    /// <c>badgeTile--locked</c>, <c>badgeTile--hasIcon</c>.</para>
    /// <para>Tokens read: <c>--grad-brand-start/end</c> (through <see cref="GradientFill"/>), <c>--c-on-primary</c>,
    /// <c>--surface-tile</c>, <c>--surface-ring</c>, <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-accent</c>,
    /// <c>--radius-pill</c>, <c>--space-1..2</c>, <c>--text-caption</c>, <c>--font-body-semibold</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class BadgeTile : VisualElement
    {
        public const string RootClass = "badgeTile";
        public const string DiscClass = "badgeTile__disc";
        public const string IconClass = "badgeTile__icon";
        public const string TitleClass = "badgeTile__title";
        public const string StatusClass = "badgeTile__status";
        public const string UnlockedClass = "badgeTile--unlocked";
        public const string LockedClass = "badgeTile--locked";
        public const string HasIconClass = "badgeTile--hasIcon";

        private readonly VisualElement disc;
        private readonly VisualElement icon;
        private readonly Label title;
        private readonly Label status;
        private readonly GradientFill fill = new(GradientPreset.Brand, GradientDirection.Vertical);
        private bool unlocked;
        private bool hasIcon;
        private float size = 64f;
        private float baseFontSize = float.NaN;

        /// <summary>
        /// The smallest font size a long word shrinks to.
        /// </summary>
        public const float MinTitleFontSize = 9f;

        public event Action Clicked;

        public VisualElement Disc => disc;

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set
            {
                title.text = value ?? string.Empty;
                title.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
                FitTitle();
            }
        }

        public Label TitleLabel => title;

        [UxmlAttribute("status")]
        public string Status
        {
            get => status.text;
            set
            {
                status.text = value ?? string.Empty;
                status.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("unlocked")]
        public bool Unlocked
        {
            get => unlocked;
            set
            {
                unlocked = value;
                EnableInClassList(UnlockedClass, unlocked);
                EnableInClassList(LockedClass, !unlocked);
                ApplyFill();
            }
        }

        /// <summary>
        /// Disc diameter. Badge art fills it; the star's box follows the boards, 30 in a 64 disc (FS10_8) and 48 in a
        /// 96 disc (FS10_7), and scales between them.
        /// </summary>
        [UxmlAttribute("size")]
        public float Size
        {
            get => size;
            set
            {
                size = Mathf.Max(24f, value);
                disc.style.width = size;
                disc.style.height = size;
                ApplyIconSize();
            }
        }

        public BadgeTile()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            focusable = true;
            tabIndex = 0;

            disc = UIToolkitExtensions.CreateVisualElement(this, DiscClass);
            disc.pickingMode = PickingMode.Ignore;
            disc.MakePill();
            icon = UIToolkitExtensions.CreateVisualElement(disc, IconClass);
            icon.pickingMode = PickingMode.Ignore;
            icon.MakePill();
            title = UIToolkitExtensions.CreateVisualElement<Label>(this, TitleClass);
            title.pickingMode = PickingMode.Ignore;
            status = UIToolkitExtensions.CreateVisualElement<Label>(this, StatusClass);
            status.pickingMode = PickingMode.Ignore;

            title.RegisterCallback<GeometryChangedEvent>(_ => FitTitle());
            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke());
            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                Clicked?.Invoke();
                evt.StopPropagation();
            });

            Size = size;
            Unlocked = false;
            TitleText = null;
            Status = null;
        }

        public BadgeTile(string titleText, string statusText, bool isUnlocked)
            : this()
        {
            TitleText = titleText;
            Status = statusText;
            Unlocked = isUnlocked;
        }

        /// <summary>
        /// Shrinks the title's font when its longest word is wider than the tile, so no word breaks.
        /// </summary>
        private void FitTitle()
        {
            if (title.panel == null || string.IsNullOrEmpty(title.text))
            {
                return;
            }

            var width = title.contentRect.width;
            if (float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            // The USS size is read whenever no shrunk size is set inline, so a text-scale change is picked up.
            if (title.style.fontSize.keyword == StyleKeyword.Null || float.IsNaN(baseFontSize))
            {
                baseFontSize = title.resolvedStyle.fontSize;
            }

            if (float.IsNaN(baseFontSize) || baseFontSize <= 0f)
            {
                return;
            }

            var widest = 0f;
            foreach (var word in title.text.Split(' '))
            {
                if (word.Length > 0)
                {
                    // Measured at the current size, then scaled back to the base size.
                    var measured = title.MeasureTextSize(word, 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined).x;
                    var current = title.resolvedStyle.fontSize;
                    widest = Mathf.Max(widest, current > 0f ? measured * (baseFontSize / current) : measured);
                }
            }

            // Whole points only: UI Toolkit draws a fractional size at the next whole point, which would not fit.
            var target = widest > width ? Mathf.Max(MinTitleFontSize, Mathf.Floor(baseFontSize * (width / widest))) : baseFontSize;
            if (Mathf.Abs(target - title.resolvedStyle.fontSize) > 0.1f)
            {
                title.style.fontSize = target < baseFontSize ? new StyleLength(target) : new StyleLength(StyleKeyword.Null);
            }
        }

        /// <summary>
        /// Badge art in place of the star, such as the image from an achievement's config. It fills the disc untinted,
        /// scaled and cropped, inside a subtle ring. A default value restores the star.
        /// </summary>
        public void SetIcon(Background image)
        {
            hasIcon = image.texture != null || image.sprite != null || image.vectorImage != null || image.renderTexture != null;
            icon.style.backgroundImage = hasIcon ? new StyleBackground(image) : new StyleBackground(StyleKeyword.Null);
            EnableInClassList(HasIconClass, hasIcon);
            ApplyIconSize();
            ApplyFill();
        }

        /// <summary>
        /// The primary gradient fills an unlocked star's disc; badge art covers the disc, so it has none.
        /// </summary>
        private void ApplyFill()
        {
            if (unlocked && !hasIcon)
            {
                if (fill.target == null)
                {
                    disc.AddManipulator(fill);
                }
            }
            else
            {
                fill.target?.RemoveManipulator(fill);
            }
        }

        /// <summary>
        /// The star's box for a disc diameter: 30 at 64 and 48 at 96, as on the boards.
        /// </summary>
        public static float StarBoxFor(float discSize) => Mathf.Max(12f, Mathf.Round((discSize * 0.5625f) - 6f));

        private void ApplyIconSize()
        {
            var length = hasIcon ? Length.Percent(100f) : new Length(StarBoxFor(size));
            icon.style.width = length;
            icon.style.height = length;
        }
    }
}
