/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum SpeechState
    {
        Listening,
        Processing,
        Matched,
        NoMatch,
        Unavailable,
    }

    /// <summary>
    /// The speech input panel: a status header with a pulsing dot and a level meter, the transcript in quotes
    /// (partial muted, final full colour), a matched or no-match row, primary and secondary actions and a footnote.
    /// </summary>
    /// <remarks>
    /// <para>Recognition happens elsewhere; the owner reports the state, transcript and match. The meter animates by
    /// itself while listening, scaled by <see cref="SetLevel"/> when a level is available; with reduce motion the dot
    /// and meter hold still.</para>
    /// <para>Pass localised strings. The English status defaults are a fallback: "Listening", "Working it out",
    /// "Listening", "Not sure I caught that", "Speech is not available".</para>
    /// <para>Props: <c>state</c>, <c>status-text</c>, <c>primary-text</c>, <c>secondary-text</c>, <c>footnote</c>,
    /// <c>match-eyebrow</c>, <c>no-match-eyebrow</c>. Methods: <see cref="SetState"/>, <see cref="SetTranscript"/>,
    /// <see cref="SetMatch"/>, <see cref="SetLevel"/>. Events: <see cref="PrimaryClicked"/>, <see cref="SecondaryClicked"/>.</para>
    /// <para>USS classes: <c>speechInputPanel</c> (root), <c>speechInputPanel__header</c>, <c>speechInputPanel__dot</c>,
    /// <c>speechInputPanel__status</c>, <c>speechInputPanel__meter</c>, <c>speechInputPanel__bar</c>,
    /// <c>speechInputPanel__transcript</c>, <c>speechInputPanel__match</c>, <c>speechInputPanel__matchEyebrow</c>,
    /// <c>speechInputPanel__matchText</c>, <c>speechInputPanel__actions</c>, <c>speechInputPanel__primary</c>,
    /// <c>speechInputPanel__secondary</c>, <c>speechInputPanel__footnote</c>.</para>
    /// <para>Modifier classes: <c>speechInputPanel--listening</c>, <c>--processing</c>, <c>--matched</c>,
    /// <c>--noMatch</c>, <c>--unavailable</c>, <c>speechInputPanel--partial</c> (transcript not final),
    /// <c>speechInputPanel--pulse</c> (the bright moment of the dot).</para>
    /// <para>Tokens read: <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-accent</c>, <c>--surface-highlight</c>,
    /// <c>--surface-highlight-border</c>, <c>--c-warn</c>, <c>--radius-pill</c>, <c>--space-2..4</c>, <c>--text-micro</c>,
    /// <c>--text-caption</c>, <c>--text-label</c>, <c>--text-title</c>, <c>--font-display</c>, <c>--font-body</c>,
    /// <c>--font-body-medium</c>, <c>--motion-fast</c>, plus the PillButton tokens.</para>
    /// </remarks>
    [UxmlElement]
    public partial class SpeechInputPanel : VisualElement
    {
        public const string RootClass = "speechInputPanel";
        public const string HeaderClass = "speechInputPanel__header";
        public const string DotClass = "speechInputPanel__dot";
        public const string StatusClass = "speechInputPanel__status";
        public const string MeterClass = "speechInputPanel__meter";
        public const string BarClass = "speechInputPanel__bar";
        public const string TranscriptClass = "speechInputPanel__transcript";
        public const string MatchClass = "speechInputPanel__match";
        public const string MatchEyebrowClass = "speechInputPanel__matchEyebrow";
        public const string MatchTextClass = "speechInputPanel__matchText";
        public const string ActionsClass = "speechInputPanel__actions";
        public const string PrimaryClass = "speechInputPanel__primary";
        public const string SecondaryClass = "speechInputPanel__secondary";
        public const string FootnoteClass = "speechInputPanel__footnote";
        public const string PartialClass = "speechInputPanel--partial";
        public const string PulseClass = "speechInputPanel--pulse";

        /// <summary>
        /// Bars in the level meter.
        /// </summary>
        public const int BarCount = 6;

        /// <summary>
        /// Time between meter frames.
        /// </summary>
        public const int MeterIntervalMs = 120;

        private static readonly float[] Rest = { 0.33f, 0.75f, 1f, 0.58f, 0.83f, 0.42f };

        private readonly Label status;
        private readonly List<VisualElement> bars = new();
        private readonly Label transcript;
        private readonly VisualElement match;
        private readonly Label matchEyebrow;
        private readonly Label matchText;
        private readonly PillButton primary;
        private readonly PillButton secondary;
        private readonly Label footnote;

        private SpeechState state = SpeechState.Listening;
        private string statusOverride;
        private string matchEyebrowText = "MATCHED";
        private string noMatchEyebrowText = "NO MATCH";
        private float level = -1f;
        private int frame;
        private DisplayedTimer animation;

        public event Action PrimaryClicked;
        public event Action SecondaryClicked;

        public PillButton PrimaryButton => primary;
        public PillButton SecondaryButton => secondary;

        /// <summary>
        /// Whether the dot and meter are animating now (listening, attached, no reduce-motion ancestor).
        /// </summary>
        public bool IsAnimating => state == SpeechState.Listening && panel != null && !this.IsReduceMotion();

        [UxmlAttribute("state")]
        public SpeechState State
        {
            get => state;
            set => SetState(value);
        }

        /// <summary>
        /// The header text; empty uses the English default for the state.
        /// </summary>
        [UxmlAttribute("status-text")]
        public string StatusText
        {
            get => status.text;
            set
            {
                statusOverride = value;
                ApplyState();
            }
        }

        [UxmlAttribute("primary-text")]
        public string PrimaryText
        {
            get => primary.Text;
            set => SetButton(primary, value);
        }

        [UxmlAttribute("secondary-text")]
        public string SecondaryText
        {
            get => secondary.Text;
            set => SetButton(secondary, value);
        }

        [UxmlAttribute("footnote")]
        public string Footnote
        {
            get => footnote.text;
            set
            {
                footnote.text = value ?? string.Empty;
                footnote.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("match-eyebrow")]
        public string MatchEyebrow
        {
            get => matchEyebrowText;
            set
            {
                matchEyebrowText = value ?? string.Empty;
                ApplyState();
            }
        }

        [UxmlAttribute("no-match-eyebrow")]
        public string NoMatchEyebrow
        {
            get => noMatchEyebrowText;
            set
            {
                noMatchEyebrowText = value ?? string.Empty;
                ApplyState();
            }
        }

        public SpeechInputPanel()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var header = UIToolkitExtensions.CreateVisualElement(this, HeaderClass);
            var dot = UIToolkitExtensions.CreateVisualElement(header, DotClass);
            dot.pickingMode = PickingMode.Ignore;
            status = UIToolkitExtensions.CreateVisualElement<Label>(header, StatusClass);
            var meter = UIToolkitExtensions.CreateVisualElement(header, MeterClass);
            meter.pickingMode = PickingMode.Ignore;
            for (var i = 0; i < BarCount; i++)
            {
                var bar = UIToolkitExtensions.CreateVisualElement(meter, BarClass);
                bar.pickingMode = PickingMode.Ignore;
                bars.Add(bar);
            }

            transcript = UIToolkitExtensions.CreateVisualElement<Label>(this, TranscriptClass);

            match = UIToolkitExtensions.CreateVisualElement(this, MatchClass);
            matchEyebrow = UIToolkitExtensions.CreateVisualElement<Label>(match, MatchEyebrowClass);
            matchText = UIToolkitExtensions.CreateVisualElement<Label>(match, MatchTextClass);

            var actions = UIToolkitExtensions.CreateVisualElement(this, ActionsClass);
            primary = new PillButton { Variant = PillButtonVariant.Primary };
            primary.AddToClassList(PrimaryClass);
            primary.Clicked += () => PrimaryClicked?.Invoke();
            actions.Add(primary);
            secondary = new PillButton { Variant = PillButtonVariant.Ghost };
            secondary.AddToClassList(SecondaryClass);
            secondary.Clicked += () => SecondaryClicked?.Invoke();
            actions.Add(secondary);

            footnote = UIToolkitExtensions.CreateVisualElement<Label>(this, FootnoteClass);

            RegisterCallback<AttachToPanelEvent>(_ => UpdateAnimation());
            RegisterCallback<DetachFromPanelEvent>(_ => StopAnimation());

            PrimaryText = null;
            SecondaryText = null;
            Footnote = null;
            SetTranscript(null, true);
            SetMatch(null);
            ApplyBars();
            ApplyState();
        }

        /// <summary>
        /// Changes the state; <paramref name="statusText"/> replaces the header text when given.
        /// </summary>
        public void SetState(SpeechState newState, string statusText = null)
        {
            state = newState;
            if (statusText != null)
            {
                statusOverride = statusText;
            }

            ApplyState();
        }

        /// <summary>
        /// Shows what was heard, in quotes. A partial transcript is muted; a final one is full colour. Empty hides it.
        /// </summary>
        public void SetTranscript(string text, bool isFinal)
        {
            var has = !string.IsNullOrEmpty(text);
            transcript.text = has ? $"“{text}”" : string.Empty;
            transcript.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(PartialClass, has && !isFinal);
        }

        /// <summary>
        /// The text of the matched or unmatched row, such as "2 · What is going on here?". It shows in the
        /// <see cref="SpeechState.Matched"/> and <see cref="SpeechState.NoMatch"/> states. Null hides it.
        /// </summary>
        public void SetMatch(string text)
        {
            matchText.text = text ?? string.Empty;
            ApplyState();
        }

        /// <summary>
        /// The input level, 0 to 1, scaling the meter; negative lets the meter animate on its own.
        /// </summary>
        public void SetLevel(float value)
        {
            level = value < 0f ? -1f : Mathf.Clamp01(value);
        }

        /// <summary>
        /// The English fallback for a state's header.
        /// </summary>
        public static string DefaultStatus(SpeechState speechState) => speechState switch
        {
            SpeechState.Listening => "Listening",
            SpeechState.Processing => "Working it out",
            SpeechState.Matched => "Listening",
            SpeechState.NoMatch => "Not sure I caught that",
            SpeechState.Unavailable => "Speech is not available",
            _ => string.Empty,
        };

        private static void SetButton(PillButton button, string text)
        {
            button.Text = text ?? string.Empty;
            button.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void ApplyState()
        {
            foreach (SpeechState value in Enum.GetValues(typeof(SpeechState)))
            {
                var name = value.ToString();
                EnableInClassList($"{RootClass}--{char.ToLowerInvariant(name[0])}{name.Substring(1)}", value == state);
            }

            status.text = string.IsNullOrEmpty(statusOverride) ? DefaultStatus(state) : statusOverride;

            var showMatch = !string.IsNullOrEmpty(matchText.text) && (state == SpeechState.Matched || state == SpeechState.NoMatch);
            match.style.display = showMatch ? DisplayStyle.Flex : DisplayStyle.None;
            matchEyebrow.text = state == SpeechState.NoMatch ? noMatchEyebrowText : matchEyebrowText;

            if (state != SpeechState.Listening)
            {
                RemoveFromClassList(PulseClass);
                ApplyBars();
            }

            UpdateAnimation();
        }

        /// <summary>
        /// The meter runs only while listening, so an idle panel schedules nothing.
        /// </summary>
        private void UpdateAnimation()
        {
            if (state == SpeechState.Listening && panel != null)
            {
                if (animation == null)
                {
                    StartAnimation();
                }
            }
            else
            {
                StopAnimation();
            }
        }

        private void StartAnimation()
        {
            StopAnimation();
            animation = DisplayedTimer.Start(this, Tick, MeterIntervalMs);
        }

        private void StopAnimation()
        {
            animation?.Stop();
            animation = null;
        }

        private void Tick()
        {
            if (!IsAnimating)
            {
                RemoveFromClassList(PulseClass);
                ApplyBars();
                return;
            }

            frame++;
            if (frame % 5 == 0)
            {
                ToggleInClassList(PulseClass);
            }

            for (var i = 0; i < bars.Count; i++)
            {
                // A loose wave per bar, scaled by the level when one is reported.
                var wave = 0.35f + (0.65f * Mathf.Abs(Mathf.Sin((frame * 0.55f) + (i * 1.3f))));
                var scale = level < 0f ? 1f : Mathf.Lerp(0.2f, 1f, level);
                bars[i].style.height = Length.Percent(Mathf.Clamp(wave * scale, 0.2f, 1f) * 100f);
            }
        }

        /// <summary>
        /// The resting meter: the design's still shape.
        /// </summary>
        private void ApplyBars()
        {
            for (var i = 0; i < bars.Count; i++)
            {
                bars[i].style.height = Length.Percent(Rest[i % Rest.Length] * 100f);
            }
        }
    }
}
