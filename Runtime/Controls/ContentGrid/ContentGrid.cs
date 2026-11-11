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
    /// <para>Props: <c>columns</c> (0 for automatic), <c>min-tile-width</c>, <c>thumbnail-aspect</c>, <c>selection-mode</c>, <c>is-loading</c>,
    /// <c>empty-title</c>, <c>empty-body</c>, <c>empty-action-text</c>. Methods: <see cref="SetItems"/>,
    /// <see cref="RefreshItem"/>, <see cref="SetSelection"/>, <see cref="ClearSelection"/>.
    /// Events: <see cref="ItemTapped"/>, <see cref="SelectionChanged"/>, <see cref="DragStarted"/>,
    /// <see cref="EmptyActionClicked"/>.</para>
    /// <para>Drag-out: hold a tile for <see cref="LongPressMs"/>, then move; <see cref="DragStarted"/> carries the item
    /// and the pointer position, and the owner takes over (for example to place the item in the world).</para>
    /// <para>USS classes: <c>contentGrid</c> (root), <c>contentGrid__list</c>, <c>contentGrid__row</c>,
    /// <c>contentGrid__tile</c>, <c>contentGrid__thumb</c>, <c>contentGrid__badge</c>, <c>contentGrid__check</c>,
    /// <c>contentGrid__name</c>, <c>contentGrid__meta</c>, <c>contentGrid__skeleton</c>, <c>contentGrid__empty</c>.</para>
    /// <para>Modifier classes: <c>contentGrid--loading</c>, <c>contentGrid--empty</c>; on tiles
    /// <c>contentGrid__tile--selected</c>, <c>contentGrid__tile--armed</c> (long-press reached),
    /// <c>contentGrid__tile--hidden</c> (padding cell in the last row), <c>contentGrid__tile--disabled</c> (the item's
    /// <see cref="ContentGridItem.IsEnabled"/> is false: thumbnail, badge and text at 45% opacity).</para>
    /// <para>Tokens read: <c>--surface-raised</c>, <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-border</c>,
    /// <c>--surface-accent</c>, <c>--surface-skeleton</c>, <c>--c-scrim</c>, <c>--c-on-primary</c>, <c>--radius-card</c>,
    /// <c>--radius-pill</c>, <c>--space-1..3</c>, <c>--text-label</c>, <c>--text-caption</c>, <c>--font-body</c>,
    /// <c>--font-body-medium</c>, <c>--font-body-semibold</c>, <c>--motion-fast</c>.</para>
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
        // Whether tiles keep the meta line: true while any item has one.
        private bool showMetaLine;
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

        public IReadOnlyList<ContentGridItem> Items => items;

        /// <summary>
        /// The columns in use now.
        /// </summary>
        public int ColumnCount => columns;

        public ListView ListView => list;

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
                // Legacy: virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight.
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                horizontalScrollingEnabled = false,
            };
            list.AddToClassList(ListClass);
            hierarchy.Add(list);

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

            var removed = selection.RemoveWhere(id => !ids.Contains(id)) > 0;
            UpdateMetaLine();
            RebuildRows();
            UpdateState();
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
        }

        /// <summary>
        /// The list's visible width (less any scroller), or the grid's when the list has no layout yet; NaN when neither has.
        /// </summary>
        private float RowWidth()
        {
            var viewport = list.Q<ScrollView>()?.contentViewport.layout.width ?? float.NaN;
            var width = !float.IsNaN(viewport) && viewport > 0f ? viewport : contentRect.width;
            return float.IsNaN(width) || width <= 0f ? float.NaN : width;
        }

        /// <summary>
        /// Recounts the rows. A change of items refreshes the list, which keeps its scroll offset and its recycled rows, as when a
        /// page of items is appended; a change of columns rebuilds it, since every row's tiles change.
        /// </summary>
        /// <remarks>Legacy: every change rebuilt the list, which also forgot every measured row height.</remarks>
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
            list.schedule.Execute(ApplyRowHeight);
        }

        /// <summary>
        /// Sets the list's row height to that of its tallest tile now showing an item, in whole device pixels. Every tile is bound
        /// with the same lines, so the tiles agree; the tallest is taken in case one does not, such as a name in a taller fallback
        /// font. The height is read from the tiles as they are, not from whichever event came last, so it cannot flip between two
        /// tiles. Each change is checked again after the next layout pass: unchanged there, the height has settled. After
        /// <see cref="RowHeightChangeBudget"/> changes without settling it only grows, so no feedback, such as a scroller that
        /// appears and narrows the tiles, can become a loop.
        /// </summary>
        /// <remarks>Legacy: the last tile to report set the height and the list was rebuilt each time. A padding cell made with
        /// empty labels, or a tile with no meta, measured shorter than its neighbours, so the list rebuilt between the two heights
        /// every frame or two, and no tap landed on a tile that lived long enough to be clicked.</remarks>
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
            if (Mathf.Abs(change) <= pixel + 0.001f)
            {
                // Settled: the rows fit their tiles, so a later change of content or style may shrink them again.
                rowHeightChanges = 0;
                return;
            }

            if (change < 0f && rowHeightChanges >= RowHeightChangeBudget)
            {
                // Changed too often without settling: from here it only grows, so tiles are never cut off and no two heights
                // can take turns.
                return;
            }

            rowHeightChanges++;
            list.fixedItemHeight = height;
            // The setter refreshes the list, which lays out the content for the new height but leaves the rows already made at
            // the old one, so they are resized here, in place. Legacy: the list was rebuilt, which made every row and tile again
            // and cancelled any press on them.
            list.Query(className: RowClass).ForEach(row => row.style.height = height);
            ScheduleRowHeightCheck();
        }

        /// <summary>
        /// The height of the tallest laid-out tile showing an item in a row the list is showing; NaN when there is none.
        /// </summary>
        private float TallestTileHeight()
        {
            var tallest = float.NaN;
            list.Query(className: TileClass).ForEach(tile =>
            {
                if (tile.userData is not ContentGridItem || tile.ClassListContains(HiddenTileClass) || tile.parent?.resolvedStyle.display == DisplayStyle.None)
                {
                    return;
                }

                var height = tile.layout.height;
                if (!float.IsNaN(height) && height > 0f && (float.IsNaN(tallest) || height > tallest))
                {
                    tallest = height;
                }
            });

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
            thumb.style.height = width / thumbnailAspect;
        }

        private void UpdateState()
        {
            var isEmpty = !isLoading && items.Count == 0;
            EnableInClassList(LoadingClass, isLoading);
            EnableInClassList(EmptyModifierClass, isEmpty);
            list.style.display = !isLoading && !isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            skeleton.style.display = isLoading ? DisplayStyle.Flex : DisplayStyle.None;
            empty.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement MakeRow()
        {
            var row = UIToolkitExtensions.CreateVisualElement(RowClass);
            return row;
        }

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
                    // left without an item, so taps, presses, selection and TileFor pass it by. Legacy: it kept whatever text a
                    // recycled tile had, or none when freshly made, and then measured two lines short.
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
            var thumb = UIToolkitExtensions.CreateVisualElement(tile, ThumbClass);
            thumb.pickingMode = PickingMode.Ignore;
            thumb.RegisterCallback<GeometryChangedEvent>(evt => OnThumbGeometryChanged(thumb, evt.newRect.width));

            var badge = UIToolkitExtensions.CreateVisualElement<Label>(thumb, BadgeClass).MakePill();
            badge.pickingMode = PickingMode.Ignore;
            var check = UIToolkitExtensions.CreateVisualElement(thumb, CheckClass);
            check.pickingMode = PickingMode.Ignore;

            var name = UIToolkitExtensions.CreateVisualElement<Label>(tile, NameClass);
            name.pickingMode = PickingMode.Ignore;
            var meta = UIToolkitExtensions.CreateVisualElement<Label>(tile, MetaClass);
            meta.pickingMode = PickingMode.Ignore;

            tile.RegisterCallback<ClickEvent>(evt => OnTileClicked(tile));
            tile.RegisterCallback<PointerDownEvent>(evt => OnTilePointerDown(tile, evt));
            return tile;
        }

        private void BindTile(VisualElement tile, ContentGridItem item)
        {
            tile.userData = item;
            var thumb = tile.Q(className: ThumbClass);
            var hasThumb = item.Thumbnail.texture != null || item.Thumbnail.sprite != null || item.Thumbnail.vectorImage != null || item.Thumbnail.renderTexture != null;
            thumb.style.backgroundImage = hasThumb ? new StyleBackground(item.Thumbnail) : new StyleBackground(StyleKeyword.Null);

            var badge = tile.Q<Label>(className: BadgeClass);
            badge.text = item.TypeLabel ?? string.Empty;
            badge.style.display = string.IsNullOrEmpty(item.TypeLabel) ? DisplayStyle.None : DisplayStyle.Flex;

            // Every tile has the same lines, blank where an item has no text, so every tile is the same height.
            // Legacy: an empty name measured no height and an empty meta was hidden, so such a tile measured shorter.
            SetLine(tile.Q<Label>(className: NameClass), item.Name);
            var meta = tile.Q<Label>(className: MetaClass);
            SetLine(meta, item.Meta);
            meta.style.display = showMetaLine ? DisplayStyle.Flex : DisplayStyle.None;

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
