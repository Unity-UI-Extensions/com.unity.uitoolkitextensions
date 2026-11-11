/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One nearby place for <see cref="WayfinderChips"/>. The name is display text.
    /// </summary>
    public sealed class WayfinderChipItem
    {
        public string Id;
        public string Name;

        /// <summary>
        /// Distance in metres; negative hides it.
        /// </summary>
        public float DistanceMetres = -1f;

        /// <summary>
        /// Direction relative to the camera, in degrees clockwise from straight ahead.
        /// </summary>
        public float BearingDegrees;

        /// <summary>
        /// An optional colour for the arrow disc, such as a category colour. Null uses the accent.
        /// </summary>
        public Color? Tint;

        public WayfinderChipItem() { }

        public WayfinderChipItem(string id, string name, float distanceMetres, float bearingDegrees, Color? tint = null)
        {
            Id = id;
            Name = name;
            DistanceMetres = distanceMetres;
            BearingDegrees = bearingDegrees;
            Tint = tint;
        }
    }

    /// <summary>
    /// Nearby places as a row of chips, nearest first, each with an arrow pointing the way and the distance. Tap a
    /// chip to navigate or focus. The map-free alternative to a map button.
    /// </summary>
    /// <remarks>
    /// <para>The <c>max</c> property sets how many chips show (default 3). <see cref="SetItems"/> sorts by distance.
    /// <see cref="UpdateItem"/> moves one arrow and distance without a rebuild, for example every frame.
    /// Distances use <see cref="WorldAnchoredLabel.FormatDistance"/>. The root is a horizontal scroll row.</para>
    /// </remarks>
    [UxmlElement]
    public partial class WayfinderChips : ScrollView
    {
        public const string RootClass = "wayfinderChips";
        public const string ChipClass = "wayfinderChips__chip";
        public const string DiscClass = "wayfinderChips__disc";
        public const string ArrowClass = "wayfinderChips__arrow";
        public const string NameClass = "wayfinderChips__name";
        public const string DistanceClass = "wayfinderChips__distance";

        private readonly List<WayfinderChipItem> items = new();
        private readonly Dictionary<string, VisualElement> chips = new();
        private int max = 3;

        public event Action<string> ItemTapped;

        public IReadOnlyList<WayfinderChipItem> Items => items;

        [UxmlAttribute("max")]
        public int Max
        {
            get => max;
            set
            {
                max = Mathf.Max(1, value);
                SetItems(new List<WayfinderChipItem>(items));
            }
        }

        public WayfinderChips()
            : base(ScrollViewMode.Horizontal)
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        /// <summary>
        /// Shows the nearest <see cref="Max"/> items, nearest first; items without a distance go last.
        /// </summary>
        public void SetItems(IReadOnlyList<WayfinderChipItem> newItems)
        {
            items.Clear();
            chips.Clear();
            Clear();

            if (newItems == null)
            {
                return;
            }

            var sorted = new List<WayfinderChipItem>(newItems);
            sorted.Sort((a, b) => SortKey(a).CompareTo(SortKey(b)));
            for (var i = 0; i < sorted.Count && i < max; i++)
            {
                var item = sorted[i];
                items.Add(item);
                var chip = UIToolkitExtensions.CreateVisualElement(this, ChipClass).MakePill();
                chip.focusable = true;
                chip.tabIndex = 0;
                var id = item.Id;
                chip.RegisterActivate(() => ItemTapped?.Invoke(id));

                var disc = UIToolkitExtensions.CreateVisualElement(chip, DiscClass).IgnorePicking();
                UIToolkitExtensions.CreateVisualElement(disc, ArrowClass).pickingMode = PickingMode.Ignore;
                var name = UIToolkitExtensions.CreateVisualElement<Label>(chip, NameClass).IgnorePicking();
                var distance = UIToolkitExtensions.CreateVisualElement<Label>(chip, DistanceClass).IgnorePicking();

                chips[id] = chip;
                Apply(chip, item);
            }
        }

        /// <summary>
        /// Updates one chip's distance and bearing in place. Order is kept until the next <see cref="SetItems"/>.
        /// </summary>
        public void UpdateItem(string id, float distanceMetres, float bearingDegrees)
        {
            WayfinderChipItem item = null;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Id == id)
                {
                    item = items[i];
                    break;
                }
            }

            if (item == null || !chips.TryGetValue(id, out var chip))
            {
                return;
            }

            item.DistanceMetres = distanceMetres;
            item.BearingDegrees = bearingDegrees;
            Apply(chip, item);
        }

        /// <summary>
        /// The chip for an item, or null.
        /// </summary>
        public VisualElement ChipElement(string id) => chips.TryGetValue(id, out var chip) ? chip : null;

        private static float SortKey(WayfinderChipItem item) => item.DistanceMetres < 0f ? float.MaxValue : item.DistanceMetres;

        private static void Apply(VisualElement chip, WayfinderChipItem item)
        {
            chip.Q<Label>(className: NameClass).text = item.Name ?? string.Empty;
            var distance = chip.Q<Label>(className: DistanceClass);
            distance.text = item.DistanceMetres < 0f ? string.Empty : WorldAnchoredLabel.FormatDistance(item.DistanceMetres);
            distance.style.display = item.DistanceMetres < 0f ? DisplayStyle.None : DisplayStyle.Flex;

            var disc = chip.Q(className: DiscClass);
            disc.style.backgroundColor = item.Tint.HasValue ? new StyleColor(item.Tint.Value) : new StyleColor(StyleKeyword.Null);
            disc.Q(className: ArrowClass).style.rotate = new Rotate(Mathf.DeltaAngle(0f, item.BearingDegrees));
        }
    }
}
