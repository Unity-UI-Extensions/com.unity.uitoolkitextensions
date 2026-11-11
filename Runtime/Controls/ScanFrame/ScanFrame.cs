/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The colour of a <see cref="ScanFrame"/>'s brackets.
    /// </summary>
    public enum ScanFrameTone
    {
        /// <summary>
        /// Accent: looking for a code or a marker.
        /// </summary>
        Neutral,

        /// <summary>
        /// Found and recognised.
        /// </summary>
        Success,

        /// <summary>
        /// Found but not usable, such as a code from another app.
        /// </summary>
        Warn,
    }

    /// <summary>
    /// Four corner brackets over the camera view that frame where to point, or hug a tracked marker.
    /// </summary>
    /// <remarks>
    /// <para>The frame fills its parent as a pass-through layer. By default the brackets frame a centred square of
    /// <see cref="Size"/>; padding on the root moves that centre. <see cref="SetTargetRect"/> moves them around a
    /// rectangle in panel space, such as a tracked image projected to the screen, with <see cref="TargetPadding"/>
    /// around it; <see cref="ClearTarget"/> returns to the centred square.</para>
    /// <para>While <see cref="Searching"/>, the brackets pulse gently. The pulse stops with reduce motion.
    /// The class <c>scanFrame--pulsed</c> marks the far point of the pulse.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ScanFrame : VisualElement
    {
        public const string RootClass = "scanFrame";
        public const string FrameClass = "scanFrame__frame";
        public const string CornerClass = "scanFrame__corner";
        public const string SuccessClass = "scanFrame--success";
        public const string WarnClass = "scanFrame--warn";
        public const string SearchingClass = "scanFrame--searching";
        public const string PulsedClass = "scanFrame--pulsed";
        public const string TargetedClass = "scanFrame--targeted";

        /// <summary>
        /// The default side of the centred square, in pixels.
        /// </summary>
        public const float DefaultSize = 250f;

        /// <summary>
        /// Time between pulse phases in milliseconds.
        /// </summary>
        public const int PulseIntervalMs = 900;

        private static readonly string[] CornerNames = { "topLeft", "topRight", "bottomLeft", "bottomRight" };

        private readonly VisualElement frame;
        private ScanFrameTone tone;
        private float size = DefaultSize;
        private float targetPadding = 20f;
        private bool searching;
        private bool targeted;
        private DisplayedTimer pulse;

        public VisualElement Frame => frame;

        [UxmlAttribute("tone")]
        public ScanFrameTone Tone
        {
            get => tone;
            set
            {
                tone = value;
                EnableInClassList(SuccessClass, tone == ScanFrameTone.Success);
                EnableInClassList(WarnClass, tone == ScanFrameTone.Warn);
            }
        }

        [UxmlAttribute("size")]
        public float Size
        {
            get => size;
            set
            {
                size = Mathf.Max(0f, value);
                if (!targeted)
                {
                    frame.style.width = size;
                    frame.style.height = size;
                }
            }
        }

        /// <summary>
        /// Space between a target rectangle and the brackets, in pixels.
        /// </summary>
        [UxmlAttribute("target-padding")]
        public float TargetPadding
        {
            get => targetPadding;
            set => targetPadding = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Pulses the brackets while looking for a code or marker.
        /// </summary>
        [UxmlAttribute("searching")]
        public bool Searching
        {
            get => searching;
            set
            {
                searching = value;
                EnableInClassList(SearchingClass, searching);
                if (searching)
                {
                    StartPulse();
                }
                else
                {
                    StopPulse();
                }
            }
        }

        public bool IsTargeted => targeted;

        /// <summary>
        /// Whether the pulse is running now (searching, attached, no reduce-motion ancestor).
        /// </summary>
        public bool IsPulsing => searching && panel != null && !this.IsReduceMotion();

        public ScanFrame()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            frame = UIToolkitExtensions.CreateVisualElement(this, FrameClass).IgnorePicking();
            foreach (var name in CornerNames)
            {
                var corner = UIToolkitExtensions.CreateVisualElement(frame, CornerClass);
                corner.AddToClassList($"{CornerClass}--{name}");
                corner.pickingMode = PickingMode.Ignore;
            }

            RegisterCallback<AttachToPanelEvent>(_ => { if (searching) StartPulse(); });
            RegisterCallback<DetachFromPanelEvent>(_ => StopPulse());

            Size = DefaultSize;
        }

        /// <summary>
        /// Moves the brackets around a rectangle in panel space, such as a tracked marker's screen bounds converted
        /// with <c>RuntimePanelUtils.ScreenToPanel</c>. Call it each frame while tracking.
        /// </summary>
        public void SetTargetRect(Rect panelRect)
        {
            var local = this.WorldToLocal(panelRect);
            targeted = true;
            AddToClassList(TargetedClass);
            frame.style.left = local.xMin - targetPadding;
            frame.style.top = local.yMin - targetPadding;
            frame.style.width = local.width + targetPadding * 2f;
            frame.style.height = local.height + targetPadding * 2f;
        }

        public void ClearTarget()
        {
            targeted = false;
            RemoveFromClassList(TargetedClass);
            frame.style.left = StyleKeyword.Null;
            frame.style.top = StyleKeyword.Null;
            Size = size;
        }

        private void StartPulse()
        {
            StopPulse();
            pulse = DisplayedTimer.Start(this, () =>
            {
                if (IsPulsing)
                {
                    ToggleInClassList(PulsedClass);
                }
                else
                {
                    RemoveFromClassList(PulsedClass);
                }
            }, PulseIntervalMs, PulseIntervalMs);
        }

        private void StopPulse()
        {
            pulse?.Stop();
            pulse = null;
            RemoveFromClassList(PulsedClass);
        }
    }
}
