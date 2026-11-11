/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum EntryGateLayout
    {
        /// <summary>
        /// A message: logo at the top, then a panel with icon, title, body, content and actions.
        /// </summary>
        Message,

        /// <summary>
        /// An experience launch: a brand hero card with logos, eyebrow, title and subtitle; content below; actions at the bottom.
        /// </summary>
        Hero,
    }

    public enum EntryGateBackdrop
    {
        /// <summary>
        /// <c>--c-navy</c> to <c>--c-navy-2</c>: brand, notice and permission screens.
        /// </summary>
        Navy,

        /// <summary>
        /// <c>--c-bg</c> to <c>--c-bg-2</c>: experience entry screens.
        /// </summary>
        Deep,
    }

    public enum EntryGateIconTone
    {
        /// <summary>
        /// A primary-colour gradient disc.
        /// </summary>
        Brand,

        /// <summary>
        /// A filled warning disc.
        /// </summary>
        Warn,

        /// <summary>
        /// A warning ring on a faint warning fill.
        /// </summary>
        WarnOutline,
    }

    /// <summary>
    /// One requirement of an <see cref="EntryGate"/>, such as "Camera" or "Motion". Localise the label.
    /// </summary>
    public readonly struct EntryRequirement
    {
        public string Label { get; }
        public Background Icon { get; }

        public EntryRequirement(string label, Background icon = default)
        {
            Label = label;
            Icon = icon;
        }
    }

    /// <summary>
    /// The full-screen gate before an experience or the camera: a brandable launch layout with a gradient backdrop,
    /// logos, a title and body, the requirements the Start button unlocks, a content slot (for example a
    /// <see cref="StatusList"/>), one primary Start action with optional secondary actions, and consent links.
    /// </summary>
    /// <remarks>
    /// <para>One component covers the brand, safety notice, permission failure, entry and could-not-load screens:
    /// <see cref="Layout"/> picks the message or hero arrangement, <see cref="Backdrop"/> the navy or deep ground,
    /// <see cref="PanelCard"/> whether the message sits on a card. Children added to the gate go into its content slot.</para>
    /// <para>The gate is always on the deep palette. <see cref="Skipped"/> is the cancel link.</para>
    /// </remarks>
    [UxmlElement]
    public partial class EntryGate : VisualElement
    {
        public const string RootClass = "entryGate";
        public const string LogoClass = "entryGate__logo";
        public const string HeroClass = "entryGate__hero";
        public const string HeroLogosClass = "entryGate__heroLogos";
        public const string BrandTileClass = "entryGate__brandTile";
        public const string BrandLogoClass = "entryGate__brandLogo";
        public const string OrgLogoClass = "entryGate__orgLogo";
        public const string EyebrowClass = "entryGate__eyebrow";
        public const string TitleClass = "entryGate__title";
        public const string SubtitleClass = "entryGate__subtitle";
        public const string MainClass = "entryGate__main";
        public const string PanelClass = "entryGate__panel";
        public const string IconClass = "entryGate__icon";
        public const string IconImageClass = "entryGate__iconImage";
        public const string BodyClass = "entryGate__body";
        public const string RequirementsClass = "entryGate__requirements";
        public const string RequirementClass = "entryGate__requirement";
        public const string RequirementIconClass = "entryGate__requirementIcon";
        public const string RequirementLabelClass = "entryGate__requirementLabel";
        public const string ContentClass = "entryGate__content";
        public const string ActionsClass = "entryGate__actions";
        public const string StartClass = "entryGate__start";
        public const string SecondaryClass = "entryGate__secondary";
        public const string LinkClass = "entryGate__link";
        public const string ConsentClass = "entryGate__consent";
        public const string ConsentLinkClass = "entryGate__consentLink";
        public const string FooterClass = "entryGate__footer";
        public const string CancelClass = "entryGate__cancel";
        public const string NoteClass = "entryGate__note";
        public const string FooterEndClass = "entryGate__footerEnd";
        public const string HeroModifierClass = "entryGate--hero";
        public const string MessageModifierClass = "entryGate--message";
        public const string NavyClass = "entryGate--navy";
        public const string DeepClass = "entryGate--deep";
        public const string CardClass = "entryGate--card";
        private const string LogoEmptyClass = LogoClass + "--empty";
        private const string IconBrandClass = IconClass + "--brand";
        private const string IconWarnClass = IconClass + "--warn";
        private const string IconWarnOutlineClass = IconClass + "--warnOutline";

        private readonly GradientFill backdropFill = new(GradientPreset.Navy, GradientDirection.Vertical);
        private readonly VisualElement logo;
        private readonly VisualElement hero;
        private readonly VisualElement brandLogo;
        private readonly VisualElement orgLogo;
        private readonly Label eyebrow;
        private readonly Label heroTitle;
        private readonly Label subtitle;
        private readonly VisualElement main;
        private readonly VisualElement messagePanel;
        private readonly VisualElement icon;
        private readonly VisualElement iconImage;
        private readonly Label messageTitle;
        private readonly Label body;
        private readonly VisualElement requirements;
        private readonly VisualElement content;
        private readonly VisualElement actions;
        private readonly PillButton startButton;
        private readonly PillButton secondaryButton;
        private readonly Label link;
        private readonly VisualElement consent;
        private readonly VisualElement footer;
        private readonly Label cancel;
        private readonly Label note;
        private readonly Label footerEnd;

        private EntryGateLayout layoutMode;
        private EntryGateBackdrop backdrop;
        private EntryGateIconTone iconTone;
        private string titleText = string.Empty;

        /// <summary>
        /// Raised when the primary action is tapped (and enabled).
        /// </summary>
        public event Action Started;
        public event Action SecondaryClicked;
        public event Action LinkClicked;

        /// <summary>
        /// Raised when the cancel link is tapped.
        /// </summary>
        public event Action Skipped;

        /// <summary>
        /// Raised with the id of a consent link when it is tapped.
        /// </summary>
        public event Action<string> ConsentLinkClicked;

        public PillButton StartButton => startButton;
        public PillButton SecondaryButton => secondaryButton;

        public override VisualElement contentContainer => content;

        [UxmlAttribute("layout")]
        public EntryGateLayout Layout
        {
            get => layoutMode;
            set
            {
                layoutMode = value;
                ApplyLayout();
            }
        }

        [UxmlAttribute("backdrop")]
        public EntryGateBackdrop Backdrop
        {
            get => backdrop;
            set
            {
                backdrop = value;
                EnableInClassList(NavyClass, backdrop == EntryGateBackdrop.Navy);
                EnableInClassList(DeepClass, backdrop == EntryGateBackdrop.Deep);
            }
        }

        /// <summary>
        /// Whether the message sits on a panel card (notice, could-not-load) or straight on the backdrop (permission failure).
        /// </summary>
        [UxmlAttribute("panel-card")]
        public bool PanelCard
        {
            get => ClassListContains(CardClass);
            set => EnableInClassList(CardClass, value);
        }

        /// <summary>
        /// A small capitals line above the hero title, such as "Opening experience".
        /// </summary>
        [UxmlAttribute("eyebrow")]
        public string Eyebrow
        {
            get => eyebrow.text;
            set => eyebrow.SetTextOrHide(value);
        }

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => titleText;
            set
            {
                titleText = value ?? string.Empty;
                ApplyLayout();
            }
        }

        /// <summary>
        /// The hero's line under the title, such as "Organisation · 12 stops · about 25 min".
        /// </summary>
        [UxmlAttribute("subtitle")]
        public string Subtitle
        {
            get => subtitle.text;
            set => subtitle.SetTextOrHide(value);
        }

        /// <summary>
        /// The message body. Separate paragraphs with a blank line.
        /// </summary>
        [UxmlAttribute("body-text")]
        public string BodyText
        {
            get => body.text;
            set => body.SetTextOrHide(value);
        }

        [UxmlAttribute("icon-tone")]
        public EntryGateIconTone IconTone
        {
            get => iconTone;
            set
            {
                iconTone = value;
                icon.EnableInClassList(IconBrandClass, iconTone == EntryGateIconTone.Brand);
                icon.EnableInClassList(IconWarnClass, iconTone == EntryGateIconTone.Warn);
                icon.EnableInClassList(IconWarnOutlineClass, iconTone == EntryGateIconTone.WarnOutline);
            }
        }

        /// <summary>
        /// The primary action, such as "Start the tour"; empty hides it.
        /// </summary>
        [UxmlAttribute("start-text")]
        public string StartText
        {
            get => startButton.Text;
            set => SetButton(startButton, value);
        }

        /// <summary>
        /// Whether the primary action can be tapped yet, for example while content loads.
        /// </summary>
        [UxmlAttribute("start-enabled")]
        public bool StartEnabled
        {
            get => startButton.enabledSelf;
            set => startButton.SetEnabled(value);
        }

        [UxmlAttribute("secondary-text")]
        public string SecondaryText
        {
            get => secondaryButton.Text;
            set => SetButton(secondaryButton, value);
        }

        /// <summary>
        /// A text action under the buttons, such as "Go to the home screen".
        /// </summary>
        [UxmlAttribute("link-text")]
        public string LinkText
        {
            get => link.text;
            set => link.SetTextOrHide(value);
        }

        /// <summary>
        /// The footer's left link, such as "Cancel"; raises <see cref="Skipped"/>.
        /// </summary>
        [UxmlAttribute("cancel-text")]
        public string CancelText
        {
            get => cancel.text;
            set
            {
                cancel.SetTextOrHide(value);
                UpdateFooter();
            }
        }

        /// <summary>
        /// The footer's left caption when there is no cancel link, such as the notice timing.
        /// </summary>
        [UxmlAttribute("footer-note")]
        public string FooterNote
        {
            get => note.text;
            set
            {
                note.SetTextOrHide(value);
                UpdateFooter();
            }
        }

        /// <summary>
        /// The footer's right caption, such as "2 of 2" or "Part 1 of 3".
        /// </summary>
        [UxmlAttribute("footer-end-text")]
        public string FooterEndText
        {
            get => footerEnd.text;
            set
            {
                footerEnd.SetTextOrHide(value);
                UpdateFooter();
            }
        }

        [UxmlAttribute("top-inset")]
        public float TopInset
        {
            get => resolvedStyle.paddingTop;
            set => style.paddingTop = Mathf.Max(0f, value);
        }

        [UxmlAttribute("bottom-inset")]
        public float BottomInset
        {
            get => resolvedStyle.paddingBottom;
            set => style.paddingBottom = Mathf.Max(0f, value);
        }

        public EntryGate()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            this.AddManipulator(backdropFill);

            logo = UIToolkitExtensions.CreateVisualElement(LogoClass).IgnorePicking();
            hierarchy.Add(logo);

            hero = UIToolkitExtensions.CreateVisualElement(HeroClass);
            hero.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            hierarchy.Add(hero);
            var heroLogos = UIToolkitExtensions.CreateVisualElement(hero, HeroLogosClass);
            var brandTile = UIToolkitExtensions.CreateVisualElement(heroLogos, BrandTileClass);
            brandLogo = UIToolkitExtensions.CreateVisualElement(brandTile, BrandLogoClass);
            orgLogo = UIToolkitExtensions.CreateVisualElement(heroLogos, OrgLogoClass);
            eyebrow = UIToolkitExtensions.CreateVisualElement<Label>(hero, EyebrowClass);
            heroTitle = UIToolkitExtensions.CreateVisualElement<Label>(hero, TitleClass);
            subtitle = UIToolkitExtensions.CreateVisualElement<Label>(hero, SubtitleClass);

            main = UIToolkitExtensions.CreateVisualElement(MainClass);
            hierarchy.Add(main);
            messagePanel = UIToolkitExtensions.CreateVisualElement(main, PanelClass);
            icon = UIToolkitExtensions.CreateVisualElement(messagePanel, IconClass).IgnorePicking();
            iconImage = UIToolkitExtensions.CreateVisualElement(icon, IconImageClass).IgnorePicking();
            messageTitle = UIToolkitExtensions.CreateVisualElement<Label>(messagePanel, TitleClass);
            body = UIToolkitExtensions.CreateVisualElement<Label>(messagePanel, BodyClass);
            requirements = UIToolkitExtensions.CreateVisualElement(messagePanel, RequirementsClass);
            content = UIToolkitExtensions.CreateVisualElement(messagePanel, ContentClass);

            actions = UIToolkitExtensions.CreateVisualElement(messagePanel, ActionsClass);
            startButton = new PillButton { Variant = PillButtonVariant.Primary };
            startButton.AddToClassList(StartClass);
            startButton.Clicked += () => Started?.Invoke();
            actions.Add(startButton);
            secondaryButton = new PillButton { Variant = PillButtonVariant.Ghost };
            secondaryButton.AddToClassList(SecondaryClass);
            secondaryButton.Clicked += () => SecondaryClicked?.Invoke();
            actions.Add(secondaryButton);
            link = UIToolkitExtensions.CreateVisualElement<Label>(actions, LinkClass);
            link.RegisterCallback<ClickEvent>(_ => LinkClicked?.Invoke());
            consent = UIToolkitExtensions.CreateVisualElement(actions, ConsentClass);

            footer = UIToolkitExtensions.CreateVisualElement(FooterClass);
            hierarchy.Add(footer);
            cancel = UIToolkitExtensions.CreateVisualElement<Label>(footer, CancelClass);
            cancel.RegisterCallback<ClickEvent>(_ => Skipped?.Invoke());
            note = UIToolkitExtensions.CreateVisualElement<Label>(footer, NoteClass);
            UIToolkitExtensions.CreateVisualElement(footer, "entryGate__footerSpacer").pickingMode = PickingMode.Ignore;
            footerEnd = UIToolkitExtensions.CreateVisualElement<Label>(footer, FooterEndClass);

            SetLogo(default);
            SetOrgLogo(default);
            SetIcon(default);
            SetRequirements(null);
            SetConsentLinks(null);
            Eyebrow = null;
            Subtitle = null;
            BodyText = null;
            StartText = null;
            SecondaryText = null;
            LinkText = null;
            CancelText = null;
            FooterNote = null;
            FooterEndText = null;
            IconTone = EntryGateIconTone.Brand;
            Backdrop = EntryGateBackdrop.Navy;
            PanelCard = true;
            Layout = EntryGateLayout.Message;
        }

        /// <summary>
        /// The logo: centred at the top in the message layout, in the white tile of the hero. A default value hides it.
        /// </summary>
        public void SetLogo(Background image)
        {
            var has = logo.SetImageOrNone(image);
            brandLogo.SetImageOrNone(image);
            logo.EnableInClassList(LogoEmptyClass, !has);
            brandLogo.parent.SetDisplayed(has);
            ApplyLayout();
        }

        /// <summary>
        /// The organisation's logo in the hero, beside the brand tile. A default value hides it.
        /// </summary>
        public void SetOrgLogo(Background image)
        {
            var has = orgLogo.SetImageOrNone(image);
            orgLogo.SetDisplayed(has);
        }

        /// <summary>
        /// The icon in the message panel's disc. A default value hides the disc.
        /// </summary>
        public void SetIcon(Background image)
        {
            var has = iconImage.SetImageOrNone(image);
            icon.SetDisplayed(has);
        }

        /// <summary>
        /// The things the Start action unlocks, such as camera, motion and audio. Null or empty hides the list.
        /// </summary>
        public void SetRequirements(IEnumerable<EntryRequirement> items)
        {
            requirements.Clear();
            if (items != null)
            {
                foreach (var item in items)
                {
                    var row = UIToolkitExtensions.CreateVisualElement(requirements, RequirementClass);
                    var rowIcon = UIToolkitExtensions.CreateVisualElement(row, RequirementIconClass);
                    rowIcon.SetDisplayed(rowIcon.SetImageOrNone(item.Icon));
                    var label = UIToolkitExtensions.CreateVisualElement<Label>(row, RequirementLabelClass);
                    label.text = item.Label ?? string.Empty;
                }
            }

            requirements.SetDisplayed(requirements.childCount > 0);
        }

        /// <summary>
        /// Links under the actions, such as the privacy notice and terms: an id and a display text each.
        /// </summary>
        public void SetConsentLinks(IEnumerable<KeyValuePair<string, string>> links)
        {
            consent.Clear();
            if (links != null)
            {
                foreach (var pair in links)
                {
                    var id = pair.Key;
                    var label = UIToolkitExtensions.CreateVisualElement<Label>(consent, ConsentLinkClass);
                    label.text = pair.Value ?? string.Empty;
                    label.RegisterCallback<ClickEvent>(_ => ConsentLinkClicked?.Invoke(id));
                }
            }

            consent.SetDisplayed(consent.childCount > 0);
        }


        private static void SetButton(PillButton button, string text)
        {
            button.Text = text ?? string.Empty;
            button.SetDisplayed(!string.IsNullOrEmpty(text));
        }

        private void ApplyLayout()
        {
            var isHero = layoutMode == EntryGateLayout.Hero;
            EnableInClassList(HeroModifierClass, isHero);
            EnableInClassList(MessageModifierClass, !isHero);

            hero.SetDisplayed(isHero);
            logo.SetDisplayed(!isHero && !logo.ClassListContains(LogoEmptyClass));

            // The title lives in the hero card or at the top of the message panel.
            heroTitle.SetTextOrHide(isHero ? titleText : null);
            messageTitle.SetTextOrHide(isHero ? null : titleText);

            // The hero puts the actions at the bottom of the screen, above the footer; the message keeps them in its panel.
            if (isHero && actions.hierarchy.parent != this)
            {
                actions.RemoveFromHierarchy();
                hierarchy.Insert(hierarchy.IndexOf(footer), actions);
            }
            else if (!isHero && actions.hierarchy.parent != messagePanel)
            {
                actions.RemoveFromHierarchy();
                messagePanel.hierarchy.Add(actions);
            }
        }

        private void UpdateFooter()
        {
            var hasCancel = !string.IsNullOrEmpty(cancel.text);
            note.SetDisplayed(!hasCancel && !string.IsNullOrEmpty(note.text));
            footer.style.display = hasCancel || !string.IsNullOrEmpty(note.text) || !string.IsNullOrEmpty(footerEnd.text)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}
