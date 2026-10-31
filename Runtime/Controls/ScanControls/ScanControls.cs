/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The stage a <see cref="ScanControls"/> shows.
    /// </summary>
    public enum ScanControlsState
    {
        /// <summary>
        /// Before scanning (FS04_2): Help and Start scan; no ring.
        /// </summary>
        Ready,

        /// <summary>
        /// While scanning (FS04_3): the coverage ring, the timer, Cancel and Stop and save.
        /// </summary>
        Scanning,

        /// <summary>
        /// While the map is saved: the ring stays, the actions hide.
        /// </summary>
        Saving,
    }

    /// <summary>
    /// The scan controls (FS04_2, FS04_3): a coverage ring near the top, and at the bottom a status line (timer,
    /// quality chip, warning, path preview) over two actions. A content layer for a mode screen; the host adds its own
    /// coaching card.
    /// </summary>
    /// <remarks>
    /// <para>The element fills its parent as a layer: the ring sits <c>top-inset</c> from the top, the actions
    /// <c>bottom-inset</c> from the bottom, and only the controls take input. Stop stays disabled until
    /// <see cref="ElapsedSeconds"/> reaches <see cref="MinSeconds"/>; <see cref="MaxReached"/> is raised once when it
    /// reaches <see cref="MaxSeconds"/>, so the host can stop and save.</para>
    /// <para>Props: <c>state</c>, <c>coverage</c> (0 to 1), <c>coverage-label</c> (shown in capitals),
    /// <c>elapsed-seconds</c>, <c>min-seconds</c>, <c>max-seconds</c> (0 for no limit), <c>bottom-gap</c> (default 34), <c>help-text</c>,
    /// <c>start-text</c>, <c>cancel-text</c>, <c>stop-text</c>, <c>top-inset</c>, <c>bottom-inset</c>.
    /// Methods: <see cref="SetQuality"/>, <see cref="ShowWarning"/>, <see cref="ClearWarning"/>, <see cref="SetPath"/>,
    /// <see cref="SetStartIcon"/>, <see cref="SetStopIcon"/>, <see cref="FormatTime"/>. Events:
    /// <see cref="StartRequested"/>, <see cref="StopRequested"/>, <see cref="HelpRequested"/>,
    /// <see cref="CancelRequested"/>, <see cref="MaxReached"/>.</para>
    /// <para>USS classes: <c>scanControls</c> (root), <c>scanControls__ring</c> (a <see cref="ProgressRing"/>),
    /// <c>scanControls__bottom</c>, <c>scanControls__status</c>, <c>scanControls__timer</c>,
    /// <c>scanControls__quality</c> and <c>scanControls__warning</c> (<see cref="StatusChip"/>s),
    /// <c>scanControls__path</c>, <c>scanControls__actions</c>, <c>scanControls__secondary</c>,
    /// <c>scanControls__primary</c>. Modifiers: <c>scanControls--ready</c>, <c>--scanning</c>, <c>--saving</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-border</c>, <c>--surface-fg</c>, <c>--surface-accent</c>,
    /// <c>--radius-pill</c>, <c>--radius-sm</c>, <c>--space-2..6</c>, <c>--text-label</c>, <c>--font-display</c>,
    /// and those of the ring, the chips and the pills.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ScanControls : VisualElement
    {
        public const string RootClass = "scanControls";
        public const string RingClass = "scanControls__ring";
        public const string BottomClass = "scanControls__bottom";
        public const string StatusClass = "scanControls__status";
        public const string TimerClass = "scanControls__timer";
        public const string QualityClass = "scanControls__quality";
        public const string WarningClass = "scanControls__warning";
        public const string PathClass = "scanControls__path";
        public const string ActionsClass = "scanControls__actions";
        public const string SecondaryClass = "scanControls__secondary";
        public const string PrimaryClass = "scanControls__primary";
        public const string ReadyClass = "scanControls--ready";
        public const string ScanningClass = "scanControls--scanning";
        public const string SavingClass = "scanControls--saving";

        private static readonly CustomStyleProperty<Color> PathColorProperty = new("--scan-path");

        /// <summary>
        /// The default gap between the actions and the bottom edge, before the inset (FS04_2).
        /// </summary>
        public const float DefaultBottomGap = 34f;

        private readonly ProgressRing ring;
        private readonly VisualElement bottom;
        private readonly VisualElement status;
        private readonly Label timer;
        private readonly StatusChip quality;
        private readonly StatusChip warning;
        private readonly VisualElement path;
        private readonly VisualElement actions;
        private readonly PillButton secondary;
        private readonly PillButton primary;
        private readonly List<Vector2> pathPoints = new();
        private ScanControlsState state = ScanControlsState.Ready;
        private string coverageLabel = "Coverage";
        private string helpText = "Help";
        private string startText = "Start scan";
        private string cancelText = "Cancel scan";
        private string stopText = "Stop and save";
        private Background startIcon;
        private Background stopIcon;
        private float elapsed;
        private float bottomInset;
        private float bottomGap = DefaultBottomGap;
        private float minSeconds;
        private float maxSeconds;
        private bool maxRaised;
        private Color pathColor = new(0.5f, 0.78f, 0.85f);

        public event Action StartRequested;

        public event Action StopRequested;

        public event Action HelpRequested;

        public event Action CancelRequested;

        /// <summary>
        /// Raised once per scan when <see cref="ElapsedSeconds"/> reaches <see cref="MaxSeconds"/>.
        /// </summary>
        public event Action MaxReached;

        public ProgressRing Ring => ring;

        public PillButton PrimaryButton => primary;

        public PillButton SecondaryButton => secondary;

        /// <summary>
        /// True when Stop is allowed: scanning and past <see cref="MinSeconds"/>.
        /// </summary>
        public bool CanStop => state == ScanControlsState.Scanning && elapsed >= minSeconds;

        [UxmlAttribute("state")]
        public ScanControlsState State
        {
            get => state;
            set
            {
                if (value == ScanControlsState.Scanning && state != ScanControlsState.Scanning)
                {
                    maxRaised = false;
                }

                state = value;
                Refresh();
            }
        }

        [UxmlAttribute("coverage")]
        public float Coverage
        {
            get => ring.Value;
            set => ring.Value = value;
        }

        [UxmlAttribute("coverage-label")]
        public string CoverageLabel
        {
            get => coverageLabel;
            set
            {
                coverageLabel = value ?? string.Empty;
                ring.Label = coverageLabel.ToUpperInvariant();
            }
        }

        [UxmlAttribute("elapsed-seconds")]
        public float ElapsedSeconds
        {
            get => elapsed;
            set
            {
                elapsed = Mathf.Max(0f, value);
                UpdateTimer();
                if (state == ScanControlsState.Scanning && maxSeconds > 0f && elapsed >= maxSeconds && !maxRaised)
                {
                    maxRaised = true;
                    MaxReached?.Invoke();
                }
            }
        }

        /// <summary>
        /// Seconds of scanning before Stop is allowed.
        /// </summary>
        [UxmlAttribute("min-seconds")]
        public float MinSeconds
        {
            get => minSeconds;
            set
            {
                minSeconds = Mathf.Max(0f, value);
                UpdateTimer();
            }
        }

        /// <summary>
        /// The longest scan in seconds; 0 for no limit. Shown after the timer, as "0:42 / 2:00".
        /// </summary>
        [UxmlAttribute("max-seconds")]
        public float MaxSeconds
        {
            get => maxSeconds;
            set
            {
                maxSeconds = Mathf.Max(0f, value);
                UpdateTimer();
            }
        }

        [UxmlAttribute("help-text")]
        public string HelpText
        {
            get => helpText;
            set
            {
                helpText = value ?? string.Empty;
                Refresh();
            }
        }

        [UxmlAttribute("start-text")]
        public string StartText
        {
            get => startText;
            set
            {
                startText = value ?? string.Empty;
                Refresh();
            }
        }

        [UxmlAttribute("cancel-text")]
        public string CancelText
        {
            get => cancelText;
            set
            {
                cancelText = value ?? string.Empty;
                Refresh();
            }
        }

        [UxmlAttribute("stop-text")]
        public string StopText
        {
            get => stopText;
            set
            {
                stopText = value ?? string.Empty;
                Refresh();
            }
        }

        /// <summary>
        /// Space above the ring, such as the context pill's height plus the safe area.
        /// </summary>
        [UxmlAttribute("top-inset")]
        public float TopInset
        {
            get => resolvedStyle.paddingTop;
            set => style.paddingTop = Mathf.Max(0f, value);
        }

        /// <summary>
        /// The gap between the actions and the bottom edge, before <see cref="BottomInset"/>. Default 34 (FS04_2).
        /// </summary>
        [UxmlAttribute("bottom-gap")]
        public float BottomGap
        {
            get => bottomGap;
            set
            {
                bottomGap = Mathf.Max(0f, value);
                ApplyBottom();
            }
        }

        /// <summary>
        /// Space below the actions, such as the bottom safe area, added to <see cref="BottomGap"/>.
        /// </summary>
        [UxmlAttribute("bottom-inset")]
        public float BottomInset
        {
            get => bottomInset;
            set
            {
                bottomInset = Mathf.Max(0f, value);
                ApplyBottom();
            }
        }

        public ScanControls()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            ring = new ProgressRing { Size = 120f, Thickness = 8f, ShowComplete = false };
            ring.AddToClassList(RingClass);
            Add(ring);

            bottom = UIToolkitExtensions.CreateVisualElement(this, BottomClass);
            bottom.pickingMode = PickingMode.Ignore;
            status = UIToolkitExtensions.CreateVisualElement(bottom, StatusClass);
            status.pickingMode = PickingMode.Ignore;
            timer = UIToolkitExtensions.CreateVisualElement<Label>(status, TimerClass).MakePill();
            quality = new StatusChip();
            quality.AddToClassList(QualityClass);
            status.Add(quality);
            warning = new StatusChip { State = StatusState.Warn, Filled = true };
            warning.AddToClassList(WarningClass);
            status.Add(warning);
            path = UIToolkitExtensions.CreateVisualElement(status, PathClass);
            path.pickingMode = PickingMode.Ignore;
            path.generateVisualContent += DrawPath;

            actions = UIToolkitExtensions.CreateVisualElement(bottom, ActionsClass);
            actions.pickingMode = PickingMode.Ignore;
            secondary = new PillButton { Variant = PillButtonVariant.Ghost };
            secondary.AddToClassList(SecondaryClass);
            secondary.Clicked += OnSecondary;
            actions.Add(secondary);
            primary = new PillButton { Variant = PillButtonVariant.Primary };
            primary.AddToClassList(PrimaryClass);
            primary.Clicked += OnPrimary;
            actions.Add(primary);

            RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.customStyle.TryGetValue(PathColorProperty, out var color))
                {
                    pathColor = color;
                    path.MarkDirtyRepaint();
                }
            });

            CoverageLabel = coverageLabel;
            SetQuality(null, StatusState.Info);
            ClearWarning();
            SetPath(null);
            Refresh();
        }

        /// <summary>
        /// Shows a quality chip, such as "Good detail" (Ok) or "Low light" (Warn). Null or empty hides it.
        /// </summary>
        public void SetQuality(string text, StatusState chipState)
        {
            quality.Text = text;
            quality.State = chipState;
            quality.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// Shows a filled amber warning chip, such as "Slow down".
        /// </summary>
        public void ShowWarning(string text)
        {
            warning.Text = text;
            warning.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void ClearWarning() => ShowWarning(null);

        /// <summary>
        /// Draws the walked path, normalised 0 to 1, in a small square; null or fewer than two points hides it.
        /// </summary>
        public void SetPath(IReadOnlyList<Vector2> points)
        {
            pathPoints.Clear();
            if (points != null)
            {
                pathPoints.AddRange(points);
            }

            path.style.display = pathPoints.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            path.MarkDirtyRepaint();
        }

        public void SetStartIcon(Background icon)
        {
            startIcon = icon;
            Refresh();
        }

        public void SetStopIcon(Background icon)
        {
            stopIcon = icon;
            Refresh();
        }

        /// <summary>
        /// "0:42", "12:05".
        /// </summary>
        public static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        private void ApplyBottom()
        {
            bottom.style.bottom = bottomGap + bottomInset;
        }

        private void OnPrimary()
        {
            if (state == ScanControlsState.Ready)
            {
                StartRequested?.Invoke();
            }
            else if (CanStop)
            {
                StopRequested?.Invoke();
            }
        }

        private void OnSecondary()
        {
            if (state == ScanControlsState.Ready)
            {
                HelpRequested?.Invoke();
            }
            else if (state == ScanControlsState.Scanning)
            {
                CancelRequested?.Invoke();
            }
        }

        private void Refresh()
        {
            EnableInClassList(ReadyClass, state == ScanControlsState.Ready);
            EnableInClassList(ScanningClass, state == ScanControlsState.Scanning);
            EnableInClassList(SavingClass, state == ScanControlsState.Saving);

            var ready = state == ScanControlsState.Ready;
            // The ring shows coverage while scanning only; once the scan stops it has nothing more to say.
            ring.style.display = state == ScanControlsState.Scanning ? DisplayStyle.Flex : DisplayStyle.None;
            status.style.display = state == ScanControlsState.Scanning ? DisplayStyle.Flex : DisplayStyle.None;
            actions.style.display = state == ScanControlsState.Saving ? DisplayStyle.None : DisplayStyle.Flex;

            secondary.Text = ready ? helpText : cancelText;
            secondary.style.display = string.IsNullOrEmpty(secondary.Text) ? DisplayStyle.None : DisplayStyle.Flex;
            primary.Text = ready ? startText : stopText;
            primary.Variant = ready ? PillButtonVariant.Primary : PillButtonVariant.Danger;
            primary.SetIcon(ready ? startIcon : stopIcon);
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            timer.text = maxSeconds > 0f ? $"{FormatTime(elapsed)} / {FormatTime(maxSeconds)}" : FormatTime(elapsed);
            primary.SetEnabled(state == ScanControlsState.Ready || CanStop);
        }

        private void DrawPath(MeshGenerationContext context)
        {
            if (pathPoints.Count < 2)
            {
                return;
            }

            var rect = path.contentRect;
            var painter = context.painter2D;
            painter.lineWidth = 2f;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.strokeColor = pathColor;
            painter.BeginPath();
            for (var i = 0; i < pathPoints.Count; i++)
            {
                var point = new Vector2(rect.x + (pathPoints[i].x * rect.width), rect.y + (pathPoints[i].y * rect.height));
                if (i == 0)
                {
                    painter.MoveTo(point);
                }
                else
                {
                    painter.LineTo(point);
                }
            }

            painter.Stroke();
        }
    }
}
