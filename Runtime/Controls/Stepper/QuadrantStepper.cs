/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// How the segments of a <see cref="QuadrantStepper"/> share the control's width.
    /// </summary>
    public enum SegmentSizing
    {
        /// <summary>
        /// Every segment gets the same width (the original behaviour).
        /// </summary>
        Equal,

        /// <summary>
        /// Each segment is as wide as its content; the row scrolls when it is wider than the control.
        /// </summary>
        Content,
    }

    /// <summary>
    /// A segmented stepper-like control split into "quadrants" (segments).
    /// One segment can be active at a time; a colored overlay slides between segments.
    /// </summary>
    /// <remarks>
    /// <para>Segments can carry an icon (<see cref="SetSegmentIcon"/>), can be disabled
    /// (<see cref="SetSegmentEnabled"/>) and can size to their content (<see cref="Sizing"/>).
    /// The control is focusable: Left/Right (or gamepad navigation) move to the previous or next
    /// enabled segment, Home/End to the first or last.</para>
    /// <para>USS classes: <c>quadrantStepper</c> (root), <c>quadrantStepper__overlay</c>,
    /// <c>quadrantStepper__segments</c>, <c>quadrantStepper__segment</c>, <c>quadrantStepper__icon</c>,
    /// <c>quadrantStepper__label</c>.</para>
    /// <para>Modifier classes: <c>is-selected</c> (selected segment, its icon and label),
    /// <c>quadrantStepper__segment--disabled</c>, <c>quadrantStepper__segment--hasIcon</c>,
    /// <c>quadrantStepper--content</c> (content sizing), <c>quadrantStepper--scrollable</c>
    /// (content sizing and wider than the control).</para>
    /// </remarks>
    [UxmlElement]
    public partial class QuadrantStepper : VisualElement
    {
        public const string RootClass = "quadrantStepper";
        public const string OverlayClass = "quadrantStepper__overlay";
        public const string SegmentsClass = "quadrantStepper__segments";
        public const string SegmentClass = "quadrantStepper__segment";
        public const string IconClass = "quadrantStepper__icon";
        public const string LabelClass = "quadrantStepper__label";
        public const string SelectedClass = "is-selected";
        public const string SegmentDisabledClass = "quadrantStepper__segment--disabled";
        public const string SegmentHasIconClass = "quadrantStepper__segment--hasIcon";
        public const string ContentSizingClass = "quadrantStepper--content";
        public const string ScrollableClass = "quadrantStepper--scrollable";

        private readonly VisualElement overlay;
        private readonly VisualElement segmentsRoot;

        private readonly List<VisualElement> segments = new();
        private readonly List<Label> labels = new();
        private readonly List<VisualElement> icons = new();

        private int selectedIndex = -1;
        private SegmentSizing sizing = SegmentSizing.Equal;

        private ValueAnimation<float> overlayAnimation;
        private float overlayLeft;

        // Content sizing: horizontal scroll offset shared by the overlay and the segment row.
        private const float DragSlop = 8f;
        private const float WheelStep = 20f;
        private float scrollOffset;
        private int dragPointerId = PointerId.invalidPointerId;
        private float dragStartX;
        private float dragStartOffset;
        private bool isDragging;
        private bool suppressNextClick;

        // Horizontal-only shrink: 2.5% inset per side (5% total width reduction).
        private const float OverlayInsetPercentPerSideX = 0.015f;

        private const int DefaultOverlayAnimationDurationMs = 170;
        private static readonly CustomStyleProperty<int> OverlayAnimationDurationMsProperty =
            new("--quadrantStepper-animation-duration-ms");

        private int overlayAnimationDurationMs = DefaultOverlayAnimationDurationMs;

        public event Action<int, string> SelectionChanged;

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

        public int SegmentCount => segments.Count;

        [UxmlAttribute("options")]
        public List<string> Options
        {
            get
            {
                var result = new List<string>(labels.Count);
                foreach (var label in labels)
                {
                    result.Add(label.text);
                }
                return result;
            }
            set => SetOptions(value ?? new List<string>());
        }

        [UxmlAttribute("sizing")]
        public SegmentSizing Sizing
        {
            get => sizing;
            set
            {
                sizing = value;
                EnableInClassList(ContentSizingClass, sizing == SegmentSizing.Content);
                SetScrollOffset(0f);
                schedule.Execute(UpdateLayoutState);
            }
        }

        /// <summary>
        /// The selected segment, or -1 for none. Setting it does not raise <see cref="SelectionChanged"/>;
        /// out-of-range values are clamped.
        /// </summary>
        [UxmlAttribute("selected-index")]
        public int SelectedIndex
        {
            get => selectedIndex;
            set => SetSelectedIndexInternal(Mathf.Clamp(value, -1, segments.Count - 1), notify: false, animate: false);
        }

        public QuadrantStepper()
            : this(new[] { "One", "Two", "Three", "Four" })
        { }

        public QuadrantStepper(IReadOnlyList<string> options)
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            focusable = true;
            tabIndex = 0;

            overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            overlay.AddToClassList(OverlayClass);

            segmentsRoot = new VisualElement();
            segmentsRoot.AddToClassList(SegmentsClass);

            hierarchy.Add(overlay);
            hierarchy.Add(segmentsRoot);

            SetOptions(options);

            RegisterCallback<GeometryChangedEvent>(_ => UpdateLayoutState());
            segmentsRoot.RegisterCallback<GeometryChangedEvent>(_ => UpdateLayoutState());
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(_ => EndDrag());
            RegisterCallback<WheelEvent>(OnWheel);
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

            if (this.IsReduceMotion())
            {
                overlayAnimationDurationMs = 0;
            }
        }

        public void SetOptions(IReadOnlyList<string> options)
        {
            var previousSelectedIndex = selectedIndex;

            segmentsRoot.Clear();
            segments.Clear();
            labels.Clear();
            icons.Clear();

            selectedIndex = -1;

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
                    if (suppressNextClick)
                    {
                        suppressNextClick = false;
                        return;
                    }

                    if (IsSegmentEnabled(index))
                    {
                        SetSelectedIndexInternal(index, notify: true, animate: true);
                    }
                });

                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList(IconClass);
                icon.style.display = DisplayStyle.None;

                var label = new Label(options[i] ?? string.Empty) { pickingMode = PickingMode.Ignore };
                label.AddToClassList(LabelClass);

                segment.Add(icon);
                segment.Add(label);
                segmentsRoot.Add(segment);

                segments.Add(segment);
                labels.Add(label);
                icons.Add(icon);
            }

            var nextIndex = previousSelectedIndex;
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

        /// <summary>
        /// Shows an icon before the segment's label. Pass a default <see cref="Background"/> to remove it.
        /// The icon is tinted with the label colour.
        /// </summary>
        public void SetSegmentIcon(int index, Background icon)
        {
            ValidateIndex(index);

            var hasIcon = icon.texture != null || icon.sprite != null || icon.vectorImage != null || icon.renderTexture != null;
            icons[index].style.backgroundImage = hasIcon ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
            icons[index].style.display = hasIcon ? DisplayStyle.Flex : DisplayStyle.None;
            segments[index].EnableInClassList(SegmentHasIconClass, hasIcon);
        }

        /// <summary>
        /// Enables or disables one segment. A disabled segment ignores taps and is skipped by keyboard
        /// navigation; it can still be selected from code.
        /// </summary>
        public void SetSegmentEnabled(int index, bool enabled)
        {
            ValidateIndex(index);

            segments[index].SetEnabled(enabled);
            segments[index].EnableInClassList(SegmentDisabledClass, !enabled);
        }

        public bool IsSegmentEnabled(int index)
        {
            return index >= 0 && index < segments.Count && segments[index].enabledSelf;
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

        /// <summary>
        /// Selects the next enabled segment, as the Right key does. Returns false at the end.
        /// </summary>
        public bool SelectNext(bool notify = true, bool animate = true) => Step(+1, notify, animate);

        /// <summary>
        /// Selects the previous enabled segment, as the Left key does. Returns false at the start.
        /// </summary>
        public bool SelectPrevious(bool notify = true, bool animate = true) => Step(-1, notify, animate);

        private bool Step(int direction, bool notify, bool animate)
        {
            var start = selectedIndex < 0 ? (direction > 0 ? -1 : segments.Count) : selectedIndex;
            for (var i = start + direction; i >= 0 && i < segments.Count; i += direction)
            {
                if (IsSegmentEnabled(i))
                {
                    SetSelectedIndexInternal(i, notify, animate);
                    return true;
                }
            }

            return false;
        }

        private bool SelectEdge(bool first)
        {
            for (var n = 0; n < segments.Count; n++)
            {
                var i = first ? n : segments.Count - 1 - n;
                if (IsSegmentEnabled(i))
                {
                    var changed = i != selectedIndex;
                    SetSelectedIndexInternal(i, notify: true, animate: true);
                    return changed;
                }
            }

            return false;
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            var handled = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => SelectPrevious(),
                NavigationMoveEvent.Direction.Right => SelectNext(),
                _ => false,
            };

            if (handled)
            {
                evt.StopPropagation();
                focusController?.IgnoreEvent(evt);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            var handled = evt.keyCode switch
            {
                KeyCode.Home => SelectEdge(first: true),
                KeyCode.End => SelectEdge(first: false),
                _ => false,
            };

            if (handled)
            {
                evt.StopPropagation();
            }
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= segments.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
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
                // UI Toolkit animations are pooled/recycled; Stop() can throw if the object was already recycled even though we still hold a reference.
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
                EnsureSegmentVisible(selectedIndex);

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
                icons[i].EnableInClassList(SelectedClass, isSelected);
            }
        }

        private void UpdateLayoutState()
        {
            EnableInClassList(ScrollableClass, MaxScrollOffset() > 0.5f);
            SetScrollOffset(scrollOffset);

            if (overlayAnimation == null)
            {
                UpdateOverlayImmediate();
            }
        }

        /// <summary>
        /// The overlay rectangle for a segment, in the control's coordinates before scrolling.
        /// </summary>
        private bool TryGetOverlayRect(int index, out float left, out float width)
        {
            left = 0f;
            width = 0f;

            if (index < 0 || index >= segments.Count)
            {
                return false;
            }

            // The overlay is absolutely positioned, so its left is measured inside the border while
            // the segment row's layout includes it.
            var rowLeft = segmentsRoot.layout.x - resolvedStyle.borderLeftWidth;
            float segmentLeft;
            float segmentWidth;

            if (sizing == SegmentSizing.Equal)
            {
                var rowWidth = segmentsRoot.layout.width;
                if (float.IsNaN(rowWidth) || rowWidth <= 0f || float.IsNaN(rowLeft))
                {
                    return false;
                }

                segmentWidth = rowWidth / segments.Count;
                segmentLeft = rowLeft + (index * segmentWidth);
            }
            else
            {
                var segmentRect = segments[index].layout;
                if (float.IsNaN(segmentRect.width) || segmentRect.width <= 0f || float.IsNaN(rowLeft))
                {
                    return false;
                }

                segmentWidth = segmentRect.width;
                segmentLeft = rowLeft + segmentRect.x;
            }

            var insetX = segmentWidth * OverlayInsetPercentPerSideX;
            left = segmentLeft + insetX;
            width = Mathf.Max(0f, segmentWidth - (2f * insetX));
            return true;
        }

        private void UpdateOverlayImmediate()
        {
            if (selectedIndex < 0 || segments.Count == 0)
            {
                return;
            }

            if (!TryGetOverlayRect(selectedIndex, out var targetLeft, out var targetWidth))
            {
                return;
            }

            StopOverlayAnimationIfAny();
            overlay.style.width = targetWidth;
            overlay.style.left = targetLeft;
            overlayLeft = targetLeft;
        }

        private void AnimateOverlayToIndex(int index)
        {
            if (!TryGetOverlayRect(index, out var toLeft, out var targetWidth))
            {
                UpdateOverlayImmediate();
                return;
            }

            overlay.style.width = targetWidth;

            StopOverlayAnimationIfAny();

            if (overlayAnimationDurationMs <= 0)
            {
                UpdateOverlayImmediate();
                return;
            }

            var animation = overlay.experimental.animation.Start(overlayLeft, toLeft, overlayAnimationDurationMs, (e, value) =>
            {
                overlayLeft = value;
                overlay.style.left = value;
            });

            animation.KeepAlive();

            animation.easingCurve = Easing.OutCubic;
            animation.OnCompleted(() =>
            {
                if (overlayAnimation == animation)
                {
                    overlayAnimation = null;
                }
            });
            overlayAnimation = animation;
        }

        #region Content sizing scroll

        private float MaxScrollOffset()
        {
            if (sizing != SegmentSizing.Content)
            {
                return 0f;
            }

            var rowWidth = segmentsRoot.layout.width;
            var viewWidth = contentRect.width;
            if (float.IsNaN(rowWidth) || float.IsNaN(viewWidth))
            {
                return 0f;
            }

            return Mathf.Max(0f, rowWidth - viewWidth);
        }

        private void SetScrollOffset(float offset)
        {
            scrollOffset = Mathf.Clamp(offset, 0f, MaxScrollOffset());
            var translate = new Translate(-scrollOffset, 0f);
            segmentsRoot.style.translate = translate;
            overlay.style.translate = translate;
        }

        private void EnsureSegmentVisible(int index)
        {
            if (sizing != SegmentSizing.Content || index < 0 || index >= segments.Count)
            {
                return;
            }

            var segmentRect = segments[index].layout;
            var viewWidth = contentRect.width;
            if (float.IsNaN(segmentRect.width) || float.IsNaN(viewWidth) || viewWidth <= 0f)
            {
                return;
            }

            if (segmentRect.xMin < scrollOffset)
            {
                SetScrollOffset(segmentRect.xMin);
            }
            else if (segmentRect.xMax > scrollOffset + viewWidth)
            {
                SetScrollOffset(segmentRect.xMax - viewWidth);
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            suppressNextClick = false;
            if (MaxScrollOffset() <= 0f)
            {
                return;
            }

            dragPointerId = evt.pointerId;
            dragStartX = evt.position.x;
            dragStartOffset = scrollOffset;
            isDragging = false;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != dragPointerId)
            {
                return;
            }

            var dx = evt.position.x - dragStartX;
            if (!isDragging && Mathf.Abs(dx) > DragSlop)
            {
                isDragging = true;
                this.CapturePointer(dragPointerId);
            }

            if (isDragging)
            {
                SetScrollOffset(dragStartOffset - dx);
                evt.StopPropagation();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != dragPointerId)
            {
                return;
            }

            suppressNextClick = isDragging;
            EndDrag();
        }

        private void EndDrag()
        {
            if (dragPointerId != PointerId.invalidPointerId && this.HasPointerCapture(dragPointerId))
            {
                this.ReleasePointer(dragPointerId);
            }

            dragPointerId = PointerId.invalidPointerId;
            isDragging = false;
        }

        private void OnWheel(WheelEvent evt)
        {
            if (MaxScrollOffset() <= 0f)
            {
                return;
            }

            var delta = Mathf.Abs(evt.delta.x) > Mathf.Abs(evt.delta.y) ? evt.delta.x : evt.delta.y;
            SetScrollOffset(scrollOffset + (delta * WheelStep));
            evt.StopPropagation();
        }

        #endregion
    }
}
