/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A pill-shaped button with a horizontal gradient background driven by two hex colors,
    /// a brief flash feedback animation on click, and a centered text label.
    /// </summary>
    /// <remarks>
    /// Does not use the inbuilt Unity Button class, as this creates aberrant style issues.  A cleaner approach for just a clickable surface.
    /// ALso supports a colour gradient between an inner and outer colour.
    /// </remarks>
    [UxmlElement]
    public partial class PillButton : VisualElement
    {
        public const string RootClass = "pillButton";
        public const string BackgroundClass = "pillButton__background";
        public const string FlashClass = "pillButton__flash";
        public const string FlashActiveClass = "pillButton__flash--active";
        public const string LabelClass = "pillButton__label";

        private const int FlashDurationMs = 180;
        private const int GradientTextureWidth = 256;

        private readonly VisualElement background;
        private readonly VisualElement flashOverlay;
        private readonly Label label;
        private Color innerColor = Color.white;
        private Color outerColor = Color.white;
        private Texture2D backgroundTexture;
        private IVisualElementScheduledItem flashStartTimer;
        private IVisualElementScheduledItem flashClearTimer;
        private bool applyFlash = true;

        public event Action Clicked;

        public PillButton()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            background = UIToolkitExtensions.CreateVisualElement(this, BackgroundClass);

            flashOverlay = UIToolkitExtensions.CreateVisualElement(background, FlashClass);
            flashOverlay.pickingMode = PickingMode.Ignore;

            label = UIToolkitExtensions.CreateVisualElement<Label>(background, LabelClass);

            background.RegisterCallback<ClickEvent>(OnClicked);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            UpdateGradientBackground();
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
        }

        public void SetFontSize(float fontSize)
        {
            label.style.fontSize = fontSize;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            StopFlashTimers();
            DestroyBackgroundTexture();
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