/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        /// <summary>
        /// A grid tall enough that every row of 11 items in 2 columns is in view, so the list makes every row when it builds,
        /// including the last row and its padding cell. It must not shrink to the 844 document root, or the last row is never made.
        /// Taps in these tests go to the top row, which is on screen whatever the screen size.
        /// </summary>
        private IEnumerator MountTallGrid(ContentGrid grid, int settleFrames = 4)
        {
            grid.style.flexShrink = 0f;
            yield return MountGrid(grid, 390f, 1200f);
            yield return WaitFrames(settleFrames);
        }

        /// <summary>
        /// The tiles showing an item, in rows the list is showing.
        /// </summary>
        private static List<VisualElement> BoundTiles(ContentGrid grid) =>
            grid.ListView.Query(className: ContentGrid.TileClass).Where(t => t.userData is ContentGridItem && t.parent.resolvedStyle.display != DisplayStyle.None).ToList();

        /// <summary>
        /// A tap as a finger or mouse makes one: pointer down then up at the tile's centre, so UI Toolkit synthesises the click.
        /// </summary>
        private static void Tap(VisualElement tile)
        {
            var centre = tile.worldBound.center;
            SendPointer<PointerDownEvent>(tile, centre);
            SendPointer<PointerUpEvent>(tile, centre);
        }

        /// <summary>
        /// Holds a pointer on a tile for some frames before releasing it. The release goes to whatever tile shows the item by
        /// then, as a finger's would.
        /// </summary>
        private static IEnumerator Press(ContentGrid grid, string id, int frames = 3)
        {
            var position = grid.TileFor(id).worldBound.center;
            SendPointer<PointerDownEvent>(grid.TileFor(id), position);
            yield return WaitFrames(frames);
            SendPointer<PointerUpEvent>(grid.TileFor(id) ?? grid, position);
        }

        /// <summary>
        /// Fails if, over some frames, the row height changes or the tile showing an item is made again. That is what a list
        /// rebuilding itself under the pointer looks like.
        /// </summary>
        private static IEnumerator AssertRowsStay(ContentGrid grid, string id, int frames = 20)
        {
            var tile = grid.TileFor(id);
            Assert.That(tile, Is.Not.Null, id);
            var height = grid.ListView.fixedItemHeight;
            for (var frame = 1; frame <= frames; frame++)
            {
                yield return null;
                Assert.That(grid.ListView.fixedItemHeight, Is.EqualTo(height), $"frame {frame}: the row height changed");
                Assert.That(grid.TileFor(id), Is.SameAs(tile), $"frame {frame}: the tile for {id} was made again");
                Assert.That(tile.panel, Is.Not.Null, $"frame {frame}: the tile for {id} left the panel");
            }
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
        public IEnumerator Rows_ShareOneMeasuredHeight_AndANewPageKeepsTheScrollOffset()
        {
            // Rows are placed at one fixed height measured from the rows themselves, so scrolling back up never corrects the
            // offset after measuring a row (the jump DynamicHeight gave), and appending a page leaves the view where it was.
            var grid = new ContentGrid { MinTileWidth = 150f };
            grid.SetItems(Items(10));
            yield return MountGrid(grid);

            var row = grid.TileFor("id0").parent;
            Assert.That(grid.ListView.virtualizationMethod, Is.EqualTo(CollectionVirtualizationMethod.FixedHeight));
            Assert.That(row.layout.height, Is.GreaterThan(150f), "a thumbnail and two lines, not the list's default row");
            Assert.That(grid.ListView.fixedItemHeight, Is.EqualTo(row.layout.height).Within(0.5f), "the list's row height is the rows' own");

            var scroll = grid.ListView.Q<ScrollView>();
            scroll.scrollOffset = new Vector2(0f, row.layout.height * 1.5f);
            yield return WaitFrames(2);
            var offset = scroll.scrollOffset.y;
            Assert.That(offset, Is.GreaterThan(0f));

            grid.SetItems(Items(20));
            yield return WaitFrames(4);
            Assert.That(grid.ListView.itemsSource.Count, Is.EqualTo(10));
            Assert.That(scroll.scrollOffset.y, Is.EqualTo(offset).Within(0.5f), "a new page leaves the view where it was");
            Assert.That(grid.ListView.fixedItemHeight, Is.EqualTo(row.layout.height).Within(0.5f));
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
        public IEnumerator OddItemCount_AllRowsVisible_RowHeightSettles()
        {
            // 11 items in 2 columns leave a padding cell in the last row, and with every row in view the list makes it when it
            // builds. It was made with empty labels, so it measured two lines shorter than the tiles. Each height in turn rebuilt
            // the list, so the rows jumped between the two every frame or two and no tile ever received a tap.
            var grid = new ContentGrid { MinTileWidth = 150f };
            grid.SetItems(Items(11));
            yield return MountTallGrid(grid);

            Assert.That(grid.ColumnCount, Is.EqualTo(2));
            Assert.That(grid.ListView.fixedItemHeight, Is.EqualTo(grid.TileFor("id0").layout.height).Within(1f), "the row height is a tile's own");
            yield return AssertRowsStay(grid, "id0");
        }

        [UnityTest]
        public IEnumerator OddItemCount_PaddingCell_MeasuresLikeATile_AndIsInert()
        {
            var grid = new ContentGrid { MinTileWidth = 150f, SelectionMode = GridSelectionMode.Multiple };
            grid.SetItems(Items(11));
            var tapped = new List<string>();
            grid.ItemTapped += item => tapped.Add(item.Id);
            yield return MountTallGrid(grid);

            var padding = grid.ListView.Query(className: ContentGrid.TileClass).Where(t => t.ClassListContains(ContentGrid.HiddenTileClass)).First();
            Assert.That(padding, Is.Not.Null, "11 items in 2 columns leave one padding cell");
            Assert.That(padding.layout.height, Is.EqualTo(grid.TileFor("id0").layout.height).Within(1f), "it measures like a tile");
            Assert.That(padding.userData, Is.Null, "no item behind it");
            Assert.That(grid.TileFor(null), Is.Null, "TileFor never returns it");

            // Sent straight to it: the last row is below the screen, so a pointer could not reach it here.
            Click(padding);
            Assert.That(tapped, Is.Empty, "not tappable");
            Assert.That(grid.SelectedIds, Is.Empty, "not selectable");
        }

        [UnityTest]
        public IEnumerator Tap_HeldForFrames_RaisesItemTapped_WithOddCount()
        {
            // UI Toolkit sends a click only when the pointer comes up over the element it went down on. A list rebuilt between
            // the two swallowed the tap. Click() sends the click straight to the element, so it never showed this.
            var grid = new ContentGrid { MinTileWidth = 150f };
            grid.SetItems(Items(11));
            var tapped = new List<string>();
            grid.ItemTapped += item => tapped.Add(item.Id);
            yield return MountTallGrid(grid);

            yield return Press(grid, "id1");
            Assert.That(tapped, Is.EqualTo(new[] { "id1" }));
        }

        [UnityTest]
        public IEnumerator ItemsWithoutMeta_ShareTheRowHeight_WhileDetailsLoad()
        {
            // While an app is still fetching details, some items have no meta line yet. A tile without one measured a line
            // shorter than its neighbours, and the rows rebuilt between the two heights until every item had its meta.
            var grid = new ContentGrid { MinTileWidth = 150f };
            var items = Items(10);
            for (var i = 0; i < items.Count; i += 2)
            {
                items[i].Meta = null;
            }

            grid.SetItems(items);
            yield return MountTallGrid(grid);

            yield return AssertRowsStay(grid, "id1");
            var heights = BoundTiles(grid).Select(t => t.layout.height).ToList();
            Assert.That(heights.Max() - heights.Min(), Is.LessThan(1f), $"tile heights {string.Join(", ", heights)}");

            var blankMeta = grid.TileFor("id0").Q<Label>(className: ContentGrid.MetaClass);
            Assert.That(blankMeta.resolvedStyle.visibility, Is.EqualTo(Visibility.Hidden), "a blank line shows nothing");

            // A detail arrives: its tile is bound again in place.
            var tile = grid.TileFor("id1");
            items[0].Meta = "12 MB";
            grid.RefreshItem("id0");
            yield return WaitFrames(2);
            Assert.That(blankMeta.text, Is.EqualTo("12 MB"));
            Assert.That(blankMeta.resolvedStyle.visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(grid.TileFor("id1"), Is.SameAs(tile));
            yield return AssertRowsStay(grid, "id1");
        }

        [UnityTest]
        public IEnumerator NoItemHasMeta_TilesLeaveTheLineOut_UntilOneDoes()
        {
            var grid = new ContentGrid { Columns = 2 };
            var items = Items(4);
            items.ForEach(i => i.Meta = null);
            grid.SetItems(items);
            yield return MountGrid(grid);
            yield return WaitFrames(2);

            var tile = grid.TileFor("id0");
            var meta = tile.Q<Label>(className: ContentGrid.MetaClass);
            Assert.That(meta.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "no item has a meta line, so no tile keeps one");
            var shortRow = grid.ListView.fixedItemHeight;

            items[1].Meta = "3 MB";
            grid.RefreshItem("id1");
            yield return WaitFrames(4);
            Assert.That(meta.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "every tile keeps the line once one item has it");
            Assert.That(grid.ListView.fixedItemHeight, Is.GreaterThan(shortRow + 1f));
            Assert.That(grid.ListView.fixedItemHeight, Is.EqualTo(tile.layout.height).Within(1f));
            Assert.That(grid.TileFor("id0"), Is.SameAs(tile), "the rows are resized in place, not made again");

            items[1].Meta = null;
            grid.RefreshItem("id1");
            yield return WaitFrames(4);
            Assert.That(meta.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(grid.ListView.fixedItemHeight, Is.EqualTo(shortRow).Within(1f), "and shrink back when it goes");
        }

        [UnityTest]
        public IEnumerator ItemsArrivingWhileLoading_RowHeightSettles_AndTapsLand()
        {
            // An app starting up: the grid mounts loading, then items arrive a few at a time, through odd counts, with every
            // row in view.
            var grid = new ContentGrid { MinTileWidth = 150f, IsLoading = true };
            var tapped = new List<string>();
            grid.ItemTapped += item => tapped.Add(item.Id);
            yield return MountTallGrid(grid, 0);

            for (var count = 1; count <= 11; count += 2)
            {
                grid.SetItems(Items(count));
                grid.IsLoading = false;
                yield return WaitFrames(2);
            }

            yield return WaitFrames(4);
            yield return AssertRowsStay(grid, "id0");
            yield return Press(grid, "id1");
            Assert.That(tapped, Is.EqualTo(new[] { "id1" }));
        }

        [UnityTest]
        public IEnumerator Tap_RaisesItemTapped_AndSelectionModes([Values(6, 7)] int count)
        {
            var grid = new ContentGrid { Columns = 3 };
            grid.SetItems(Items(count));
            var tapped = new List<string>();
            var selections = new List<string>();
            grid.ItemTapped += item => tapped.Add(item.Id);
            grid.SelectionChanged += ids => selections.Add(string.Join(",", ids));
            yield return MountGrid(grid);

            Tap(grid.TileFor("id1"));
            Assert.That(tapped, Is.EqualTo(new[] { "id1" }));
            Assert.That(selections, Is.Empty, "no selection in None mode");

            grid.SelectionMode = GridSelectionMode.Multiple;
            Tap(grid.TileFor("id4"));
            Tap(grid.TileFor("id0"));
            Tap(grid.TileFor("id4"));
            Assert.That(selections, Is.EqualTo(new[] { "id4", "id0,id4", "id0" }));
            Assert.IsTrue(grid.TileFor("id0").ClassListContains(ContentGrid.SelectedClass));

            grid.SelectionMode = GridSelectionMode.Single;
            Tap(grid.TileFor("id5"));
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
        public IEnumerator RecycledRows_HaveNoHoverOrSelectionBackground()
        {
            var grid = new ContentGrid { Columns = 2 };
            grid.SetItems(Items(4));
            yield return MountGrid(grid);
            var row = grid.ListView.Q(className: "unity-collection-view__item");
            Assert.That(row, Is.Not.Null);
            var states = typeof(VisualElement).GetProperty("pseudoStates", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(states, Is.Not.Null);

            foreach (var state in new[] { "Hover", "Checked", "Hover, Checked, Focus" })
            {
                states.SetValue(row, System.Enum.Parse(states.PropertyType, state));
                yield return WaitFrames(2);
                Assert.That(row.resolvedStyle.backgroundColor.a, Is.EqualTo(0f), state);
                Assert.That(row.resolvedStyle.backgroundImage, Is.EqualTo(default(Background)), state);
            }
        }

        [UnityTest]
        public IEnumerator Scrolling_DoesNotTapOrSelectATile_AndNextTapStillWorks([Values(4, 5)] int count)
        {
            var grid = new ContentGrid { Columns = 2, SelectionMode = GridSelectionMode.Single };
            grid.SetItems(Items(count));
            var tapped = new List<string>();
            grid.ItemTapped += item => tapped.Add(item.Id);
            yield return MountGrid(grid);

            var tile = grid.TileFor("id0");
            var start = tile.worldBound.center;
            SendPointer<PointerDownEvent>(tile, start);
            SendPointer<PointerMoveEvent>(tile, start + new Vector2(0f, 30f));
            SendPointer<PointerUpEvent>(tile, start + new Vector2(0f, 30f));
            Assert.That(tapped, Is.Empty);
            Assert.That(grid.SelectedIds, Is.Empty);
            Assert.IsFalse(tile.ClassListContains(ContentGrid.ArmedClass));
            Assert.That(grid.ListView.selectionType, Is.EqualTo(SelectionType.None));

            SendPointer<PointerDownEvent>(tile, start);
            SendPointer<PointerUpEvent>(tile, start);
            Assert.That(tapped, Is.EqualTo(new[] { "id0" }));
            Assert.That(grid.SelectedIds, Is.EqualTo(new[] { "id0" }));
            Assert.IsFalse(grid.TileFor("id1").ClassListContains(ContentGrid.SelectedClass));
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
