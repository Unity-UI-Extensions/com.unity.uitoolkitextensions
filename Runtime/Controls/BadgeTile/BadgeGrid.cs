/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One badge for a <see cref="BadgeGrid"/>. Strings are display text.
    /// </summary>
    public sealed class BadgeInfo
    {
        public string Id;
        public string Title;

        /// <summary>
        /// Such as "Unlocked", "8 of 12 stops" or "Locked".
        /// </summary>
        public string Status;

        public bool Unlocked;

        /// <summary>
        /// Badge art; a default value shows the star.
        /// </summary>
        public Background Icon;

        public BadgeInfo() { }

        public BadgeInfo(string id, string title, string status, bool unlocked, Background icon = default)
        {
            Id = id;
            Title = title;
            Status = status;
            Unlocked = unlocked;
            Icon = icon;
        }
    }

    /// <summary>
    /// A card holding a grid of <see cref="BadgeTile"/>s, three to a row by default (FS10_8).
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>columns</c> (default 3), <c>card</c> (draw the card behind the grid, default true). Methods:
    /// <see cref="SetBadges"/>, <see cref="TileElement"/>. Event: <see cref="BadgeTapped"/> (id).</para>
    /// <para>USS classes: <c>badgeGrid</c> (root), <c>badgeGrid__cell</c>. Modifier: <c>badgeGrid--card</c>.</para>
    /// <para>Tokens read: <c>--surface-raised</c>, <c>--surface-border</c>, <c>--space-1..4</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class BadgeGrid : VisualElement
    {
        public const string RootClass = "badgeGrid";
        public const string CellClass = "badgeGrid__cell";
        public const string CardClass = "badgeGrid--card";

        private readonly List<BadgeInfo> badges = new();
        private readonly Dictionary<string, BadgeTile> tiles = new();
        private int columns = 3;
        private bool card = true;

        public event Action<string> BadgeTapped;

        public IReadOnlyList<BadgeInfo> Badges => badges;

        [UxmlAttribute("columns")]
        public int Columns
        {
            get => columns;
            set
            {
                columns = Mathf.Max(1, value);
                foreach (var cell in Children())
                {
                    cell.style.width = Length.Percent(100f / columns);
                }
            }
        }

        [UxmlAttribute("card")]
        public bool Card
        {
            get => card;
            set
            {
                card = value;
                EnableInClassList(CardClass, card);
            }
        }

        public BadgeGrid()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            Card = true;
        }

        public void SetBadges(IReadOnlyList<BadgeInfo> newBadges)
        {
            badges.Clear();
            tiles.Clear();
            Clear();
            if (newBadges == null)
            {
                return;
            }

            foreach (var badge in newBadges)
            {
                badges.Add(badge);
                var cell = UIToolkitExtensions.CreateVisualElement(this, CellClass);
                cell.style.width = Length.Percent(100f / columns);
                var tile = new BadgeTile(badge.Title, badge.Status, badge.Unlocked);
                tile.SetIcon(badge.Icon);
                var id = badge.Id;
                tile.Clicked += () => BadgeTapped?.Invoke(id);
                cell.Add(tile);
                tiles[id] = tile;
            }
        }

        /// <summary>
        /// The tile for a badge, or null.
        /// </summary>
        public BadgeTile TileElement(string id) => id != null && tiles.TryGetValue(id, out var tile) ? tile : null;
    }

    /// <summary>
    /// The badge summary card (FS10_8): "4 of 9 unlocked", the experience name and a progress bar.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>title-text</c>, <c>detail</c>, <c>progress</c> (0 to 1). Method: <see cref="SetCounts"/>
    /// (sets the progress from unlocked and total; the title stays the app's text).</para>
    /// <para>USS classes: <c>badgeSummaryCard</c> (root), <c>badgeSummaryCard__header</c>, <c>badgeSummaryCard__title</c>,
    /// <c>badgeSummaryCard__detail</c>, <c>badgeSummaryCard__bar</c> (a <see cref="LinearProgress"/>).</para>
    /// <para>Tokens read: <c>--surface-raised</c>, <c>--surface-border</c>, <c>--surface-fg</c>, <c>--surface-muted</c>,
    /// <c>--space-2..4</c>, <c>--text-caption</c>, <c>--font-display</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class BadgeSummaryCard : VisualElement
    {
        public const string RootClass = "badgeSummaryCard";
        public const string HeaderClass = "badgeSummaryCard__header";
        public const string TitleClass = "badgeSummaryCard__title";
        public const string DetailClass = "badgeSummaryCard__detail";
        public const string BarClass = "badgeSummaryCard__bar";

        private readonly Label title;
        private readonly Label detail;
        private readonly LinearProgress bar;

        public LinearProgress Bar => bar;

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

        [UxmlAttribute("progress")]
        public float Progress
        {
            get => bar.Value;
            set => bar.Value = value;
        }

        public BadgeSummaryCard()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var header = UIToolkitExtensions.CreateVisualElement(this, HeaderClass);
            title = UIToolkitExtensions.CreateVisualElement<Label>(header, TitleClass);
            detail = UIToolkitExtensions.CreateVisualElement<Label>(header, DetailClass);
            bar = new LinearProgress();
            bar.AddToClassList(BarClass);
            Add(bar);

            Detail = null;
        }

        /// <summary>
        /// Sets the bar to unlocked over total.
        /// </summary>
        public void SetCounts(int unlockedCount, int total)
        {
            Progress = total > 0 ? Mathf.Clamp01(unlockedCount / (float)total) : 0f;
        }
    }
}
