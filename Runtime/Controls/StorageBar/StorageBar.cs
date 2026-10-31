/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One part of a <see cref="StorageBar"/>. The label is display text.
    /// </summary>
    public sealed class StorageSegment
    {
        public string Id;
        public string Label;
        public long Bytes;

        public StorageSegment() { }

        public StorageSegment(string id, string label, long bytes)
        {
            Id = id;
            Label = label;
            Bytes = bytes;
        }
    }

    /// <summary>
    /// Storage use (FS10_2): a title and a free-space note, a stacked bar with one segment per item and then "Other",
    /// a legend with sizes, and an optional free-up action.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>title-text</c> (such as "412 MB used by this app"), <c>detail</c> (such as "6.1 GB free"),
    /// <c>other-label</c> (default "Other"), <c>free-up-text</c> (empty hides the action). Methods:
    /// <see cref="SetSegments"/> (items, the "Other" bytes and the bar's full scale), <see cref="FormatBytes"/>.
    /// <see cref="SizeFormatter"/> formats the legend's sizes; set it to the app's own formatter so the legend
    /// matches the title (the default, <see cref="FormatBytes"/>, uses decimal units).
    /// Event: <see cref="FreeUpRequested"/>.</para>
    /// <para>The bar's full width is <c>scaleBytes</c>, such as the space an app may use; 0 makes the segments fill it.
    /// Segment colours cycle through four classes; "Other" is always the muted one.</para>
    /// <para>USS classes: <c>storageBar</c> (root), <c>storageBar__header</c>, <c>storageBar__title</c>,
    /// <c>storageBar__detail</c>, <c>storageBar__track</c>, <c>storageBar__segment</c>, <c>storageBar__legend</c>,
    /// <c>storageBar__legendItem</c>, <c>storageBar__dot</c>, <c>storageBar__legendLabel</c>, <c>storageBar__freeUp</c>
    /// (a ghost <see cref="PillButton"/>). Segment and dot modifiers: <c>storageBar__segment--0</c> to <c>--3</c> and
    /// <c>--other</c> (the same suffixes on <c>storageBar__dot</c>).</para>
    /// <para>Tokens read: <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-skeleton</c>, <c>--c-navy-2</c>,
    /// <c>--c-primary</c>, <c>--c-accent</c>, <c>--c-fg-muted</c>, <c>--space-1..4</c>, <c>--text-micro</c>,
    /// <c>--text-caption</c>, <c>--font-display</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class StorageBar : VisualElement
    {
        public const string RootClass = "storageBar";
        public const string HeaderClass = "storageBar__header";
        public const string TitleClass = "storageBar__title";
        public const string DetailClass = "storageBar__detail";
        public const string TrackClass = "storageBar__track";
        public const string SegmentClass = "storageBar__segment";
        public const string LegendClass = "storageBar__legend";
        public const string LegendItemClass = "storageBar__legendItem";
        public const string DotClass = "storageBar__dot";
        public const string LegendLabelClass = "storageBar__legendLabel";
        public const string FreeUpClass = "storageBar__freeUp";
        public const string OtherSuffix = "--other";

        /// <summary>
        /// Segments take these colour classes in turn.
        /// </summary>
        public const int PaletteSize = 4;

        private readonly Label title;
        private readonly Label detail;
        private readonly VisualElement track;
        private readonly VisualElement legend;
        private readonly PillButton freeUp;
        private readonly List<StorageSegment> segments = new();
        private string otherLabel = "Other";
        private long otherBytes;
        private long scaleBytes;

        public event Action FreeUpRequested;

        private Func<long, string> sizeFormatter = FormatBytes;

        /// <summary>
        /// Formats the legend's sizes. Null restores <see cref="FormatBytes"/>.
        /// </summary>
        public Func<long, string> SizeFormatter
        {
            get => sizeFormatter;
            set
            {
                sizeFormatter = value ?? FormatBytes;
                Rebuild();
            }
        }

        public IReadOnlyList<StorageSegment> Segments => segments;

        public VisualElement Track => track;

        public PillButton FreeUpButton => freeUp;

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => title.text = value ?? string.Empty;
        }

        [UxmlAttribute("detail")]
        public string Detail
        {
            get => detail.text;
            set
            {
                detail.text = value ?? string.Empty;
                detail.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("other-label")]
        public string OtherLabel
        {
            get => otherLabel;
            set
            {
                otherLabel = value ?? string.Empty;
                Rebuild();
            }
        }

        [UxmlAttribute("free-up-text")]
        public string FreeUpText
        {
            get => freeUp.Text;
            set
            {
                freeUp.Text = value ?? string.Empty;
                freeUp.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public StorageBar()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var header = UIToolkitExtensions.CreateVisualElement(this, HeaderClass);
            title = UIToolkitExtensions.CreateVisualElement<Label>(header, TitleClass);
            detail = UIToolkitExtensions.CreateVisualElement<Label>(header, DetailClass);
            track = UIToolkitExtensions.CreateVisualElement(this, TrackClass);
            legend = UIToolkitExtensions.CreateVisualElement(this, LegendClass);

            freeUp = new PillButton { Variant = PillButtonVariant.Ghost };
            freeUp.AddToClassList(FreeUpClass);
            freeUp.Clicked += () => FreeUpRequested?.Invoke();
            Add(freeUp);

            Detail = null;
            FreeUpText = null;
        }

        /// <summary>
        /// Sets the items, the bytes shown as "Other" (0 hides it), and the bytes the full bar stands for (0 for the sum).
        /// </summary>
        public void SetSegments(IReadOnlyList<StorageSegment> items, long other = 0, long scale = 0)
        {
            segments.Clear();
            if (items != null)
            {
                segments.AddRange(items);
            }

            otherBytes = Math.Max(0, other);
            scaleBytes = Math.Max(0, scale);
            Rebuild();
        }

        /// <summary>
        /// "412 MB", "6.1 GB", "980 KB": decimal units, as phones report storage.
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            const double kb = 1000d;
            const double mb = kb * 1000d;
            const double gb = mb * 1000d;
            if (bytes >= gb)
            {
                return $"{bytes / gb:0.0} GB";
            }

            if (bytes >= mb)
            {
                return $"{Math.Round(bytes / mb):0} MB";
            }

            return bytes >= kb ? $"{Math.Round(bytes / kb):0} KB" : $"{Math.Max(0, bytes)} B";
        }

        private void Rebuild()
        {
            track.Clear();
            legend.Clear();

            var used = otherBytes;
            foreach (var segment in segments)
            {
                used += Math.Max(0, segment.Bytes);
            }

            var scale = scaleBytes > 0 ? Math.Max(scaleBytes, used) : used;
            for (var i = 0; i < segments.Count; i++)
            {
                var suffix = "--" + (i % PaletteSize);
                AddPart(segments[i].Label, segments[i].Bytes, scale, suffix);
            }

            if (otherBytes > 0)
            {
                AddPart(otherLabel, otherBytes, scale, OtherSuffix);
            }
        }

        private void AddPart(string label, long bytes, long scale, string suffix)
        {
            var part = UIToolkitExtensions.CreateVisualElement(track, SegmentClass, SegmentClass + suffix);
            part.pickingMode = PickingMode.Ignore;
            part.style.width = Length.Percent(scale > 0 ? Mathf.Clamp01(bytes / (float)scale) * 100f : 0f);

            var item = UIToolkitExtensions.CreateVisualElement(legend, LegendItemClass);
            UIToolkitExtensions.CreateVisualElement(item, DotClass, DotClass + suffix);
            var text = UIToolkitExtensions.CreateVisualElement<Label>(item, LegendLabelClass);
            text.text = $"{label} {sizeFormatter(bytes)}";
        }
    }
}
