/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A hold-to-talk row: a primary-colour microphone disc with a title and a subtitle. Holding raises
    /// <see cref="PressStarted"/> and, on release, <see cref="PressEnded"/>; a quick tap raises <see cref="Tapped"/>.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>title-text</c>, <c>subtitle</c>, <c>hold-threshold-ms</c>. Events: <see cref="PressStarted"/>,
    /// <see cref="PressEnded"/> (true when released over the row, false when cancelled), <see cref="Tapped"/>.
    /// Submit (Enter, Space) counts as a tap.</para>
    /// <para>USS classes: <c>holdToTalk</c> (root), <c>holdToTalk__mic</c>, <c>holdToTalk__micIcon</c>,
    /// <c>holdToTalk__texts</c>, <c>holdToTalk__title</c>, <c>holdToTalk__subtitle</c>. Modifier: <c>holdToTalk--holding</c>.</para>
    /// <para>Tokens read: <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-accent</c>, <c>--c-on-primary</c>,
    /// <c>--grad-brand-start/end</c>, <c>--radius-pill</c>, <c>--space-3</c>, <c>--text-caption</c>, <c>--text-label</c>,
    /// <c>--font-body</c>, <c>--font-body-medium</c>, <c>--motion-fast</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class HoldToTalk : VisualElement
    {
        public const string RootClass = "holdToTalk";
        public const string MicClass = "holdToTalk__mic";
        public const string MicIconClass = "holdToTalk__micIcon";
        public const string TextsClass = "holdToTalk__texts";
        public const string TitleClass = "holdToTalk__title";
        public const string SubtitleClass = "holdToTalk__subtitle";
        public const string HoldingClass = "holdToTalk--holding";

        private readonly Label title;
        private readonly Label subtitle;
        private int holdThresholdMs = 250;
        private int pointerId = PointerId.invalidPointerId;
        private bool holding;
        private IVisualElementScheduledItem holdTimer;

        /// <summary>
        /// Raised when a press has lasted the hold threshold: start listening.
        /// </summary>
        public event Action PressStarted;

        /// <summary>
        /// Raised when a hold ends: true when released over the row, false when cancelled.
        /// </summary>
        public event Action<bool> PressEnded;

        /// <summary>
        /// Raised for a press shorter than the hold threshold, for example to open typing instead.
        /// </summary>
        public event Action Tapped;

        public bool IsHolding => holding;

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => title.text = value ?? string.Empty;
        }

        [UxmlAttribute("subtitle")]
        public string Subtitle
        {
            get => subtitle.text;
            set
            {
                subtitle.text = value ?? string.Empty;
                subtitle.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("hold-threshold-ms")]
        public int HoldThresholdMs
        {
            get => holdThresholdMs;
            set => holdThresholdMs = Mathf.Max(0, value);
        }

        public HoldToTalk()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            focusable = true;
            tabIndex = 0;

            var mic = UIToolkitExtensions.CreateVisualElement(this, MicClass);
            mic.pickingMode = PickingMode.Ignore;
            mic.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            var icon = UIToolkitExtensions.CreateVisualElement(mic, MicIconClass);
            icon.pickingMode = PickingMode.Ignore;

            var texts = UIToolkitExtensions.CreateVisualElement(this, TextsClass);
            texts.pickingMode = PickingMode.Ignore;
            title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            subtitle = UIToolkitExtensions.CreateVisualElement<Label>(texts, SubtitleClass);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(_ => End(released: false));
            RegisterCallback<PointerCaptureOutEvent>(_ => End(released: false));
            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                Tapped?.Invoke();
                evt.StopPropagation();
            });

            Subtitle = null;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || pointerId != PointerId.invalidPointerId)
            {
                return;
            }

            pointerId = evt.pointerId;
            this.CapturePointer(pointerId);
            holdTimer = schedule.Execute(BeginHold).StartingIn(holdThresholdMs);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != pointerId)
            {
                return;
            }

            if (holding)
            {
                End(released: worldBound.Contains(evt.position));
            }
            else
            {
                Reset();
                Tapped?.Invoke();
            }
        }

        /// <summary>
        /// Starts the hold now; called by the threshold timer.
        /// </summary>
        internal void BeginHold()
        {
            if (pointerId == PointerId.invalidPointerId || holding)
            {
                return;
            }

            holding = true;
            AddToClassList(HoldingClass);
            PressStarted?.Invoke();
        }

        private void End(bool released)
        {
            var wasHolding = holding;
            Reset();
            if (wasHolding)
            {
                PressEnded?.Invoke(released);
            }
        }

        private void Reset()
        {
            holdTimer?.Pause();
            holdTimer = null;

            // Clear the state before releasing capture: the capture-out event re-enters End.
            holding = false;
            RemoveFromClassList(HoldingClass);
            var id = pointerId;
            pointerId = PointerId.invalidPointerId;
            if (id != PointerId.invalidPointerId && this.HasPointerCapture(id))
            {
                this.ReleasePointer(id);
            }
        }
    }
}
