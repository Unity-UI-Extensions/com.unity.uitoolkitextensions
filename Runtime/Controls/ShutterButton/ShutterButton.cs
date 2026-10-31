/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The camera shutter: a white ring around a brand-gradient disc. Tap to take a photo; with
    /// <see cref="HoldEnabled"/>, press and hold to record a video.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>size</c> (outer diameter, default 84; 58 in the HUD bar), <c>hold-enabled</c>,
    /// <c>hold-threshold-ms</c> (default 300), <c>recording</c>, <c>progress</c> (0 to 1 drawn round the ring while
    /// recording, negative hides it). Events: <see cref="Tapped"/> (also raised as <see cref="Pressed"/>),
    /// <see cref="HoldStarted"/>, <see cref="HoldEnded"/> (true when released over the button).</para>
    /// <para>While holding, the button keeps the pointer, so sliding off still ends the hold. The app sets
    /// <see cref="Recording"/> once recording has really started, and clears it when it stops (FS08_4: a red ring
    /// round a red rounded square). Disable with <c>SetEnabled(false)</c>, for example during a countdown.
    /// Focusable; submit taps it.</para>
    /// <para>USS classes: <c>shutterButton</c> (root, the ring), <c>shutterButton__disc</c>. Modifiers:
    /// <c>shutterButton--holding</c>, <c>shutterButton--recording</c>, <c>shutterButton--small</c> (under 72 px).</para>
    /// <para>Tokens read: <c>--c-fg</c>, <c>--grad-brand-start/end</c> (through <see cref="GradientFill"/>),
    /// <c>--c-record</c>, <c>--c-on-record</c>, <c>--surface-accent</c>, <c>--radius-pill</c>,
    /// <c>--radius-sm</c>, <c>--motion-fast</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ShutterButton : VisualElement
    {
        public const string RootClass = "shutterButton";
        public const string DiscClass = "shutterButton__disc";
        public const string HoldingClass = "shutterButton--holding";
        public const string RecordingClass = "shutterButton--recording";
        public const string SmallClass = "shutterButton--small";

        /// <summary>
        /// Below this diameter the ring is 3 px with a 4 px gap (FS08_1), above it 4 px with a 5 px gap.
        /// </summary>
        public const float SmallSize = 72f;

        private static readonly CustomStyleProperty<Color> ProgressColorProperty = new("--shutter-progress");

        private readonly VisualElement disc;
        private readonly GradientFill discFill = new(GradientPreset.Brand, GradientDirection.Horizontal);
        private readonly LongPressManipulator longPress;
        private float size = 84f;
        private bool holdEnabled;
        private bool recording;
        private float progress = -1f;
        private Color progressColor = Color.white;

        /// <summary>
        /// Raised for a tap: take a photo.
        /// </summary>
        public event Action Tapped;

        /// <summary>
        /// Raised with <see cref="Tapped"/>; kept for code written before hold-for-video.
        /// </summary>
        public event Action Pressed;

        /// <summary>
        /// Raised when a press has lasted the hold threshold: start recording.
        /// </summary>
        public event Action HoldStarted;

        /// <summary>
        /// Raised when a hold ends: true when released over the button, false when cancelled or released off it.
        /// </summary>
        public event Action<bool> HoldEnded;

        public bool IsHolding => longPress.IsHolding;

        /// <summary>
        /// The press-and-hold manipulator, attached while <see cref="HoldEnabled"/> is on; exposed for tests.
        /// </summary>
        public LongPressManipulator LongPress => longPress;

        [UxmlAttribute("size")]
        public float Size
        {
            get => size;
            set
            {
                size = Mathf.Max(44f, value);
                var small = size < SmallSize;
                var inset = small ? 7f : 9f;
                EnableInClassList(SmallClass, small);
                style.width = size;
                style.height = size;
                disc.style.width = size - (2f * inset);
                disc.style.height = size - (2f * inset);
                UpdateRecordingDisc();
            }
        }

        /// <summary>
        /// When on, a press that lasts <see cref="HoldThresholdMs"/> raises <see cref="HoldStarted"/> instead of a tap.
        /// </summary>
        [UxmlAttribute("hold-enabled")]
        public bool HoldEnabled
        {
            get => holdEnabled;
            set
            {
                if (holdEnabled == value)
                {
                    return;
                }

                holdEnabled = value;
                if (holdEnabled)
                {
                    this.AddManipulator(longPress);
                }
                else
                {
                    this.RemoveManipulator(longPress);
                }
            }
        }

        [UxmlAttribute("hold-threshold-ms")]
        public int HoldThresholdMs
        {
            get => longPress.ThresholdMs;
            set => longPress.ThresholdMs = value;
        }

        /// <summary>
        /// Shows the recording look: a red ring round a red rounded square.
        /// </summary>
        [UxmlAttribute("recording")]
        public bool Recording
        {
            get => recording;
            set
            {
                recording = value;
                EnableInClassList(RecordingClass, recording);
                if (recording)
                {
                    discFill.target?.RemoveManipulator(discFill);
                }
                else if (discFill.target == null)
                {
                    disc.AddManipulator(discFill);
                }

                UpdateRecordingDisc();
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Recording time used, 0 to 1, drawn round the ring while recording. Negative hides it.
        /// </summary>
        [UxmlAttribute("progress")]
        public float Progress
        {
            get => progress;
            set
            {
                progress = value < 0f ? -1f : Mathf.Clamp01(value);
                MarkDirtyRepaint();
            }
        }

        public ShutterButton()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            focusable = true;
            tabIndex = 0;

            disc = UIToolkitExtensions.CreateVisualElement(this, DiscClass);
            disc.pickingMode = PickingMode.Ignore;
            disc.AddManipulator(discFill);

            longPress = new LongPressManipulator();
            longPress.Tapped += Tap;
            longPress.HoldStarted += _ =>
            {
                AddToClassList(HoldingClass);
                HoldStarted?.Invoke();
            };
            longPress.HoldEnded += released =>
            {
                RemoveFromClassList(HoldingClass);
                HoldEnded?.Invoke(released);
            };

            RegisterCallback<ClickEvent>(_ =>
            {
                // With hold on, the manipulator decides between a tap and a hold.
                if (!holdEnabled)
                {
                    Tap();
                }
            });
            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                Tap();
                evt.StopPropagation();
            });
            RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.customStyle.TryGetValue(ProgressColorProperty, out var color))
                {
                    progressColor = color;
                    MarkDirtyRepaint();
                }
            });
            generateVisualContent += DrawProgress;

            Size = size;
        }

        private void Tap()
        {
            if (!enabledInHierarchy)
            {
                return;
            }

            Tapped?.Invoke();
            Pressed?.Invoke();
        }

        private void UpdateRecordingDisc()
        {
            if (disc == null)
            {
                return;
            }

            // The recording square is 45% of the diameter: 26 px in the 58 px HUD shutter.
            var inset = size < SmallSize ? 7f : 9f;
            var side = recording ? Mathf.Round(size * 0.45f) : size - (2f * inset);
            disc.style.width = side;
            disc.style.height = side;
        }

        private void DrawProgress(MeshGenerationContext context)
        {
            if (!recording || progress <= 0f)
            {
                return;
            }

            var border = resolvedStyle.borderTopWidth;
            var rect = new Rect(Vector2.zero, layout.size);
            var radius = (Mathf.Min(rect.width, rect.height) - border) * 0.5f;
            if (radius <= 0f || border <= 0f)
            {
                return;
            }

            // Over the ring, from twelve o'clock clockwise.
            var painter = context.painter2D;
            painter.lineWidth = border;
            painter.lineCap = LineCap.Butt;
            painter.strokeColor = progressColor;
            painter.BeginPath();
            painter.Arc(rect.center, radius, -90f, -90f + (360f * progress));
            painter.Stroke();
        }
    }
}
