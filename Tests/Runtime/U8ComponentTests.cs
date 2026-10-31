/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class DownloadsAndAchievementsTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator StorageBar_SegmentsLegendAndFreeUp()
        {
            var bar = new StorageBar { TitleText = "412 MB used", Detail = "6.1 GB free", FreeUpText = "Free up 182 MB (not kept offline)" };
            bar.style.width = 350;
            bar.SetSegments(new List<StorageSegment>
            {
                new("location", "Sample Location", 230_000_000),
                new("lobby", "Lobby", 120_000_000),
            }, other: 62_000_000, scale: 600_000_000);
            var freed = 0;
            bar.FreeUpRequested += () => freed++;
            yield return Mount(bar);

            Assert.That(StorageBar.FormatBytes(412_000_000), Is.EqualTo("412 MB"));
            Assert.That(StorageBar.FormatBytes(6_100_000_000), Is.EqualTo("6.1 GB"));
            Assert.That(bar.Track.layout.height, Is.EqualTo(10f));
            Assert.That(bar.Track.childCount, Is.EqualTo(3), "two items and Other");
            Assert.That(bar.Track[0].layout.width / bar.Track.layout.width, Is.EqualTo(230f / 600f).Within(0.01f));
            Assert.That(bar.Track[2].resolvedStyle.backgroundColor.r, Is.EqualTo(0x9F / 255f).Within(0.01f), "Other is muted");
            var legend = bar.Query<Label>(className: StorageBar.LegendLabelClass).ToList();
            Assert.That(legend[0].text, Is.EqualTo("Sample Location 230 MB"));
            Assert.That(legend[2].text, Is.EqualTo("Other 62 MB"));

            Click(bar.FreeUpButton.Q(className: PillButton.BackgroundClass));
            Assert.That(freed, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NotificationCentre_SectionsUnreadAndMarkAll()
        {
            var centre = new NotificationCentre();
            centre.style.width = 350;
            var white = Background.FromTexture2D(Texture2D.whiteTexture);
            centre.SetItems(new List<NotificationEntry>
            {
                new("a1", "Achievement unlocked: Conversationalist", "You met 3 characters · 10 min ago", "Today", true, white),
                new("s7", "A new stop opened nearby", "Stop 7, Fuel nozzle bench · 25 min ago", "Today", true, white),
                new("dl", "Sample Tour downloaded", "12 items, 48 MB · 1 h ago", "Today", false, white),
                new("ph", "Photo saved", "Yesterday", "Earlier", false, white),
            });
            var tapped = new List<string>();
            var counts = new List<int>();
            centre.ItemTapped += tapped.Add;
            centre.UnreadCountChanged += counts.Add;
            yield return Mount(centre);

            var sections = centre.Query(className: NotificationCentre.SectionClass).ToList();
            Assert.That(sections.Count, Is.EqualTo(2));
            Assert.That(sections[0].Q<Label>(className: NotificationCentre.HeadingClass).text, Is.EqualTo("TODAY"));
            Assert.That(centre.UnreadCount, Is.EqualTo(2));
            var row = centre.RowElement("a1");
            Assert.That(row.Q(className: NotificationCentre.DotClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(row.Q(className: NotificationCentre.TileClass).layout.width, Is.EqualTo(40f));
            Assert.That(centre.RowElement("dl").resolvedStyle.borderBottomWidth, Is.EqualTo(0f), "last row has no divider");
            Assert.That(centre.Q(className: NotificationCentre.EmptyClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            Click(row);
            Assert.That(tapped, Is.EqualTo(new[] { "a1" }));
            Assert.That(centre.UnreadCount, Is.EqualTo(1), "tapping marks read");

            var markAll = centre.Q<Label>(className: NotificationCentre.MarkAllClass);
            Assert.That(markAll.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Click(markAll);
            yield return WaitFrames(2);
            Assert.That(centre.UnreadCount, Is.EqualTo(0));
            Assert.That(markAll.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "nothing left to mark");
            Assert.That(counts, Is.EqualTo(new[] { 1, 0 }));

            centre.SetItems(null);
            yield return WaitFrames(2);
            Assert.That(centre.Q(className: NotificationCentre.EmptyClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator BadgeGridAndSummary_LockedUnlockedAndColumns()
        {
            var column = new VisualElement();
            column.style.width = 350;
            column.AddToClassList(ThemeTokens.LightSurfaceClass);
            var summary = new BadgeSummaryCard { TitleText = "4 of 9 unlocked", Detail = "Sample Tour" };
            summary.SetCounts(4, 9);
            var grid = new BadgeGrid();
            grid.SetBadges(new List<BadgeInfo>
            {
                new("first", "First steps", "Unlocked", true),
                new("talk", "Conversationalist", "Unlocked", true),
                new("capture", "First capture", "Unlocked", true),
                new("all", "Completionist", "8 of 12 stops", false),
            });
            var tapped = new List<string>();
            grid.BadgeTapped += tapped.Add;
            column.Add(summary);
            column.Add(grid);
            yield return Mount(column);

            Assert.That(summary.Progress, Is.EqualTo(4f / 9f).Within(0.001f));
            var unlocked = grid.TileElement("first");
            var locked = grid.TileElement("all");
            Assert.That(unlocked.Disc.layout.width, Is.EqualTo(64f));
            Assert.That(unlocked.Disc.resolvedStyle.backgroundImage.texture, Is.Not.Null, "primary gradient");
            Assert.That(locked.Disc.resolvedStyle.backgroundImage.texture, Is.Null);
            Assert.IsTrue(locked.ClassListContains(BadgeTile.LockedClass));
            Assert.That(locked.worldBound.y, Is.GreaterThan(unlocked.worldBound.yMax), "three to a row");
            Assert.That(grid.TileElement("capture").worldBound.y, Is.EqualTo(unlocked.worldBound.y).Within(0.5f));

            Click(locked);
            Assert.That(tapped, Is.EqualTo(new[] { "all" }));
        }

        [UnityTest]
        public IEnumerator BadgeTile_ArtFillsTheDiscUntinted_InARing()
        {
            var art = new Texture2D(4, 4);
            var column = new VisualElement();
            var card = new AchievementCard { TitleText = "Conversationalist" };
            card.SetIcon(Background.FromTexture2D(art));
            var locked = new BadgeTile("Completionist", "Locked", false);
            locked.SetIcon(Background.FromTexture2D(art));
            var light = new VisualElement();
            light.AddToClassList(ThemeTokens.LightSurfaceClass);
            var onLight = new BadgeTile("First steps", "Unlocked", true);
            onLight.SetIcon(Background.FromTexture2D(art));
            light.Add(onLight);
            column.Add(card);
            column.Add(locked);
            column.Add(light);
            yield return Mount(column, 3);

            var badge = card.Badge;
            var icon = badge.Q(className: BadgeTile.IconClass);
            Assert.IsTrue(badge.ClassListContains(BadgeTile.HasIconClass));
            Assert.That(badge.Disc.resolvedStyle.borderTopWidth, Is.EqualTo(1.5f).Within(0.5f), "subtle ring, snapped to whole device pixels");
            Assert.That(badge.Disc.resolvedStyle.borderTopColor.r, Is.EqualTo(0x71 / 255f).Within(0.01f), "light ring on deep");
            Assert.That(badge.Disc.resolvedStyle.backgroundImage.texture, Is.Null, "no gradient behind the art");
            Assert.That(icon.layout.width, Is.EqualTo(AchievementCard.BadgeSize - 3f).Within(1f), "the art fills the disc inside the ring");
            Assert.That(icon.layout.height, Is.EqualTo(icon.layout.width).Within(0.01f));
            Assert.That(icon.resolvedStyle.unityBackgroundImageTintColor, Is.EqualTo(Color.white), "untinted");
            Assert.That(icon.resolvedStyle.backgroundSize.sizeType, Is.EqualTo(BackgroundSizeType.Cover), "scale and crop");
            Assert.That(icon.resolvedStyle.borderTopLeftRadius, Is.EqualTo(icon.layout.width / 2f).Within(0.01f), "clipped round");
            Assert.That(icon.resolvedStyle.opacity, Is.EqualTo(1f));

            var lockedIcon = locked.Q(className: BadgeTile.IconClass);
            Assert.That(lockedIcon.resolvedStyle.opacity, Is.EqualTo(0.45f).Within(0.001f), "locked art is dimmed");
            Assert.That(lockedIcon.resolvedStyle.unityBackgroundImageTintColor, Is.EqualTo(Color.white), "not tinted muted");
            Assert.That(lockedIcon.layout.width, Is.EqualTo(64f - 3f).Within(1f));

            Assert.That(onLight.Disc.resolvedStyle.borderTopColor.r, Is.EqualTo(0xDE / 255f).Within(0.01f), "the border colour on light");

            badge.SetIcon(default);
            yield return WaitFrames(2);
            Assert.IsFalse(badge.ClassListContains(BadgeTile.HasIconClass));
            Assert.That(icon.style.backgroundImage.keyword, Is.EqualTo(StyleKeyword.Null), "the stylesheet's star");
            Assert.That(icon.layout.width, Is.EqualTo(48f), "the star is back in the board's 48 px box");
            Assert.That(badge.Disc.resolvedStyle.borderTopWidth, Is.EqualTo(0f), "no ring round the star");
            Assert.That(badge.Disc.resolvedStyle.backgroundImage.texture, Is.Not.Null, "the primary gradient is back");
            Object.Destroy(art);
        }

        [UnityTest]
        public IEnumerator AchievementCard_ContentAndActions()
        {
            var card = new AchievementCard
            {
                TitleText = "Conversationalist",
                BodyText = "You talked to all three characters in the hall. The engineers' notes are now in your library.",
            };
            card.style.width = 342;
            card.SetRewards(new[] { "Engineers' notes" });
            var log = new List<string>();
            card.ContinueClicked += () => log.Add("continue");
            card.SeeAllClicked += () => log.Add("all");
            yield return Mount(card);

            Assert.That(card.Badge.Disc.layout.width, Is.EqualTo(96f));
            Assert.That(card.Q(className: AchievementCard.HaloClass).layout.width, Is.EqualTo(116f));
            Assert.That(card.Q<Label>(className: AchievementCard.EyebrowClass).text, Is.EqualTo("ACHIEVEMENT UNLOCKED"));
            Assert.That(card.PrimaryButton.layout.width, Is.EqualTo(card.SecondaryButton.layout.width).Within(1f), "equal halves");
            Assert.That(card.Q<Label>(className: AchievementCard.RewardClass).text, Is.EqualTo("Engineers' notes"));

            Click(card.SecondaryButton.Q(className: PillButton.BackgroundClass));
            Click(card.PrimaryButton.Q(className: PillButton.BackgroundClass));
            Assert.That(log, Is.EqualTo(new[] { "all", "continue" }));

            card.Card = false;
            yield return WaitFrames(2);
            Assert.That(card.resolvedStyle.paddingTop, Is.EqualTo(0f), "content only, for a dialog");
        }
    }
}
