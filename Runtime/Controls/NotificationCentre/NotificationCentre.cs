/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One entry in a <see cref="NotificationCentre"/>. Strings are display text.
    /// </summary>
    public sealed class NotificationEntry
    {
        public string Id;
        public string Title;

        /// <summary>
        /// The second line, such as "You met 3 characters · 10 min ago".
        /// </summary>
        public string Detail;

        /// <summary>
        /// The group heading, such as "Today" or "Earlier". Groups appear in the order first met.
        /// </summary>
        public string Section;

        public Background Icon;
        public bool Unread;

        public NotificationEntry() { }

        public NotificationEntry(string id, string title, string detail, string section, bool unread = false, Background icon = default)
        {
            Id = id;
            Title = title;
            Detail = detail;
            Section = section;
            Unread = unread;
            Icon = icon;
        }
    }

    /// <summary>
    /// The notification history (FS10_6): one card per section ("Today", "Earlier"), rows with an icon tile, a title,
    /// a detail line and an unread dot, and a "Mark all read" link in the first section's header.
    /// </summary>
    /// <remarks>
    /// <para>Methods: <see cref="SetItems"/>, <see cref="MarkRead"/>, <see cref="MarkAllRead"/>,
    /// <see cref="RowElement"/>. Events: <see cref="ItemTapped"/> (id), <see cref="UnreadCountChanged"/>,
    /// <see cref="AllMarkedRead"/>. Props: <c>mark-all-text</c> (default "Mark all read"; empty hides the link),
    /// <c>mark-read-on-tap</c> (default true), <c>empty-title</c>, <c>empty-body</c> (an <see cref="EmptyState"/>
    /// when there are no items). The link shows only while something is unread.</para>
    /// <para>The centre does not scroll; host it in a <see cref="ScrollView"/> or a sheet.</para>
    /// <para>USS classes: <c>notificationCentre</c> (root), <c>notificationCentre__section</c> (a card),
    /// <c>notificationCentre__header</c>, <c>notificationCentre__heading</c>, <c>notificationCentre__markAll</c>,
    /// <c>notificationCentre__row</c>, <c>notificationCentre__tile</c>, <c>notificationCentre__icon</c>,
    /// <c>notificationCentre__texts</c>, <c>notificationCentre__title</c>, <c>notificationCentre__detail</c>,
    /// <c>notificationCentre__dot</c>, <c>notificationCentre__empty</c>. Row modifiers: <c>notificationCentre__row--unread</c>,
    /// <c>notificationCentre__row--last</c>.</para>
    /// <para>Tokens read: <c>--surface-raised</c>, <c>--surface-border</c>, <c>--surface-fg</c>, <c>--surface-muted</c>,
    /// <c>--surface-accent</c>, <c>--surface-tile</c>, <c>--surface-pressed</c>, <c>--c-primary</c>,
    /// <c>--radius-pill</c>, <c>--space-1..4</c>, <c>--text-micro</c>, <c>--text-caption</c>, <c>--text-label</c>,
    /// <c>--font-body</c>, <c>--font-body-medium</c>, <c>--font-body-semibold</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class NotificationCentre : VisualElement
    {
        public const string RootClass = "notificationCentre";
        public const string SectionClass = "notificationCentre__section";
        public const string HeaderClass = "notificationCentre__header";
        public const string HeadingClass = "notificationCentre__heading";
        public const string MarkAllClass = "notificationCentre__markAll";
        public const string RowClass = "notificationCentre__row";
        public const string TileClass = "notificationCentre__tile";
        public const string IconClass = "notificationCentre__icon";
        public const string TextsClass = "notificationCentre__texts";
        public const string TitleClass = "notificationCentre__title";
        public const string DetailClass = "notificationCentre__detail";
        public const string DotClass = "notificationCentre__dot";
        public const string EmptyClass = "notificationCentre__empty";
        public const string UnreadRowClass = "notificationCentre__row--unread";
        public const string LastRowClass = "notificationCentre__row--last";

        private readonly List<NotificationEntry> items = new();
        private readonly Dictionary<string, VisualElement> rows = new();
        private readonly EmptyState empty;
        private Label markAll;
        private string markAllText = "Mark all read";
        private bool markReadOnTap = true;

        public event Action<string> ItemTapped;

        public event Action<int> UnreadCountChanged;

        /// <summary>
        /// Raised when "Mark all read" is used, after the rows are marked.
        /// </summary>
        public event Action AllMarkedRead;

        public IReadOnlyList<NotificationEntry> Items => items;

        public int UnreadCount => items.FindAll(i => i.Unread).Count;

        [UxmlAttribute("mark-all-text")]
        public string MarkAllText
        {
            get => markAllText;
            set
            {
                markAllText = value ?? string.Empty;
                UpdateMarkAll();
            }
        }

        [UxmlAttribute("mark-read-on-tap")]
        public bool MarkReadOnTap
        {
            get => markReadOnTap;
            set => markReadOnTap = value;
        }

        [UxmlAttribute("empty-title")]
        public string EmptyTitle
        {
            get => empty.TitleText;
            set => empty.TitleText = value;
        }

        [UxmlAttribute("empty-body")]
        public string EmptyBody
        {
            get => empty.BodyText;
            set => empty.BodyText = value;
        }

        public NotificationCentre()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            empty = new EmptyState { TitleText = "No notifications yet" };
            empty.AddToClassList(EmptyClass);
            Add(empty);
        }

        public void SetItems(IReadOnlyList<NotificationEntry> newItems)
        {
            var before = UnreadCount;
            items.Clear();
            rows.Clear();
            Clear();
            markAll = null;
            Add(empty);

            var sections = new Dictionary<string, VisualElement>();
            if (newItems != null)
            {
                foreach (var item in newItems)
                {
                    items.Add(item);
                    var key = item.Section ?? string.Empty;
                    if (!sections.TryGetValue(key, out var section))
                    {
                        section = CreateSection(key, sections.Count == 0);
                        sections[key] = section;
                    }

                    rows[item.Id] = CreateRow(section, item);
                }
            }

            foreach (var section in sections.Values)
            {
                // USS has no :last-child; the last row of each card drops its divider.
                section[section.childCount - 1].AddToClassList(LastRowClass);
            }

            empty.style.display = items.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateMarkAll();
            if (before != UnreadCount)
            {
                UnreadCountChanged?.Invoke(UnreadCount);
            }
        }

        public void MarkRead(string id)
        {
            var item = items.Find(i => i.Id == id);
            if (item == null || !item.Unread)
            {
                return;
            }

            item.Unread = false;
            rows[id].RemoveFromClassList(UnreadRowClass);
            UpdateMarkAll();
            UnreadCountChanged?.Invoke(UnreadCount);
        }

        public void MarkAllRead()
        {
            var changed = false;
            foreach (var item in items)
            {
                if (item.Unread)
                {
                    item.Unread = false;
                    rows[item.Id].RemoveFromClassList(UnreadRowClass);
                    changed = true;
                }
            }

            UpdateMarkAll();
            if (changed)
            {
                UnreadCountChanged?.Invoke(0);
            }
        }

        /// <summary>
        /// The row for an item, or null.
        /// </summary>
        public VisualElement RowElement(string id) => id != null && rows.TryGetValue(id, out var row) ? row : null;

        private VisualElement CreateSection(string heading, bool first)
        {
            var section = UIToolkitExtensions.CreateVisualElement(this, SectionClass);
            var header = UIToolkitExtensions.CreateVisualElement(section, HeaderClass);
            var label = UIToolkitExtensions.CreateVisualElement<Label>(header, HeadingClass);
            label.text = heading.ToUpperInvariant();
            if (first)
            {
                markAll = UIToolkitExtensions.CreateVisualElement<Label>(header, MarkAllClass);
                markAll.focusable = true;
                markAll.tabIndex = 0;
                markAll.RegisterCallback<ClickEvent>(_ => OnMarkAll());
                markAll.RegisterCallback<NavigationSubmitEvent>(evt =>
                {
                    OnMarkAll();
                    evt.StopPropagation();
                });
            }

            return section;
        }

        private VisualElement CreateRow(VisualElement section, NotificationEntry item)
        {
            var row = UIToolkitExtensions.CreateVisualElement(section, RowClass);
            row.focusable = true;
            row.tabIndex = 0;
            row.EnableInClassList(UnreadRowClass, item.Unread);
            var id = item.Id;
            row.RegisterCallback<ClickEvent>(_ => OnRowTapped(id));
            row.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                OnRowTapped(id);
                evt.StopPropagation();
            });

            var tile = UIToolkitExtensions.CreateVisualElement(row, TileClass);
            tile.pickingMode = PickingMode.Ignore;
            var icon = UIToolkitExtensions.CreateVisualElement(tile, IconClass);
            icon.pickingMode = PickingMode.Ignore;
            icon.style.backgroundImage = new StyleBackground(item.Icon);

            var texts = UIToolkitExtensions.CreateVisualElement(row, TextsClass);
            texts.pickingMode = PickingMode.Ignore;
            var title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            title.text = item.Title ?? string.Empty;
            var detail = UIToolkitExtensions.CreateVisualElement<Label>(texts, DetailClass);
            detail.text = item.Detail ?? string.Empty;
            detail.style.display = string.IsNullOrEmpty(item.Detail) ? DisplayStyle.None : DisplayStyle.Flex;

            UIToolkitExtensions.CreateVisualElement(row, DotClass).pickingMode = PickingMode.Ignore;
            return row;
        }

        private void OnRowTapped(string id)
        {
            if (markReadOnTap)
            {
                MarkRead(id);
            }

            ItemTapped?.Invoke(id);
        }

        private void OnMarkAll()
        {
            MarkAllRead();
            AllMarkedRead?.Invoke();
        }

        private void UpdateMarkAll()
        {
            if (markAll == null)
            {
                return;
            }

            markAll.text = markAllText;
            markAll.style.display = !string.IsNullOrEmpty(markAllText) && UnreadCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
