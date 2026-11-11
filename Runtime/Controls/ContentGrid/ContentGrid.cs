/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum GridSelectionMode
    {
        /// <summary>
        /// Taps raise <see cref="ContentGrid.ItemTapped"/> only.
        /// </summary>
        None,

        /// <summary>
        /// A tap selects one item.
        /// </summary>
        Single,

        /// <summary>
        /// Taps toggle items in and out of the selection.
        /// </summary>
        Multiple,
    }

    /// <summary>
    /// One tile of a <see cref="ContentGrid"/>. Strings are display text; localise before passing them.
    /// </summary>
    public sealed class ContentGridItem
    {
        public string Id;
        public string Name;

        /// <summary>
        /// A secondary line, such as size or distance. While any item in the grid has one, every tile keeps the line, blank
        /// where it is empty, so all tiles stay one height; when no item has one, every tile leaves it out.
        /// </summary>
        public string Meta;

        /// <summary>
        /// The type badge, such as "3D" or "Audio". Empty hides it.
        /// </summary>
        public string TypeLabel;

        /// <summary>
        /// The thumbnail; set it later and call <see cref="ContentGrid.RefreshItem"/> when it loads.
        /// </summary>
        public Background Thumbnail;

        /// <summary>
        /// Anything the owner wants back in the events.
        /// </summary>
        public object UserData;

        /// <summary>
        /// False greys the tile out (<c>contentGrid__tile--disabled</c>), such as an item that cannot be placed yet. It
        /// keeps its place and its name, but raises no tap or drag, cannot be selected and shows no press feedback.
        /// After changing it on a shown item, call <see cref="ContentGrid.RefreshItem"/>.
        /// </summary>
        public bool IsEnabled = true;

        public ContentGridItem() { }

        public ContentGridItem(string id, string name, string meta = null, string typeLabel = null)
        {
            Id = id;
            Name = name;
            Meta = meta;
            TypeLabel = typeLabel;
        }
    }

    /// <summary>
    /// A virtualised grid of content tiles (thumbnail, type badge, name, meta) with tap, single or multiple
    /// selection, a long-press drag-out source, and built-in loading and empty states. The column count
    /// follows the width unless fixed.
    /// </summary>
    /// <remarks>
    /// <para>Rows are recycled by a <see cref="ListView"/>, so large catalogues stay cheap.</para>
    /// <para>Drag-out: hold a tile for <see cref="LongPressMs"/>, then move; <see cref="DragStarted"/> carries the item
    /// and the pointer position, and the owner takes over (for example to place the item in the world).</para>
    /// <para>Paging: after <see cref="EnableLoadMore"/>, <see cref="LoadMoreRequested"/> is raised when the end of the items
    /// comes within <see cref="LoadMoreThreshold"/> of the bottom of the view, including when every item fits without
    /// scrolling. Answer it by fetching the next page between <see cref="BeginLoadMore"/> and <see cref="EndLoadMore"/>,
    /// passing all items so far to <see cref="SetItems"/>; the scroll offset and the rows on screen are kept. Call
    /// <see cref="DisableLoadMore"/> when there are no more.</para>
    /// <para>A disabled tile shows its thumbnail, badge and text at 45% opacity.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ContentGrid : VisualElement
    {
        public const string RootClass = "contentGrid";
        public const string ListClass = "contentGrid__list";
        public const string RowClass = "contentGrid__row";
        public const string TileClass = "contentGrid__tile";
        public const string ThumbClass = "contentGrid__thumb";
        public const string BadgeClass = "contentGrid__badge";
        public const string CheckClass = "contentGrid__check";
        public const string NameClass = "contentGrid__name";
        public const string MetaClass = "contentGrid__meta";
        public const string SkeletonClass = "contentGrid__skeleton";
        public const string EmptyClass = "contentGrid__empty";
        public const string LoadingClass = "contentGrid--loading";
        public const string EmptyModifierClass = "contentGrid--empty";
        public const string SelectedClass = "contentGrid__tile--selected";
        public const string ArmedClass = "contentGrid__tile--armed";
        public const string HiddenTileClass = "contentGrid__tile--hidden";
        public const string DisabledTileClass = "contentGrid__tile--disabled";
        public const string LoadingMoreClass = "contentGrid--loading-more";

        /// <summary>
        /// Hold time before a tile can be dragged out.
        /// </summary>
        public const int LongPressMs = 450;

        private const float DragSlop = 10f;

        // Hidden text that holds a blank line open (see SetLine). A capital and a descender, so it is never shorter than a line
        // of real text.
        private const string BlankLine = "Ag";

        // The row height may change this often without settling. After that it only grows until it settles or the width,
        // columns, thumbnail aspect or meta line change, so no disagreement between tiles can turn into a loop (see ApplyRowHeight).
        private const int RowHeightChangeBudget = 4;

        // What a padding cell is bound to, so it has the same lines as a tile and measures like one.
        private static readonly ContentGridItem PaddingItem = new();

        private readonly ListView list;
        private readonly ScrollView scrollView;
        private readonly UQueryState<VisualElement> tileQuery;
        private readonly UQueryState<VisualElement> rowQuery;
        private readonly Action applyRowHeight;
        private readonly Action onLoadMoreCheckDue;
        private readonly SkeletonTile skeleton;
        private readonly EmptyState empty;
        private readonly List<ContentGridItem> items = new();
        private readonly List<int> rows = new();
        private readonly HashSet<string> selection = new();

        private int fixedColumns;
        private float boundWidth = float.NaN;
        // The width every thumbnail has at the bound width, once one has been laid out, so a row is the right height on its first
        // layout rather than one pass later. NaN until measured, and again after the width or the columns change.
        private float measuredThumbWidth = float.NaN;
        // A tile laid out, or the row height changed, and the height is due a check after the layout pass.
        private bool rowHeightScheduled;
        // Row height changes since the height last settled or the width, columns, thumbnail aspect or meta line changed.
        private int rowHeightChanges;
        // Row height checks in a row that found nothing to change. Two span a layout pass, so the height has settled and the
        // content can be measured for paging.
        private int rowHeightQuietChecks;
        // Whether tiles keep the meta line: true while any item has one.
        private bool showMetaLine;

        // Paging (see EnableLoadMore).
        private bool loadMoreEnabled;
        // Between BeginLoadMore and EndLoadMore.
        private bool loadMoreInFlight;
        // A request was raised and not yet answered by a new item count or EndLoadMore.
        private bool loadMoreOutstanding;
        // The item count when the last request was raised.
        private int loadMoreRequestCount = -1;
        // The item count at which a request ended with nothing added, or -1. No request at that count until the view leaves the
        // end of the items and comes back.
        private int loadMoreStalledCount = -1;
        private bool loadMoreLeftEnd;
        private bool loadMoreCheckPending;
        private bool loadMoreCheckScheduled;
        private float loadMoreThreshold = 0.25f;
        private int columns = 2;
        private float minTileWidth = 150f;
        private float thumbnailAspect = 1f;
        private GridSelectionMode selectionMode = GridSelectionMode.None;
        private bool isLoading;

        private ContentGridItem pressItem;
        private VisualElement pressTile;
        private Vector2 pressStart;
        private bool pressArmed;
        private bool pressDragging;
        private bool suppressNextClick;
        private int pressPointerId = PointerId.invalidPointerId;
        private IVisualElementScheduledItem pressTimer;

        public event Action<ContentGridItem> ItemTapped;

        /// <summary>
        /// Raised with the selected ids, in item order, when the selection changes.
        /// </summary>
        public event Action<IReadOnlyList<string>> SelectionChanged;

        /// <summary>
        /// Raised when a held tile starts moving: the item and the pointer position in panel space.
        /// </summary>
        public event Action<ContentGridItem, Vector2> DragStarted;

        public event Action EmptyActionClicked;

        /// <summary>
        /// Raised while paging is on (<see cref="EnableLoadMore"/>) when the end of the items comes within
        /// <see cref="LoadMoreThreshold"/> of the bottom of the view, including when every item fits without scrolling. Raised
        /// once per page: not again until the item count changes or <see cref="EndLoadMore"/> is called, and never while
        /// <see cref="IsLoading"/>, between <see cref="BeginLoadMore"/> and <see cref="EndLoadMore"/>, or before the row height
        /// has settled.
        /// </summary>
        public event Action LoadMoreRequested;

        public IReadOnlyList<ContentGridItem> Items => items;

        /// <summary>
        /// The columns in use now.
        /// </summary>
        public int ColumnCount => columns;

        public ListView ListView => list;

        /// <summary>
        /// True between <see cref="EnableLoadMore"/> and <see cref="DisableLoadMore"/>.
        /// </summary>
        public bool IsLoadMoreEnabled => loadMoreEnabled;

        /// <summary>
        /// True between <see cref="BeginLoadMore"/> and <see cref="EndLoadMore"/>.
        /// </summary>
        public bool IsLoadingMore => loadMoreInFlight;

        /// <summary>
        /// How near the end of the items comes to the bottom of the view before <see cref="LoadMoreRequested"/> is raised, as a
        /// fraction of the view's height. The default, 0.25, asks when less than a quarter of a view is left to scroll.
        /// </summary>
        [UxmlAttribute("load-more-threshold")]
        public float LoadMoreThreshold
        {
            get => loadMoreThreshold;
            set
            {
                loadMoreThreshold = Mathf.Max(0f, value);
                RequestLoadMoreCheck();
            }
        }

        public IReadOnlyList<string> SelectedIds
        {
            get
            {
                var ids = new List<string>();
                foreach (var item in items)
                {
                    if (selection.Contains(item.Id))
                    {
                        ids.Add(item.Id);
                    }
                }

                return ids;
            }
        }

        /// <summary>
        /// Fixed column count; 0 picks it from the width and <see cref="MinTileWidth"/>.
        /// </summary>
        [UxmlAttribute("columns")]
        public int Columns
        {
            get => fixedColumns;
            set
            {
                fixedColumns = Mathf.Max(0, value);
                UpdateColumns();
            }
        }

        [UxmlAttribute("min-tile-width")]
        public float MinTileWidth
        {
            get => minTileWidth;
            set
            {
                minTileWidth = Mathf.Max(48f, value);
                UpdateColumns();
            }
        }

        /// <summary>
        /// Width over height of the thumbnails; 1 is square, 1.5 is 3:2. The loading skeleton follows it.
        /// </summary>
        [UxmlAttribute("thumbnail-aspect")]
        public float ThumbnailAspect
        {
            get => thumbnailAspect;
            set
            {
                thumbnailAspect = value > 0.01f ? value : 1f;
                // The tiles change height, so the rows may shrink as well as grow.
                rowHeightChanges = 0;
                skeleton.ThumbnailAspect = thumbnailAspect;
                list.Query(className: ThumbClass).ForEach(thumb => thumb.style.height = thumb.layout.width / thumbnailAspect);
                MarkRowsChanged();
                RequestLoadMoreCheck();
            }
        }

        [UxmlAttribute("selection-mode")]
        public GridSelectionMode SelectionMode
        {
            get => selectionMode;
            set
            {
                selectionMode = value;
                if (selectionMode == GridSelectionMode.None && selection.Count > 0)
                {
                    ClearSelection();
                }
            }
        }

        /// <summary>
        /// Shows skeleton tiles in place of the items.
        /// </summary>
        [UxmlAttribute("is-loading")]
        public bool IsLoading
        {
            get => isLoading;
            set
            {
                isLoading = value;
                UpdateState();
                // The list shows or hides, so the rows settle again before paging measures them.
                MarkRowsChanged();
                RequestLoadMoreCheck();
            }
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

        [UxmlAttribute("empty-action-text")]
        public string EmptyActionText
        {
            get => empty.ActionText;
            set => empty.ActionText = value;
        }

        public ContentGrid()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            list = new ListView
            {
                itemsSource = rows,
                makeItem = MakeRow,
                bindItem = BindRow,
                selectionType = UnityEngine.UIElements.SelectionType.None,
                // Every row is the same height, so rows are placed by index. With DynamicHeight the list estimated the height of a
                // row scrolling in from the top, measured it, then corrected the scroll offset a frame later: a visible jump on every
                // scroll back towards the start. The height is measured from the rows themselves (OnRowGeometryChanged).
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                horizontalScrollingEnabled = false,
            };
            list.AddToClassList(ListClass);
            hierarchy.Add(list);
            tileQuery = list.Query(className: TileClass).Build();
            rowQuery = list.Query(className: RowClass).Build();
            applyRowHeight = ApplyRowHeight;
            onLoadMoreCheckDue = OnLoadMoreCheckDue;

            // Paging: scrolling, or the view resizing (a sheet changing detent), can bring the end of the items into reach.
            scrollView = list.Q<ScrollView>();
            scrollView.verticalScroller.valueChanged += _ => RequestLoadMoreCheck();
            scrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => RequestLoadMoreCheck());

            skeleton = new SkeletonTile(SkeletonShape.Tile, 4);
            skeleton.AddToClassList(SkeletonClass);
            hierarchy.Add(skeleton);

            empty = new EmptyState("Nothing here yet");
            empty.AddToClassList(EmptyClass);
            empty.ActionClicked += () => EmptyActionClicked?.Invoke();
            hierarchy.Add(empty);

            RegisterCallback<GeometryChangedEvent>(_ => UpdateColumns());
            RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            RegisterCallback<PointerCancelEvent>(_ => CancelPress(), TrickleDown.TrickleDown);
            RegisterCallback<PointerCaptureOutEvent>(_ => CancelPress());

            UpdateState();
        }

        /// <summary>
        /// Replaces the items; the selection keeps ids that are still present and enabled.
        /// </summary>
        public void SetItems(IReadOnlyList<ContentGridItem> newItems)
        {
            var previousCount = items.Count;
            items.Clear();
            if (newItems != null)
            {
                items.AddRange(newItems);
            }

            var ids = new HashSet<string>();
            foreach (var item in items)
            {
                if (item.IsEnabled)
                {
                    ids.Add(item.Id);
                }
            }

            var removed = selection.Count > 0 && selection.RemoveWhere(id => !ids.Contains(id)) > 0;
            UpdateMetaLine();
            RebuildRows();
            UpdateState();
            if (items.Count != previousCount)
            {
                // A new item count answers a request for more, and ends any wait after a page that added nothing.
                loadMoreOutstanding = false;
                loadMoreStalledCount = -1;
            }

            MarkRowsChanged();
            RequestLoadMoreCheck();
            if (removed)
            {
                SelectionChanged?.Invoke(SelectedIds);
            }
        }

        /// <summary>
        /// Re-binds the tile for an item, for example after its thumbnail loads or it is disabled.
        /// </summary>
        public void RefreshItem(string id)
        {
            var index = items.FindIndex(i => i.Id == id);
            if (index < 0)
            {
                return;
            }

            // A disabled item cannot stay selected.
            var deselected = !items[index].IsEnabled && selection.Remove(id);
            if (UpdateMetaLine())
            {
                // The first meta line arrived or the last one went, so every tile gains or loses the line.
                list.RefreshItems();
            }
            else if (columns > 0)
            {
                list.RefreshItem(index / columns);
            }

            if (deselected)
            {
                SelectionChanged?.Invoke(SelectedIds);
            }
        }

        public void SetSelection(IEnumerable<string> ids)
        {
            selection.Clear();
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    if (items.Exists(i => i.Id == id && i.IsEnabled))
                    {
                        selection.Add(id);
                        if (selectionMode == GridSelectionMode.Single)
                        {
                            break;
                        }
                    }
                }
            }

            list.RefreshItems();
            SelectionChanged?.Invoke(SelectedIds);
        }

        public void ClearSelection() => SetSelection(null);

        /// <summary>
        /// Turns paging on, so <see cref="LoadMoreRequested"/> is raised when more items are wanted, at once if they already
        /// fit. Calling it again starts over, for example for a new search, or to retry after a page that added nothing.
        /// </summary>
        public void EnableLoadMore()
        {
            loadMoreEnabled = true;
            loadMoreOutstanding = false;
            loadMoreStalledCount = -1;
            RequestLoadMoreCheck();
        }

        /// <summary>
        /// Turns paging off, for example when the server has no more items. Ends any <see cref="BeginLoadMore"/>.
        /// </summary>
        public void DisableLoadMore()
        {
            loadMoreEnabled = false;
            loadMoreOutstanding = false;
            loadMoreStalledCount = -1;
            loadMoreCheckPending = false;
            SetLoadingMore(false);
        }

        /// <summary>
        /// Marks a fetch as started: no request is raised until <see cref="EndLoadMore"/>, and the grid has
        /// <see cref="LoadingMoreClass"/>.
        /// </summary>
        public void BeginLoadMore() => SetLoadingMore(true);

        /// <summary>
        /// Marks a fetch as finished, after its items have been passed to <see cref="SetItems"/>. The grid asks again if the
        /// items still do not reach past the threshold. After a page that added nothing it waits until the view leaves the end
        /// of the items and comes back, so an empty page cannot start a loop.
        /// </summary>
        public void EndLoadMore()
        {
            if (loadMoreOutstanding && items.Count == loadMoreRequestCount)
            {
                loadMoreStalledCount = items.Count;
                loadMoreLeftEnd = false;
            }

            loadMoreOutstanding = false;
            SetLoadingMore(false);
            RequestLoadMoreCheck();
        }

        private void SetLoadingMore(bool loading)
        {
            loadMoreInFlight = loading;
            EnableInClassList(LoadingMoreClass, loading);
        }

        /// <summary>
        /// The tile element showing an item, or null when it is scrolled out of view.
        /// </summary>
        public VisualElement TileFor(string id)
        {
            // A padding cell has no item, so it never matches, not even a null id.
            return list.Query(className: TileClass).Where(t => t.userData is ContentGridItem item && item.Id == id).First();
        }

        /// <summary>
        /// Tiles keep the meta line while any item has one, so every tile has the same lines and the same height. True when that
        /// changed, and the tiles with it.
        /// </summary>
        private bool UpdateMetaLine()
        {
            var show = items.Exists(i => !string.IsNullOrWhiteSpace(i.Meta));
            if (show == showMetaLine)
            {
                return false;
            }

            showMetaLine = show;
            // The tiles change height, so the rows may shrink as well as grow.
            rowHeightChanges = 0;
            MarkRowsChanged();
            return true;
        }

        private void UpdateColumns()
        {
            // Rows take the visible width in pixels (see BindRow), so a new width rebinds them even when the columns stay.
            var rowWidth = RowWidth();
            if (!float.IsNaN(rowWidth) && !Mathf.Approximately(rowWidth, boundWidth))
            {
                boundWidth = rowWidth;
                // The thumbnails take a new width, so the rows measure again and may shrink as well as grow.
                measuredThumbWidth = float.NaN;
                rowHeightChanges = 0;
                list.RefreshItems();
                MarkRowsChanged();
                RequestLoadMoreCheck();
            }

            var next = fixedColumns;
            if (next == 0)
            {
                var width = contentRect.width;
                next = float.IsNaN(width) || width <= 0f ? columns : Mathf.Max(1, Mathf.FloorToInt(width / minTileWidth));
            }

            if (next == columns)
            {
                return;
            }

            columns = next;
            skeleton.Columns = columns;
            skeleton.Count = columns * 2;
            measuredThumbWidth = float.NaN;
            rowHeightChanges = 0;
            RebuildRows(rebuild: true);
            MarkRowsChanged();
            RequestLoadMoreCheck();
        }

        /// <summary>
        /// The list's visible width (less any scroller), or the grid's when the list has no layout yet; NaN when neither has.
        /// </summary>
        private float RowWidth()
        {
            var viewport = scrollView.contentViewport.layout.width;
            var width = !float.IsNaN(viewport) && viewport > 0f ? viewport : contentRect.width;
            return float.IsNaN(width) || width <= 0f ? float.NaN : width;
        }

        /// <summary>
        /// Recounts the rows. A change of items refreshes the list, which keeps its scroll offset and its recycled rows, as when a
        /// page of items is appended; a change of columns rebuilds it, since every row's tiles change.
        /// </summary>
        private void RebuildRows(bool rebuild = false)
        {
            rows.Clear();
            var rowCount = columns > 0 ? Mathf.CeilToInt(items.Count / (float)columns) : 0;
            for (var i = 0; i < rowCount; i++)
            {
                rows.Add(i);
            }

            if (rebuild)
            {
                list.Rebuild();
            }
            else
            {
                list.RefreshItems();
            }
        }

        /// <summary>
        /// A row is as tall as its tiles, and every row is one height. The tiles are measured, not the rows: the list forces each
        /// row to the fixed height, and the tiles keep their own height (ContentGrid.uss). A tile showing an item that lays out
        /// asks for the height to be checked after the layout pass; a padding cell never does.
        /// </summary>
        private void OnTileGeometryChanged(VisualElement tile)
        {
            if (tile.userData is ContentGridItem && !tile.ClassListContains(HiddenTileClass))
            {
                ScheduleRowHeightCheck();
            }
        }

        /// <summary>
        /// Checks the row height after the layout pass, once however many tiles lay out in it. Not inside a geometry event, since
        /// changing the height there would lay the list out again mid-pass.
        /// </summary>
        private void ScheduleRowHeightCheck()
        {
            if (rowHeightScheduled)
            {
                return;
            }

            rowHeightScheduled = true;
            list.schedule.Execute(applyRowHeight);
        }

        /// <summary>
        /// Sets the list's row height to that of its tallest tile now showing an item, in whole device pixels. Every tile is bound
        /// with the same lines, so the tiles agree; the tallest is taken in case one does not, such as a name in a taller fallback
        /// font. The height is read from the tiles as they are, not from whichever event came last, so it cannot flip between two
        /// tiles. Each change is checked again after the next layout pass, and the height has settled once two checks in a row
        /// find nothing to change; only then does paging measure the content. After
        /// <see cref="RowHeightChangeBudget"/> changes without settling it only grows, so no feedback, such as a scroller that
        /// appears and narrows the tiles, can become a loop.
        /// </summary>
        private void ApplyRowHeight()
        {
            rowHeightScheduled = false;
            var height = TallestTileHeight();
            if (float.IsNaN(height))
            {
                return;
            }

            // Whole device pixels, so every row starts on a pixel and its tiles measure the same in every row. A difference of a
            // pixel or less is left alone: rounding to the pixel grid alone can make one.
            var pixelsPerPoint = list.scaledPixelsPerPoint > 0f ? list.scaledPixelsPerPoint : 1f;
            var pixel = 1f / pixelsPerPoint;
            height = Mathf.Ceil((height * pixelsPerPoint) - 0.01f) * pixel;
            var change = height - list.fixedItemHeight;
            var unchanged = Mathf.Abs(change) <= pixel + 0.001f;
            if (unchanged)
            {
                // Settled: the rows fit their tiles, so a later change of content or style may shrink them again.
                rowHeightChanges = 0;
            }

            // Changed too often without settling: from here it only grows, so tiles are never cut off and no two heights can
            // take turns.
            var held = change < 0f && rowHeightChanges >= RowHeightChangeBudget;
            if (unchanged || held)
            {
                // Two such checks in a row span a layout pass, so the height has settled and paging can measure the content.
                if (++rowHeightQuietChecks < 2)
                {
                    ScheduleRowHeightCheck();
                }
                else if (loadMoreCheckPending)
                {
                    CheckLoadMore();
                }

                return;
            }

            rowHeightQuietChecks = 0;
            // The content height changes with the rows, so paging measures it again once they settle.
            loadMoreCheckPending = true;
            rowHeightChanges++;
            list.fixedItemHeight = height;
            // The setter refreshes the list, which lays out the content for the new height but leaves the rows already made at
            // the old one, so they are resized here, in place.
            foreach (var row in rowQuery)
            {
                row.style.height = height;
            }

            ScheduleRowHeightCheck();
        }

        /// <summary>
        /// Something that can change the rows' height happened, such as new items or a new width: the height has to be checked
        /// again, twice without a change, before it counts as settled.
        /// </summary>
        private void MarkRowsChanged()
        {
            rowHeightQuietChecks = 0;
            ScheduleRowHeightCheck();
        }

        /// <summary>
        /// Asks for the content to be measured for paging: on the next update if the row height has settled, otherwise once it
        /// has, so a row height still being measured cannot make the content look short and ask early or twice.
        /// </summary>
        private void RequestLoadMoreCheck()
        {
            if (!loadMoreEnabled)
            {
                return;
            }

            loadMoreCheckPending = true;
            if (rowHeightQuietChecks < 2)
            {
                ScheduleRowHeightCheck();
            }
            else if (!loadMoreCheckScheduled)
            {
                // Not inside the event that asked (a scroll, say), so a host answering at once does not change the list mid-event.
                loadMoreCheckScheduled = true;
                list.schedule.Execute(onLoadMoreCheckDue);
            }
        }

        private void OnLoadMoreCheckDue()
        {
            loadMoreCheckScheduled = false;
            if (rowHeightQuietChecks < 2)
            {
                // The rows changed since this was asked for; the settled row height check runs it instead.
                ScheduleRowHeightCheck();
                return;
            }

            CheckLoadMore();
        }

        /// <summary>
        /// Raises <see cref="LoadMoreRequested"/> when paging is on, nothing is outstanding and the end of the items is within
        /// <see cref="LoadMoreThreshold"/> of the bottom of the view.
        /// </summary>
        private void CheckLoadMore()
        {
            loadMoreCheckPending = false;
            if (!loadMoreEnabled || loadMoreInFlight || loadMoreOutstanding || isLoading || items.Count == 0)
            {
                return;
            }

            var nearEnd = IsNearEnd();
            if (loadMoreStalledCount == items.Count)
            {
                // The last request added nothing. Ask again only once the view has left the end and come back: a page that adds
                // nothing cannot start a loop, and scrolling back to the end still retries.
                if (!nearEnd)
                {
                    loadMoreLeftEnd = true;
                    return;
                }

                if (!loadMoreLeftEnd)
                {
                    return;
                }
            }

            if (!nearEnd)
            {
                return;
            }

            loadMoreOutstanding = true;
            loadMoreRequestCount = items.Count;
            loadMoreStalledCount = -1;
            LoadMoreRequested?.Invoke();
        }

        /// <summary>
        /// Whether the end of the items is within <see cref="LoadMoreThreshold"/> of the bottom of the view; true when every item
        /// fits. The content height is the rows at the list's row height, not a laid-out size, so it is current as soon as the
        /// items or the row height change.
        /// </summary>
        private bool IsNearEnd()
        {
            var viewport = scrollView.contentViewport.layout.height;
            if (float.IsNaN(viewport) || viewport <= 0f)
            {
                return false;
            }

            var remaining = (rows.Count * list.fixedItemHeight) - scrollView.scrollOffset.y - viewport;
            return remaining <= viewport * loadMoreThreshold;
        }

        /// <summary>
        /// The height of the tallest laid-out tile showing an item in a row the list is showing; NaN when there is none.
        /// </summary>
        private float TallestTileHeight()
        {
            var tallest = float.NaN;
            foreach (var tile in tileQuery)
            {
                if (tile.userData is not ContentGridItem || tile.ClassListContains(HiddenTileClass) || tile.parent?.resolvedStyle.display == DisplayStyle.None)
                {
                    continue;
                }

                var height = tile.layout.height;
                if (!float.IsNaN(height) && height > 0f && (float.IsNaN(tallest) || height > tallest))
                {
                    tallest = height;
                }
            }

            return tallest;
        }

        /// <summary>
        /// Keeps a thumbnail's shape as it takes its width, and remembers that width so later rows are sized when bound.
        /// </summary>
        private void OnThumbGeometryChanged(VisualElement thumb, float width)
        {
            if (float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            measuredThumbWidth = width;
            var height = width / thumbnailAspect;
            if (Mathf.Abs(thumb.layout.height - height) > 0.5f)
            {
                // The tile takes a new height on the next layout pass, so the rows have not settled yet.
                MarkRowsChanged();
            }

            thumb.style.height = height;
        }

        private void UpdateState()
        {
            var isEmpty = !isLoading && items.Count == 0;
            EnableInClassList(LoadingClass, isLoading);
            EnableInClassList(EmptyModifierClass, isEmpty);
            list.SetDisplayed(!isLoading && !isEmpty);
            skeleton.SetDisplayed(isLoading);
            empty.SetDisplayed(isEmpty);
        }

        private VisualElement MakeRow() => UIToolkitExtensions.CreateVisualElement(RowClass);

        private void BindRow(VisualElement row, int rowIndex)
        {
            // Rows are recycled and the column count can change, so the tile count follows it.
            while (row.childCount < columns)
            {
                row.Add(MakeTile());
            }

            while (row.childCount > columns)
            {
                row.RemoveAt(row.childCount - 1);
            }

            // The row spans the visible width even if the list's content container lags behind a resize, as it can
            // inside a sheet that changes width; the tiles then share the row by percentage.
            var rowWidth = RowWidth();
            row.style.width = float.IsNaN(rowWidth) ? new StyleLength(StyleKeyword.Null) : new StyleLength(rowWidth);

            for (var c = 0; c < columns; c++)
            {
                var tile = row[c];
                var index = (rowIndex * columns) + c;
                tile.style.width = Length.Percent(100f / columns);
                if (!float.IsNaN(measuredThumbWidth))
                {
                    // Sized now, so a fresh row is measured at its true height on its first layout.
                    tile.Q(className: ThumbClass).style.height = measuredThumbWidth / thumbnailAspect;
                }

                if (index >= items.Count)
                {
                    // A padding cell in a short last row. It is bound like a tile with blank lines, so it measures like one, then
                    // left without an item, so taps, presses, selection and TileFor pass it by.
                    BindTile(tile, PaddingItem);
                    tile.userData = null;
                    tile.AddToClassList(HiddenTileClass);
                    continue;
                }

                tile.RemoveFromClassList(HiddenTileClass);
                BindTile(tile, items[index]);
            }
        }

        private VisualElement MakeTile()
        {
            var tile = UIToolkitExtensions.CreateVisualElement(TileClass);
            tile.RegisterCallback<GeometryChangedEvent>(_ => OnTileGeometryChanged(tile));
            var thumb = UIToolkitExtensions.CreateVisualElement(tile, ThumbClass).IgnorePicking();
            thumb.RegisterCallback<GeometryChangedEvent>(evt => OnThumbGeometryChanged(thumb, evt.newRect.width));

            var badge = UIToolkitExtensions.CreateVisualElement<Label>(thumb, BadgeClass).MakePill().IgnorePicking();
            var check = UIToolkitExtensions.CreateVisualElement(thumb, CheckClass).IgnorePicking();

            var name = UIToolkitExtensions.CreateVisualElement<Label>(tile, NameClass).IgnorePicking();
            var meta = UIToolkitExtensions.CreateVisualElement<Label>(tile, MetaClass).IgnorePicking();

            tile.RegisterCallback<ClickEvent>(_ => OnTileClicked(tile));
            tile.RegisterCallback<PointerDownEvent>(evt => OnTilePointerDown(tile, evt));
            return tile;
        }

        private void BindTile(VisualElement tile, ContentGridItem item)
        {
            tile.userData = item;
            var thumb = tile.Q(className: ThumbClass);
            var hasThumb = item.Thumbnail.HasContent();
            thumb.style.backgroundImage = hasThumb ? new StyleBackground(item.Thumbnail) : new StyleBackground(StyleKeyword.Null);

            var badge = tile.Q<Label>(className: BadgeClass);
            badge.SetTextOrHide(item.TypeLabel);

            // Every tile has the same lines, blank where an item has no text, so every tile is the same height.
            SetLine(tile.Q<Label>(className: NameClass), item.Name);
            var meta = tile.Q<Label>(className: MetaClass);
            SetLine(meta, item.Meta);
            meta.SetDisplayed(showMetaLine);

            tile.EnableInClassList(SelectedClass, selection.Contains(item.Id));
            tile.EnableInClassList(DisabledTileClass, !item.IsEnabled);
            tile.RemoveFromClassList(ArmedClass);
        }

        /// <summary>
        /// Shows a line of text, or a blank line of the same height. A label with no text, or only spaces, measures no height, so
        /// a blank line holds hidden placeholder text instead.
        /// </summary>
        private static void SetLine(Label label, string text)
        {
            var blank = string.IsNullOrWhiteSpace(text);
            label.text = blank ? BlankLine : text;
            label.style.visibility = blank ? new StyleEnum<Visibility>(Visibility.Hidden) : new StyleEnum<Visibility>(StyleKeyword.Null);
        }

        private void OnTileClicked(VisualElement tile)
        {
            if (suppressNextClick)
            {
                // The click that ends a drag-out is not a tap.
                suppressNextClick = false;
                return;
            }

            if (tile.userData is not ContentGridItem item || pressDragging || !item.IsEnabled)
            {
                return;
            }

            switch (selectionMode)
            {
                case GridSelectionMode.Single:
                    if (!(selection.Count == 1 && selection.Contains(item.Id)))
                    {
                        selection.Clear();
                        selection.Add(item.Id);
                        list.RefreshItems();
                        SelectionChanged?.Invoke(SelectedIds);
                    }

                    break;
                case GridSelectionMode.Multiple:
                    if (!selection.Remove(item.Id))
                    {
                        selection.Add(item.Id);
                    }

                    tile.EnableInClassList(SelectedClass, selection.Contains(item.Id));
                    SelectionChanged?.Invoke(SelectedIds);
                    break;
            }

            ItemTapped?.Invoke(item);
        }

        #region Long-press drag-out

        private void OnTilePointerDown(VisualElement tile, PointerDownEvent evt)
        {
            // A disabled tile is never armed, so it gets no press feedback and cannot be dragged out.
            if (tile.userData is not ContentGridItem item || evt.button != 0 || !item.IsEnabled)
            {
                return;
            }

            CancelPress();
            suppressNextClick = false;
            pressItem = item;
            pressTile = tile;
            pressStart = evt.position;
            pressPointerId = evt.pointerId;
            pressTimer = schedule.Execute(ArmPress).StartingIn(LongPressMs);
        }

        internal void ArmPress()
        {
            if (pressTile == null)
            {
                return;
            }

            pressArmed = true;
            pressTile.AddToClassList(ArmedClass);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (pressTile == null || evt.pointerId != pressPointerId)
            {
                return;
            }

            if (Vector2.Distance(evt.position, pressStart) < DragSlop)
            {
                return;
            }

            if (!pressArmed)
            {
                // Moved before the hold: it is a scroll, not a drag.
                CancelPress();
                suppressNextClick = true;
                return;
            }

            if (!pressDragging)
            {
                pressDragging = true;
                var item = pressItem;
                pressTile.RemoveFromClassList(ArmedClass);
                DragStarted?.Invoke(item, evt.position);
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != pressPointerId)
            {
                return;
            }

            var dragged = pressDragging;
            CancelPress();
            suppressNextClick = dragged;
        }

        private void CancelPress()
        {
            pressTimer?.Pause();
            pressTimer = null;
            pressTile?.RemoveFromClassList(ArmedClass);
            pressTile = null;
            pressItem = null;
            pressArmed = false;
            pressDragging = false;
            pressPointerId = PointerId.invalidPointerId;
        }

        #endregion
    }
}
