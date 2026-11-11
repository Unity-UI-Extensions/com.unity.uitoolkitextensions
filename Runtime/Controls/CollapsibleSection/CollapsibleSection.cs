/// Credit SimonDarksideJ

using System;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum CollapsibleSectionVariant
    {
        /// <summary>
        /// The original plain accordion.
        /// </summary>
        Default,

        /// <summary>
        /// A bordered card styled from ThemeTokens.
        /// </summary>
        Card,
    }

    /// <summary>
    /// A collapsible accordion section. The header toggles an expanded/collapsed body that
    /// animates via USS max-height transitions driven by the <see cref="ExpandedClass"/> modifier.
    /// </summary>
    /// <remarks>
    /// <para>The header holds an optional leading slot (<see cref="SetLeading"/>, for example an icon tile), the title
    /// with an optional subtitle under it, an optional trailing slot (<see cref="SetTrailing"/>, for example a status
    /// badge) and the chevron. The chevron shows when <see cref="ShowChevron"/> is true and the trailing slot is empty.</para>
    /// <para>With <see cref="Interactive"/> false, header taps do not toggle; the owner opens and closes the section
    /// from state. <see cref="ToggleOnPointerDown"/> (default true, the original behaviour) toggles as soon as the
    /// pointer goes down; set it false inside scrolling lists so a scroll that starts on a header does not toggle it.
    /// The header is focusable and toggles on submit (Enter, Space, gamepad A).</para>
    /// </remarks>
    [UxmlElement]
    public partial class CollapsibleSection : VisualElement
    {
        public const string RootClass = "collapsibleSection";
        public const string HeaderClass = "collapsibleSection__header";
        public const string LeadingClass = "collapsibleSection__leading";
        public const string TitlesClass = "collapsibleSection__titles";
        public const string TitleClass = "collapsibleSection__title";
        public const string SubtitleClass = "collapsibleSection__subtitle";
        public const string TrailingClass = "collapsibleSection__trailing";
        public const string ChevronClass = "collapsibleSection__chevron";
        public const string BodyClass = "collapsibleSection__body";
        public const string BodyContentClass = "collapsibleSection__bodyContent";
        public const string BodyTextClass = "collapsibleSection__bodyText";
        public const string ExpandedClass = "collapsibleSection--expanded";
        public const string CardClass = "collapsibleSection--card";
        public const string HasLeadingClass = "collapsibleSection--hasLeading";
        public const string HasTrailingClass = "collapsibleSection--hasTrailing";
        public const string HasSubtitleClass = "collapsibleSection--hasSubtitle";
        public const string StaticClass = "collapsibleSection--static";

        private readonly VisualElement header;
        private readonly VisualElement leading;
        private readonly Label titleLabel;
        private readonly Label subtitleLabel;
        private readonly VisualElement trailing;
        private readonly VisualElement chevron;
        private readonly VisualElement bodyContent;
        private bool isExpanded;
        private bool interactive = true;
        private bool showChevron = true;
        private bool toggleOnPointerDown = true;
        private CollapsibleSectionVariant variant;

        public event Action<bool> OnExpandedChanged;

        /// <summary>
        /// Leading slot in the header; prefer <see cref="SetLeading"/>, which keeps the modifier classes right.
        /// </summary>
        public VisualElement HeaderLeading => leading;

        /// <summary>
        /// Trailing slot in the header; prefer <see cref="SetTrailing"/>, which keeps the chevron rule right.
        /// </summary>
        public VisualElement HeaderTrailing => trailing;

        public VisualElement Header => header;

        [UxmlAttribute("expanded")]
        public bool IsExpanded
        {
            get => isExpanded;
            set => SetExpanded(value);
        }

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => titleLabel.text;
            set => titleLabel.text = value;
        }

        /// <summary>
        /// A line under the title, such as "REQUIRED"; empty hides it.
        /// </summary>
        [UxmlAttribute("subtitle-text")]
        public string SubtitleText
        {
            get => subtitleLabel.text;
            set
            {
                subtitleLabel.text = value ?? string.Empty;
                var has = !string.IsNullOrEmpty(value);
                subtitleLabel.SetDisplayed(has);
                EnableInClassList(HasSubtitleClass, has);
            }
        }

        [UxmlAttribute("body-text")]
        public string BodyText
        {
            get
            {
                var existing = bodyContent.Q<Label>(className: BodyTextClass);
                return existing != null ? existing.text : string.Empty;
            }
            set => SetBodyText(value);
        }

        /// <summary>
        /// Whether header taps and submit toggle the section.
        /// </summary>
        [UxmlAttribute("interactive")]
        public bool Interactive
        {
            get => interactive;
            set
            {
                interactive = value;
                EnableInClassList(StaticClass, !interactive);
                header.focusable = interactive;
            }
        }

        /// <summary>
        /// Whether the chevron shows; it also hides while the trailing slot has content.
        /// </summary>
        [UxmlAttribute("show-chevron")]
        public bool ShowChevron
        {
            get => showChevron;
            set
            {
                showChevron = value;
                UpdateChevron();
            }
        }

        /// <summary>
        /// Toggle on pointer down (the original behaviour) or on a completed tap.
        /// </summary>
        [UxmlAttribute("toggle-on-pointer-down")]
        public bool ToggleOnPointerDown
        {
            get => toggleOnPointerDown;
            set => toggleOnPointerDown = value;
        }

        [UxmlAttribute("variant")]
        public CollapsibleSectionVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                EnableInClassList(CardClass, variant == CollapsibleSectionVariant.Card);
                if (variant == CollapsibleSectionVariant.Card)
                {
                    this.UseThemeTokens();
                }
            }
        }

        public CollapsibleSection()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            header = UIToolkitExtensions.CreateVisualElement(HeaderClass);
            header.pickingMode = PickingMode.Position;
            header.focusable = true;
            header.tabIndex = 0;
            hierarchy.Add(header);

            leading = UIToolkitExtensions.CreateVisualElement(header, LeadingClass).IgnorePicking();
            leading.style.display = DisplayStyle.None;

            var titles = UIToolkitExtensions.CreateVisualElement(header, TitlesClass).IgnorePicking();

            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(titles, TitleClass).IgnorePicking();

            subtitleLabel = UIToolkitExtensions.CreateVisualElement<Label>(titles, SubtitleClass).IgnorePicking();

            trailing = UIToolkitExtensions.CreateVisualElement(header, TrailingClass).IgnorePicking();
            trailing.style.display = DisplayStyle.None;

            chevron = UIToolkitExtensions.CreateVisualElement(header, ChevronClass).IgnorePicking();

            var body = UIToolkitExtensions.CreateVisualElement(BodyClass);
            hierarchy.Add(body);

            bodyContent = UIToolkitExtensions.CreateVisualElement(body, BodyContentClass);

            header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
            header.RegisterCallback<ClickEvent>(OnHeaderClicked);
            header.RegisterCallback<NavigationSubmitEvent>(OnHeaderSubmit);

            SubtitleText = null;
        }

        public override VisualElement contentContainer => bodyContent;

        public void AddBodyContent(VisualElement element)
        {
            bodyContent.Add(element);
        }

        public Label SetBodyText(string text)
        {
            var existing = bodyContent.Q<Label>(className: BodyTextClass);
            existing?.RemoveFromHierarchy();

            var label = UIToolkitExtensions.CreateVisualElement<Label>(bodyContent, BodyTextClass);
            label.text = text;
            return label;
        }

        /// <summary>
        /// Puts an element in the header's leading slot, replacing what was there; null empties it.
        /// </summary>
        public void SetLeading(VisualElement element)
        {
            EnableInClassList(HasLeadingClass, FillSlot(leading, element));
        }

        private static bool FillSlot(VisualElement slot, VisualElement element)
        {
            slot.Clear();
            if (element != null)
            {
                slot.Add(element);
            }

            var has = element != null;
            slot.SetDisplayed(has);
            return has;
        }

        /// <summary>
        /// Puts an element in the header's trailing slot, replacing what was there; null empties it.
        /// </summary>
        public void SetTrailing(VisualElement element)
        {
            EnableInClassList(HasTrailingClass, FillSlot(trailing, element));
            UpdateChevron();
        }

        public void Toggle() => SetExpanded(!isExpanded);

        private void UpdateChevron()
        {
            chevron.SetDisplayed(showChevron && !ClassListContains(HasTrailingClass));
        }

        private void OnHeaderPointerDown(PointerDownEvent evt)
        {
            if (!interactive || !toggleOnPointerDown)
            {
                return;
            }

            evt.StopPropagation();
            Toggle();
        }

        private void OnHeaderClicked(ClickEvent evt)
        {
            if (!interactive || toggleOnPointerDown)
            {
                return;
            }

            evt.StopPropagation();
            Toggle();
        }

        private void OnHeaderSubmit(NavigationSubmitEvent evt)
        {
            if (!interactive)
            {
                return;
            }

            evt.StopPropagation();
            Toggle();
        }

        private void SetExpanded(bool expanded)
        {
            if (isExpanded == expanded)
            {
                return;
            }

            isExpanded = expanded;
            EnableInClassList(ExpandedClass, isExpanded);
            OnExpandedChanged?.Invoke(isExpanded);
        }
    }
}
