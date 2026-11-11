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
    /// <para>Recognition happens elsewhere. The owner reports the state, transcript and match. The meter animates by
    /// itself while listening, scaled by <see cref="SetLevel"/> when a level is available. With reduce motion the dot
    /// and meter hold still.</para>
    /// <para>Pass localised strings. The English status defaults are a fallback: "Listening", "Working it out",
    /// "Listening", "Not sure I caught that", "Speech is not available".</para>
    /// <para>The class <c>speechInputPanel--partial</c> marks a transcript that is not final.
    /// The class <c>speechInputPanel--pulse</c> marks the bright moment of the dot.</para>
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

        public const int BarCount = 6;

        /// <summary>
        /// Time between meter frames in milliseconds.
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
                footnote.SetTextOrHide(value);
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
            var dot = UIToolkitExtensions.CreateVisualElement(header, DotClass).IgnorePicking();
            status = UIToolkitExtensions.CreateVisualElement<Label>(header, StatusClass);
            var meter = UIToolkitExtensions.CreateVisualElement(header, MeterClass).IgnorePicking();
            for (var i = 0; i < BarCount; i++)
            {
                var bar = UIToolkitExtensions.CreateVisualElement(meter, BarClass).IgnorePicking();
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
        /// Sets the state. <paramref name="statusText"/> replaces the header text when given.
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
            transcript.SetDisplayed(has);
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
        /// Sets the input level, 0 to 1, that scales the meter. A negative value lets the meter animate on its own.
        /// </summary>
        public void SetLevel(float value)
        {
            level = value < 0f ? -1f : Mathf.Clamp01(value);
        }

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
            button.SetDisplayed(!string.IsNullOrEmpty(text));
        }

        private static readonly string[] StateClasses =
        {
            RootClass + "--listening",
            RootClass + "--processing",
            RootClass + "--matched",
            RootClass + "--noMatch",
            RootClass + "--unavailable",
        };

        private void ApplyState()
        {
            for (var i = 0; i < StateClasses.Length; i++)
            {
                EnableInClassList(StateClasses[i], i == (int)state);
            }

            status.text = string.IsNullOrEmpty(statusOverride) ? DefaultStatus(state) : statusOverride;

            var showMatch = !string.IsNullOrEmpty(matchText.text) && (state == SpeechState.Matched || state == SpeechState.NoMatch);
            match.SetDisplayed(showMatch);
            matchEyebrow.text = state == SpeechState.NoMatch ? noMatchEyebrowText : matchEyebrowText;

            if (state != SpeechState.Listening)
            {
                RemoveFromClassList(PulseClass);
                ApplyBars();
            }

            UpdateAnimation();
        }

        // The meter runs only while listening, so an idle panel schedules nothing.
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

            var scale = level < 0f ? 1f : Mathf.Lerp(0.2f, 1f, level);
            for (var i = 0; i < bars.Count; i++)
            {
                // A loose wave per bar, scaled by the level when one is reported.
                var wave = 0.35f + (0.65f * Mathf.Abs(Mathf.Sin((frame * 0.55f) + (i * 1.3f))));
                bars[i].style.height = Length.Percent(Mathf.Clamp(wave * scale, 0.2f, 1f) * 100f);
            }
        }

        private void ApplyBars()
        {
            for (var i = 0; i < bars.Count; i++)
            {
                bars[i].style.height = Length.Percent(Rest[i % Rest.Length] * 100f);
            }
        }
    }
}
