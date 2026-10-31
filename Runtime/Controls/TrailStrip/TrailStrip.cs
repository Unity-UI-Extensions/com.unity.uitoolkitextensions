/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// When a <see cref="TrailStrip"/> collapses to its "4 of 8" form.
    /// </summary>
    public enum TrailCompactMode
    {
        /// <summary>
        /// When the stops do not fit the width.
        /// </summary>
        Auto,
        Always,
        Never,
    }

    /// <summary>
    /// One stop on a <see cref="TrailStrip"/>. Strings are display text.
    /// </summary>
    public sealed class TrailStop
    {
        public string Id;

        /// <summary>
        /// Shown in the disc, such as "4". Empty shows the stop's position.
        /// </summary>
        public string Label;

        /// <summary>
        /// The stop's name, used as the disc's tooltip.
        /// </summary>
        public string Title;

        public TrailStop() { }

        public TrailStop(string id, string title = null, string label = null)
        {
            Id = id;
            Title = title;
            Label = label;
        }
    }

    /// <summary>
    /// Numbered stops joined by a progress line: done, current and pending. Tap a stop to see it. Collapses to
    /// "4 of 8" with a progress bar when the stops do not fit.
    /// </summary>
    /// <remarks>
    /// <para>Stops before <see cref="CurrentIndex"/> are done; the one at it is current. Props: <c>current-index</c>,
    /// <c>compact</c> (a <see cref="TrailCompactMode"/>), <c>compact-format</c> (default "{0} of {1}"; localise it).
    /// Method: <see cref="SetStops"/>. Event: <see cref="StopTapped"/> (id).</para>
    /// <para>USS classes: <c>trailStrip</c> (root), <c>trailStrip__track</c>, <c>trailStrip__stop</c>,
    /// <c>trailStrip__stopLabel</c>, <c>trailStrip__line</c>, <c>trailStrip__compact</c>,
    /// <c>trailStrip__compactLabel</c>, <c>trailStrip__compactBar</c> (a <see cref="LinearProgress"/>). Modifiers:
    /// <c>trailStrip--compact</c>; on stops <c>trailStrip__stop--done</c>, <c>--current</c>, <c>--pending</c>; on
    /// lines <c>trailStrip__line--done</c>.</para>
    /// <para>Tokens read: <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-border-strong</c>,
    /// <c>--surface-accent</c>, <c>--c-primary</c>, <c>--c-on-primary</c>, <c>--radius-pill</c>, <c>--space-1..2</c>,
    /// <c>--text-micro</c>, <c>--text-caption</c>, <c>--font-display</c>, <c>--font-body-medium</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class TrailStrip : VisualElement
    {
        public const string RootClass = "trailStrip";
        public const string TrackClass = "trailStrip__track";
        public const string StopClass = "trailStrip__stop";
        public const string StopLabelClass = "trailStrip__stopLabel";
        public const string LineClass = "trailStrip__line";
        public const string CompactClass = "trailStrip__compact";
        public const string CompactLabelClass = "trailStrip__compactLabel";
        public const string CompactBarClass = "trailStrip__compactBar";
        public const string CompactModifierClass = "trailStrip--compact";
        public const string DoneStopClass = "trailStrip__stop--done";
        public const string CurrentStopClass = "trailStrip__stop--current";
        public const string PendingStopClass = "trailStrip__stop--pending";
        public const string DoneLineClass = "trailStrip__line--done";

        /// <summary>
        /// Width each stop needs before the strip collapses: a 28 px disc and a 12 px line.
        /// </summary>
        public const float StopPitch = 40f;

        private readonly VisualElement track;
        private readonly VisualElement compact;
        private readonly Label compactLabel;
        private readonly LinearProgress compactBar;
        private readonly List<TrailStop> stops = new();
        private readonly List<VisualElement> stopElements = new();
        private readonly List<VisualElement> lines = new();
        private int currentIndex;
        private TrailCompactMode compactMode = TrailCompactMode.Auto;
        private string compactFormat = "{0} of {1}";

        public event Action<string> StopTapped;

        public IReadOnlyList<TrailStop> Stops => stops;

        public bool IsCompact => ClassListContains(CompactModifierClass);

        [UxmlAttribute("current-index")]
        public int CurrentIndex
        {
            get => currentIndex;
            set
            {
                currentIndex = Mathf.Max(0, value);
                UpdateStates();
            }
        }

        [UxmlAttribute("compact")]
        public TrailCompactMode Compact
        {
            get => compactMode;
            set
            {
                compactMode = value;
                UpdateCompact();
            }
        }

        /// <summary>
        /// The collapsed text, with {0} the current stop number and {1} the count.
        /// </summary>
        [UxmlAttribute("compact-format")]
        public string CompactFormat
        {
            get => compactFormat;
            set
            {
                compactFormat = string.IsNullOrEmpty(value) ? "{0} of {1}" : value;
                UpdateStates();
            }
        }

        public TrailStrip()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            track = UIToolkitExtensions.CreateVisualElement(this, TrackClass);
            compact = UIToolkitExtensions.CreateVisualElement(this, CompactClass);
            compact.pickingMode = PickingMode.Ignore;
            compactLabel = UIToolkitExtensions.CreateVisualElement<Label>(compact, CompactLabelClass);
            compactBar = new LinearProgress();
            compactBar.AddToClassList(CompactBarClass);
            compact.Add(compactBar);

            RegisterCallback<GeometryChangedEvent>(_ => UpdateCompact());
            UpdateCompact();
        }

        public void SetStops(IReadOnlyList<TrailStop> newStops, int current = -1)
        {
            stops.Clear();
            stopElements.Clear();
            lines.Clear();
            track.Clear();

            if (newStops != null)
            {
                for (var i = 0; i < newStops.Count; i++)
                {
                    var stop = newStops[i];
                    stops.Add(stop);
                    if (i > 0)
                    {
                        var line = UIToolkitExtensions.CreateVisualElement(track, LineClass);
                        line.pickingMode = PickingMode.Ignore;
                        lines.Add(line);
                    }

                    var element = UIToolkitExtensions.CreateVisualElement(track, StopClass);
                    element.focusable = true;
                    element.tabIndex = 0;
                    element.tooltip = stop.Title;
                    var id = stop.Id;
                    element.RegisterCallback<ClickEvent>(_ => StopTapped?.Invoke(id));
                    element.RegisterCallback<NavigationSubmitEvent>(evt =>
                    {
                        StopTapped?.Invoke(id);
                        evt.StopPropagation();
                    });

                    var label = UIToolkitExtensions.CreateVisualElement<Label>(element, StopLabelClass);
                    label.pickingMode = PickingMode.Ignore;
                    label.text = string.IsNullOrEmpty(stop.Label) ? (i + 1).ToString() : stop.Label;
                    stopElements.Add(element);
                }
            }

            if (current >= 0)
            {
                currentIndex = current;
            }

            UpdateStates();
            UpdateCompact();
        }

        /// <summary>
        /// The disc for a stop, for focus or tests.
        /// </summary>
        public VisualElement StopElement(int index) => stopElements[index];

        private void UpdateStates()
        {
            for (var i = 0; i < stopElements.Count; i++)
            {
                stopElements[i].EnableInClassList(DoneStopClass, i < currentIndex);
                stopElements[i].EnableInClassList(CurrentStopClass, i == currentIndex);
                stopElements[i].EnableInClassList(PendingStopClass, i > currentIndex);
            }

            for (var i = 0; i < lines.Count; i++)
            {
                // The line before stop i + 1 is done once that stop is reached.
                lines[i].EnableInClassList(DoneLineClass, i + 1 <= currentIndex);
            }

            var count = stops.Count;
            var shown = count == 0 ? 0 : Mathf.Min(currentIndex + 1, count);
            compactLabel.text = string.Format(compactFormat, shown, count);
            compactBar.Value = count == 0 ? 0f : shown / (float)count;
        }

        private void UpdateCompact()
        {
            var collapse = compactMode switch
            {
                TrailCompactMode.Always => true,
                TrailCompactMode.Never => false,
                _ => !float.IsNaN(layout.width) && layout.width > 0f && layout.width < stops.Count * StopPitch,
            };

            EnableInClassList(CompactModifierClass, collapse);
        }
    }
}
