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
    public class ContentGridTests : UIDocumentTestBase
    {
        private static List<ContentGridItem> Items(int count) =>
            Enumerable.Range(0, count).Select(i => new ContentGridItem($"id{i}", $"Item {i}", $"{i} MB", i % 2 == 0 ? "3D" : null)).ToList();

        private IEnumerator MountGrid(ContentGrid grid, float width = 390f, float height = 600f)
        {
            grid.style.width = width;
            grid.style.height = height;
            yield return Mount(grid, 4);
        }

        [UnityTest]
        public IEnumerator AutoColumns_FollowWidth_AndTilesBind()
        {
            var grid = new ContentGrid { MinTileWidth = 150f };
            grid.SetItems(Items(7));
            yield return MountGrid(grid);

            Assert.That(grid.ColumnCount, Is.EqualTo(2), "390 wide / 150 minimum");
            var tile = grid.TileFor("id2");
            Assert.That(tile, Is.Not.Null);
            Assert.That(tile.Q<Label>(className: ContentGrid.NameClass).text, Is.EqualTo("Item 2"));
            Assert.That(tile.Q<Label>(className: ContentGrid.BadgeClass).text, Is.EqualTo("3D"));
            Assert.That(grid.TileFor("id1").Q<Label>(className: ContentGrid.BadgeClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            var thumb = tile.Q(className: ContentGrid.ThumbClass);
            Assert.That(thumb.layout.height, Is.EqualTo(thumb.layout.width).Within(1f), "square thumbnails");

            grid.style.width = 820f;
            yield return WaitFrames(4);
            Assert.That(grid.ColumnCount, Is.EqualTo(5), "tablet width");
            Assert.That(grid.ListView.itemsSource.Count, Is.EqualTo(2), "7 items in 5 columns is 2 rows");
        }

        [UnityTest]
        public IEnumerator AutoColumns_TilesFillTheRow_OnATablet()
        {
            var grid = new ContentGrid { MinTileWidth = 160f };
            grid.SetItems(Items(4));
            yield return MountGrid(grid, 780f);

            Assert.That(grid.ColumnCount, Is.EqualTo(4));
            var first = grid.TileFor("id0");
            var last = grid.TileFor("id3");
            Assert.That(last.worldBound.xMax, Is.EqualTo(grid.worldBound.xMax - 4f).Within(1.5f), "no gap on the right");
            Assert.That(first.layout.width, Is.EqualTo((780f - 8f) / 4f).Within(1.5f));

            grid.style.width = 390f;
            yield return WaitFrames(4);
            grid.style.width = 780f;
            yield return WaitFrames(4);
            last = grid.TileFor("id3");
            Assert.That(last.worldBound.xMax, Is.EqualTo(grid.worldBound.xMax - 4f).Within(1.5f), "after a resize too");
        }

        [UnityTest]
        public IEnumerator AutoColumns_FillTheRow_InASheetOpeningOnATablet()
        {
            // The screens session's browser: an 820 x 1180 host, a half sheet animating open, the grid with 20 px side
            // margins, a hidden scroller and a 1.5 thumbnail aspect.
            var host = new VisualElement();
            host.style.width = 820f;
            host.style.height = 1180f;
            var sheet = new BottomSheet();
            host.Add(sheet);
            var grid = new ContentGrid { Columns = 0, MinTileWidth = 160f, SelectionMode = GridSelectionMode.None, ThumbnailAspect = 1.5f };
            grid.style.flexGrow = 1f;
            grid.style.marginLeft = 20f;
            grid.style.marginRight = 20f;
            grid.ListView.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
            grid.SetItems(Items(4));
            sheet.Add(grid);
            yield return Mount(host, 2);

            sheet.Open(SheetDetent.Half);
            yield return WaitMs(600);

            Assert.That(grid.ColumnCount, Is.EqualTo(4));
            var last = grid.TileFor("id3");
            Assert.That(last.worldBound.xMax, Is.EqualTo(grid.worldBound.xMax - 4f).Within(1.5f), $"grid {grid.worldBound}, last tile {last.worldBound}");
        }

        [UnityTest]
        public IEnumerator Tap_RaisesItemTapped_AndSelectionModes()
        {
            var grid = new ContentGrid { Columns = 3 };
            grid.SetItems(Items(6));
            var tapped = new List<string>();
            var selections = new List<string>();
            grid.ItemTapped += item => tapped.Add(item.Id);
            grid.SelectionChanged += ids => selections.Add(string.Join(",", ids));
            yield return MountGrid(grid);

            Click(grid.TileFor("id1"));
            Assert.That(tapped, Is.EqualTo(new[] { "id1" }));
            Assert.That(selections, Is.Empty, "no selection in None mode");

            grid.SelectionMode = GridSelectionMode.Multiple;
            Click(grid.TileFor("id4"));
            Click(grid.TileFor("id0"));
            Click(grid.TileFor("id4"));
            Assert.That(selections, Is.EqualTo(new[] { "id4", "id0,id4", "id0" }));
            Assert.IsTrue(grid.TileFor("id0").ClassListContains(ContentGrid.SelectedClass));

            grid.SelectionMode = GridSelectionMode.Single;
            Click(grid.TileFor("id5"));
            yield return WaitFrames(2);
            Assert.That(grid.SelectedIds, Is.EqualTo(new[] { "id5" }));
            Assert.IsFalse(grid.TileFor("id0").ClassListContains(ContentGrid.SelectedClass));
        }

        [UnityTest]
        public IEnumerator SetItems_KeepsSurvivingSelection()
        {
            var grid = new ContentGrid { Columns = 2, SelectionMode = GridSelectionMode.Multiple };
            grid.SetItems(Items(4));
            yield return MountGrid(grid);

            grid.SetSelection(new[] { "id1", "id3" });
            grid.SetItems(Items(3));
            Assert.That(grid.SelectedIds, Is.EqualTo(new[] { "id1" }));
        }

        [UnityTest]
        public IEnumerator LoadingAndEmptyStates()
        {
            var grid = new ContentGrid { EmptyTitle = "No content yet", EmptyActionText = "Refresh" };
            var refreshes = 0;
            grid.EmptyActionClicked += () => refreshes++;
            yield return MountGrid(grid);

            Assert.IsTrue(grid.ClassListContains(ContentGrid.EmptyModifierClass));
            var empty = grid.Q<EmptyState>(className: ContentGrid.EmptyClass);
            Assert.That(empty.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Click(empty.ActionButton.Q(className: PillButton.BackgroundClass));
            Assert.That(refreshes, Is.EqualTo(1));

            grid.IsLoading = true;
            yield return WaitFrames(2);
            Assert.IsTrue(grid.ClassListContains(ContentGrid.LoadingClass));
            Assert.That(grid.Q<SkeletonTile>(className: ContentGrid.SkeletonClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(empty.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            grid.SetItems(Items(2));
            grid.IsLoading = false;
            yield return WaitFrames(2);
            Assert.That(grid.ListView.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator LongPressThenMove_RaisesDragStarted_MoveFirstDoesNot()
        {
            var grid = new ContentGrid { Columns = 2 };
            grid.SetItems(Items(4));
            var drags = new List<string>();
            grid.DragStarted += (item, _) => drags.Add(item.Id);
            yield return MountGrid(grid);

            var tile = grid.TileFor("id2");
            var start = tile.worldBound.center;
            SendPointer<PointerDownEvent>(tile, start);
            SendPointer<PointerMoveEvent>(tile, start + new Vector2(0f, 30f));
            yield return WaitMs(ContentGrid.LongPressMs + 100);
            Assert.That(drags, Is.Empty, "moving before the hold is a scroll");

            SendPointer<PointerDownEvent>(tile, start);
            yield return WaitMs(ContentGrid.LongPressMs + 100);
            Assert.IsTrue(tile.ClassListContains(ContentGrid.ArmedClass));
            SendPointer<PointerMoveEvent>(tile, start + new Vector2(0f, -40f));
            Assert.That(drags, Is.EqualTo(new[] { "id2" }));
            SendPointer<PointerUpEvent>(tile, start + new Vector2(0f, -40f));
        }

        [UnityTest]
        public IEnumerator DisabledTile_GreyedInPlace_NoTapDragOrSelection()
        {
            var grid = new ContentGrid { Columns = 2, SelectionMode = GridSelectionMode.Multiple };
            var items = Items(4);
            items[1].IsEnabled = false;
            grid.SetItems(items);
            var tapped = new List<string>();
            var drags = new List<string>();
            var selections = new List<string>();
            grid.ItemTapped += item => tapped.Add(item.Id);
            grid.DragStarted += (item, _) => drags.Add(item.Id);
            grid.SelectionChanged += ids => selections.Add(string.Join(",", ids));
            yield return MountGrid(grid);

            var enabled = grid.TileFor("id0");
            var disabled = grid.TileFor("id1");
            Assert.IsTrue(disabled.ClassListContains(ContentGrid.DisabledTileClass));
            Assert.IsFalse(enabled.ClassListContains(ContentGrid.DisabledTileClass));
            Assert.That(disabled.layout.width, Is.EqualTo(enabled.layout.width).Within(0.01f), "same space as any tile");
            Assert.That(disabled.layout.height, Is.EqualTo(enabled.layout.height).Within(0.01f));
            Assert.That(disabled.Q(className: ContentGrid.ThumbClass).resolvedStyle.opacity, Is.EqualTo(0.45f).Within(0.001f), "thumbnail and badge dimmed");
            Assert.That(disabled.Q(className: ContentGrid.NameClass).resolvedStyle.opacity, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(disabled.Q(className: ContentGrid.MetaClass).resolvedStyle.opacity, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(enabled.Q(className: ContentGrid.ThumbClass).resolvedStyle.opacity, Is.EqualTo(1f));
            Assert.That(disabled.Q<Label>(className: ContentGrid.NameClass).text, Is.EqualTo("Item 1"), "the name is unchanged");
            Assert.That(disabled.enabledSelf, Is.True, "not UI Toolkit's disabled state, so it keeps its focus behaviour");

            Click(disabled);
            Assert.That(tapped, Is.Empty, "no tap");
            Assert.That(selections, Is.Empty, "not selected");
            grid.SetSelection(new[] { "id1", "id2" });
            Assert.That(grid.SelectedIds, Is.EqualTo(new[] { "id2" }), "SetSelection skips it");

            var start = disabled.worldBound.center;
            SendPointer<PointerDownEvent>(disabled, start);
            yield return WaitMs(ContentGrid.LongPressMs + 100);
            Assert.IsFalse(disabled.ClassListContains(ContentGrid.ArmedClass), "no press feedback");
            SendPointer<PointerMoveEvent>(disabled, start + new Vector2(0f, -40f));
            SendPointer<PointerUpEvent>(disabled, start + new Vector2(0f, -40f));
            Assert.That(drags, Is.Empty, "no drag-out");

            // Disabling a selected item drops it from the selection.
            items[2].IsEnabled = false;
            grid.RefreshItem("id2");
            yield return WaitFrames(2);
            Assert.That(grid.SelectedIds, Is.Empty);
            Assert.IsTrue(grid.TileFor("id2").ClassListContains(ContentGrid.DisabledTileClass));

            items[1].IsEnabled = true;
            grid.RefreshItem("id1");
            yield return WaitFrames(2);
            Assert.IsFalse(grid.TileFor("id1").ClassListContains(ContentGrid.DisabledTileClass));
            Click(grid.TileFor("id1"));
            Assert.That(tapped, Is.EqualTo(new[] { "id1" }), "taps again once enabled");
        }

        private static void SendPointer<T>(VisualElement target, Vector2 position) where T : PointerEventBase<T>, new()
        {
            var type = typeof(T) == typeof(PointerDownEvent) ? EventType.MouseDown
                : typeof(T) == typeof(PointerUpEvent) ? EventType.MouseUp
                : EventType.MouseDrag;
            var imgui = new Event { type = type, mousePosition = position, button = 0, clickCount = 1 };
            using var evt = PointerEventBase<T>.GetPooled(imgui);
            evt.target = target;
            target.SendEvent(evt);
        }
    }
}
