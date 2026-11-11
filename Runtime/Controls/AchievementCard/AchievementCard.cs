/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The unlock moment: a large badge disc with a halo, an "Achievement unlocked" eyebrow, the title, the
    /// reason, optional reward chips, and See all (ghost) and Continue (primary).
    /// </summary>
    /// <remarks>
    /// <para>With <c>card</c> on (the default) the element draws its own dark card; turn it off to host the content in a
    /// <see cref="ModalDialog"/> or a sheet. The card does not dim the screen or trap focus; the host does.</para>
    /// <para>The eyebrow defaults to "Achievement unlocked", shown in capitals. <c>primary-text</c> defaults to "Continue".
    /// <c>secondary-text</c> defaults to "See all"; empty hides it. The badge is a <see cref="BadgeTile"/> at 96 px.
    /// Strings are display text.</para>
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
        /// Badge disc diameter in pixels.
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
                eyebrow.SetDisplayed(!string.IsNullOrEmpty(value));
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
                body.SetTextOrHide(value);
            }
        }

        [UxmlAttribute("primary-text")]
        public string PrimaryText
        {
            get => primary.Text;
            set
            {
                primary.Text = value ?? string.Empty;
                primary.SetDisplayed(!string.IsNullOrEmpty(value));
            }
        }

        [UxmlAttribute("secondary-text")]
        public string SecondaryText
        {
            get => secondary.Text;
            set
            {
                secondary.Text = value ?? string.Empty;
                secondary.SetDisplayed(!string.IsNullOrEmpty(value));
            }
        }

        /// <summary>
        /// Draws the dark card behind the content. Off to host the content in a dialog.
        /// </summary>
        [UxmlAttribute("card")]
        public bool Card
        {
            get => ClassListContains(CardClass);
            set => EnableInClassList(CardClass, value);
        }

        public AchievementCard()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var halo = UIToolkitExtensions.CreateVisualElement(this, HaloClass).IgnorePicking();
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

            rewards.SetDisplayed(rewards.childCount > 0);
        }
    }
}
