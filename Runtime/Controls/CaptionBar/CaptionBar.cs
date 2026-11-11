/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Caption text sizes, the four a viewer can choose from.
    /// </summary>
    public enum CaptionSize
    {
        Small,
        Medium,
        Large,
        ExtraLarge,
    }

    /// <summary>
    /// What sits behind the caption text.
    /// </summary>
    public enum CaptionBackground
    {
        /// <summary>
        /// The translucent dark card with the accent edge.
        /// </summary>
        Panel,

        /// <summary>
        /// An opaque dark card, for busy scenes.
        /// </summary>
        Solid,

        /// <summary>
        /// No card; the text has a shadow.
        /// </summary>
        None,
    }

    /// <summary>
    /// Where a floating <see cref="CaptionBar"/> sits in its parent.
    /// </summary>
    public enum CaptionPlacement
    {
        Bottom,
        Top,

        /// <summary>
        /// Starts at the bottom; the viewer drags it up or down.
        /// </summary>
        Free,
    }

    /// <summary>
    /// One timed caption for <see cref="CaptionBar.SetCues"/>. Times are in seconds.
    /// </summary>
    public readonly struct CaptionCue
    {
        public readonly float Start;
        public readonly float End;
        public readonly string Speaker;
        public readonly string Text;

        public CaptionCue(float start, float end, string speaker, string text)
        {
            Start = start;
            End = end;
            Speaker = speaker;
            Text = text;
        }
    }

    /// <summary>
    /// Captions for speech: a dark card with an accent left edge, the speaker as a small capitals label in a left
    /// column, and the line beside it, clamped to <see cref="MaxLines"/>. A new cue cross-fades in.
    /// </summary>
    /// <remarks>
    /// <para>Captions follow the text-scale root classes through <c>--text-label</c>, and the line clamp follows the font
    /// size. The cross-fade uses <c>--motion-fast</c>, so <c>reduce-motion</c> swaps the text at once. The bar only shows
    /// text and plays nothing. Set cues with <see cref="SetCue"/>, or set timed cues and drive <see cref="Position"/>
    /// yourself.</para>
    /// <para>A bar with <c>announce</c> on speaks each cue through <see cref="A11yAnnouncer"/>. <see cref="SyncTo"/> follows any
    /// clock the owner passes in, such as an audio or video player's time. Captions sit over the camera, so they use the deep
    /// palette directly.</para>
    /// </remarks>
    [UxmlElement]
    public partial class CaptionBar : VisualElement
    {
        public const string RootClass = "captionBar";
        public const string SpeakerClass = "captionBar__speaker";
        public const string TextClass = "captionBar__text";
        public const string EmptyClass = "captionBar--empty";
        public const string ChangingClass = "captionBar--changing";
        public const string NoSpeakerClass = "captionBar--noSpeaker";
        public const string SmallClass = "captionBar--small";
        public const string LargeClass = "captionBar--large";
        public const string ExtraLargeClass = "captionBar--extraLarge";
        public const string SolidClass = "captionBar--solid";
        public const string PlainClass = "captionBar--plain";
        public const string FloatingClass = "captionBar--floating";
        public const string TopClass = "captionBar--top";
        public const string FreeClass = "captionBar--free";

        /// <summary>
        /// How often <see cref="SyncTo"/> reads the clock.
        /// </summary>
        public const int SyncIntervalMs = 100;

        private readonly Label speakerLabel;
        private readonly Label textLabel;
        private readonly List<CaptionCue> cues = new();
        private CaptionSize captionSize = CaptionSize.Medium;
        private CaptionBackground background = CaptionBackground.Panel;
        private CaptionPlacement placement = CaptionPlacement.Bottom;
        private bool floating;
        private float topInset;
        private float bottomInset;
        private float freeOffset;
        private bool announce;
        private float position;
        private int cueIndex = -1;
        private Func<float> syncTime;
        private DisplayedTimer syncTimer;
        private int dragPointer = PointerId.invalidPointerId;
        private float dragStartY;
        private float dragStartOffset;
        private int maxLines = 3;
        private string lineSample = string.Empty;
        private int lineSampleLines;
        private IVisualElementScheduledItem swap;
        private string pendingSpeaker;
        private string pendingText;

        /// <summary>
        /// Raised when the timed cue changes: its index in <see cref="SetCues"/>, or -1 between cues.
        /// </summary>
        public event Action<int> CueChanged;

        /// <summary>
        /// Raised when the viewer drags a free bar: its offset up from the bottom position, in pixels.
        /// </summary>
        public event Action<float> Moved;

        /// <summary>
        /// True when no cue is shown.
        /// </summary>
        public bool IsEmpty => ClassListContains(EmptyClass);

        /// <summary>
        /// The index of the timed cue shown, or -1.
        /// </summary>
        public int CueIndex => cueIndex;

        [UxmlAttribute("caption-size")]
        public CaptionSize CaptionSize
        {
            get => captionSize;
            set
            {
                captionSize = value;
                EnableInClassList(SmallClass, value == CaptionSize.Small);
                EnableInClassList(LargeClass, value == CaptionSize.Large);
                EnableInClassList(ExtraLargeClass, value == CaptionSize.ExtraLarge);
                ApplyLineClamp();
            }
        }

        [UxmlAttribute("background")]
        public CaptionBackground BackgroundStyle
        {
            get => background;
            set
            {
                background = value;
                EnableInClassList(SolidClass, value == CaptionBackground.Solid);
                EnableInClassList(PlainClass, value == CaptionBackground.None);
            }
        }

        /// <summary>
        /// The bar positions itself in its parent, full width less 16 px each side, by <see cref="Placement"/>.
        /// </summary>
        [UxmlAttribute("floating")]
        public bool Floating
        {
            get => floating;
            set
            {
                floating = value;
                EnableInClassList(FloatingClass, floating);
                ApplyPlacement();
            }
        }

        [UxmlAttribute("placement")]
        public CaptionPlacement Placement
        {
            get => placement;
            set
            {
                placement = value;
                EnableInClassList(TopClass, value == CaptionPlacement.Top);
                EnableInClassList(FreeClass, value == CaptionPlacement.Free);
                pickingMode = value == CaptionPlacement.Free ? PickingMode.Position : PickingMode.Ignore;
                ApplyPlacement();
            }
        }

        /// <summary>
        /// Space kept clear at the top when floating at the top, such as the context pill.
        /// </summary>
        [UxmlAttribute("top-inset")]
        public float TopInset
        {
            get => topInset;
            set
            {
                topInset = Mathf.Max(0f, value);
                ApplyPlacement();
            }
        }

        /// <summary>
        /// Space kept clear at the bottom when floating at the bottom, such as a sheet's visible height.
        /// </summary>
        [UxmlAttribute("bottom-inset")]
        public float BottomInset
        {
            get => bottomInset;
            set
            {
                bottomInset = Mathf.Max(0f, value);
                ApplyPlacement();
            }
        }

        /// <summary>
        /// A free bar's distance up from its bottom position, in pixels.
        /// </summary>
        public float FreeOffset
        {
            get => freeOffset;
            set
            {
                freeOffset = Mathf.Max(0f, value);
                ApplyPlacement();
            }
        }

        /// <summary>
        /// Speaks each new cue through <see cref="A11yAnnouncer"/>, as a live region.
        /// </summary>
        [UxmlAttribute("announce")]
        public bool Announce
        {
            get => announce;
            set => announce = value;
        }

        /// <summary>
        /// The playback time in seconds; shows the cue that covers it from <see cref="SetCues"/>.
        /// </summary>
        public float Position
        {
            get => position;
            set
            {
                position = Mathf.Max(0f, value);
                ShowCueAt(position);
            }
        }

        public Label TextLabel => textLabel;

        [UxmlAttribute("speaker")]
        public string Speaker
        {
            get => speakerLabel.text;
            set => SetCue(value, Text);
        }

        [UxmlAttribute("text")]
        public string Text
        {
            get => textLabel.text;
            set => SetCue(Speaker, value);
        }

        /// <summary>
        /// Lines of caption shown at most; the rest is cut. 0 or less shows everything.
        /// </summary>
        [UxmlAttribute("max-lines")]
        public int MaxLines
        {
            get => maxLines;
            set
            {
                maxLines = value;
                ApplyLineClamp();
            }
        }

        public CaptionBar()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            speakerLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, SpeakerClass).IgnorePicking();
            textLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, TextClass).IgnorePicking();
            textLabel.RegisterCallback<GeometryChangedEvent>(_ => ApplyLineClamp());

            RegisterCallback<PointerDownEvent>(OnDragStart);
            RegisterCallback<PointerMoveEvent>(OnDragMove);
            RegisterCallback<PointerUpEvent>(OnDragEnd);
            RegisterCallback<PointerCaptureOutEvent>(_ => dragPointer = PointerId.invalidPointerId);
            RegisterCallback<GeometryChangedEvent>(_ => ApplyPlacement());
            RegisterCallback<DetachFromPanelEvent>(_ => StopSync());

            Apply(null, null);
        }

        /// <summary>
        /// Sets the timed cues; <see cref="Position"/> or <see cref="SyncTo"/> then shows them.
        /// </summary>
        public void SetCues(IReadOnlyList<CaptionCue> newCues)
        {
            cues.Clear();
            if (newCues != null)
            {
                cues.AddRange(newCues);
            }

            cueIndex = -2;
            ShowCueAt(position);
        }

        /// <summary>
        /// Follows a clock: every 100 ms the bar reads <paramref name="currentTime"/> in seconds and shows the cue for that
        /// time. Pass any time source, for example <c>() =&gt; audioSource.time</c> or <c>() =&gt; (float)videoPlayer.time</c>.
        /// Null stops following. The sync pauses while the bar is hidden, stops on detach, and stops if the clock throws,
        /// for example because its source was destroyed.
        /// </summary>
        public void SyncTo(Func<float> currentTime)
        {
            StopSync();
            syncTime = currentTime;
            if (currentTime == null)
            {
                return;
            }

            // Paused while the bar is hidden; the next tick catches up with the clock.
            syncTimer = DisplayedTimer.Start(this, () =>
            {
                if (syncTime == null)
                {
                    return;
                }

                float seconds;
                try
                {
                    seconds = syncTime();
                }
                catch (Exception exception)
                {
                    StopSync();
                    Debug.LogException(exception);
                    return;
                }

                Position = seconds;
            }, SyncIntervalMs);
        }

        public void StopSync()
        {
            syncTimer?.Stop();
            syncTimer = null;
            syncTime = null;
        }

        /// <summary>
        /// Shows a cue. The speaker shows in capitals; null or empty hides the speaker column. A different line
        /// cross-fades in; empty text clears the bar.
        /// </summary>
        public void SetCue(string speaker, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                ClearCue();
                return;
            }

            var upper = string.IsNullOrEmpty(speaker) ? string.Empty : speaker.ToUpperInvariant();
            if (upper == speakerLabel.text && text == textLabel.text && !IsEmpty)
            {
                return;
            }

            var fadeMs = IsEmpty ? 0 : FadeDurationMs();
            swap?.Pause();
            swap = null;
            if (fadeMs <= 0)
            {
                Apply(upper, text);
                return;
            }

            pendingSpeaker = upper;
            pendingText = text;
            AddToClassList(ChangingClass);
            swap = schedule.Execute(() =>
            {
                swap = null;
                Apply(pendingSpeaker, pendingText);
            }).StartingIn(fadeMs);
        }

        /// <summary>
        /// Hides the bar until the next cue.
        /// </summary>
        public void ClearCue()
        {
            swap?.Pause();
            swap = null;
            Apply(null, null);
        }

        private void Apply(string speaker, string text)
        {
            speakerLabel.text = speaker ?? string.Empty;
            textLabel.text = text ?? string.Empty;
            speakerLabel.SetDisplayed(!string.IsNullOrEmpty(speaker));
            EnableInClassList(NoSpeakerClass, string.IsNullOrEmpty(speaker));
            EnableInClassList(EmptyClass, string.IsNullOrEmpty(text));
            RemoveFromClassList(ChangingClass);
            ApplyLineClamp();

            if (announce && !string.IsNullOrEmpty(text))
            {
                A11yAnnouncer.Announce(string.IsNullOrEmpty(speaker) ? text : $"{speaker}: {text}");
            }
        }

        private void ShowCueAt(float time)
        {
            var found = -1;
            for (var i = 0; i < cues.Count; i++)
            {
                if (time >= cues[i].Start && time < cues[i].End)
                {
                    found = i;
                    break;
                }
            }

            if (found == cueIndex)
            {
                return;
            }

            cueIndex = found;
            if (found >= 0)
            {
                SetCue(cues[found].Speaker, cues[found].Text);
            }
            else if (cues.Count > 0)
            {
                ClearCue();
            }

            CueChanged?.Invoke(found);
        }

        private void ApplyPlacement()
        {
            if (!floating)
            {
                style.top = StyleKeyword.Null;
                style.bottom = StyleKeyword.Null;
                return;
            }

            if (placement == CaptionPlacement.Top)
            {
                style.top = topInset;
                style.bottom = StyleKeyword.Null;
                return;
            }

            // Bottom and free: from the bottom inset, a free bar raised by its offset but kept inside the parent.
            var parentHeight = hierarchy.parent?.layout.height ?? float.NaN;
            var maxOffset = float.IsNaN(parentHeight) || float.IsNaN(layout.height) ? float.MaxValue : Mathf.Max(0f, parentHeight - bottomInset - topInset - layout.height);
            var offset = placement == CaptionPlacement.Free ? Mathf.Min(freeOffset, maxOffset) : 0f;
            style.top = StyleKeyword.Null;
            style.bottom = bottomInset + offset;
        }

        private void OnDragStart(PointerDownEvent evt)
        {
            if (!floating || placement != CaptionPlacement.Free || dragPointer != PointerId.invalidPointerId)
            {
                return;
            }

            dragPointer = evt.pointerId;
            dragStartY = evt.position.y;
            dragStartOffset = freeOffset;
            this.CapturePointer(dragPointer);
            evt.StopPropagation();
        }

        private void OnDragMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != dragPointer)
            {
                return;
            }

            // Dragging up (a smaller y) raises the bar.
            FreeOffset = dragStartOffset + (dragStartY - evt.position.y);
        }

        private void OnDragEnd(PointerUpEvent evt)
        {
            if (evt.pointerId != dragPointer)
            {
                return;
            }

            this.ReleasePointer(dragPointer);
            dragPointer = PointerId.invalidPointerId;
            Moved?.Invoke(freeOffset);
        }

        private int FadeDurationMs() => Mathf.CeilToInt(textLabel.LongestTransitionMs());

        private void ApplyLineClamp()
        {
            if (maxLines <= 0)
            {
                textLabel.style.maxHeight = StyleKeyword.Null;
                return;
            }

            // USS has no line clamp. Measure that many lines in the current font, so line spacing counts.
            // Then add the label padding and borders, because max-height includes them.
            if (lineSampleLines != maxLines)
            {
                lineSampleLines = maxLines;
                lineSample = "Ag";
                for (var i = 1; i < maxLines; i++)
                {
                    lineSample += "\nAg";
                }
            }

            var height = textLabel.MeasureTextSize(lineSample, 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined).y;
            if (float.IsNaN(height) || height <= 0f)
            {
                return;
            }

            static float OrZero(float value) => float.IsNaN(value) ? 0f : value;

            var box = textLabel.resolvedStyle;
            var chrome = OrZero(box.paddingTop) + OrZero(box.paddingBottom) + OrZero(box.borderTopWidth) + OrZero(box.borderBottomWidth);
            textLabel.style.maxHeight = Mathf.Ceil(height + chrome) + 1f;
        }
    }
}
