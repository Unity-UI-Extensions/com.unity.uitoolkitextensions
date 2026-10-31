/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The unlock moment (FS10_7): a large badge disc with a halo, an "Achievement unlocked" eyebrow, the title, the
    /// reason, optional reward chips, and See all (ghost) and Continue (primary).
    /// </summary>
    /// <remarks>
    /// <para>With <c>card</c> on (the default) the element draws its own dark card; turn it off to host the content in a
    /// <see cref="ModalDialog"/> or a sheet. The card does not dim the screen or trap focus; the host does.</para>
    /// <para>Props: <c>eyebrow</c> (default "Achievement unlocked", shown in capitals), <c>title-text</c>,
    /// <c>body-text</c>, <c>primary-text</c> (default "Continue"), <c>secondary-text</c> (default "See all"; empty
    /// hides it), <c>card</c>. Methods: <see cref="SetIcon"/>, <see cref="SetRewards"/>. Events:
    /// <see cref="ContinueClicked"/>, <see cref="SeeAllClicked"/>. Strings are display text.</para>
    /// <para>USS classes: <c>achievementCard</c> (root), <c>achievementCard__halo</c>, <c>achievementCard__badge</c>
    /// (a <see cref="BadgeTile"/> at 96 px), <c>achievementCard__eyebrow</c>, <c>achievementCard__title</c>,
    /// <c>achievementCard__body</c>, <c>achievementCard__rewards</c>, <c>achievementCard__reward</c>,
    /// <c>achievementCard__actions</c>, <c>achievementCard__secondary</c>, <c>achievementCard__primary</c>.
    /// Modifier: <c>achievementCard--card</c>.</para>
    /// <para>Tokens read: <c>--surface-overlay</c>, <c>--surface-border</c>, <c>--surface-fg</c>,
    /// <c>--surface-fg-soft</c>, <c>--surface-accent</c>, <c>--surface-highlight</c>, <c>--surface-tile</c>,
    /// <c>--radius-pill</c>, <c>--space-1..6</c>, <c>--text-micro</c>, <c>--text-caption</c>, <c>--text-label-sm</c>,
    /// <c>--font-display</c>, <c>--font-body</c>, <c>--font-body-medium</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class AchievementCard : VisualElement
    {
        public const string RootClass = "achievementCard";
        public const string CardClass = "achievementCard--card";
        public const string HaloClass = "achievementCard__halo";
        public const string BadgeClass = "achievementCard__badge";
        public const string EyebrowClass = "achievementCard__eyebrow";
        public const string TitleClass = "achievementCard__title";
        public const string BodyClass = "achievementCard__body";
        public const string RewardsClass = "achievementCard__rewards";
        public const string RewardClass = "achievementCard__reward";
        public const string ActionsClass = "achievementCard__actions";
        public const string SecondaryClass = "achievementCard__secondary";
        public const string PrimaryClass = "achievementCard__primary";

        /// <summary>
        /// Badge disc diameter in FS10_7.
        /// </summary>
        public const float BadgeSize = 96f;

        private readonly BadgeTile badge;
        private readonly Label eyebrow;
        private readonly Label title;
        private readonly Label body;
        private readonly VisualElement rewards;
        private readonly PillButton secondary;
        private readonly PillButton primary;
        private string eyebrowText;
        private bool card = true;

        public event Action ContinueClicked;

        public event Action SeeAllClicked;

        public BadgeTile Badge => badge;

        public PillButton PrimaryButton => primary;

        public PillButton SecondaryButton => secondary;

        [UxmlAttribute("eyebrow")]
        public string Eyebrow
        {
            get => eyebrowText;
            set
            {
                eyebrowText = value ?? string.Empty;
                eyebrow.text = eyebrowText.ToUpperInvariant();
                eyebrow.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => title.text = value ?? string.Empty;
        }

        [UxmlAttribute("body-text")]
        public string BodyText
        {
            get => body.text;
            set
            {
                body.text = value ?? string.Empty;
                body.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("primary-text")]
        public string PrimaryText
        {
            get => primary.Text;
            set
            {
                primary.Text = value ?? string.Empty;
                primary.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("secondary-text")]
        public string SecondaryText
        {
            get => secondary.Text;
            set
            {
                secondary.Text = value ?? string.Empty;
                secondary.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Draws the dark card behind the content. Off to host the content in a dialog.
        /// </summary>
        [UxmlAttribute("card")]
        public bool Card
        {
            get => card;
            set
            {
                card = value;
                EnableInClassList(CardClass, card);
            }
        }

        public AchievementCard()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var halo = UIToolkitExtensions.CreateVisualElement(this, HaloClass);
            halo.pickingMode = PickingMode.Ignore;
            badge = new BadgeTile { Size = BadgeSize, Unlocked = true, focusable = false };
            badge.AddToClassList(BadgeClass);
            badge.pickingMode = PickingMode.Ignore;
            halo.Add(badge);

            eyebrow = UIToolkitExtensions.CreateVisualElement<Label>(this, EyebrowClass);
            title = UIToolkitExtensions.CreateVisualElement<Label>(this, TitleClass);
            body = UIToolkitExtensions.CreateVisualElement<Label>(this, BodyClass);
            rewards = UIToolkitExtensions.CreateVisualElement(this, RewardsClass);

            var actions = UIToolkitExtensions.CreateVisualElement(this, ActionsClass);
            secondary = new PillButton { Variant = PillButtonVariant.Ghost };
            secondary.AddToClassList(SecondaryClass);
            secondary.Clicked += () => SeeAllClicked?.Invoke();
            actions.Add(secondary);
            primary = new PillButton { Variant = PillButtonVariant.Primary };
            primary.AddToClassList(PrimaryClass);
            primary.Clicked += () => ContinueClicked?.Invoke();
            actions.Add(primary);

            Card = true;
            Eyebrow = "Achievement unlocked";
            BodyText = null;
            PrimaryText = "Continue";
            SecondaryText = "See all";
            SetRewards(null);
        }

        /// <summary>
        /// Badge art in place of the star.
        /// </summary>
        public void SetIcon(Background image) => badge.SetIcon(image);

        /// <summary>
        /// Small chips under the body, such as "+50 XP" or "Engineers' notes". Null or empty hides the row.
        /// </summary>
        public void SetRewards(IReadOnlyList<string> rewardTexts)
        {
            rewards.Clear();
            if (rewardTexts != null)
            {
                foreach (var text in rewardTexts)
                {
                    var chip = UIToolkitExtensions.CreateVisualElement<Label>(rewards, RewardClass).MakePill();
                    chip.text = text;
                }
            }

            rewards.style.display = rewards.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
