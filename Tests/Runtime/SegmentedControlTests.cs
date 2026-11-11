/// Credit SimonDarksideJ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class SegmentedControlTests : UIDocumentTestBase
    {
        private const string UxmlPath = "Packages/com.unity.uitoolkitextensions/Tests/Runtime/SegmentedControlTest.uxml";

        private static List<VisualElement> Segments(QuadrantStepper stepper) =>
            stepper.Query(className: QuadrantStepper.SegmentClass).ToList();

        private static VisualElement Overlay(QuadrantStepper stepper) =>
            stepper.Q(className: QuadrantStepper.OverlayClass);

        private static void AssertOverlayCovers(QuadrantStepper stepper, int index)
        {
            var segment = Segments(stepper)[index].worldBound;
            var overlay = Overlay(stepper).worldBound;
            Assert.That(overlay.center.x, Is.EqualTo(segment.center.x).Within(1f), $"overlay is centred on segment {index}");
            Assert.That(overlay.width, Is.EqualTo(segment.width * 0.97f).Within(1f), "overlay keeps the 1.5% side inset");
        }

        [UnityTest]
        public IEnumerator QuadrantStepper_LegacyApiAndClasses_Unchanged()
        {
            var stepper = new QuadrantStepper();
            yield return Mount(stepper);

            Assert.IsTrue(stepper.ClassListContains(QuadrantStepper.RootClass));
            Assert.IsFalse(stepper.ClassListContains(SegmentedControl.RootClass));
            Assert.That(stepper.Q(className: QuadrantStepper.SegmentsClass), Is.Not.Null);
            Assert.That(Overlay(stepper), Is.Not.Null);
            Assert.That(Segments(stepper).Count, Is.EqualTo(4));
            Assert.That(stepper.Query<Label>(className: QuadrantStepper.LabelClass).ToList().Count, Is.EqualTo(4));
            Assert.That(stepper.SelectedIndex, Is.EqualTo(0));

            stepper.SetOptions(new[] { "A", "B", "C" }, 2);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(2));
            Assert.That(stepper.SelectedText, Is.EqualTo("C"));

            Assert.IsTrue(stepper.SetOptions(new[] { "X", "Y" }, "Y"));
            Assert.That(stepper.SelectedIndex, Is.EqualTo(1));
            Assert.IsTrue(stepper.TrySetSelectedText("X"));
            Assert.That(stepper.SelectedIndex, Is.EqualTo(0));

            stepper.ForceUnselect();
            Assert.That(stepper.SelectedIndex, Is.EqualTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stepper.SetSelectedIndex(5));

            stepper.SelectedIndex = 9;
            Assert.That(stepper.SelectedIndex, Is.EqualTo(1), "the property setter clamps");
        }

        [UnityTest]
        public IEnumerator Click_SelectsSegment_AndRaisesSelectionChanged()
        {
            var stepper = new QuadrantStepper();
            yield return Mount(stepper);

            var raised = new List<(int, string)>();
            stepper.SelectionChanged += (index, text) => raised.Add((index, text));

            Click(Segments(stepper)[2]);

            Assert.That(stepper.SelectedIndex, Is.EqualTo(2));
            Assert.That(raised, Is.EqualTo(new[] { (2, "Three") }));
            Assert.IsTrue(Segments(stepper)[2].ClassListContains(QuadrantStepper.SelectedClass));
        }

        [UnityTest]
        public IEnumerator DisabledSegment_IgnoresClick_AndIsSkippedByKeyboard()
        {
            var stepper = new QuadrantStepper();
            yield return Mount(stepper);

            stepper.SetSegmentEnabled(1, false);
            Assert.IsFalse(stepper.IsSegmentEnabled(1));
            Assert.IsTrue(Segments(stepper)[1].ClassListContains(QuadrantStepper.SegmentDisabledClass));

            Click(Segments(stepper)[1]);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(0), "a disabled segment ignores taps");

            Navigate(stepper, NavigationMoveEvent.Direction.Right);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(2), "navigation skips the disabled segment");

            Navigate(stepper, NavigationMoveEvent.Direction.Left);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(0));

            stepper.SetSelectedIndex(1);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(1), "code can still select a disabled segment");
        }

        [UnityTest]
        public IEnumerator Keyboard_HomeEndAndArrows_MoveSelection()
        {
            var stepper = new SegmentedControl(new[] { "One", "Two", "Three", "Four", "Five" });
            yield return Mount(stepper);

            var raised = new List<int>();
            stepper.SelectionChanged += (index, _) => raised.Add(index);

            Assert.IsTrue(stepper.focusable);
            PressKey(stepper, KeyCode.End);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(4));
            Navigate(stepper, NavigationMoveEvent.Direction.Right);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(4), "stays at the end");
            Navigate(stepper, NavigationMoveEvent.Direction.Left);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(3));
            PressKey(stepper, KeyCode.Home);
            Assert.That(stepper.SelectedIndex, Is.EqualTo(0));

            Assert.That(raised, Is.EqualTo(new[] { 4, 3, 0 }));
        }

        [UnityTest]
        public IEnumerator Icon_ShowsAndClears()
        {
            var stepper = new SegmentedControl();
            yield return Mount(stepper);

            var icon = stepper.Query(className: QuadrantStepper.IconClass).ToList()[0];
            Assert.That(icon.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            stepper.SetSegmentIcon(0, Background.FromTexture2D(Texture2D.whiteTexture));
            yield return WaitFrames(2);
            Assert.That(icon.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(icon.resolvedStyle.width, Is.EqualTo(20f), "SegmentedControl icon size");
            Assert.IsTrue(Segments(stepper)[0].ClassListContains(QuadrantStepper.SegmentHasIconClass));

            stepper.SetSegmentIcon(0, default);
            yield return WaitFrames(2);
            Assert.That(icon.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator EqualSizing_OverlayCoversSelectedSegment()
        {
            var stepper = new SegmentedControl(new[] { "A", "B", "C", "D" });
            stepper.style.width = 400;
            yield return Mount(stepper);

            stepper.SetSelectedIndex(2);
            yield return WaitFrames(2);
            AssertOverlayCovers(stepper, 2);

            stepper.SetSelectedIndex(3);
            yield return WaitFrames(2);
            AssertOverlayCovers(stepper, 3);

            var quadrant = new QuadrantStepper();
            quadrant.style.width = 400;
            yield return Mount(quadrant);
            quadrant.SetSelectedIndex(3);
            yield return WaitFrames(2);
            AssertOverlayCovers(quadrant, 3);
        }

        [UnityTest]
        public IEnumerator ContentSizing_ScrollsSelectionIntoView()
        {
            var options = Enumerable.Range(1, 8).Select(i => $"Category number {i}").ToArray();
            var stepper = new SegmentedControl(options) { Sizing = SegmentSizing.Content };
            stepper.style.width = 200;
            yield return Mount(stepper, 3);

            Assert.IsTrue(stepper.ClassListContains(QuadrantStepper.ContentSizingClass));
            Assert.IsTrue(stepper.ClassListContains(QuadrantStepper.ScrollableClass));

            var row = stepper.Q(className: QuadrantStepper.SegmentsClass);
            Assert.That(row.layout.width, Is.GreaterThan(200f));

            stepper.SetSelectedIndex(7, notify: true, animate: false);
            yield return WaitFrames(2);

            var last = Segments(stepper)[7];
            var offset = -row.resolvedStyle.translate.x;
            Assert.That(offset, Is.GreaterThan(0f));
            Assert.That(last.layout.xMax - offset, Is.LessThanOrEqualTo(stepper.contentRect.width + 0.5f), "selected segment is inside the view");
            Assert.That(Overlay(stepper).resolvedStyle.translate.x, Is.EqualTo(row.resolvedStyle.translate.x), "overlay scrolls with the row");
        }

        [UnityTest]
        public IEnumerator SegmentedControl_ReadsThemeTokens_OverQuadrantDefaults()
        {
            var stepper = new SegmentedControl();
            yield return Mount(stepper);

            Assert.IsTrue(stepper.ClassListContains(SegmentedControl.RootClass));
            Assert.IsTrue(stepper.ClassListContains(QuadrantStepper.RootClass));
            Assert.IsTrue(stepper.ClassListContains(ThemeTokens.ThemedClass));

            var overlay = Overlay(stepper).resolvedStyle.backgroundColor;
            Assert.That(overlay.r, Is.EqualTo(0x4F / 255f).Within(0.01f), "overlay is --c-primary, not the QuadrantStepper default");
            Assert.That(overlay.g, Is.EqualTo(0x6B / 255f).Within(0.01f));
            Assert.That(overlay.b, Is.EqualTo(0xED / 255f).Within(0.01f));
            Assert.That(stepper.resolvedStyle.backgroundColor.r, Is.EqualTo(0x27 / 255f).Within(0.01f), "track is --surface-raised");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_MovesOverlayWithoutAnimation()
        {
            Root.SetReduceMotion(true);
            var stepper = new SegmentedControl(new[] { "A", "B", "C", "D" });
            stepper.style.width = 400;
            yield return Mount(stepper);

            Click(Segments(stepper)[3]);
            yield return WaitFrames(1);

            AssertOverlayCovers(stepper, 3);
        }

        [UnityTest]
        public IEnumerator Uxml_TagsAndAttributes_Instantiate()
        {
#if UNITY_EDITOR
            var tree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(tree, Is.Not.Null);
            var instance = tree.Instantiate();
            yield return Mount(instance);

            var segmented = instance.Q<SegmentedControl>("segmented");
            Assert.That(segmented, Is.Not.Null);
            Assert.That(segmented.Options, Is.EqualTo(new[] { "Day", "Week", "Month" }));
            Assert.That(segmented.Sizing, Is.EqualTo(SegmentSizing.Content));
            Assert.That(segmented.SelectedIndex, Is.EqualTo(1));

            var quadrant = instance.Q<QuadrantStepper>("quadrant");
            Assert.That(quadrant, Is.Not.Null);
            Assert.That(quadrant, Is.Not.InstanceOf<SegmentedControl>());
            Assert.That(quadrant.SegmentCount, Is.EqualTo(4));
#else
            Assert.Ignore("Loads a UXML asset through the AssetDatabase.");
            yield break;
#endif
        }
    }
}
