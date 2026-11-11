/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
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
    /// The notification history: one card per section ("Today", "Earlier"), rows with an icon tile, a title,
    /// a detail line and an unread dot, and a "Mark all read" link in the first section's header.
    /// </summary>
    /// <remarks>
    /// <para><c>mark-all-text</c> defaults to "Mark all read"; empty hides the link. The link shows only while something is
    /// unread. <c>mark-read-on-tap</c> defaults to true. With no items, an <see cref="EmptyState"/> shows.</para>
    /// <para>The centre does not scroll; host it in a <see cref="ScrollView"/> or a sheet.</para>
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

        public int UnreadCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i].Unread)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

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

            empty.SetDisplayed(items.Count == 0);
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
                markAll.RegisterActivate(OnMarkAll);
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
            row.RegisterActivate(() => OnRowTapped(id));

            var tile = UIToolkitExtensions.CreateVisualElement(row, TileClass).IgnorePicking();
            var icon = UIToolkitExtensions.CreateVisualElement(tile, IconClass).IgnorePicking();
            icon.style.backgroundImage = new StyleBackground(item.Icon);

            var texts = UIToolkitExtensions.CreateVisualElement(row, TextsClass).IgnorePicking();
            var title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            title.text = item.Title ?? string.Empty;
            var detail = UIToolkitExtensions.CreateVisualElement<Label>(texts, DetailClass);
            detail.SetTextOrHide(item.Detail);

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
            markAll.SetDisplayed(!string.IsNullOrEmpty(markAllText) && UnreadCount > 0);
        }
    }
}
