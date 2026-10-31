/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// How a <see cref="PillButton"/> is styled.
    /// </summary>
    public enum PillButtonVariant
    {
        /// <summary>
        /// The inner and outer colour gradient (the original behaviour).
        /// </summary>
        Custom,

        /// <summary>
        /// The brand gradient from ThemeTokens; the main call to action.
        /// </summary>
        Primary,

        /// <summary>
        /// Transparent with a strong border.
        /// </summary>
        Ghost,

        /// <summary>
        /// Ghost in the warning colour.
        /// </summary>
        Warn,

        /// <summary>
        /// Filled with the danger gradient; a destructive or ending action, such as "Stop and save".
        /// </summary>
        Danger,
    }

    /// <summary>
    /// A pill-shaped button with a horizontal gradient background driven by two hex colors,
    /// a brief flash feedback animation on click, and a centered text label.
    /// </summary>
    /// <remarks>
    /// <para>Does not use the inbuilt Unity Button class, as this creates aberrant style issues.  A cleaner approach for just a clickable surface.
    /// ALso supports a colour gradient between an inner and outer colour.</para>
    /// <para>Variants (<see cref="Variant"/>) style the button from ThemeTokens instead of the inner and outer colours:
    /// <c>pillButton--primary</c> (brand gradient fill), <c>pillButton--ghost</c> (transparent with a strong border),
    /// <c>pillButton--warn</c> (ghost in the warning colour), <c>pillButton--danger</c> (danger gradient fill).
    /// <see cref="Compact"/> adds <c>pillButton--compact</c>
    /// (46 px instead of 52 px, smaller label). A variant button disabled with <c>SetEnabled(false)</c> becomes a
    /// half-strength outline with muted text. <see cref="SetTextColor"/> and <see cref="SetFontSize"/> set inline
    /// styles and win over a variant.</para>
    /// <para><see cref="SetIcon"/> adds a 20 px icon before the label (<c>pillButton__icon</c>, modifier
    /// <c>pillButton--hasIcon</c>); the icon and label are centred together. The token variants tint the icon to
    /// match the label; <see cref="SetTextColor"/> tints it too.</para>
    /// <para>Tokens read by the variants: <c>--grad-brand-start/end</c> and <c>--grad-danger-start/end</c> (through
    /// <see cref="GradientFill"/>), <c>--c-on-primary</c>, <c>--c-on-danger</c>, <c>--c-warn</c>, <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-border</c>,
    /// <c>--surface-border-strong</c>, <c>--radius-pill</c>, <c>--space-4</c>, <c>--text-label</c>,
    /// <c>--text-label-sm</c>, <c>--font-body-medium</c>, <c>--font-body-semibold</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class PillButton : VisualElement
    {
        public const string RootClass = "pillButton";
        public const string BackgroundClass = "pillButton__background";
        public const string FillClass = "pillButton__fill";
        public const string FlashClass = "pillButton__flash";
        public const string FlashActiveClass = "pillButton__flash--active";
        public const string LabelClass = "pillButton__label";
        public const string IconClass = "pillButton__icon";
        public const string HasIconClass = "pillButton--hasIcon";
        public const string PrimaryClass = "pillButton--primary";
        public const string GhostClass = "pillButton--ghost";
        public const string WarnClass = "pillButton--warn";
        public const string DangerClass = "pillButton--danger";
        public const string CompactClass = "pillButton--compact";

        private const int FlashDurationMs = 180;
        private const int GradientTextureWidth = 256;

        private readonly VisualElement background;
        private readonly VisualElement flashOverlay;
        private readonly VisualElement icon;
        private readonly Label label;
        private Color innerColor = Color.white;
        private Color outerColor = Color.white;
        private Texture2D backgroundTexture;
        private IVisualElementScheduledItem flashStartTimer;
        private IVisualElementScheduledItem flashClearTimer;
        private bool applyFlash = true;
        private PillButtonVariant variant = PillButtonVariant.Custom;
        private bool compact;
        private VisualElement fill;

        public event Action Clicked;

        public PillButton()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            background = UIToolkitExtensions.CreateVisualElement(this, BackgroundClass);

            flashOverlay = UIToolkitExtensions.CreateVisualElement(background, FlashClass);
            flashOverlay.pickingMode = PickingMode.Ignore;

            icon = UIToolkitExtensions.CreateVisualElement(background, IconClass);
            icon.pickingMode = PickingMode.Ignore;

            label = UIToolkitExtensions.CreateVisualElement<Label>(background, LabelClass);

            background.RegisterCallback<ClickEvent>(OnClicked);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            SetIcon(default);
            UpdateGradientBackground();
        }

        public VisualElement Icon => icon;

        /// <summary>
        /// Sets an icon before the label from a texture, sprite or vector image; a default value removes it.
        /// </summary>
        public void SetIcon(Background image)
        {
            var has = image.texture != null || image.sprite != null || image.vectorImage != null || image.renderTexture != null;
            icon.style.backgroundImage = has ? new StyleBackground(image) : new StyleBackground(StyleKeyword.None);
            icon.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(HasIconClass, has);
        }

        /// <summary>
        /// Selects a token-styled look. <see cref="PillButtonVariant.Custom"/> (the default) keeps the
        /// inner and outer colour gradient.
        /// </summary>
        [UxmlAttribute("variant")]
        public PillButtonVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                ApplyVariant();
            }
        }

        /// <summary>
        /// The shorter variant height with a smaller label.
        /// </summary>
        [UxmlAttribute("compact")]
        public bool Compact
        {
            get => compact;
            set
            {
                compact = value;
                EnableInClassList(CompactClass, compact);
            }
        }

        [UxmlAttribute("text")]
        public string Text
        {
            get => label.text;
            set => label.text = value;
        }

        [UxmlAttribute("apply-flash")]
        public bool ApplyFlash
        {
            get => applyFlash;
            set => applyFlash = value;
        }   

        [UxmlAttribute("inner-color")]
        public Color InnerColor
        {
            get => innerColor;
            set
            {
                innerColor = value;
                UpdateGradientBackground();
            }
        }

        [UxmlAttribute("outer-color")]
        public Color OuterColor
        {
            get => outerColor;
            set
            {
                outerColor = value;
                UpdateGradientBackground();
            }
        }

        public void SetInnerColor(string colorHex)
        {
            InnerColor = ColorUtility.TryParseHtmlString(colorHex, out var parsed) ? parsed : Color.white;
        }

        public void SetOuterColor(string colorHex)
        {
            OuterColor = ColorUtility.TryParseHtmlString(colorHex, out var parsed) ? parsed : Color.white;
        }

        public void SetTextColor(Color color)
        {
            label.style.color = color;
            icon.style.unityBackgroundImageTintColor = color;
        }

        public void SetFontSize(float fontSize)
        {
            label.style.fontSize = fontSize;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // The texture is released on detach; a re-attached custom button needs it back.
            if (variant == PillButtonVariant.Custom && backgroundTexture == null)
            {
                UpdateGradientBackground();
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            StopFlashTimers();
            DestroyBackgroundTexture();
        }

        private void ApplyVariant()
        {
            EnableInClassList(PrimaryClass, variant == PillButtonVariant.Primary);
            EnableInClassList(GhostClass, variant == PillButtonVariant.Ghost);
            EnableInClassList(WarnClass, variant == PillButtonVariant.Warn);
            EnableInClassList(DangerClass, variant == PillButtonVariant.Danger);

            if (variant == PillButtonVariant.Custom)
            {
                fill?.RemoveFromHierarchy();
                fill = null;
                background.ClearPill();
                UpdateGradientBackground();
                return;
            }

            this.UseThemeTokens();
            background.MakePill();
            DestroyBackgroundTexture();
            background.style.backgroundImage = StyleKeyword.Null;

            if (variant is PillButtonVariant.Primary or PillButtonVariant.Danger)
            {
                if (fill == null)
                {
                    fill = UIToolkitExtensions.CreateVisualElement(FillClass);
                    fill.pickingMode = PickingMode.Ignore;
                    fill.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
                    fill.MakePill();
                    background.Insert(0, fill);
                }
            }
            else
            {
                fill?.RemoveFromHierarchy();
                fill = null;
            }
        }

        private void OnClicked(ClickEvent evt)
        {
            if(applyFlash)
            {
                PlayFlash();
            }
            Clicked?.Invoke();
        }

        private void PlayFlash()
        {
            StopFlashTimers();
            flashOverlay.RemoveFromClassList(FlashActiveClass);

            flashStartTimer = flashOverlay.schedule.Execute(() =>
            {
                flashStartTimer = null;
                flashOverlay.AddToClassList(FlashActiveClass);

                flashClearTimer = flashOverlay.schedule.Execute(() =>
                {
                    flashClearTimer = null;
                    flashOverlay.RemoveFromClassList(FlashActiveClass);
                }).StartingIn(FlashDurationMs);
            }).StartingIn(0);
        }

        private void StopFlashTimers()
        {
            flashStartTimer?.Pause();
            flashStartTimer = null;

            flashClearTimer?.Pause();
            flashClearTimer = null;

            flashOverlay.RemoveFromClassList(FlashActiveClass);
        }

        private void UpdateGradientBackground()
        {
            if (variant != PillButtonVariant.Custom)
            {
                return;
            }

            DestroyBackgroundTexture();
            backgroundTexture = CreateGradientTexture();
            background.style.backgroundImage = new StyleBackground(backgroundTexture);
        }

        private void DestroyBackgroundTexture()
        {
            if (backgroundTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(backgroundTexture);
                backgroundTexture = null;
            }
        }

        private Texture2D CreateGradientTexture()
        {
            return ProceduralTextureUtility.CreateHorizontalGradient(innerColor, outerColor, GradientTextureWidth);
        }
    }
}