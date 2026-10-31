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
    /// <summary>
    /// Checks for the FS03_1 content browser: thumbnail aspect and the pinned sheet footer.
    /// </summary>
    public class BrowserRequestTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator ContentGrid_ThumbnailAspect_AppliesToTilesAndSkeleton()
        {
            var grid = new ContentGrid { Columns = 3, ThumbnailAspect = 1.5f };
            grid.style.width = 390;
            grid.style.height = 600;
            grid.SetItems(Enumerable.Range(0, 6).Select(i => new ContentGridItem($"i{i}", $"Item {i}")).ToList());
            yield return Mount(grid, 4);

            var thumb = grid.TileFor("i0").Q(className: ContentGrid.ThumbClass);
            Assert.That(thumb.layout.height, Is.EqualTo(thumb.layout.width / 1.5f).Within(1f), "3:2 thumbnails");

            grid.IsLoading = true;
            yield return WaitFrames(3);
            var skeletonThumb = grid.Q<SkeletonTile>(className: ContentGrid.SkeletonClass).Q(className: SkeletonTile.ThumbClass);
            Assert.That(skeletonThumb.layout.height, Is.EqualTo(skeletonThumb.layout.width / 1.5f).Within(1f), "the skeleton follows the aspect");

            grid.IsLoading = false;
            grid.ThumbnailAspect = 1f;
            yield return WaitFrames(3);
            thumb = grid.TileFor("i0").Q(className: ContentGrid.ThumbClass);
            Assert.That(thumb.layout.height, Is.EqualTo(thumb.layout.width).Within(1f), "back to square");
        }

        private BottomSheet sheet;
        private VisualElement footer;
        private ScrollView scroll;

        private IEnumerator MountSheet(bool reduceMotion)
        {
            Root.SetReduceMotion(reduceMotion);
            sheet = new BottomSheet { FullTopOffset = 44f, BottomInset = 34f, HalfRatio = 0.71f };
            scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            for (var i = 0; i < 40; i++)
            {
                scroll.Add(new Label($"Row {i}"));
            }

            sheet.Add(scroll);
            // A plain element: the default theme gives a Label margins that would blur the inset check.
            footer = new VisualElement();
            footer.style.height = 48;
            sheet.SetFooter(footer);
            yield return Mount(sheet, 3);
        }

        private float FooterBottom() => sheet.Footer.worldBound.yMax;

        [UnityTest]
        public IEnumerator Footer_PinnedToVisibleBottom_AtHalfAndFull_HiddenAtPeek()
        {
            yield return MountSheet(reduceMotion: true);

            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(3);
            Assert.IsTrue(sheet.ClassListContains(BottomSheet.HasFooterClass));
            Assert.That(FooterBottom(), Is.EqualTo(sheet.worldBound.yMax).Within(1f), "footer on the visible bottom edge at half");
            Assert.That(footer.worldBound.yMax, Is.EqualTo(sheet.worldBound.yMax - 34f).Within(1f), "footer content above the bottom inset");
            Assert.That(scroll.worldBound.yMax, Is.LessThanOrEqualTo(sheet.Footer.worldBound.yMin + 1f), "content ends at the footer, so it scrolls in the visible area");

            sheet.SnapTo(SheetDetent.Full);
            yield return WaitFrames(3);
            Assert.That(FooterBottom(), Is.EqualTo(sheet.worldBound.yMax).Within(1f), "and at full");
            Assert.That(scroll.worldBound.yMax, Is.LessThanOrEqualTo(sheet.Footer.worldBound.yMin + 1f));

            sheet.SnapTo(SheetDetent.Peek);
            yield return WaitFrames(3);
            Assert.That(sheet.Footer.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "hidden at peek");

            sheet.SetFooter(null);
            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(3);
            Assert.That(sheet.Footer.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(scroll.worldBound.yMax, Is.EqualTo(sheet.worldBound.yMax - 34f).Within(1f), "without a footer the content ends above the inset");
        }

        [UnityTest]
        public IEnumerator Footer_StaysPinned_WhileAnimatingAndDragging()
        {
            yield return MountSheet(reduceMotion: false);
            sheet.SnapTo(SheetDetent.Half, animate: false);
            yield return WaitFrames(3);

            sheet.SnapTo(SheetDetent.Full);
            yield return WaitFrames(3);
            Assert.That(sheet.Panel.resolvedStyle.translate.y, Is.GreaterThan(1f), "still animating");
            Assert.That(FooterBottom(), Is.EqualTo(sheet.worldBound.yMax).Within(2f), "pinned mid-animation");

            yield return WaitMs(ThemeTokens.Motion.BaseMs + 200);
            sheet.DragStart(1, 300f, 10f);
            sheet.DragMove(340f, 10.5f);
            sheet.DragMove(380f, 11f);
            yield return WaitFrames(2);
            Assert.IsTrue(sheet.ClassListContains(BottomSheet.DraggingClass));
            Assert.That(FooterBottom(), Is.EqualTo(sheet.worldBound.yMax).Within(1f), "pinned mid-drag");
            sheet.DragEnd(11.5f);
        }
    }
}
