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
    public class PillButton : VisualElement
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
        private string innerColorHex = "#FFFFFF";
        private string outerColorHex = "#FFFFFF";
        private Texture2D backgroundTexture;
        private IVisualElementScheduledItem flashStartTimer;
        private IVisualElementScheduledItem flashClearTimer;

        public event Action Clicked;

        public PillButton()
        {
            AddToClassList(RootClass);

            background = UIToolkitExtensions.CreateVisualElement(this, BackgroundClass);

            flashOverlay = UIToolkitExtensions.CreateVisualElement(background, FlashClass);
            flashOverlay.pickingMode = PickingMode.Ignore;

            label = UIToolkitExtensions.CreateVisualElement<Label>(background, LabelClass);

            background.RegisterCallback<ClickEvent>(OnClicked);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            UpdateGradientBackground();
        }

        /// <summary>Gets or sets the button label text.</summary>
        public string Text
        {
            get => label.text;
            set => label.text = value;
        }

        /// <summary>Sets the gradient start color (left side) from a hex string, e.g. "#4A90E2".</summary>
        public void SetInnerColor(string colorHex)
        {
            innerColorHex = colorHex;
            UpdateGradientBackground();
        }

        /// <summary>Sets the gradient end color (right side) from a hex string, e.g. "#7B68EE".</summary>
        public void SetOuterColor(string colorHex)
        {
            outerColorHex = colorHex;
            UpdateGradientBackground();
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
            PlayFlash();
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
                UnityEngine.Object.Destroy(backgroundTexture);
                backgroundTexture = null;
            }
        }

        private Texture2D CreateGradientTexture()
        {
            Color innerColor = ColorUtility.TryParseHtmlString(innerColorHex, out Color parsedInner) ? parsedInner : Color.white;
            Color outerColor = ColorUtility.TryParseHtmlString(outerColorHex, out Color parsedOuter) ? parsedOuter : Color.white;
            return ProceduralTextureUtility.CreateHorizontalGradient(innerColor, outerColor, GradientTextureWidth);
        }
    }
}
