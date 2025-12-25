/// Credit SimonDarksideJ  

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A segmented stepper-like control split into equal "quadrants" (segments).
    /// One segment can be active at a time; a colored overlay slides between segments.
    ///
    /// Styling is driven via USS classes (see ApplicationStyles.uss).
    /// </summary>
    public class QuadrantStepper : VisualElement
    {
        public const string RootClass = "quadrantStepper";
        public const string OverlayClass = "quadrantStepper__overlay";
        public const string SegmentsClass = "quadrantStepper__segments";
        public const string SegmentClass = "quadrantStepper__segment";
        public const string LabelClass = "quadrantStepper__label";
        public const string SelectedClass = "is-selected";

        private readonly VisualElement overlay;
        private readonly VisualElement segmentsRoot;

        private readonly List<VisualElement> segments = new();
        private readonly List<Label> labels = new();

        private int selectedIndex = -1;

        private ValueAnimation<float> overlayAnimation;
        private float overlayLeft;

        // Horizontal-only shrink: 2.5% inset per side (5% total width reduction).
        private const float OverlayInsetPercentPerSideX = 0.015f;

        private const int DefaultOverlayAnimationDurationMs = 170;
        private static readonly CustomStyleProperty<int> OverlayAnimationDurationMsProperty =
            new("--quadrantStepper-animation-duration-ms");

        private int overlayAnimationDurationMs = DefaultOverlayAnimationDurationMs;

        public event Action<int, string> SelectionChanged;

        public int SelectedIndex => selectedIndex;

        public string SelectedText
        {
            get
            {
                if (selectedIndex < 0 || selectedIndex >= labels.Count)
                {
                    return string.Empty;
                }

                return labels[selectedIndex].text;
            }
        }

        public QuadrantStepper()
            : this(new[] { "One", "Two", "Three", "Four" })
        {
        }

        public QuadrantStepper(IReadOnlyList<string> options)
        {
            AddToClassList(RootClass);

            overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            overlay.AddToClassList(OverlayClass);

            segmentsRoot = new VisualElement();
            segmentsRoot.AddToClassList(SegmentsClass);

            hierarchy.Add(overlay);
            hierarchy.Add(segmentsRoot);

            SetOptions(options);

            RegisterCallback<GeometryChangedEvent>(_ => UpdateOverlayImmediate());
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(OverlayAnimationDurationMsProperty, out var durationMs))
            {
                if (durationMs < 0)
                {
                    durationMs = 0;
                }

                overlayAnimationDurationMs = durationMs;
            }
            else
            {
                overlayAnimationDurationMs = DefaultOverlayAnimationDurationMs;
            }
        }

        public void SetOptions(IReadOnlyList<string> options)
        {
            segmentsRoot.Clear();
            segments.Clear();
            labels.Clear();

            if (options == null || options.Count == 0)
            {
                SetSelectedIndexInternal(-1, notify: false, animate: false);
                return;
            }

            for (var i = 0; i < options.Count; i++)
            {
                var index = i;

                var segment = new VisualElement();
                segment.AddToClassList(SegmentClass);
                segment.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    SetSelectedIndexInternal(index, notify: true, animate: true);
                });

                var label = new Label(options[i] ?? string.Empty);
                label.AddToClassList(LabelClass);

                segment.Add(label);
                segmentsRoot.Add(segment);

                segments.Add(segment);
                labels.Add(label);
            }

            // Keep selection if possible, otherwise select first segment.
            var nextIndex = selectedIndex;
            if (nextIndex < 0 || nextIndex >= options.Count)
            {
                nextIndex = 0;
            }

            SetSelectedIndexInternal(nextIndex, notify: false, animate: false);
            UpdateOverlayImmediate();
        }

        public void SetOptions(IReadOnlyList<string> options, int defaultSelectedIndex)
        {
            SetOptions(options);

            if (options == null || options.Count == 0)
            {
                return;
            }

            var clampedIndex = defaultSelectedIndex;
            if (clampedIndex < -1)
            {
                clampedIndex = -1;
            }
            else if (clampedIndex >= options.Count)
            {
                clampedIndex = options.Count - 1;
            }

            SetSelectedIndexInternal(clampedIndex, notify: false, animate: false);
        }

        public bool SetOptions(IReadOnlyList<string> options, string defaultSelectedText)
        {
            SetOptions(options);

            if (options == null || options.Count == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(defaultSelectedText))
            {
                return false;
            }

            return TrySetSelectedText(defaultSelectedText, notify: false, animate: false);
        }

        public void SetSelectedIndex(int index)
        {
            SetSelectedIndexInternal(index, notify: false, animate: false);
        }

        public void SetSelectedIndex(int index, bool notify, bool animate)
        {
            SetSelectedIndexInternal(index, notify: notify, animate: animate);
        }

        public bool TrySetSelectedText(string text, bool notify = false, bool animate = false)
        {
            if (labels.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < labels.Count; i++)
            {
                if (string.Equals(labels[i].text, text, StringComparison.Ordinal))
                {
                    SetSelectedIndexInternal(i, notify: notify, animate: animate);
                    return true;
                }
            }

            return false;
        }

        public void ForceUnselect()
        {
            SetSelectedIndexInternal(-1, notify: false, animate: false);
        }

        private void StopOverlayAnimationIfAny()
        {
            if (overlayAnimation == null)
            {
                return;
            }

            try
            {
                overlayAnimation.Stop();
            }
            catch (InvalidOperationException)
            {
                // UI Toolkit animations are pooled/recycled; Stop() can throw if the object
                // was already recycled even though we still hold a reference.
            }
            finally
            {
                overlayAnimation = null;
            }
        }

        private void SetSelectedIndexInternal(int index, bool notify, bool animate)
        {
            if (index < -1 || index >= segments.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (selectedIndex == index)
            {
                return;
            }

            selectedIndex = index;
            UpdateSelectedClasses();

            if (selectedIndex >= 0)
            {
                if (animate)
                {
                    AnimateOverlayToIndex(selectedIndex);
                }
                else
                {
                    UpdateOverlayImmediate();
                }

                if (notify)
                {
                    SelectionChanged?.Invoke(selectedIndex, SelectedText);
                }
            }
            else
            {
                // Unselected state: keep overlay at 0 width to visually hide it.
                StopOverlayAnimationIfAny();
                overlay.style.width = 0;
                overlay.style.left = 0;
                overlayLeft = 0;

                if (notify)
                {
                    SelectionChanged?.Invoke(-1, string.Empty);
                }
            }
        }

        private void UpdateSelectedClasses()
        {
            for (var i = 0; i < segments.Count; i++)
            {
                var isSelected = i == selectedIndex;
                segments[i].EnableInClassList(SelectedClass, isSelected);
                labels[i].EnableInClassList(SelectedClass, isSelected);
            }
        }

        private void UpdateOverlayImmediate()
        {
            if (selectedIndex < 0 || segments.Count == 0)
            {
                return;
            }

            var width = resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            var segmentWidth = width / segments.Count;
            var insetX = segmentWidth * OverlayInsetPercentPerSideX;

            var targetLeft = (selectedIndex * segmentWidth) + insetX;
            var targetWidth = segmentWidth - (2f * insetX);

            if (targetWidth < 0f)
            {
                targetWidth = 0f;
            }

            overlay.style.width = targetWidth;
            overlay.style.left = targetLeft;
            overlayLeft = targetLeft;
        }

        private void AnimateOverlayToIndex(int index)
        {
            var width = resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f || segments.Count == 0)
            {
                UpdateOverlayImmediate();
                return;
            }

            var segmentWidth = width / segments.Count;
            var insetX = segmentWidth * OverlayInsetPercentPerSideX;

            var toLeft = (index * segmentWidth) + insetX;
            var targetWidth = segmentWidth - (2f * insetX);

            if (targetWidth < 0f)
            {
                targetWidth = 0f;
            }

            overlay.style.width = targetWidth;

            StopOverlayAnimationIfAny();

            if (overlayAnimationDurationMs <= 0)
            {
                UpdateOverlayImmediate();
                return;
            }

            // Smooth but snappy: ease-out power curve.
            overlayAnimation = overlay.experimental.animation.Start(overlayLeft, toLeft, overlayAnimationDurationMs, (e, value) =>
            {
                overlayLeft = value;
                overlay.style.left = value;
            });

            // Keep a strong reference without Unity recycling the animation object
            // while this element still holds onto it.
            overlayAnimation.KeepAlive();

            overlayAnimation.easingCurve = Easing.OutCubic;
        }
    }
}