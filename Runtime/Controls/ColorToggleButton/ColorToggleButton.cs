/// Credit SimonDarksideJ

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A <see cref="ToggleButton"/> variant with a tint color, ripple animation on selection,
    /// and a selected-state overlay. Extends the base toggle with color-coded visual feedback.
    /// </summary>
    public class ColorToggleButton : ToggleButton
    {
        public const string IconClass = "toggleButton__icon";
        public const string RippleOverlayClass = "toggleButton__ripple";
        public const string RippleOverlayVisibleClass = "toggleButton__ripple--visible";
        public const string RippleOverlaySecondaryClass = "toggleButton__rippleSecondary";
        public const string RippleOverlaySecondaryVisibleClass = "toggleButton__rippleSecondary--visible";
        public const string SelectedOverlayClass = "toggleButton__selectedOverlay";
        public const string SelectedOverlayVisibleClass = "toggleButton__selectedOverlay--visible";

        private Color tintColor;
        private Color selectedTintColor;
        private readonly VisualElement rippleOverlay;
        private readonly VisualElement rippleOverlaySecondary;
        private readonly VisualElement selectedOverlay;

        public Color TintColor => tintColor;
        public Color SelectedTintColor => selectedTintColor;

        public ColorToggleButton(Color tintColor) : this(tintColor, tintColor) { }

        public ColorToggleButton(Color tintColor, Color selectedTintColor)
        {
            image.AddToClassList(IconClass);

            rippleOverlay = UIToolkitExtensions.CreateVisualElement(this, RippleOverlayClass);
            rippleOverlay.pickingMode = PickingMode.Ignore;

            rippleOverlaySecondary = UIToolkitExtensions.CreateVisualElement(this, RippleOverlaySecondaryClass);
            rippleOverlaySecondary.pickingMode = PickingMode.Ignore;

            selectedOverlay = UIToolkitExtensions.CreateVisualElement(this, SelectedOverlayClass);
            selectedOverlay.pickingMode = PickingMode.Ignore;

            SetTintColor(tintColor);
            SetSelectedTintColor(selectedTintColor);
        }

        public void SetTintColor(Color color)
        {
            tintColor = color;
            image.style.unityBackgroundImageTintColor = color;
        }

        public void SetSelectedTintColor(Color color)
        {
            selectedTintColor = color;
            selectedOverlay.style.unityBackgroundImageTintColor = color;
            rippleOverlay.style.unityBackgroundImageTintColor = color;
            rippleOverlaySecondary.style.unityBackgroundImageTintColor = color;
        }

        protected override void ApplySelectionState(bool selected)
        {
            base.ApplySelectionState(selected);
            if (selected)
            {
                selectedOverlay.AddToClassList(SelectedOverlayVisibleClass);
                PlayRipple();
            }
            else
            {
                selectedOverlay.RemoveFromClassList(SelectedOverlayVisibleClass);
                ResetRipple();
            }
        }

        private void PlayRipple()
        {
            rippleOverlay.AddToClassList(RippleOverlayVisibleClass);
            rippleOverlaySecondary.AddToClassList(RippleOverlaySecondaryVisibleClass);

            PlayRippleWave(rippleOverlay, 1f, 5f, 1500);
            PlayRippleWave(rippleOverlaySecondary, 0.35f, 3.2f, 1500);
        }

        private void ResetRipple()
        {
            rippleOverlay.RemoveFromClassList(RippleOverlayVisibleClass);
            rippleOverlaySecondary.RemoveFromClassList(RippleOverlaySecondaryVisibleClass);
            rippleOverlay.style.opacity = 0f;
            rippleOverlay.style.scale = new Scale(Vector3.one);
            rippleOverlaySecondary.style.opacity = 0f;
            rippleOverlaySecondary.style.scale = new Scale(Vector3.one);
        }

        private void PlayRippleWave(VisualElement ripple, float startOpacity, float endScale, int durationMs)
        {
            ripple.style.transitionProperty = null;
            ripple.style.transitionDuration = null;
            ripple.style.transitionTimingFunction = null;
            ripple.style.scale = new Scale(Vector3.one);
            ripple.style.opacity = startOpacity;

            var properties = new List<StylePropertyName>
            {
                new("scale"),
                new("opacity")
            };

            ripple.style.transitionProperty = new StyleList<StylePropertyName>(properties);
            ripple.style.transitionDuration = new List<TimeValue>
            {
                new(durationMs, TimeUnit.Millisecond),
                new(durationMs, TimeUnit.Millisecond)
            };
            ripple.style.transitionTimingFunction = new StyleList<EasingFunction>(
                new List<EasingFunction>
                {
                    new(EasingMode.EaseOut),
                    new(EasingMode.EaseOut)
                });

            ripple.schedule.Execute(() =>
            {
                ripple.style.scale = new Scale(new Vector3(endScale, endScale, 1f));
                ripple.style.opacity = 0f;
            }).StartingIn(1);
        }
    }
}
