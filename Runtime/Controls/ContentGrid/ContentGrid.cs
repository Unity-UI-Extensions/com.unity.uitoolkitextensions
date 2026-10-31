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
        /// A secondary line, such as size or distance.
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

        private readonly ListView list;
        private readonly SkeletonTile skeleton;
        private readonly EmptyState empty;
        private readonly List<ContentGridItem> items = new();
        private readonly List<int> rows = new();
        private readonly HashSet<string> selection = new();

        private int fixedColumns;
        private float boundWidth = float.NaN;
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
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
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
            if (columns > 0)
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
            return list.Query(className: TileClass).Where(t => (t.userData as ContentGridItem)?.Id == id).First();
        }

        private void UpdateColumns()
        {
            // Rows take the visible width in pixels (see BindRow), so a new width rebinds them even when the columns stay.
            var rowWidth = RowWidth();
            if (!float.IsNaN(rowWidth) && !Mathf.Approximately(rowWidth, boundWidth))
            {
                boundWidth = rowWidth;
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
            RebuildRows();
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

        private void RebuildRows()
        {
            rows.Clear();
            var rowCount = columns > 0 ? Mathf.CeilToInt(items.Count / (float)columns) : 0;
            for (var i = 0; i < rowCount; i++)
            {
                rows.Add(i);
            }

            list.Rebuild();
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
                if (index >= items.Count)
                {
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
            var thumb = UIToolkitExtensions.CreateVisualElement(tile, ThumbClass);
            thumb.pickingMode = PickingMode.Ignore;
            thumb.RegisterCallback<GeometryChangedEvent>(evt => thumb.style.height = evt.newRect.width / thumbnailAspect);

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

            tile.Q<Label>(className: NameClass).text = item.Name ?? string.Empty;
            var meta = tile.Q<Label>(className: MetaClass);
            meta.text = item.Meta ?? string.Empty;
            meta.style.display = string.IsNullOrEmpty(item.Meta) ? DisplayStyle.None : DisplayStyle.Flex;

            tile.EnableInClassList(SelectedClass, selection.Contains(item.Id));
            tile.EnableInClassList(DisabledTileClass, !item.IsEnabled);
            tile.RemoveFromClassList(ArmedClass);
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
