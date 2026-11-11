/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum ScrollingTabsVariant
    {
        /// <summary>
        /// Text tabs with a sliding underline.
        /// </summary>
        Tabs,

        /// <summary>
        /// Pill chips; the selected chip is filled.
        /// </summary>
        Chips,
    }

    /// <summary>
    /// One tab or chip. Labels are display text; localise before passing them.
    /// </summary>
    public sealed class TabItem
    {
        public string Id;
        public string Label;

        /// <summary>
        /// Optional count shown after the label; negative hides it.
        /// </summary>
        public int Count = -1;

        public TabItem() { }

        public TabItem(string id, string label, int count = -1)
        {
            Id = id;
            Label = label;
            Count = count;
        }
    }

    /// <summary>
    /// A horizontally scrolling row of tabs or chips for dynamic categories, with an animated
    /// indicator, fades at the edges that have more content, and an optional leading search tab.
    /// </summary>
    /// <remarks>
    /// <para>The <c>labels</c> UXML shorthand makes each label its own id.</para>
    /// <para>Left and Right (or gamepad navigation) move the selection while the row has focus.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ScrollingTabs : VisualElement
    {
        public const string RootClass = "scrollingTabs";
        public const string ScrollClass = "scrollingTabs__scroll";
        public const string RowClass = "scrollingTabs__row";
        public const string TabClass = "scrollingTabs__tab";
        public const string LabelClass = "scrollingTabs__label";
        public const string CountClass = "scrollingTabs__count";
        public const string SearchClass = "scrollingTabs__search";
        public const string IndicatorClass = "scrollingTabs__indicator";
        public const string FadeClass = "scrollingTabs__fade";
        public const string FadeStartClass = "scrollingTabs__fade--start";
        public const string FadeEndClass = "scrollingTabs__fade--end";
        public const string FadeVisibleClass = "scrollingTabs__fade--visible";
        public const string ActiveClass = "scrollingTabs__tab--active";
        public const string TabsVariantClass = "scrollingTabs--tabs";
        public const string ChipsVariantClass = "scrollingTabs--chips";

        private static readonly CustomStyleProperty<Color> FadeColorProperty = new("--scrollingTabs-fade-color");

        private readonly ScrollView scroll;
        private readonly VisualElement row;
        private readonly VisualElement indicator;
        private readonly VisualElement search;
        private readonly VisualElement fadeStart;
        private readonly VisualElement fadeEnd;
        private readonly List<TabItem> items = new();
        private readonly List<VisualElement> tabs = new();

        private ScrollingTabsVariant variant = ScrollingTabsVariant.Tabs;
        private int selectedIndex = -1;
        private VisualElement pendingScrollTab;
        private Color fadeColor = Color.clear;
        private Texture2D fadeStartTexture;
        private Texture2D fadeEndTexture;

        /// <summary>
        /// Raised when the user selects a tab: its id and index. Not raised by <see cref="SelectedId"/>.
        /// </summary>
        public event Action<string, int> SelectionChanged;

        public event Action SearchRequested;

        public IReadOnlyList<TabItem> Items => items;

        public int SelectedIndex => selectedIndex;

        public ScrollView ScrollView => scroll;

        [UxmlAttribute("labels")]
        public List<string> Labels
        {
            get => items.ConvertAll(i => i.Label);
            set
            {
                SetItems(value?.ConvertAll(l => new TabItem(l, l)) ?? new List<TabItem>(), SelectedId);
            }
        }

        /// <summary>
        /// The selected tab's id, or null. Setting it does not raise <see cref="SelectionChanged"/>.
        /// </summary>
        [UxmlAttribute("selected-id")]
        public string SelectedId
        {
            get => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex].Id : null;
            set => SetSelected(items.FindIndex(i => i.Id == value), notify: false);
        }

        [UxmlAttribute("variant")]
        public ScrollingTabsVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                EnableInClassList(TabsVariantClass, variant == ScrollingTabsVariant.Tabs);
                EnableInClassList(ChipsVariantClass, variant == ScrollingTabsVariant.Chips);
                foreach (var tab in tabs)
                {
                    ApplyTabShape(tab);
                }

                if (search != null)
                {
                    ApplyTabShape(search);
                }

                schedule.Execute(() => UpdateIndicator(animate: false));
            }
        }

        [UxmlAttribute("show-search")]
        public bool ShowSearch
        {
            get => search.style.display != DisplayStyle.None;
            set => search.SetDisplayed(value);
        }

        public ScrollingTabs()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            focusable = true;
            tabIndex = 0;

            scroll = new ScrollView(ScrollViewMode.Horizontal)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
                touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped,
            };
            scroll.AddToClassList(ScrollClass);
            hierarchy.Add(scroll);

            row = UIToolkitExtensions.CreateVisualElement(RowClass);
            scroll.Add(row);

            search = UIToolkitExtensions.CreateVisualElement(row, TabClass, SearchClass);
            ApplyTabShape(search);
            search.RegisterCallback<ClickEvent>(_ => SearchRequested?.Invoke());
            search.style.display = DisplayStyle.None;

            indicator = UIToolkitExtensions.CreateVisualElement(IndicatorClass).IgnorePicking();
            row.Add(indicator);

            fadeStart = UIToolkitExtensions.CreateVisualElement(FadeClass, FadeStartClass).IgnorePicking();
            hierarchy.Add(fadeStart);
            fadeEnd = UIToolkitExtensions.CreateVisualElement(FadeClass, FadeEndClass).IgnorePicking();
            hierarchy.Add(fadeEnd);

            scroll.horizontalScroller.valueChanged += _ => UpdateFades();
            RegisterCallback<GeometryChangedEvent>(_ => { UpdateIndicator(animate: false); UpdateFades(); ScrollPendingIntoView(); });
            row.RegisterCallback<GeometryChangedEvent>(_ => { UpdateIndicator(animate: false); UpdateFades(); ScrollPendingIntoView(); });
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            RegisterCallback<DetachFromPanelEvent>(_ => ReleaseFadeTextures());

            Variant = ScrollingTabsVariant.Tabs;
        }

        /// <summary>
        /// Replaces the tabs. The selection is kept when its id is still present, else the first tab is selected.
        /// </summary>
        public void SetItems(IReadOnlyList<TabItem> newItems, string selectedId = null)
        {
            var keep = selectedId ?? SelectedId;

            foreach (var tab in tabs)
            {
                tab.RemoveFromHierarchy();
            }

            items.Clear();
            tabs.Clear();
            selectedIndex = -1;

            if (newItems != null)
            {
                for (var i = 0; i < newItems.Count; i++)
                {
                    var index = i;
                    var item = newItems[i];
                    items.Add(item);

                    var tab = UIToolkitExtensions.CreateVisualElement(TabClass);
                    ApplyTabShape(tab);
                    tab.RegisterCallback<ClickEvent>(_ => SetSelected(index, notify: true));

                    var label = UIToolkitExtensions.CreateVisualElement<Label>(tab, LabelClass);
                    label.text = item.Label ?? string.Empty;
                    label.pickingMode = PickingMode.Ignore;

                    var count = UIToolkitExtensions.CreateVisualElement<Label>(tab, CountClass);
                    count.text = item.Count >= 0 ? item.Count.ToString() : string.Empty;
                    count.SetDisplayed(item.Count >= 0);
                    count.pickingMode = PickingMode.Ignore;

                    row.Insert(row.IndexOf(indicator), tab);
                    tabs.Add(tab);
                }
            }

            var target = items.FindIndex(i => i.Id == keep);
            SetSelected(target >= 0 ? target : (items.Count > 0 ? 0 : -1), notify: false);
        }

        /// <summary>
        /// Selects a tab by id, raising <see cref="SelectionChanged"/> when it changes.
        /// </summary>
        public bool Select(string id)
        {
            var index = items.FindIndex(i => i.Id == id);
            if (index < 0)
            {
                return false;
            }

            SetSelected(index, notify: true);
            return true;
        }

        public VisualElement TabElement(int index) => tabs[index];

        // Chips are pills. Text tabs keep the stylesheet's corners.
        private void ApplyTabShape(VisualElement tab)
        {
            if (variant == ScrollingTabsVariant.Chips)
            {
                tab.MakePill();
            }
            else
            {
                tab.ClearPill();
            }
        }

        private void SetSelected(int index, bool notify)
        {
            if (index < -1 || index >= items.Count || index == selectedIndex)
            {
                return;
            }

            selectedIndex = index;
            for (var i = 0; i < tabs.Count; i++)
            {
                tabs[i].EnableInClassList(ActiveClass, i == selectedIndex);
            }

            UpdateIndicator(animate: true);
            if (selectedIndex >= 0)
            {
                ScrollIntoView(tabs[selectedIndex]);
            }

            if (notify && selectedIndex >= 0)
            {
                SelectionChanged?.Invoke(items[selectedIndex].Id, selectedIndex);
            }
        }

        private void ScrollIntoView(VisualElement tab)
        {
            var rect = tab.layout;
            var view = scroll.contentViewport.layout.width;
            if (float.IsNaN(rect.width) || float.IsNaN(view) || view <= 0f)
            {
                // Not laid out yet, or hidden: finish on the next layout rather than polling every frame.
                pendingScrollTab = tab;
                return;
            }

            pendingScrollTab = null;

            var offset = scroll.scrollOffset.x;
            if (rect.xMin < offset)
            {
                scroll.scrollOffset = new Vector2(rect.xMin, 0f);
            }
            else if (rect.xMax > offset + view)
            {
                scroll.scrollOffset = new Vector2(rect.xMax - view, 0f);
            }
        }

        // True while a selected tab waits for a layout to scroll into view.
        internal bool HasPendingScroll => pendingScrollTab != null;

        private void ScrollPendingIntoView()
        {
            if (pendingScrollTab == null)
            {
                return;
            }

            if (pendingScrollTab.parent == null)
            {
                pendingScrollTab = null;
                return;
            }

            ScrollIntoView(pendingScrollTab);
        }

        private void UpdateIndicator(bool animate)
        {
            var show = variant == ScrollingTabsVariant.Tabs && selectedIndex >= 0 && selectedIndex < tabs.Count;
            indicator.SetDisplayed(show);
            if (!show)
            {
                return;
            }

            var rect = tabs[selectedIndex].layout;
            if (float.IsNaN(rect.width))
            {
                return;
            }

            // Without animation the inline change must not start the USS transition.
            indicator.style.transitionDuration = animate ? StyleKeyword.Null : new List<TimeValue> { new(0f) };
            indicator.style.left = rect.xMin;
            indicator.style.width = rect.width;
        }

        private void UpdateFades()
        {
            var max = scroll.horizontalScroller.highValue;
            var value = scroll.horizontalScroller.value;
            var scrollable = !float.IsNaN(max) && max > 0.5f;
            fadeStart.EnableInClassList(FadeVisibleClass, scrollable && value > 0.5f);
            fadeEnd.EnableInClassList(FadeVisibleClass, scrollable && value < max - 0.5f);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (!evt.customStyle.TryGetValue(FadeColorProperty, out var color) || color == fadeColor)
            {
                return;
            }

            fadeColor = color;
            ReleaseFadeTextures();
            var clear = new Color(color.r, color.g, color.b, 0f);
            fadeStartTexture = ProceduralTextureUtility.CreateHorizontalGradient(color, clear, 64);
            fadeEndTexture = ProceduralTextureUtility.CreateHorizontalGradient(clear, color, 64);
            fadeStartTexture.hideFlags = HideFlags.HideAndDontSave;
            fadeEndTexture.hideFlags = HideFlags.HideAndDontSave;
            fadeStart.style.backgroundImage = new StyleBackground(fadeStartTexture);
            fadeEnd.style.backgroundImage = new StyleBackground(fadeEndTexture);
        }

        private void ReleaseFadeTextures()
        {
            fadeColor = Color.clear;
            ProceduralTextureUtility.Release(ref fadeStartTexture);
            ProceduralTextureUtility.Release(ref fadeEndTexture);
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            var step = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => -1,
                NavigationMoveEvent.Direction.Right => 1,
                _ => 0,
            };

            if (step == 0 || items.Count == 0)
            {
                return;
            }

            var next = Mathf.Clamp(selectedIndex + step, 0, items.Count - 1);
            if (next != selectedIndex)
            {
                SetSelected(next, notify: true);
                evt.StopPropagation();
                focusController?.IgnoreEvent(evt);
            }
        }
    }
}
