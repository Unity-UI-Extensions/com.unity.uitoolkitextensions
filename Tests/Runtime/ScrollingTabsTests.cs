/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class ScrollingTabsTests : UIDocumentTestBase
    {
        private static List<TabItem> Categories(int count) =>
            Enumerable.Range(0, count).Select(i => new TabItem($"cat{i}", $"Category {i}", i)).ToList();

        private IEnumerator MountTabs(ScrollingTabs tabs, float width = 390f)
        {
            Root.SetReduceMotion(true);
            tabs.style.width = width;
            yield return Mount(tabs, 3);
        }

        [UnityTest]
        public IEnumerator SetItems_SelectsFirst_AndClickSelects()
        {
            var tabs = new ScrollingTabs();
            tabs.SetItems(Categories(3));
            var changes = new List<(string, int)>();
            tabs.SelectionChanged += (id, index) => changes.Add((id, index));
            yield return MountTabs(tabs);

            Assert.That(tabs.SelectedId, Is.EqualTo("cat0"));
            Assert.IsTrue(tabs.TabElement(0).ClassListContains(ScrollingTabs.ActiveClass));
            Assert.That(tabs.TabElement(1).Q<Label>(className: ScrollingTabs.CountClass).text, Is.EqualTo("1"));

            Click(tabs.TabElement(2));
            yield return WaitFrames(2);

            Assert.That(changes, Is.EqualTo(new[] { ("cat2", 2) }));
            var indicator = tabs.Q(className: ScrollingTabs.IndicatorClass);
            Assert.That(indicator.layout.xMin, Is.EqualTo(tabs.TabElement(2).layout.xMin).Within(1f), "indicator under the selected tab");
            Assert.That(indicator.layout.width, Is.EqualTo(tabs.TabElement(2).layout.width).Within(1f));
        }

        [UnityTest]
        public IEnumerator SetItems_KeepsSelectionById()
        {
            var tabs = new ScrollingTabs();
            tabs.SetItems(Categories(4), "cat2");
            yield return MountTabs(tabs);

            var reordered = Categories(4);
            reordered.Reverse();
            tabs.SetItems(reordered);
            Assert.That(tabs.SelectedId, Is.EqualTo("cat2"));
            Assert.That(tabs.SelectedIndex, Is.EqualTo(1));

            tabs.SetItems(Categories(2));
            Assert.That(tabs.SelectedId, Is.EqualTo("cat0"), "falls back to the first when the id is gone");
        }

        [UnityTest]
        public IEnumerator Overflow_ScrollsSelectionIntoView_AndShowsFades()
        {
            var tabs = new ScrollingTabs();
            tabs.SetItems(Categories(12));
            yield return MountTabs(tabs, 300f);

            var fadeStart = tabs.Q(className: ScrollingTabs.FadeStartClass);
            var fadeEnd = tabs.Q(className: ScrollingTabs.FadeEndClass);
            Assert.IsFalse(fadeStart.ClassListContains(ScrollingTabs.FadeVisibleClass));
            Assert.IsTrue(fadeEnd.ClassListContains(ScrollingTabs.FadeVisibleClass), "more content to the right");
            Assert.That(fadeEnd.resolvedStyle.backgroundImage.texture, Is.Not.Null, "fade texture from --scrollingTabs-fade-color");

            Assert.IsTrue(tabs.Select("cat11"));
            yield return WaitFrames(3);

            var view = tabs.ScrollView.contentViewport.layout.width;
            var last = tabs.TabElement(11).layout;
            Assert.That(last.xMax - tabs.ScrollView.scrollOffset.x, Is.LessThanOrEqualTo(view + 0.5f));
            Assert.IsTrue(fadeStart.ClassListContains(ScrollingTabs.FadeVisibleClass));
        }

        [UnityTest]
        public IEnumerator Chips_HideIndicator_AndFillActive()
        {
            var tabs = new ScrollingTabs { Variant = ScrollingTabsVariant.Chips };
            tabs.SetItems(Categories(3));
            yield return MountTabs(tabs);

            Assert.IsTrue(tabs.ClassListContains(ScrollingTabs.ChipsVariantClass));
            Assert.That(tabs.Q(className: ScrollingTabs.IndicatorClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(tabs.TabElement(0).resolvedStyle.backgroundColor.r, Is.EqualTo(0x3D / 255f).Within(0.01f), "--c-primary");
        }

        [UnityTest]
        public IEnumerator SearchTab_RaisesSearchRequested_AndKeyboardMoves()
        {
            var tabs = new ScrollingTabs { ShowSearch = true };
            tabs.SetItems(Categories(3));
            var searches = 0;
            tabs.SearchRequested += () => searches++;
            yield return MountTabs(tabs);

            var search = tabs.Q(className: ScrollingTabs.SearchClass);
            Assert.That(search.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Click(search);
            Assert.That(searches, Is.EqualTo(1));
            Assert.That(tabs.SelectedId, Is.EqualTo("cat0"), "search does not change the selection");

            Navigate(tabs, NavigationMoveEvent.Direction.Right);
            Assert.That(tabs.SelectedId, Is.EqualTo("cat1"));
        }
    }
}
