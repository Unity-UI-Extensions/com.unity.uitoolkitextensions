/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A thin progress bar: a track and a fill in the primary-to-accent gradient.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>value</c> (0 to 1), <c>indeterminate</c> (a short segment that sweeps back and forth when the
    /// amount is unknown; it rests in the middle with reduce motion). The fill animates with <c>--motion-base</c>, the
    /// sweep with <c>--motion-slow</c>.</para>
    /// <para>USS classes: <c>linearProgress</c> (root, the track), <c>linearProgress__fill</c>.
    /// Modifiers: <c>linearProgress--complete</c> (value 1), <c>linearProgress--indeterminate</c>,
    /// <c>linearProgress--swept</c> (the far end of the sweep).</para>
    /// <para>Tokens read: <c>--surface-border</c>, <c>--c-primary</c>, <c>--c-accent</c>, <c>--motion-base</c>,
    /// <c>--motion-slow</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class LinearProgress : VisualElement
    {
        public const string RootClass = "linearProgress";
        public const string FillClass = "linearProgress__fill";
        public const string CompleteClass = "linearProgress--complete";
        public const string IndeterminateClass = "linearProgress--indeterminate";
        public const string SweptClass = "linearProgress--swept";

        /// <summary>
        /// Time between sweep phases.
        /// </summary>
        public const int SweepIntervalMs = 600;

        /// <summary>
        /// The width of the sweeping segment, in percent of the track.
        /// </summary>
        public const float SegmentPercent = 35f;

        private readonly VisualElement fill;
        private float value;
        private bool indeterminate;
        private DisplayedTimer sweep;

        public VisualElement Fill => fill;

        [UxmlAttribute("value")]
        public float Value
        {
            get => value;
            set
            {
                this.value = Mathf.Clamp01(float.IsNaN(value) ? 0f : value);
                fill.style.width = Length.Percent(indeterminate ? SegmentPercent : this.value * 100f);
                EnableInClassList(CompleteClass, !indeterminate && this.value >= 1f);
            }
        }

        /// <summary>
        /// Shows a sweeping segment in place of the amount.
        /// </summary>
        [UxmlAttribute("indeterminate")]
        public bool Indeterminate
        {
            get => indeterminate;
            set
            {
                indeterminate = value;
                EnableInClassList(IndeterminateClass, indeterminate);
                Value = this.value;
                if (indeterminate)
                {
                    StartSweep();
                }
                else
                {
                    StopSweep();
                }
            }
        }

        /// <summary>
        /// Whether the sweep is running now (indeterminate, attached, no reduce-motion ancestor).
        /// </summary>
        public bool IsSweeping => indeterminate && panel != null && !this.IsReduceMotion();

        public LinearProgress()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            fill = UIToolkitExtensions.CreateVisualElement(this, FillClass);
            fill.pickingMode = PickingMode.Ignore;
            fill.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));

            RegisterCallback<AttachToPanelEvent>(_ => { if (indeterminate) StartSweep(); });
            RegisterCallback<DetachFromPanelEvent>(_ => StopSweep());

            Value = 0f;
        }

        public LinearProgress(float value)
            : this()
        {
            Value = value;
        }

        private void StartSweep()
        {
            StopSweep();
            sweep = DisplayedTimer.Start(this, () =>
            {
                if (IsSweeping)
                {
                    ToggleInClassList(SweptClass);
                }
                else
                {
                    RemoveFromClassList(SweptClass);
                }
            }, SweepIntervalMs, SweepIntervalMs);
        }

        private void StopSweep()
        {
            sweep?.Stop();
            sweep = null;
            RemoveFromClassList(SweptClass);
        }
    }
}
