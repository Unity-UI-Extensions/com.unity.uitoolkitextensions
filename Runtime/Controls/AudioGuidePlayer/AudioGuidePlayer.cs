/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One line of a transcript, from <see cref="Start"/> seconds until the next cue.
    /// </summary>
    public readonly struct TranscriptCue
    {
        public float Start { get; }
        public string Text { get; }

        public TranscriptCue(float start, string text)
        {
            Start = start;
            Text = text;
        }
    }

    /// <summary>
    /// Media controls for an audio guide or a video: play, pause and replay, a scrubber, optional 15 second skips,
    /// loop, optional mute and playback rate, and the transcript line for the current position.
    /// </summary>
    /// <remarks>
    /// <para>The player holds no media. The owner sets <see cref="Duration"/>, <see cref="Position"/>,
    /// <see cref="IsPlaying"/>, <see cref="IsLooping"/>, <see cref="IsMuted"/> and <see cref="Rate"/> from its audio or video source (each frame
    /// for the position) and answers the request events. While the user drags the scrubber, position updates are held
    /// back so the thumb stays under the finger.</para>
    /// <para>Props: <c>title-text</c>, <c>duration</c>, <c>position</c>, <c>is-playing</c>, <c>is-looping</c>, <c>is-muted</c>,
    /// <c>rate</c>, <c>show-skip</c>, <c>show-rate</c>, <c>show-loop</c>, <c>show-mute</c>. Methods: <see cref="SetCues"/>,
    /// <see cref="SetRates"/>.
    /// Events: <see cref="PlayPauseRequested"/> (true to play), <see cref="ReplayRequested"/>,
    /// <see cref="SeekRequested"/> (seconds), <see cref="SkipRequested"/> (plus or minus seconds),
    /// <see cref="LoopToggled"/>, <see cref="MuteToggled"/> (true when muted), <see cref="RateChanged"/>,
    /// <see cref="CueChanged"/> (index, text; -1 before the first).</para>
    /// <para>Keyboard: submit (Enter, Space) plays or pauses; Left and Right request a seek of 5 seconds.</para>
    /// <para>USS classes: <c>audioGuidePlayer</c> (root), <c>audioGuidePlayer__row</c>, <c>audioGuidePlayer__play</c>,
    /// <c>audioGuidePlayer__playIcon</c>, <c>audioGuidePlayer__skip</c>, <c>audioGuidePlayer__texts</c>,
    /// <c>audioGuidePlayer__title</c>, <c>audioGuidePlayer__time</c>, <c>audioGuidePlayer__loop</c>,
    /// <c>audioGuidePlayer__mute</c>, <c>audioGuidePlayer__rate</c>, <c>audioGuidePlayer__scrubber</c>, <c>audioGuidePlayer__track</c>,
    /// <c>audioGuidePlayer__fill</c>, <c>audioGuidePlayer__thumb</c>, <c>audioGuidePlayer__cue</c>.</para>
    /// <para>Modifier classes: <c>audioGuidePlayer--playing</c>, <c>audioGuidePlayer--ended</c>,
    /// <c>audioGuidePlayer--looping</c>, <c>audioGuidePlayer--muted</c>, <c>audioGuidePlayer--scrubbing</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-border</c>, <c>--surface-fg</c>, <c>--surface-fg-soft</c>,
    /// <c>--surface-muted</c>, <c>--surface-accent</c>, <c>--surface-pressed</c>, <c>--c-accent</c>, <c>--c-on-primary</c>,
    /// <c>--grad-brand-start/end</c> (play button, through <see cref="GradientFill"/>), <c>--radius-card</c>,
    /// <c>--radius-pill</c>, <c>--space-1..4</c>, <c>--text-label</c>, <c>--text-caption</c>, <c>--font-body</c>,
    /// <c>--font-body-medium</c>, <c>--font-body-semibold</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class AudioGuidePlayer : VisualElement
    {
        public const string RootClass = "audioGuidePlayer";
        public const string RowClass = "audioGuidePlayer__row";
        public const string PlayClass = "audioGuidePlayer__play";
        public const string PlayIconClass = "audioGuidePlayer__playIcon";
        public const string SkipClass = "audioGuidePlayer__skip";
        public const string TextsClass = "audioGuidePlayer__texts";
        public const string TitleClass = "audioGuidePlayer__title";
        public const string TimeClass = "audioGuidePlayer__time";
        public const string LoopClass = "audioGuidePlayer__loop";
        public const string MuteClass = "audioGuidePlayer__mute";
        public const string RateClass = "audioGuidePlayer__rate";
        public const string ScrubberClass = "audioGuidePlayer__scrubber";
        public const string TrackClass = "audioGuidePlayer__track";
        public const string FillClass = "audioGuidePlayer__fill";
        public const string ThumbClass = "audioGuidePlayer__thumb";
        public const string CueClass = "audioGuidePlayer__cue";
        public const string PlayingClass = "audioGuidePlayer--playing";
        public const string EndedClass = "audioGuidePlayer--ended";
        public const string LoopingClass = "audioGuidePlayer--looping";
        public const string MutedClass = "audioGuidePlayer--muted";
        public const string ScrubbingClass = "audioGuidePlayer--scrubbing";

        /// <summary>
        /// Seconds per skip button press.
        /// </summary>
        public const float SkipSeconds = 15f;

        /// <summary>
        /// Seconds per Left or Right key press.
        /// </summary>
        public const float KeySeekSeconds = 5f;

        private readonly VisualElement playButton;
        private readonly VisualElement playIcon;
        private readonly VisualElement backSkip;
        private readonly VisualElement forwardSkip;
        private readonly Label titleLabel;
        private readonly Label timeLabel;
        private readonly VisualElement loopButton;
        private readonly VisualElement muteButton;
        private readonly Label rateButton;
        private readonly VisualElement scrubber;
        private readonly VisualElement fill;
        private readonly VisualElement thumb;
        private readonly Label cueLabel;

        private readonly List<TranscriptCue> cues = new();
        private readonly List<float> rates = new() { 1f, 1.25f, 1.5f, 0.75f };

        private float duration;
        private float position;
        private bool isPlaying;
        private bool isLooping;
        private bool isMuted;
        private float rate = 1f;
        private int cueIndex = -1;

        private int scrubPointerId = PointerId.invalidPointerId;
        private float scrubPosition;
        private float heldPosition;

        public event Action<bool> PlayPauseRequested;
        public event Action ReplayRequested;
        public event Action<float> SeekRequested;
        public event Action<float> SkipRequested;
        public event Action<bool> LoopToggled;

        /// <summary>
        /// Raised from the mute button with the new state: true when the sound is now off.
        /// </summary>
        public event Action<bool> MuteToggled;
        public event Action<float> RateChanged;
        public event Action<int, string> CueChanged;

        /// <summary>
        /// Playback has reached the end and stopped: the play button offers a replay.
        /// </summary>
        public bool IsEnded => duration > 0f && position >= duration - 0.05f && !isPlaying;

        public bool IsScrubbing => scrubPointerId != PointerId.invalidPointerId;

        /// <summary>
        /// The position the controls show: the drag position while scrubbing.
        /// </summary>
        public float DisplayedPosition => IsScrubbing ? scrubPosition : position;

        public VisualElement Scrubber => scrubber;

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => titleLabel.text;
            set
            {
                titleLabel.text = value ?? string.Empty;
                titleLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Length in seconds.
        /// </summary>
        [UxmlAttribute("duration")]
        public float Duration
        {
            get => duration;
            set
            {
                duration = Mathf.Max(0f, value);
                Refresh();
            }
        }

        /// <summary>
        /// Playback position in seconds. Held back while the user scrubs.
        /// </summary>
        [UxmlAttribute("position")]
        public float Position
        {
            get => position;
            set
            {
                var clamped = Mathf.Clamp(value, 0f, duration > 0f ? duration : Mathf.Max(0f, value));
                if (IsScrubbing)
                {
                    heldPosition = clamped;
                    return;
                }

                position = clamped;
                Refresh();
            }
        }

        [UxmlAttribute("is-playing")]
        public bool IsPlaying
        {
            get => isPlaying;
            set
            {
                isPlaying = value;
                Refresh();
            }
        }

        [UxmlAttribute("is-looping")]
        public bool IsLooping
        {
            get => isLooping;
            set
            {
                isLooping = value;
                EnableInClassList(LoopingClass, isLooping);
            }
        }

        /// <summary>
        /// True while the source's sound is off; the mute button shows a crossed-out speaker.
        /// </summary>
        [UxmlAttribute("is-muted")]
        public bool IsMuted
        {
            get => isMuted;
            set
            {
                isMuted = value;
                EnableInClassList(MutedClass, isMuted);
            }
        }

        [UxmlAttribute("rate")]
        public float Rate
        {
            get => rate;
            set
            {
                rate = value <= 0f ? 1f : value;
                rateButton.text = FormatRate(rate);
            }
        }

        [UxmlAttribute("show-skip")]
        public bool ShowSkip
        {
            get => backSkip.style.display != DisplayStyle.None;
            set
            {
                backSkip.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                forwardSkip.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        [UxmlAttribute("show-rate")]
        public bool ShowRate
        {
            get => rateButton.style.display != DisplayStyle.None;
            set => rateButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        [UxmlAttribute("show-loop")]
        public bool ShowLoop
        {
            get => loopButton.style.display != DisplayStyle.None;
            set => loopButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Shows the mute button. Off by default, for sources without sound.
        /// </summary>
        [UxmlAttribute("show-mute")]
        public bool ShowMute
        {
            get => muteButton.style.display != DisplayStyle.None;
            set => muteButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public AudioGuidePlayer()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            focusable = true;
            tabIndex = 0;

            var row = UIToolkitExtensions.CreateVisualElement(this, RowClass);

            backSkip = CreateSkip(row, "-15", -SkipSeconds);

            playButton = UIToolkitExtensions.CreateVisualElement(row, PlayClass);
            playButton.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            playButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                PlayPressed();
            });
            playIcon = UIToolkitExtensions.CreateVisualElement(playButton, PlayIconClass);
            playIcon.pickingMode = PickingMode.Ignore;

            forwardSkip = CreateSkip(row, "+15", SkipSeconds);

            var texts = UIToolkitExtensions.CreateVisualElement(row, TextsClass);
            texts.pickingMode = PickingMode.Ignore;
            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            timeLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, TimeClass);

            muteButton = UIToolkitExtensions.CreateVisualElement(row, MuteClass);
            muteButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                IsMuted = !isMuted;
                MuteToggled?.Invoke(isMuted);
            });

            loopButton = UIToolkitExtensions.CreateVisualElement(row, LoopClass);
            loopButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                IsLooping = !isLooping;
                LoopToggled?.Invoke(isLooping);
            });

            rateButton = UIToolkitExtensions.CreateVisualElement<Label>(row, RateClass).MakePill();
            rateButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                CycleRate();
            });

            scrubber = UIToolkitExtensions.CreateVisualElement(this, ScrubberClass);
            var track = UIToolkitExtensions.CreateVisualElement(scrubber, TrackClass);
            track.pickingMode = PickingMode.Ignore;
            fill = UIToolkitExtensions.CreateVisualElement(track, FillClass);
            fill.pickingMode = PickingMode.Ignore;
            thumb = UIToolkitExtensions.CreateVisualElement(scrubber, ThumbClass);
            thumb.pickingMode = PickingMode.Ignore;
            scrubber.RegisterCallback<PointerDownEvent>(OnScrubDown);
            scrubber.RegisterCallback<PointerMoveEvent>(OnScrubMove);
            scrubber.RegisterCallback<PointerUpEvent>(OnScrubUp);
            scrubber.RegisterCallback<PointerCaptureOutEvent>(_ => EndScrub(commit: false));

            cueLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, CueClass);
            cueLabel.pickingMode = PickingMode.Ignore;

            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                PlayPressed();
                evt.StopPropagation();
            });
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);

            TitleText = null;
            ShowSkip = false;
            ShowMute = false;
            Rate = 1f;
            cueLabel.style.display = DisplayStyle.None;
            Refresh();
        }

        /// <summary>
        /// Sets the transcript; cues are sorted by start time. Null or empty hides the cue line.
        /// </summary>
        public void SetCues(IEnumerable<TranscriptCue> newCues)
        {
            cues.Clear();
            if (newCues != null)
            {
                cues.AddRange(newCues);
            }

            cues.Sort((a, b) => a.Start.CompareTo(b.Start));
            cueIndex = -2;
            cueLabel.style.display = cues.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            Refresh();
        }

        /// <summary>
        /// The rates the rate button cycles through, in order. The first is used when the current rate is not in the list.
        /// </summary>
        public void SetRates(IEnumerable<float> newRates)
        {
            rates.Clear();
            if (newRates != null)
            {
                foreach (var r in newRates)
                {
                    if (r > 0f)
                    {
                        rates.Add(r);
                    }
                }
            }

            if (rates.Count == 0)
            {
                rates.Add(1f);
            }
        }

        /// <summary>
        /// Formats seconds as m:ss, or h:mm:ss from an hour.
        /// </summary>
        public static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            var hours = total / 3600;
            var minutes = (total / 60) % 60;
            var secs = total % 60;
            return hours > 0 ? $"{hours}:{minutes:00}:{secs:00}" : $"{minutes}:{secs:00}";
        }

        private static string FormatRate(float value) => $"{value:0.##}x";

        private VisualElement CreateSkip(VisualElement parent, string text, float delta)
        {
            var skip = UIToolkitExtensions.CreateVisualElement<Label>(parent, SkipClass).MakePill();
            skip.text = text;
            skip.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                SkipRequested?.Invoke(delta);
            });
            return skip;
        }

        private void PlayPressed()
        {
            if (IsEnded)
            {
                ReplayRequested?.Invoke();
                return;
            }

            PlayPauseRequested?.Invoke(!isPlaying);
        }

        private void CycleRate()
        {
            var index = rates.IndexOf(rate);
            var next = rates[(index + 1) % rates.Count];
            Rate = next;
            RateChanged?.Invoke(rate);
        }

        private void Refresh()
        {
            var shown = DisplayedPosition;
            var fraction = duration > 0f ? Mathf.Clamp01(shown / duration) : 0f;
            fill.style.width = Length.Percent(fraction * 100f);
            thumb.style.left = Length.Percent(fraction * 100f);
            timeLabel.text = $"{FormatTime(shown)} / {FormatTime(duration)}";

            EnableInClassList(PlayingClass, isPlaying);
            EnableInClassList(EndedClass, IsEnded);
            playIcon.EnableInClassList("audioGuidePlayer__playIcon--pause", isPlaying);
            playIcon.EnableInClassList("audioGuidePlayer__playIcon--replay", IsEnded);

            UpdateCue(shown);
        }

        private void UpdateCue(float at)
        {
            if (cues.Count == 0)
            {
                return;
            }

            var index = -1;
            for (var i = 0; i < cues.Count && cues[i].Start <= at; i++)
            {
                index = i;
            }

            if (index == cueIndex)
            {
                return;
            }

            cueIndex = index;
            var text = index >= 0 ? cues[index].Text : string.Empty;
            cueLabel.text = text;
            CueChanged?.Invoke(index, text);
        }

        #region Scrubbing

        private float FractionAt(Vector2 panelPosition)
        {
            var local = scrubber.WorldToLocal(panelPosition);
            var width = scrubber.contentRect.width;
            return width > 0f ? Mathf.Clamp01(local.x / width) : 0f;
        }

        private void OnScrubDown(PointerDownEvent evt)
        {
            if (duration <= 0f || evt.button != 0)
            {
                return;
            }

            scrubPointerId = evt.pointerId;
            heldPosition = position;
            scrubPosition = FractionAt(evt.position) * duration;
            scrubber.CapturePointer(evt.pointerId);
            AddToClassList(ScrubbingClass);
            Refresh();
            evt.StopPropagation();
        }

        private void OnScrubMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != scrubPointerId)
            {
                return;
            }

            scrubPosition = FractionAt(evt.position) * duration;
            Refresh();
        }

        private void OnScrubUp(PointerUpEvent evt)
        {
            if (evt.pointerId != scrubPointerId)
            {
                return;
            }

            scrubPosition = FractionAt(evt.position) * duration;
            EndScrub(commit: true);
        }

        private void EndScrub(bool commit)
        {
            if (!IsScrubbing)
            {
                return;
            }

            var pointerId = scrubPointerId;
            var target = scrubPosition;
            scrubPointerId = PointerId.invalidPointerId;
            if (scrubber.HasPointerCapture(pointerId))
            {
                scrubber.ReleasePointer(pointerId);
            }

            RemoveFromClassList(ScrubbingClass);

            // Show the requested position until the owner reports the new one.
            position = commit ? target : heldPosition;
            Refresh();
            if (commit)
            {
                SeekRequested?.Invoke(target);
            }
        }

        #endregion

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            var delta = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => -KeySeekSeconds,
                NavigationMoveEvent.Direction.Right => KeySeekSeconds,
                _ => 0f,
            };

            if (delta == 0f || duration <= 0f)
            {
                return;
            }

            SeekRequested?.Invoke(Mathf.Clamp(position + delta, 0f, duration));
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }
    }
}
