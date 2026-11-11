/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One row of a <see cref="SectionList"/>. Strings are display text; localise before passing them.
    /// </summary>
    public sealed class SectionListRow
    {
        public string Id;
        public string Title;
        public string Subtitle;

        /// <summary>
        /// A trailing value, such as a size or a distance.
        /// </summary>
        public string Meta;

        public Background Icon;
        public object UserData;

        public SectionListRow() { }

        public SectionListRow(string id, string title, string subtitle = null, string meta = null)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            Meta = meta;
        }
    }

    /// <summary>
    /// One expandable section of a <see cref="SectionList"/>.
    /// </summary>
    public sealed class SectionListSection
    {
        public string Id;
        public string Title;

        /// <summary>
        /// The count shown in the header; negative shows the number of rows.
        /// </summary>
        public int Count = -1;

        public List<SectionListRow> Rows = new();

        public SectionListSection() { }

        public SectionListSection(string id, string title, IEnumerable<SectionListRow> rows = null)
        {
            Id = id;
            Title = title;
            if (rows != null)
            {
                Rows.AddRange(rows);
            }
        }
    }

    /// <summary>
    /// Data-bound expandable sections with counts and rows, for list-first browsers, storage and catalogues.
    /// Each section is a <see cref="CollapsibleSection"/>; rows are <see cref="SettingsRow"/>s unless a
    /// <see cref="RowFactory"/> builds them.
    /// </summary>
    [UxmlElement]
    public partial class SectionList : VisualElement
    {
        public const string RootClass = "sectionList";
        public const string SectionClass = "sectionList__section";
        public const string CountClass = "sectionList__count";
        public const string ChevronClass = "sectionList__chevron";
        public const string RowClass = "sectionList__row";

        private readonly List<SectionListSection> sections = new();
        private readonly Dictionary<string, CollapsibleSection> sectionElements = new();
        private bool singleOpen;
        private bool suppressToggleEvents;

        /// <summary>
        /// Raised when a section opens or closes, with its id and whether it is open.
        /// </summary>
        public event Action<string, bool> SectionToggled;

        public event Action<string, SectionListRow> RowAction;

        /// <summary>
        /// Builds the element for a row instead of the default <see cref="SettingsRow"/>. The list raises
        /// <see cref="RowAction"/> when the returned element is clicked.
        /// </summary>
        public Func<SectionListRow, VisualElement> RowFactory { get; set; }

        public IReadOnlyList<SectionListSection> Sections => sections;

        [UxmlAttribute("single-open")]
        public bool SingleOpen
        {
            get => singleOpen;
            set
            {
                singleOpen = value;
                if (singleOpen)
                {
                    var open = ExpandedIds;
                    for (var i = 1; i < open.Count; i++)
                    {
                        SetExpanded(open[i], false);
                    }
                }
            }
        }

        /// <summary>
        /// The open sections, in display order. Setting it opens exactly these, without events.
        /// </summary>
        public IReadOnlyList<string> ExpandedIds
        {
            get
            {
                var ids = new List<string>();
                foreach (var section in sections)
                {
                    if (sectionElements.TryGetValue(section.Id, out var element) && element.IsExpanded)
                    {
                        ids.Add(section.Id);
                    }
                }

                return ids;
            }
            set
            {
                var wanted = new HashSet<string>(value ?? Array.Empty<string>());
                suppressToggleEvents = true;
                foreach (var pair in sectionElements)
                {
                    pair.Value.IsExpanded = wanted.Contains(pair.Key);
                }

                suppressToggleEvents = false;
            }
        }

        public SectionList()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
        }

        /// <summary>
        /// Replaces the sections. Sections that were open stay open when their id survives.
        /// </summary>
        public void SetSections(IReadOnlyList<SectionListSection> newSections)
        {
            var open = new HashSet<string>(ExpandedIds);
            Clear();
            sections.Clear();
            sectionElements.Clear();

            if (newSections != null)
            {
                foreach (var section in newSections)
                {
                    sections.Add(section);
                    var element = BuildSection(section);
                    sectionElements[section.Id] = element;
                    Add(element);
                }
            }

            ExpandedIds = new List<string>(open);
        }

        /// <summary>
        /// Opens or closes a section, raising <see cref="SectionToggled"/> when it changes.
        /// </summary>
        public bool SetExpanded(string id, bool expanded)
        {
            if (!sectionElements.TryGetValue(id, out var element))
            {
                return false;
            }

            element.IsExpanded = expanded;
            return true;
        }

        public CollapsibleSection SectionElement(string id) => sectionElements.TryGetValue(id, out var element) ? element : null;

        private CollapsibleSection BuildSection(SectionListSection section)
        {
            var element = new CollapsibleSection
            {
                TitleText = section.Title,
                Variant = CollapsibleSectionVariant.Card,
                ToggleOnPointerDown = false,
            };
            element.AddToClassList(SectionClass);

            var trailing = new VisualElement();
            trailing.style.flexDirection = FlexDirection.Row;
            var count = UIToolkitExtensions.CreateVisualElement<Label>(trailing, CountClass).MakePill();
            count.text = (section.Count >= 0 ? section.Count : section.Rows.Count).ToString();
            UIToolkitExtensions.CreateVisualElement(trailing, ChevronClass);
            element.SetTrailing(trailing);

            VisualElement last = null;
            foreach (var row in section.Rows)
            {
                last = BuildRow(section, row);
                element.Add(last);
            }

            // USS has no :last-child, so the last default row drops its divider here.
            if (last is SettingsRow lastRow)
            {
                lastRow.ShowDivider = false;
            }

            element.OnExpandedChanged += expanded => OnSectionToggled(section.Id, expanded);
            return element;
        }

        private VisualElement BuildRow(SectionListSection section, SectionListRow row)
        {
            VisualElement element;
            if (RowFactory != null)
            {
                element = RowFactory(row);
                element.RegisterCallback<ClickEvent>(_ => RowAction?.Invoke(section.Id, row));
            }
            else
            {
                var settingsRow = new SettingsRow(row.Title, row.Subtitle, string.IsNullOrEmpty(row.Meta) ? SettingsRowTrailing.Chevron : SettingsRowTrailing.Value)
                {
                    ValueText = row.Meta,
                };
                settingsRow.SetIcon(row.Icon);
                settingsRow.Clicked += () => RowAction?.Invoke(section.Id, row);
                element = settingsRow;
            }

            element.AddToClassList(RowClass);
            element.userData = row;
            return element;
        }

        private void OnSectionToggled(string id, bool expanded)
        {
            if (suppressToggleEvents)
            {
                return;
            }

            if (expanded && singleOpen)
            {
                suppressToggleEvents = true;
                foreach (var pair in sectionElements)
                {
                    if (pair.Key != id && pair.Value.IsExpanded)
                    {
                        pair.Value.IsExpanded = false;
                        SectionToggled?.Invoke(pair.Key, false);
                    }
                }

                suppressToggleEvents = false;
            }

            SectionToggled?.Invoke(id, expanded);
        }
    }
}
