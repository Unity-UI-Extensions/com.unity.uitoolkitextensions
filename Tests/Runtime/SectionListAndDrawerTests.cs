/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class SectionListTests : UIDocumentTestBase
    {
        private static List<SectionListSection> Sections() => new()
        {
            new SectionListSection("characters", "Characters", new[]
            {
                new SectionListRow("greeter", "Lobby greeter", "Talks about the building", "12 MB"),
                new SectionListRow("guide", "Harbour guide"),
            }),
            new SectionListSection("objects", "Objects", new[] { new SectionListRow("planet", "Planet") }) { Count = 9 },
        };

        [UnityTest]
        public IEnumerator Sections_ShowCounts_AndRowsRaiseRowAction()
        {
            Root.SetReduceMotion(true);
            var list = new SectionList();
            list.SetSections(Sections());
            var actions = new List<string>();
            list.RowAction += (section, row) => actions.Add($"{section}/{row.Id}");
            yield return Mount(list);

            var characters = list.SectionElement("characters");
            Assert.That(characters.Variant, Is.EqualTo(CollapsibleSectionVariant.Card));
            Assert.That(characters.Q<Label>(className: SectionList.CountClass).text, Is.EqualTo("2"), "row count by default");
            Assert.That(list.SectionElement("objects").Q<Label>(className: SectionList.CountClass).text, Is.EqualTo("9"), "explicit count");

            var rows = characters.Query<SettingsRow>(className: SectionList.RowClass).ToList();
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows[0].Trailing, Is.EqualTo(SettingsRowTrailing.Value), "meta shows as the value");
            Assert.IsFalse(rows[1].ShowDivider, "the last row has no divider");

            Click(rows[1]);
            Assert.That(actions, Is.EqualTo(new[] { "characters/guide" }));
        }

        [UnityTest]
        public IEnumerator SingleOpen_ClosesOthers_AndExpandedIdsSurviveRebind()
        {
            var list = new SectionList { SingleOpen = true };
            list.SetSections(Sections());
            var toggles = new List<string>();
            list.SectionToggled += (id, open) => toggles.Add($"{id}:{open}");
            yield return Mount(list);

            Click(list.SectionElement("characters").Header);
            Click(list.SectionElement("objects").Header);

            Assert.That(list.ExpandedIds, Is.EqualTo(new[] { "objects" }));
            Assert.That(toggles, Is.EqualTo(new[] { "characters:True", "characters:False", "objects:True" }));

            list.SetSections(Sections());
            Assert.That(list.ExpandedIds, Is.EqualTo(new[] { "objects" }), "open sections stay open after new data");

            list.ExpandedIds = new[] { "characters" };
            Assert.That(list.ExpandedIds, Is.EqualTo(new[] { "characters" }));
        }

        [UnityTest]
        public IEnumerator RowFactory_BuildsCustomRows()
        {
            var list = new SectionList { RowFactory = row => new Label(row.Title.ToUpperInvariant()) };
            list.SetSections(Sections());
            var actions = new List<string>();
            list.RowAction += (_, row) => actions.Add(row.Id);
            yield return Mount(list);

            var custom = list.SectionElement("objects").Q<Label>(className: SectionList.RowClass);
            Assert.That(custom.text, Is.EqualTo("PLANET"));
            Click(custom);
            Assert.That(actions, Is.EqualTo(new[] { "planet" }));
        }
    }

    public class SideDrawerTests : UIDocumentTestBase
    {
        private SideDrawer drawer;
        private int opened;
        private int closed;

        private IEnumerator MountDrawer(DrawerAnchor anchor = DrawerAnchor.Left)
        {
            Root.SetReduceMotion(true);
            drawer = new SideDrawer { Anchor = anchor };
            drawer.Opened += () => opened++;
            drawer.Closed += () => closed++;
            opened = closed = 0;
            drawer.AddSection("Account").Add(new SettingsRow("Profile"));
            yield return Mount(drawer, 3);
        }

        private float VisibleWidth() => drawer.Anchor == DrawerAnchor.Left
            ? drawer.Panel.worldBound.xMax - drawer.worldBound.xMin
            : drawer.worldBound.xMax - drawer.Panel.worldBound.xMin;

        [UnityTest]
        public IEnumerator OpenClose_MovesPanelAndScrim_AndRaisesEvents()
        {
            yield return MountDrawer();
            Assert.IsFalse(drawer.IsOpen);
            Assert.That(VisibleWidth(), Is.EqualTo(0f).Within(1f));
            Assert.That(drawer.Panel.layout.width, Is.EqualTo(320f), "320 is under 85% of 390");

            drawer.Open();
            yield return WaitFrames(2);
            Assert.IsTrue(drawer.ClassListContains(SideDrawer.OpenClass));
            Assert.That(VisibleWidth(), Is.EqualTo(320f).Within(1f));
            Assert.That(drawer.Q(className: SideDrawer.ScrimClass).resolvedStyle.opacity, Is.EqualTo(1f));

            Click(drawer.Q(className: SideDrawer.ScrimClass));
            yield return WaitFrames(2);
            Assert.IsFalse(drawer.IsOpen);
            Assert.That((opened, closed), Is.EqualTo((1, 1)));
            Assert.That(drawer.Q<Label>(className: SideDrawer.SectionTitleClass).text, Is.EqualTo("Account"));
        }

        [UnityTest]
        public IEnumerator EdgeSwipe_Opens_AndDragCloses()
        {
            yield return MountDrawer();

            drawer.DragStart(1, new Vector2(5f, 300f), 1f);
            drawer.DragMove(new Vector2(150f, 302f), 1.2f);
            drawer.DragMove(new Vector2(260f, 303f), 1.4f);
            drawer.DragEnd(1.45f);
            Assert.IsTrue(drawer.IsOpen, "past half way opens");

            drawer.DragStart(1, new Vector2(300f, 300f), 2f);
            drawer.DragMove(new Vector2(260f, 300f), 2.01f);
            drawer.DragMove(new Vector2(220f, 300f), 2.02f);
            drawer.DragEnd(2.03f);
            Assert.IsFalse(drawer.IsOpen, "a quick flick toward the edge closes");
        }

        [UnityTest]
        public IEnumerator VerticalMove_IsAScroll_NotADrag()
        {
            yield return MountDrawer();
            drawer.Open();

            drawer.DragStart(1, new Vector2(200f, 300f), 1f);
            drawer.DragMove(new Vector2(190f, 380f), 1.1f);
            drawer.DragMove(new Vector2(40f, 400f), 1.2f);
            drawer.DragEnd(1.3f);

            Assert.IsTrue(drawer.IsOpen);
            Assert.IsFalse(drawer.ClassListContains(SideDrawer.DraggingClass));
        }

        [UnityTest]
        public IEnumerator Right_Permanent_AndBack()
        {
            yield return MountDrawer(DrawerAnchor.Right);
            drawer.Open();
            Assert.IsTrue(drawer.HandleBack());
            Assert.IsFalse(drawer.IsOpen);
            Assert.IsFalse(drawer.HandleBack(), "closed: back is the caller's");

            drawer.Variant = DrawerVariant.Permanent;
            yield return WaitFrames(2);
            Assert.IsTrue(drawer.IsOpen);
            Assert.That(VisibleWidth(), Is.EqualTo(320f).Within(1f));
            Assert.That(drawer.Q(className: SideDrawer.ScrimClass).resolvedStyle.opacity, Is.EqualTo(0f));
            Assert.IsFalse(drawer.HandleBack(), "a permanent drawer does not close on back");
        }
    }
}
