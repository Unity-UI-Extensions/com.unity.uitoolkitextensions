/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// What a <see cref="CoachingOverlay"/> is asking the user to do.
    /// </summary>
    public enum CoachingMode
    {
        LookAround,
        FindSurface,
        MoveSlower,
        TooDark,
    }

    public enum CoachingPlacement
    {
        /// <summary>
        /// A compact cue in the middle of the screen.
        /// </summary>
        Centre,

        /// <summary>
        /// A full-width card near the top, with the icon and title on one row and the detail below.
        /// </summary>
        Top,

        /// <summary>
        /// The same card near the bottom, above bottom actions, so it leaves the middle of the view clear.
        /// </summary>
        Bottom,
    }

    /// <summary>
    /// A non-blocking coaching cue for AR tracking: a device glyph that sways, and one line of plain-language
    /// guidance. Replaces the pulsing logo. The sway stops with reduce motion.
    /// </summary>
    /// <remarks>
    /// <para>The overlay fills its parent as a pass-through layer; only the cue takes taps, and only when
    /// <see cref="Dismissible"/>. Pass localised text to <see cref="Show"/>; the English defaults are a fallback.</para>
    /// <para>Props: <c>mode</c>, <c>text</c>, <c>detail</c>, <c>action-text</c>, <c>secondary-action-text</c>,
    /// <c>progress</c>, <c>progress-indeterminate</c>, <c>placement</c>, <c>top-inset</c>, <c>bottom-inset</c>,
    /// <c>dismissible</c>.
    /// Methods: <see cref="Show"/>, <see cref="Hide"/>, <see cref="SetIcon"/> (a gradient disc with an icon, in place of
    /// the device glyph). Events: <see cref="Dismissed"/> (tapped while dismissible), <see cref="ActionInvoked"/> (the
    /// optional action, a compact ghost <see cref="PillButton"/> at the bottom right, such as "Cancel placing"),
    /// <see cref="SecondaryActionInvoked"/> (a second action, a primary pill to the right of the first, such as
    /// "Keep trying"; with both shown they share the row equally).</para>
    /// <para><see cref="Progress"/> draws a thin <see cref="LinearProgress"/> under the detail: 0 to 1, negative hides
    /// it. <see cref="ProgressIndeterminate"/> shows a sweeping segment instead.</para>
    /// <para>USS classes: <c>coachingOverlay</c> (root), <c>coachingOverlay__cue</c>, <c>coachingOverlay__header</c>,
    /// <c>coachingOverlay__visual</c>, <c>coachingOverlay__device</c>, <c>coachingOverlay__ground</c>,
    /// <c>coachingOverlay__disc</c>, <c>coachingOverlay__discIcon</c>, <c>coachingOverlay__text</c>,
    /// <c>coachingOverlay__detail</c>, <c>coachingOverlay__progress</c>, <c>coachingOverlay__actions</c>,
    /// <c>coachingOverlay__action</c>, <c>coachingOverlay__secondaryAction</c>.</para>
    /// <para>Modifier classes: <c>coachingOverlay--shown</c>, <c>coachingOverlay--warn</c> (move slower, too dark),
    /// <c>coachingOverlay--lookAround</c>, <c>--findSurface</c>, <c>--moveSlower</c>, <c>--tooDark</c>,
    /// <c>coachingOverlay--swayed</c> (the far point of the sway), <c>coachingOverlay--card</c> (top or bottom),
    /// <c>coachingOverlay--top</c>, <c>coachingOverlay--bottom</c>, <c>coachingOverlay--hasIcon</c>,
    /// <c>coachingOverlay--twoActions</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-fg</c>, <c>--surface-border</c>, <c>--surface-accent</c>,
    /// <c>--c-warn</c>, <c>--radius-card</c>, <c>--radius-sm</c>, <c>--space-2..5</c>, <c>--text-body</c>,
    /// <c>--font-body-medium</c>, <c>--motion-base</c>, <c>--motion-slow</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class CoachingOverlay : VisualElement
    {
        public const string RootClass = "coachingOverlay";
        public const string CueClass = "coachingOverlay__cue";
        public const string HeaderClass = "coachingOverlay__header";
        public const string VisualClass = "coachingOverlay__visual";
        public const string DiscClass = "coachingOverlay__disc";
        public const string DiscIconClass = "coachingOverlay__discIcon";
        public const string DetailClass = "coachingOverlay__detail";
        public const string ActionsClass = "coachingOverlay__actions";
        public const string ActionClass = "coachingOverlay__action";
        public const string SecondaryActionClass = "coachingOverlay__secondaryAction";
        public const string ProgressClass = "coachingOverlay__progress";
        public const string TwoActionsClass = "coachingOverlay--twoActions";
        public const string TopClass = "coachingOverlay--top";
        public const string BottomClass = "coachingOverlay--bottom";
        public const string CardClass = "coachingOverlay--card";
        public const string HasIconClass = "coachingOverlay--hasIcon";
        public const string DeviceClass = "coachingOverlay__device";
        public const string GroundClass = "coachingOverlay__ground";
        public const string TextClass = "coachingOverlay__text";
        public const string ShownClass = "coachingOverlay--shown";
        public const string WarnClass = "coachingOverlay--warn";
        public const string SwayedClass = "coachingOverlay--swayed";

        /// <summary>
        /// Time between sway phases.
        /// </summary>
        public const int SwayIntervalMs = 900;

        private readonly VisualElement cue;
        private readonly VisualElement visual;
        private readonly VisualElement disc;
        private readonly VisualElement discIcon;
        private readonly Label textLabel;
        private readonly Label detailLabel;
        private readonly VisualElement actions;
        private readonly PillButton actionButton;
        private readonly PillButton secondaryActionButton;
        private readonly LinearProgress progressBar;
        private float progress = -1f;
        private CoachingPlacement placement;
        private CoachingMode mode = CoachingMode.LookAround;
        private string customText;
        private bool dismissible;
        private bool isShown;
        private DisplayedTimer sway;

        /// <summary>
        /// Raised when a dismissible cue is tapped; the overlay hides itself first.
        /// </summary>
        public event Action Dismissed;

        /// <summary>
        /// Raised when the action button is tapped. The overlay stays shown; the owner decides.
        /// </summary>
        public event Action ActionInvoked;

        /// <summary>
        /// Raised when the secondary action button is tapped. The overlay stays shown; the owner decides.
        /// </summary>
        public event Action SecondaryActionInvoked;

        public PillButton ActionButton => actionButton;

        public PillButton SecondaryActionButton => secondaryActionButton;

        public LinearProgress ProgressBar => progressBar;

        /// <summary>
        /// The optional action, a ghost pill, such as "Cancel placing" or "Rescan"; empty hides it.
        /// </summary>
        [UxmlAttribute("action-text")]
        public string ActionText
        {
            get => actionButton.Text;
            set
            {
                actionButton.Text = value ?? string.Empty;
                UpdateActions();
            }
        }

        /// <summary>
        /// A second action, a primary pill to the right of the first, such as "Keep trying"; empty hides it.
        /// </summary>
        [UxmlAttribute("secondary-action-text")]
        public string SecondaryActionText
        {
            get => secondaryActionButton.Text;
            set
            {
                secondaryActionButton.Text = value ?? string.Empty;
                UpdateActions();
            }
        }

        /// <summary>
        /// Progress under the detail, 0 to 1. Negative hides the bar.
        /// </summary>
        [UxmlAttribute("progress")]
        public float Progress
        {
            get => progress;
            set
            {
                progress = float.IsNaN(value) ? -1f : Mathf.Min(value, 1f);
                if (progress >= 0f)
                {
                    progressBar.Value = progress;
                }

                UpdateProgress();
            }
        }

        /// <summary>
        /// Shows the progress bar as a sweeping segment, whatever <see cref="Progress"/> holds.
        /// </summary>
        [UxmlAttribute("progress-indeterminate")]
        public bool ProgressIndeterminate
        {
            get => progressBar.Indeterminate;
            set
            {
                progressBar.Indeterminate = value;
                UpdateProgress();
            }
        }

        public bool IsShown => isShown;

        /// <summary>
        /// Whether the sway is running now (shown with the device glyph, attached, no reduce-motion ancestor).
        /// </summary>
        public bool IsSwaying => isShown && panel != null && !ClassListContains(HasIconClass) && !this.IsReduceMotion();

        [UxmlAttribute("mode")]
        public CoachingMode Mode
        {
            get => mode;
            set
            {
                mode = value;
                ApplyMode();
            }
        }

        /// <summary>
        /// The guidance line; empty uses the English default for the mode.
        /// </summary>
        [UxmlAttribute("text")]
        public string Text
        {
            get => textLabel.text;
            set
            {
                customText = value;
                ApplyMode();
            }
        }

        /// <summary>
        /// A second line under the guidance; empty hides it.
        /// </summary>
        [UxmlAttribute("detail")]
        public string Detail
        {
            get => detailLabel.text;
            set
            {
                detailLabel.text = value ?? string.Empty;
                detailLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("placement")]
        public CoachingPlacement Placement
        {
            get => placement;
            set
            {
                placement = value;
                EnableInClassList(TopClass, placement == CoachingPlacement.Top);
                EnableInClassList(BottomClass, placement == CoachingPlacement.Bottom);
                EnableInClassList(CardClass, placement != CoachingPlacement.Centre);
            }
        }

        /// <summary>
        /// Top safe-area inset, added above the card in the top placement.
        /// </summary>
        [UxmlAttribute("top-inset")]
        public float TopInset
        {
            get => resolvedStyle.paddingTop;
            set => style.paddingTop = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Space kept below the card in the bottom placement, such as the safe area plus a row of actions.
        /// </summary>
        [UxmlAttribute("bottom-inset")]
        public float BottomInset
        {
            get => resolvedStyle.paddingBottom;
            set => style.paddingBottom = Mathf.Max(0f, value);
        }

        [UxmlAttribute("dismissible")]
        public bool Dismissible
        {
            get => dismissible;
            set
            {
                dismissible = value;
                cue.pickingMode = dismissible ? PickingMode.Position : PickingMode.Ignore;
            }
        }

        public CoachingOverlay()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            cue = UIToolkitExtensions.CreateVisualElement(this, CueClass);
            var header = UIToolkitExtensions.CreateVisualElement(cue, HeaderClass);
            header.pickingMode = PickingMode.Ignore;

            visual = UIToolkitExtensions.CreateVisualElement(header, VisualClass);
            visual.pickingMode = PickingMode.Ignore;
            var device = UIToolkitExtensions.CreateVisualElement(visual, DeviceClass);
            device.pickingMode = PickingMode.Ignore;
            var ground = UIToolkitExtensions.CreateVisualElement(visual, GroundClass);
            ground.pickingMode = PickingMode.Ignore;

            disc = UIToolkitExtensions.CreateVisualElement(header, DiscClass);
            disc.pickingMode = PickingMode.Ignore;
            disc.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            discIcon = UIToolkitExtensions.CreateVisualElement(disc, DiscIconClass);
            discIcon.pickingMode = PickingMode.Ignore;

            textLabel = UIToolkitExtensions.CreateVisualElement<Label>(header, TextClass);
            textLabel.pickingMode = PickingMode.Ignore;
            detailLabel = UIToolkitExtensions.CreateVisualElement<Label>(cue, DetailClass);
            detailLabel.pickingMode = PickingMode.Ignore;

            progressBar = new LinearProgress();
            progressBar.AddToClassList(ProgressClass);
            cue.Add(progressBar);

            actions = UIToolkitExtensions.CreateVisualElement(cue, ActionsClass);
            actions.pickingMode = PickingMode.Ignore;
            actionButton = new PillButton { Variant = PillButtonVariant.Ghost, Compact = true };
            actionButton.AddToClassList(ActionClass);
            actionButton.Clicked += () => ActionInvoked?.Invoke();
            actions.Add(actionButton);
            secondaryActionButton = new PillButton { Variant = PillButtonVariant.Primary, Compact = true };
            secondaryActionButton.AddToClassList(SecondaryActionClass);
            secondaryActionButton.Clicked += () => SecondaryActionInvoked?.Invoke();
            actions.Add(secondaryActionButton);

            cue.RegisterCallback<ClickEvent>(OnCueClicked);
            RegisterCallback<AttachToPanelEvent>(_ => { if (isShown) StartSway(); });
            RegisterCallback<DetachFromPanelEvent>(_ => StopSway());

            Dismissible = false;
            Detail = null;
            ActionText = null;
            SecondaryActionText = null;
            Progress = -1f;
            SetIcon(default);
            ApplyMode();
        }

        /// <summary>
        /// Shows an icon in a gradient disc in place of the device glyph; a default value brings the glyph back.
        /// </summary>
        public void SetIcon(Background icon)
        {
            var has = icon.texture != null || icon.sprite != null || icon.vectorImage != null || icon.renderTexture != null;
            discIcon.style.backgroundImage = has ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
            disc.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            visual.style.display = has ? DisplayStyle.None : DisplayStyle.Flex;
            EnableInClassList(HasIconClass, has);
        }

        /// <summary>
        /// Shows the cue for a mode, with localised text when given. A null detail keeps the current one.
        /// </summary>
        public void Show(CoachingMode coachingMode, string text = null, string detail = null)
        {
            mode = coachingMode;
            customText = text;
            if (detail != null)
            {
                Detail = detail;
            }

            ApplyMode();

            isShown = true;
            AddToClassList(ShownClass);
            StartSway();
        }

        public void Hide()
        {
            isShown = false;
            RemoveFromClassList(ShownClass);
            StopSway();
        }

        /// <summary>
        /// The English fallback for a mode.
        /// </summary>
        public static string DefaultText(CoachingMode coachingMode) => coachingMode switch
        {
            CoachingMode.LookAround => "Move your phone slowly to look around",
            CoachingMode.FindSurface => "Point your phone at the floor or a table",
            CoachingMode.MoveSlower => "Moving too fast. Slow down a little.",
            CoachingMode.TooDark => "It is too dark here. Find more light.",
            _ => string.Empty,
        };

        private void ApplyMode()
        {
            foreach (CoachingMode value in Enum.GetValues(typeof(CoachingMode)))
            {
                var name = value.ToString();
                EnableInClassList($"{RootClass}--{char.ToLowerInvariant(name[0])}{name.Substring(1)}", value == mode);
            }

            EnableInClassList(WarnClass, mode is CoachingMode.MoveSlower or CoachingMode.TooDark);
            textLabel.text = string.IsNullOrEmpty(customText) ? DefaultText(mode) : customText;
        }

        private void UpdateActions()
        {
            var primary = !string.IsNullOrEmpty(actionButton.Text);
            var secondary = !string.IsNullOrEmpty(secondaryActionButton.Text);
            actionButton.style.display = primary ? DisplayStyle.Flex : DisplayStyle.None;
            secondaryActionButton.style.display = secondary ? DisplayStyle.Flex : DisplayStyle.None;
            actions.style.display = primary || secondary ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(TwoActionsClass, primary && secondary);
        }

        private void UpdateProgress()
        {
            progressBar.style.display = progress >= 0f || progressBar.Indeterminate ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnCueClicked(ClickEvent evt)
        {
            if (!dismissible || !isShown)
            {
                return;
            }

            evt.StopPropagation();
            Hide();
            Dismissed?.Invoke();
        }

        private void StartSway()
        {
            StopSway();
            sway = DisplayedTimer.Start(this, () =>
            {
                if (IsSwaying)
                {
                    ToggleInClassList(SwayedClass);
                }
                else
                {
                    RemoveFromClassList(SwayedClass);
                }
            }, SwayIntervalMs, SwayIntervalMs);
        }

        private void StopSway()
        {
            sway?.Stop();
            sway = null;
            RemoveFromClassList(SwayedClass);
        }
    }
}
