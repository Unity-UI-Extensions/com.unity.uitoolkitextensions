/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum DrawerAnchor
    {
        Left,
        Right,
    }

    public enum DrawerVariant
    {
        /// <summary>
        /// Slides over the content with a scrim; opens by edge swipe or code, closes by scrim, swipe or back.
        /// </summary>
        Temporary,

        /// <summary>
        /// Always shown, without a scrim, for wide layouts.
        /// </summary>
        Permanent,
    }

    /// <summary>
    /// A side drawer anchored left or right: a scrim, an edge swipe to open, a drag with velocity to close,
    /// titled sections, and a permanent variant for wide layouts. Replaces BaseSidePanel's panel.
    /// </summary>
    /// <remarks>
    /// <para>The drawer fills its parent as an overlay layer; only the panel, the visible scrim and the edge strip
    /// take input. Children added to the drawer go into the panel's scrolling content; <see cref="AddSection"/>
    /// adds a titled group.</para>
    /// <para>Props: <c>anchor</c>, <c>variant</c>, <c>is-open</c>, <c>swipe-area-width</c>, <c>drawer-width</c>,
    /// <c>top-inset</c>, <c>bottom-inset</c>. Methods: <see cref="Open"/>, <see cref="Close"/>, <see cref="Toggle"/>,
    /// <see cref="HandleBack"/>. Events: <see cref="Opened"/>, <see cref="Closed"/>.</para>
    /// <para>USS classes: <c>sideDrawer</c> (root), <c>sideDrawer__scrim</c>, <c>sideDrawer__edge</c>,
    /// <c>sideDrawer__panel</c>, <c>sideDrawer__content</c>, <c>sideDrawer__section</c>,
    /// <c>sideDrawer__sectionTitle</c>.</para>
    /// <para>Modifier classes: <c>sideDrawer--open</c>, <c>sideDrawer--left</c>, <c>sideDrawer--right</c>,
    /// <c>sideDrawer--permanent</c>, <c>sideDrawer--dragging</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-border</c>, <c>--surface-muted</c>, <c>--c-scrim</c>,
    /// <c>--radius-sheet</c>, <c>--space-2..4</c>, <c>--text-caption</c>, <c>--font-body-semibold</c>,
    /// <c>--motion-base</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class SideDrawer : VisualElement
    {
        public const string RootClass = "sideDrawer";
        public const string ScrimClass = "sideDrawer__scrim";
        public const string EdgeClass = "sideDrawer__edge";
        public const string PanelClass = "sideDrawer__panel";
        public const string ContentClass = "sideDrawer__content";
        public const string SectionClass = "sideDrawer__section";
        public const string SectionTitleClass = "sideDrawer__sectionTitle";
        public const string OpenClass = "sideDrawer--open";
        public const string LeftClass = "sideDrawer--left";
        public const string RightClass = "sideDrawer--right";
        public const string PermanentClass = "sideDrawer--permanent";
        public const string DraggingClass = "sideDrawer--dragging";

        /// <summary>
        /// Release speed, in pixels per second, above which a drag opens or closes in its direction.
        /// </summary>
        public const float FlingVelocity = 700f;

        private const float DragSlop = 8f;
        private const float ReleaseHoldSeconds = 0.1f;

        private readonly VisualElement scrim;
        private readonly VisualElement edge;
        private readonly VisualElement drawerPanel;
        private readonly ScrollView content;

        private DrawerAnchor anchor = DrawerAnchor.Left;
        private DrawerVariant variant = DrawerVariant.Temporary;
        private bool isOpen;
        private float drawerWidth = 320f;

        private int dragPointerId = PointerId.invalidPointerId;
        private bool isDragging;
        private float dragStartX;
        private float dragStartY;
        private float dragStartOpenness;
        private float openness;
        private float lastMoveX;
        private float lastMoveTime;
        private float velocity;

        public event Action Opened;
        public event Action Closed;

        public VisualElement Panel => drawerPanel;

        public override VisualElement contentContainer => content.contentContainer;

        [UxmlAttribute("anchor")]
        public DrawerAnchor Anchor
        {
            get => anchor;
            set
            {
                anchor = value;
                EnableInClassList(LeftClass, anchor == DrawerAnchor.Left);
                EnableInClassList(RightClass, anchor == DrawerAnchor.Right);
                ApplyLayout(animate: false);
            }
        }

        [UxmlAttribute("variant")]
        public DrawerVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                EnableInClassList(PermanentClass, variant == DrawerVariant.Permanent);
                CancelDrag();
                ApplyLayout(animate: false);
            }
        }

        /// <summary>
        /// Whether the drawer is open. Setting it moves without animation and without events.
        /// </summary>
        [UxmlAttribute("is-open")]
        public bool IsOpen
        {
            get => variant == DrawerVariant.Permanent || isOpen;
            set => SetOpen(value, animate: false, notify: false);
        }

        /// <summary>
        /// Width of the strip at the anchored edge that starts an opening swipe; 0 turns edge swipe off.
        /// </summary>
        [UxmlAttribute("swipe-area-width")]
        public float SwipeAreaWidth
        {
            get => edge.resolvedStyle.width;
            set => edge.style.width = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Panel width, capped at 85% of the parent.
        /// </summary>
        [UxmlAttribute("drawer-width")]
        public float DrawerWidth
        {
            get => drawerWidth;
            set
            {
                drawerWidth = Mathf.Max(120f, value);
                ApplyLayout(animate: false);
            }
        }

        [UxmlAttribute("top-inset")]
        public float TopInset
        {
            get => drawerPanel.resolvedStyle.paddingTop;
            set => drawerPanel.style.paddingTop = Mathf.Max(0f, value);
        }

        [UxmlAttribute("bottom-inset")]
        public float BottomInset
        {
            get => drawerPanel.resolvedStyle.paddingBottom;
            set => drawerPanel.style.paddingBottom = Mathf.Max(0f, value);
        }

        public SideDrawer()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            scrim = UIToolkitExtensions.CreateVisualElement(ScrimClass);
            scrim.RegisterCallback<ClickEvent>(OnScrimClicked);
            hierarchy.Add(scrim);

            edge = UIToolkitExtensions.CreateVisualElement(EdgeClass);
            edge.RegisterCallback<PointerDownEvent>(OnPointerDown);
            hierarchy.Add(edge);

            drawerPanel = UIToolkitExtensions.CreateVisualElement(PanelClass);
            drawerPanel.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            hierarchy.Add(drawerPanel);

            content = new ScrollView(ScrollViewMode.Vertical);
            content.AddToClassList(ContentClass);
            drawerPanel.Add(content);

            RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            RegisterCallback<PointerCancelEvent>(_ => CancelDrag());
            RegisterCallback<GeometryChangedEvent>(_ => ApplyLayout(animate: false));
            RegisterCallback<NavigationCancelEvent>(evt =>
            {
                if (HandleBack())
                {
                    evt.StopPropagation();
                }
            });

            Anchor = DrawerAnchor.Left;
            SwipeAreaWidth = 20f;
            ApplyLayout(animate: false);
        }

        /// <summary>
        /// Adds a titled group to the drawer's content and returns its container.
        /// </summary>
        public VisualElement AddSection(string title)
        {
            var section = UIToolkitExtensions.CreateVisualElement(SectionClass);
            if (!string.IsNullOrEmpty(title))
            {
                var label = UIToolkitExtensions.CreateVisualElement<Label>(section, SectionTitleClass);
                label.text = title;
            }

            Add(section);
            return section;
        }

        public void Open() => SetOpen(true, animate: true, notify: true);

        public void Close() => SetOpen(false, animate: true, notify: true);

        public void Toggle() => SetOpen(!isOpen, animate: true, notify: true);

        /// <summary>
        /// The back-button rule: closes an open temporary drawer.
        /// </summary>
        /// <returns>False when there was nothing to close, so the caller can handle back itself.</returns>
        public bool HandleBack()
        {
            if (variant == DrawerVariant.Permanent || !isOpen)
            {
                return false;
            }

            Close();
            return true;
        }

        private void SetOpen(bool open, bool animate, bool notify)
        {
            CancelDrag();
            var changed = open != isOpen;
            isOpen = open;
            ApplyLayout(animate);

            if (changed && notify && variant == DrawerVariant.Temporary)
            {
                if (isOpen)
                {
                    Opened?.Invoke();
                }
                else
                {
                    Closed?.Invoke();
                }
            }
        }

        private float PanelWidth()
        {
            var host = layout.width;
            return float.IsNaN(host) || host <= 0f ? drawerWidth : Mathf.Min(drawerWidth, host * 0.85f);
        }

        private void ApplyLayout(bool animate)
        {
            var permanent = variant == DrawerVariant.Permanent;
            var open = permanent || isOpen;
            EnableInClassList(OpenClass, open);

            drawerPanel.style.width = PanelWidth();
            scrim.pickingMode = !permanent && isOpen ? PickingMode.Position : PickingMode.Ignore;
            edge.pickingMode = !permanent && !isOpen ? PickingMode.Position : PickingMode.Ignore;

            if (isDragging)
            {
                return;
            }

            EnableInClassList(DraggingClass, !animate);
            SetOpenness(open ? 1f : 0f);
            if (!animate)
            {
                schedule.Execute(() => EnableInClassList(DraggingClass, isDragging));
            }
        }

        /// <summary>
        /// Places the panel between closed (0) and open (1).
        /// </summary>
        private void SetOpenness(float value)
        {
            openness = Mathf.Clamp01(value);
            var hidden = PanelWidth() * (1f - openness);
            drawerPanel.style.translate = new Translate(anchor == DrawerAnchor.Left ? -hidden : hidden, 0f);
            scrim.style.opacity = variant == DrawerVariant.Permanent ? 0f : openness;
        }

        private void OnScrimClicked(ClickEvent evt)
        {
            if (variant == DrawerVariant.Temporary && isOpen)
            {
                evt.StopPropagation();
                Close();
            }
        }

        #region Drag

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (variant == DrawerVariant.Permanent || evt.button != 0)
            {
                return;
            }

            DragStart(evt.pointerId, evt.position, evt.timestamp / 1000f);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != dragPointerId)
            {
                return;
            }

            DragMove(evt.position, evt.timestamp / 1000f);
            if (isDragging)
            {
                evt.StopPropagation();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != dragPointerId)
            {
                return;
            }

            var wasDragging = isDragging;
            DragEnd(evt.timestamp / 1000f);
            if (wasDragging)
            {
                evt.StopPropagation();
            }
        }

        internal void DragStart(int pointerId, Vector2 pointer, float time)
        {
            var pointerX = pointer.x;
            dragPointerId = pointerId;
            dragStartX = pointerX;
            dragStartY = pointer.y;
            dragStartOpenness = openness;
            lastMoveX = pointerX;
            lastMoveTime = time;
            velocity = 0f;
            isDragging = false;
        }

        internal void DragMove(Vector2 pointer, float time)
        {
            var pointerX = pointer.x;
            var delta = pointerX - dragStartX;
            if (!isDragging)
            {
                var vertical = Mathf.Abs(pointer.y - dragStartY);
                if (Mathf.Abs(delta) < DragSlop && vertical < DragSlop)
                {
                    return;
                }

                if (vertical > Mathf.Abs(delta))
                {
                    // Mostly vertical: a scroll of the drawer's content, not a drag of the drawer.
                    dragPointerId = PointerId.invalidPointerId;
                    return;
                }

                isDragging = true;
                AddToClassList(DraggingClass);
                if (dragPointerId != PointerId.invalidPointerId && !this.HasPointerCapture(dragPointerId))
                {
                    this.CapturePointer(dragPointerId);
                }
            }

            var dt = time - lastMoveTime;
            if (dt > 0.0001f)
            {
                velocity = Mathf.Lerp(velocity, (pointerX - lastMoveX) / dt, 0.6f);
            }

            lastMoveX = pointerX;
            lastMoveTime = time;

            // Toward the content opens: rightwards for a left drawer, leftwards for a right one.
            var direction = anchor == DrawerAnchor.Left ? 1f : -1f;
            SetOpenness(dragStartOpenness + (delta * direction / PanelWidth()));
        }

        internal void DragEnd(float time)
        {
            if (dragPointerId != PointerId.invalidPointerId && this.HasPointerCapture(dragPointerId))
            {
                this.ReleasePointer(dragPointerId);
            }

            dragPointerId = PointerId.invalidPointerId;
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            RemoveFromClassList(DraggingClass);

            if (time - lastMoveTime > ReleaseHoldSeconds)
            {
                velocity = 0f;
            }

            var opening = velocity * (anchor == DrawerAnchor.Left ? 1f : -1f);
            var open = Mathf.Abs(opening) >= FlingVelocity ? opening > 0f : openness >= 0.5f;
            SetOpen(open, animate: true, notify: true);
        }

        private void CancelDrag()
        {
            if (dragPointerId != PointerId.invalidPointerId && this.HasPointerCapture(dragPointerId))
            {
                this.ReleasePointer(dragPointerId);
            }

            dragPointerId = PointerId.invalidPointerId;
            if (isDragging)
            {
                isDragging = false;
                RemoveFromClassList(DraggingClass);
            }
        }

        #endregion
    }
}
