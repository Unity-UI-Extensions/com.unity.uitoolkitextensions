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
        /// The translucent dark card with the accent edge (FS07_2).
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
    /// size. The cross-fade uses <c>--motion-fast</c>, so <c>reduce-motion</c> swaps the text at once. The owner syncs
    /// cues to the audio; the bar only shows them.</para>
    /// <para>Props: <c>speaker</c>, <c>text</c>, <c>max-lines</c>, <c>caption-size</c> (Small, Medium, Large,
    /// ExtraLarge), <c>background</c> (Panel, Solid, None), <c>floating</c> (the bar positions itself in its parent),
    /// <c>placement</c> (Bottom, Top, Free: drag to move), <c>top-inset</c>, <c>bottom-inset</c> (keep clear of a sheet
    /// or the HUD bar), <c>announce</c> (speak each cue through <see cref="A11yAnnouncer"/>). Methods:
    /// <see cref="SetCue"/>, <see cref="ClearCue"/>, <see cref="SetCues"/> with <see cref="Position"/> (shows the cue for a
    /// time), <see cref="SyncTo"/> (follows an <see cref="AudioSource"/>), <see cref="StopSync"/>. Events:
    /// <see cref="CueChanged"/> (cue index, or -1), <see cref="Moved"/> (the free offset). Property: <see cref="IsEmpty"/>.</para>
    /// <para>USS classes: <c>captionBar</c> (root), <c>captionBar__speaker</c>, <c>captionBar__text</c>.</para>
    /// <para>Modifier classes: <c>captionBar--empty</c> (no cue: hidden), <c>captionBar--changing</c> (the faded-out
    /// moment of a cross-fade), <c>captionBar--noSpeaker</c>, <c>captionBar--small</c>, <c>--large</c>,
    /// <c>--extraLarge</c>, <c>captionBar--solid</c>, <c>captionBar--plain</c>, <c>captionBar--floating</c>,
    /// <c>captionBar--top</c>, <c>captionBar--free</c>.</para>
    /// <para>Tokens read: <c>--c-panel</c>, <c>--c-divider</c>, <c>--c-primary</c>, <c>--c-accent</c>, <c>--c-fg</c>,
    /// <c>--c-panel-strong</c>, <c>--space-3..4</c>, <c>--text-micro</c>, <c>--text-caption</c>, <c>--text-label</c>,
    /// <c>--text-body</c>, <c>--text-subtitle</c>, <c>--font-body</c>, <c>--font-body-medium</c>, <c>--motion-fast</c>.
    /// Captions sit over the camera, so they use the deep palette directly.</para>
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
        /// How often <see cref="SyncTo"/> reads the audio position.
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
        private AudioSource syncSource;
        private DisplayedTimer syncTimer;
        private int dragPointer = PointerId.invalidPointerId;
        private float dragStartY;
        private float dragStartOffset;
        private int maxLines = 3;
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

            speakerLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, SpeakerClass);
            speakerLabel.pickingMode = PickingMode.Ignore;
            textLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, TextClass);
            textLabel.pickingMode = PickingMode.Ignore;
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
        /// Follows an audio source: every 100 ms the bar shows the cue for its time. Pauses while the bar is hidden and
        /// stops on detach.
        /// </summary>
        public void SyncTo(AudioSource source)
        {
            StopSync();
            syncSource = source;
            if (source == null)
            {
                return;
            }

            // Paused while the bar is hidden; the next tick catches up with the source's time.
            syncTimer = DisplayedTimer.Start(this, () =>
            {
                if (syncSource != null)
                {
                    Position = syncSource.time;
                }
            }, SyncIntervalMs);
        }

        public void StopSync()
        {
            syncTimer?.Stop();
            syncTimer = null;
            syncSource = null;
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
            speakerLabel.style.display = string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;
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
            var found = cues.FindIndex(c => time >= c.Start && time < c.End);
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

        private int FadeDurationMs()
        {
            var longest = 0f;
            foreach (var duration in textLabel.resolvedStyle.transitionDuration)
            {
                longest = Mathf.Max(longest, duration.unit == TimeUnit.Second ? duration.value * 1000f : duration.value);
            }

            return Mathf.CeilToInt(longest);
        }

        private void ApplyLineClamp()
        {
            if (maxLines <= 0)
            {
                textLabel.style.maxHeight = StyleKeyword.Null;
                return;
            }

            // USS has no line clamp: measure that many lines in the current font, so line spacing counts, then add
            // the label's padding and borders, which max-height includes.
            var lines = new string[maxLines];
            for (var i = 0; i < maxLines; i++)
            {
                lines[i] = "Ag";
            }

            var height = textLabel.MeasureTextSize(string.Join("\n", lines), 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined).y;
            if (float.IsNaN(height) || height <= 0f)
            {
                return;
            }

            var box = textLabel.resolvedStyle;
            var chrome = Sum(box.paddingTop, box.paddingBottom, box.borderTopWidth, box.borderBottomWidth);
            textLabel.style.maxHeight = Mathf.Ceil(height + chrome) + 1f;
        }

        private static float Sum(params float[] values)
        {
            var total = 0f;
            foreach (var value in values)
            {
                total += float.IsNaN(value) ? 0f : value;
            }

            return total;
        }
    }
}
