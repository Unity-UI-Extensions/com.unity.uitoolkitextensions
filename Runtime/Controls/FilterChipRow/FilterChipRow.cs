/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One filter of a <see cref="FilterChipRow"/>. Strings are display text.
    /// </summary>
    public sealed class FilterDefinition
    {
        public string Id;

        /// <summary>
        /// The chip's name, such as "Type".
        /// </summary>
        public string Label;

        /// <summary>
        /// The values to pick from, such as "Video", "Audio", "3D".
        /// </summary>
        public List<string> Options = new();

        /// <summary>
        /// The menu entry that clears this filter. Empty leaves it out.
        /// </summary>
        public string AllLabel = "All";

        public FilterDefinition() { }

        public FilterDefinition(string id, string label, IEnumerable<string> options, string allLabel = "All")
        {
            Id = id;
            Label = label;
            Options = new List<string>(options ?? Array.Empty<string>());
            AllLabel = allLabel;
        }
    }

    /// <summary>
    /// A horizontally scrolling row of filter chips. Each chip opens a menu of its values and shows the active one
    /// ("Type: Video"); an active chip is filled. A Clear chip at the end resets them all.
    /// </summary>
    /// <remarks>
    /// <para>The root is a horizontal <see cref="ScrollView"/>. <c>clear-text</c> defaults to "Clear"; empty hides the chip.
    /// <c>use-menu</c> defaults to true: a <see cref="DropDownMenuControl"/> lists the values. When it is off, the app shows its
    /// own picker from <see cref="FilterTapped"/>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class FilterChipRow : ScrollView
    {
        public const string RootClass = "filterChipRow";
        public const string ChipClass = "filterChipRow__chip";
        public const string LabelClass = "filterChipRow__label";
        public const string ChevronClass = "filterChipRow__chevron";
        public const string ClearClass = "filterChipRow__clear";
        public const string ActiveClass = "filterChipRow__chip--active";

        private readonly List<FilterDefinition> filters = new();
        private readonly Dictionary<string, int> values = new();
        private readonly Dictionary<string, VisualElement> chips = new();
        private readonly Dictionary<string, GradientFill> fills = new();
        private readonly Label clear;
        private readonly DropDownMenuControl menu = new();
        private string clearText = "Clear";
        private bool useMenu = true;

        /// <summary>
        /// Raised when a chip is tapped, before any menu opens.
        /// </summary>
        public event Action<string> FilterTapped;

        /// <summary>
        /// Raised when a filter's value changes: id, option index (-1 for all) and the option text.
        /// </summary>
        public event Action<string, int, string> FilterChanged;

        /// <summary>
        /// Raised after <see cref="ClearAll"/> or the Clear chip.
        /// </summary>
        public event Action Cleared;

        public IReadOnlyList<FilterDefinition> Filters => filters;

        public bool HasActive
        {
            get
            {
                foreach (var value in values.Values)
                {
                    if (value >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [UxmlAttribute("clear-text")]
        public string ClearText
        {
            get => clearText;
            set
            {
                clearText = value ?? string.Empty;
                UpdateClear();
            }
        }

        [UxmlAttribute("use-menu")]
        public bool UseMenu
        {
            get => useMenu;
            set => useMenu = value;
        }

        public FilterChipRow()
            : base(ScrollViewMode.Horizontal)
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            verticalScrollerVisibility = ScrollerVisibility.Hidden;

            clear = UIToolkitExtensions.CreateVisualElement<Label>(this, ChipClass, ClearClass).MakePill();
            clear.focusable = true;
            clear.tabIndex = 0;
            clear.RegisterActivate(ClearAll);
            RegisterCallback<DetachFromPanelEvent>(_ => menu.Close());
            UpdateClear();
        }

        public void SetFilters(IReadOnlyList<FilterDefinition> newFilters)
        {
            foreach (var chip in chips.Values)
            {
                chip.RemoveFromHierarchy();
            }

            filters.Clear();
            chips.Clear();
            fills.Clear();
            var kept = new Dictionary<string, int>(values);
            values.Clear();

            if (newFilters != null)
            {
                foreach (var filter in newFilters)
                {
                    filters.Add(filter);
                    var chip = UIToolkitExtensions.CreateVisualElement(ChipClass).MakePill();
                    chip.focusable = true;
                    chip.tabIndex = 0;
                    var id = filter.Id;
                    chip.RegisterActivate(() => OnChipTapped(id));

                    var label = UIToolkitExtensions.CreateVisualElement<Label>(chip, LabelClass).IgnorePicking();
                    UIToolkitExtensions.CreateVisualElement(chip, ChevronClass).pickingMode = PickingMode.Ignore;
                    contentContainer.Insert(contentContainer.IndexOf(clear), chip);
                    chips[id] = chip;
                    values[id] = kept.TryGetValue(id, out var value) && value < filter.Options.Count ? value : -1;
                    ApplyChip(filter);
                }
            }

            UpdateClear();
        }

        /// <summary>
        /// Sets a filter's value by option index; -1 clears it. Raises <see cref="FilterChanged"/> when it changes.
        /// </summary>
        public void SetValue(string id, int optionIndex)
        {
            var filter = filters.Find(f => f.Id == id);
            if (filter == null)
            {
                return;
            }

            var value = optionIndex >= 0 && optionIndex < filter.Options.Count ? optionIndex : -1;
            if (values[id] == value)
            {
                return;
            }

            values[id] = value;
            ApplyChip(filter);
            UpdateClear();
            FilterChanged?.Invoke(id, value, value >= 0 ? filter.Options[value] : null);
        }

        /// <summary>
        /// The option index of a filter, or -1 when it is not set.
        /// </summary>
        public int GetValue(string id) => values.TryGetValue(id, out var value) ? value : -1;

        public void ClearAll()
        {
            foreach (var filter in filters)
            {
                SetValue(filter.Id, -1);
            }

            Cleared?.Invoke();
        }

        /// <summary>
        /// The chip for a filter, or null.
        /// </summary>
        public VisualElement ChipElement(string id) => id != null && chips.TryGetValue(id, out var chip) ? chip : null;

        private void OnChipTapped(string id)
        {
            FilterTapped?.Invoke(id);
            var filter = filters.Find(f => f.Id == id);
            if (!useMenu || filter == null || !chips.TryGetValue(id, out var chip))
            {
                return;
            }

            var options = new List<DropDownMenuControl.DropDownOption>();
            if (!string.IsNullOrEmpty(filter.AllLabel))
            {
                options.Add(new DropDownMenuControl.DropDownOption(filter.AllLabel, () => SetValue(id, -1)));
            }

            for (var i = 0; i < filter.Options.Count; i++)
            {
                var index = i;
                options.Add(new DropDownMenuControl.DropDownOption(filter.Options[i], () => SetValue(id, index)));
            }

            menu.Open(chip, options, DropDownMenuControl.Placement.CenteredOnAnchor);
        }

        private void ApplyChip(FilterDefinition filter)
        {
            var chip = chips[filter.Id];
            var value = values[filter.Id];
            var active = value >= 0;
            chip.Q<Label>(className: LabelClass).text = active ? $"{filter.Label}: {filter.Options[value]}" : filter.Label;
            chip.EnableInClassList(ActiveClass, active);

            if (active && !fills.ContainsKey(filter.Id))
            {
                var fill = new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal);
                chip.AddManipulator(fill);
                fills[filter.Id] = fill;
            }
            else if (!active && fills.TryGetValue(filter.Id, out var fill))
            {
                chip.RemoveManipulator(fill);
                fills.Remove(filter.Id);
            }
        }

        private void UpdateClear()
        {
            clear.text = clearText;
            clear.SetDisplayed(!string.IsNullOrEmpty(clearText) && HasActive);
        }
    }
}
