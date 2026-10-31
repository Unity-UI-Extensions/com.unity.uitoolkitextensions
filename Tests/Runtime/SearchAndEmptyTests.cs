/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class SearchFieldTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Typing_RaisesQueryChangedOnceAfterDebounce()
        {
            var search = new SearchField { DebounceMs = 150 };
            var queries = new List<string>();
            search.QueryChanged += queries.Add;
            yield return Mount(search);

            search.TextField.value = "p";
            search.TextField.value = "pl";
            search.TextField.value = "pla";
            Assert.That(queries, Is.Empty, "nothing before the pause");
            Assert.IsTrue(search.ClassListContains(SearchField.HasValueClass));

            yield return WaitMs(300);
            Assert.That(queries, Is.EqualTo(new[] { "pla" }));
        }

        [UnityTest]
        public IEnumerator Enter_Submits_AndClearEmpties()
        {
            var search = new SearchField { DebounceMs = 1000 };
            var submitted = new List<string>();
            var queries = new List<string>();
            var cleared = 0;
            search.Submitted += submitted.Add;
            search.QueryChanged += queries.Add;
            search.Cleared += () => cleared++;
            yield return Mount(search);

            search.TextField.value = "lobby";
            PressKey(search.TextField, KeyCode.Return);
            Assert.That(submitted, Is.EqualTo(new[] { "lobby" }));
            Assert.That(queries, Is.EqualTo(new[] { "lobby" }), "submit flushes the debounce");

            var clear = search.Q(className: SearchField.ClearClass);
            Assert.That(clear.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Click(clear);

            Assert.That(search.Value, Is.Empty);
            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(queries, Is.EqualTo(new[] { "lobby", string.Empty }));
            yield return WaitFrames(2);
            Assert.That(clear.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator ClearQuery_CancelsDebounceAndPreservesChildren()
        {
            var search = new SearchField { DebounceMs = 150 };
            var queries = new List<string>();
            var cleared = 0;
            search.QueryChanged += queries.Add;
            search.Cleared += () => cleared++;
            yield return Mount(search);

            var childCount = search.childCount;
            search.TextField.value = "lobby";
            search.ClearQuery();
            search.ClearQuery();
            yield return WaitMs(300);

            Assert.That(search.Value, Is.Empty);
            Assert.That(queries, Is.EqualTo(new[] { string.Empty }));
            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(search.childCount, Is.EqualTo(childCount));
        }

        [UnityTest]
        public IEnumerator ResultCount_AndValueSetter_DoNotRaise()
        {
            var search = new SearchField();
            var queries = 0;
            search.QueryChanged += _ => queries++;
            yield return Mount(search);

            var count = search.Q<Label>(className: SearchField.CountClass);
            Assert.That(count.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            search.ResultCount = 12;
            search.Value = "trail";
            yield return WaitMs(450);

            Assert.That(count.text, Is.EqualTo("12"));
            Assert.That(count.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(queries, Is.EqualTo(0));
            Assert.That(search.KeyboardType, Is.EqualTo(TouchScreenKeyboardType.Search));
            Assert.That(search.resolvedStyle.borderTopLeftRadius, Is.EqualTo(search.layout.height / 2f).Within(0.5f), "a pill: half the height");
        }
    }

    public class EmptyStateTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator ShowsTexts_AndOneAction()
        {
            var empty = new EmptyState("Nothing here yet", "Scan a space to start placing content.", "Start scanning");
            var actions = 0;
            empty.ActionClicked += () => actions++;
            yield return Mount(empty);

            Assert.That(empty.Q<Label>(className: EmptyState.TitleClass).text, Is.EqualTo("Nothing here yet"));
            Assert.That(empty.ActionButton.Variant, Is.EqualTo(PillButtonVariant.Primary));
            Assert.That(empty.ActionButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.IsFalse(empty.ClassListContains(EmptyState.HasIconClass));

            Click(empty.ActionButton.Q(className: PillButton.BackgroundClass));
            Assert.That(actions, Is.EqualTo(1));

            empty.ActionText = null;
            empty.BodyText = null;
            yield return WaitFrames(2);
            Assert.That(empty.ActionButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(empty.Q<Label>(className: EmptyState.BodyClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }
    }

    public class SkeletonTileTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Tiles_BuildCountAndShape()
        {
            var skeleton = new SkeletonTile(SkeletonShape.Tile, 4) { Columns = 2 };
            skeleton.style.width = 300;
            yield return Mount(skeleton, 3);

            var items = skeleton.Query(className: SkeletonTile.ItemClass).ToList();
            Assert.That(items.Count, Is.EqualTo(4));
            Assert.IsTrue(skeleton.ClassListContains("skeletonTile--tile"));
            Assert.That(items[0].layout.width, Is.EqualTo(150f).Within(1f), "two columns");
            var thumb = items[0].Q(className: SkeletonTile.ThumbClass);
            Assert.That(thumb.layout.height, Is.EqualTo(thumb.layout.width).Within(1f), "square thumbnail");
            Assert.That(items[0].Query(className: SkeletonTile.LineClass).ToList().Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Pulse_TogglesDim_UnlessReduceMotion()
        {
            var skeleton = new SkeletonTile(SkeletonShape.Row, 3);
            yield return Mount(skeleton);

            Assert.IsTrue(skeleton.IsPulsing);
            var sawDim = false;
            for (var i = 0; i < 12 && !sawDim; i++)
            {
                yield return WaitMs(200);
                sawDim |= skeleton.ClassListContains(SkeletonTile.DimClass);
            }

            Assert.IsTrue(sawDim, "the pulse dims the placeholders");

            Root.SetReduceMotion(true);
            yield return WaitMs(SkeletonTile.PulseIntervalMs + 100);
            Assert.IsFalse(skeleton.IsPulsing);
            Assert.IsFalse(skeleton.ClassListContains(SkeletonTile.DimClass), "holds still with reduce motion");
        }
    }
}
