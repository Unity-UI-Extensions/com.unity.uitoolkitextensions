/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A resting height of a <see cref="BottomSheet"/>, from closed to tallest.
    /// </summary>
    public enum SheetDetent
    {
        Hidden,
        Peek,
        Half,
        Full,
    }

    /// <summary>
    /// A bottom sheet with detents (peek, half, full), a grabber, drag with velocity, an optional
    /// scrim and a docked variant that becomes a right-hand column on wide layouts.
    /// </summary>
    /// <remarks>
    /// <para>The sheet fills its parent as an overlay layer; only the panel and, when modal, the scrim
    /// take input. Children added to the sheet go into its content area; <see cref="Header"/> holds
    /// optional header content below the grabber. <see cref="SetFooter"/> pins a footer to the visible bottom
    /// edge at half and full height (it hides at peek), above <see cref="BottomInset"/>.</para>
    /// <para>The content area always ends at the visible bottom edge (or the footer), so a ScrollView in it
    /// scrolls through everything at every detent.</para>
    /// <para>Panel movement is a USS transition on <c>translate</c>, so <c>reduce-motion</c> makes it instant.</para>
    /// </remarks>
    [UxmlElement]
    public partial class BottomSheet : VisualElement
    {
        public const string RootClass = "bottomSheet";
        public const string ScrimClass = "bottomSheet__scrim";
        public const string PanelClass = "bottomSheet__panel";
        public const string HandleClass = "bottomSheet__handle";
        public const string GrabberClass = "bottomSheet__grabber";
        public const string HeaderClass = "bottomSheet__header";
        public const string ContentClass = "bottomSheet__content";
        public const string FooterClass = "bottomSheet__footer";
        public const string HasFooterClass = "bottomSheet--hasFooter";
        public const string HiddenClass = "bottomSheet--hidden";
        public const string PeekClass = "bottomSheet--peek";
        public const string HalfClass = "bottomSheet--half";
        public const string FullClass = "bottomSheet--full";
        public const string DockedClass = "bottomSheet--docked";
        public const string ModalClass = "bottomSheet--modal";
        public const string ScrimVisibleClass = "bottomSheet--scrimVisible";
        public const string DraggingClass = "bottomSheet--dragging";

        /// <summary>
        /// Release speed, in pixels per second, above which a drag moves one detent in its direction.
        /// </summary>
        public const float FlingVelocity = 900f;

        private const float DragSlop = 6f;
        private const float ReleaseHoldSeconds = 0.1f;

        private readonly VisualElement scrim;
        private readonly VisualElement sheetPanel;
        private readonly VisualElement handle;
        private readonly VisualElement header;
        private readonly VisualElement content;
        private readonly VisualElement footer;
        private bool hasFooter;

        private readonly List<SheetDetent> detents = new() { SheetDetent.Peek, SheetDetent.Half, SheetDetent.Full };
        private SheetDetent current = SheetDetent.Hidden;
        private bool modal;
        private bool docked;
        private bool dismissible = true;
        private float peekHeight = 120f;
        private float halfRatio = 0.5f;
        private float fullTopOffset = 48f;
        private float bottomInset;

        private int dragPointerId = PointerId.invalidPointerId;
        private readonly Action restoreDragging;
        private bool isDragging;
        private float dragStartPointerY;
        private float dragStartOffset;
        private float dragOffset;
        private float lastMoveY;
        private float lastMoveTime;
        private float velocity;

        /// <summary>
        /// Raised when the sheet settles on a new detent, from code or from a drag.
        /// </summary>
        public event Action<SheetDetent> DetentChanged;

        /// <summary>
        /// Raised when the visible scrim is tapped, before a dismissible sheet closes.
        /// </summary>
        public event Action ScrimClicked;

        /// <summary>
        /// Header content shown under the grabber, above the content area.
        /// </summary>
        public VisualElement Header => header;

        /// <summary>
        /// The sliding panel; exposed for layout measurement and tests.
        /// </summary>
        public VisualElement Panel => sheetPanel;

        /// <summary>
        /// The footer slot; prefer <see cref="SetFooter"/>, which shows it and moves the bottom inset onto it.
        /// </summary>
        public VisualElement Footer => footer;

        public override VisualElement contentContainer => content;

        /// <summary>
        /// The detents a drag can settle on, in any order. <see cref="SheetDetent.Hidden"/> is implied when <see cref="Dismissible"/>.
        /// </summary>
        [UxmlAttribute("detents")]
        public List<SheetDetent> Detents
        {
            get => new(detents);
            set
            {
                detents.Clear();
                if (value != null)
                {
                    foreach (var detent in value)
                    {
                        if (detent != SheetDetent.Hidden && !detents.Contains(detent))
                        {
                            detents.Add(detent);
                        }
                    }
                }

                if (detents.Count == 0)
                {
                    detents.Add(SheetDetent.Full);
                }

                detents.Sort();
            }
        }

        /// <summary>
        /// The current detent. Setting it moves the sheet without animation and without raising <see cref="DetentChanged"/>.
        /// </summary>
        [UxmlAttribute("current")]
        public SheetDetent Current
        {
            get => current;
            set => SnapTo(value, animate: false, notify: false);
        }

        /// <summary>
        /// Whether a scrim covers the content behind the sheet at half and full height.
        /// </summary>
        [UxmlAttribute("modal")]
        public bool Modal
        {
            get => modal;
            set
            {
                modal = value;
                EnableInClassList(ModalClass, modal);
                UpdateScrim();
            }
        }

        /// <summary>
        /// Whether dragging below the lowest detent, or tapping the scrim, closes the sheet.
        /// </summary>
        [UxmlAttribute("dismissible")]
        public bool Dismissible
        {
            get => dismissible;
            set => dismissible = value;
        }

        /// <summary>
        /// Shows the sheet as a full-height right-hand column; dragging is off while docked.
        /// </summary>
        [UxmlAttribute("docked")]
        public bool Docked
        {
            get => docked;
            set
            {
                docked = value;
                EnableInClassList(DockedClass, docked);
                CancelDrag();
                ApplyLayout(animate: false);
            }
        }

        [UxmlAttribute("peek-height")]
        public float PeekHeight
        {
            get => peekHeight;
            set
            {
                peekHeight = Mathf.Max(0f, value);
                ApplyLayout(animate: false);
            }
        }

        /// <summary>
        /// Half height as a fraction of the sheet's parent height.
        /// </summary>
        [UxmlAttribute("half-ratio")]
        public float HalfRatio
        {
            get => halfRatio;
            set
            {
                halfRatio = Mathf.Clamp01(value);
                ApplyLayout(animate: false);
            }
        }

        /// <summary>
        /// Sets <see cref="HalfRatio"/>; with <paramref name="animate"/> a sheet resting at half glides to its new height.
        /// </summary>
        public void SetHalfRatio(float ratio, bool animate)
        {
            halfRatio = Mathf.Clamp01(ratio);
            ApplyLayout(animate);
        }

        /// <summary>
        /// Gap kept above the sheet at full height, normally the status bar and safe-area top.
        /// </summary>
        [UxmlAttribute("full-top-offset")]
        public float FullTopOffset
        {
            get => fullTopOffset;
            set
            {
                fullTopOffset = Mathf.Max(0f, value);
                ApplyLayout(animate: false);
            }
        }

        /// <summary>
        /// Bottom safe-area inset; padding below the content so it clears the home indicator.
        /// </summary>
        [UxmlAttribute("bottom-inset")]
        public float BottomInset
        {
            get => bottomInset;
            set
            {
                bottomInset = Mathf.Max(0f, value);
                ApplyContentPadding();
            }
        }

        public BottomSheet()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;
            restoreDragging = () => EnableInClassList(DraggingClass, isDragging);

            scrim = UIToolkitExtensions.CreateVisualElement(ScrimClass);
            scrim.RegisterCallback<ClickEvent>(OnScrimClicked);
            hierarchy.Add(scrim);

            sheetPanel = UIToolkitExtensions.CreateVisualElement(PanelClass);
            hierarchy.Add(sheetPanel);

            handle = UIToolkitExtensions.CreateVisualElement(sheetPanel, HandleClass);
            handle.focusable = true;
            handle.tabIndex = 0;
            var grabber = UIToolkitExtensions.CreateVisualElement(handle, GrabberClass).MakePill().IgnorePicking();

            header = UIToolkitExtensions.CreateVisualElement(sheetPanel, HeaderClass);
            content = UIToolkitExtensions.CreateVisualElement(sheetPanel, ContentClass);

            footer = UIToolkitExtensions.CreateVisualElement(sheetPanel, FooterClass);
            footer.RegisterCallback<GeometryChangedEvent>(_ => ApplyContentPadding());

            handle.RegisterCallback<PointerDownEvent>(OnPointerDown);
            header.RegisterCallback<PointerDownEvent>(OnPointerDown);
            sheetPanel.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            sheetPanel.RegisterCallback<PointerUpEvent>(OnPointerUp);
            sheetPanel.RegisterCallback<PointerCancelEvent>(_ => CancelDrag());
            sheetPanel.RegisterCallback<PointerCaptureOutEvent>(_ => CancelDrag());
            handle.RegisterCallback<NavigationMoveEvent>(OnHandleNavigation);

            RegisterCallback<GeometryChangedEvent>(_ => ApplyLayout(animate: false));

            SetDetentClasses();
            UpdateScrim();
        }

        /// <summary>
        /// Pins an element to the visible bottom edge of the sheet, replacing the previous footer; null removes it.
        /// The footer hides at peek, and the bottom inset moves from the content onto it.
        /// </summary>
        public void SetFooter(VisualElement element)
        {
            footer.Clear();
            if (element != null)
            {
                footer.Add(element);
            }

            hasFooter = element != null;
            EnableInClassList(HasFooterClass, hasFooter);
            SetPanelOffset(dragOffset);
            ApplyContentPadding();
        }

        /// <summary>
        /// Opens the sheet at a detent (half by default, or the nearest allowed detent).
        /// </summary>
        public void Open(SheetDetent detent = SheetDetent.Half) => SnapTo(detent);

        public void Close() => SnapTo(SheetDetent.Hidden);

        /// <summary>
        /// Moves the sheet to a detent. A detent that is not in <see cref="Detents"/> moves to the nearest one that is.
        /// </summary>
        public void SnapTo(SheetDetent detent, bool animate = true) => SnapTo(detent, animate, notify: true);

        /// <summary>
        /// The back-button rule: full goes to half (or the next lower detent), anything else closes.
        /// </summary>
        /// <returns>False when the sheet was already hidden, so the caller can handle back itself.</returns>
        public bool HandleBack()
        {
            if (current == SheetDetent.Hidden)
            {
                return false;
            }

            if (current == SheetDetent.Full && !docked)
            {
                var lower = NextDetent(current, -1, includeHidden: false);
                if (lower != current)
                {
                    SnapTo(lower);
                    return true;
                }
            }

            Close();
            return true;
        }

        /// <summary>
        /// Height of the visible part of the panel at a detent, for the current parent size.
        /// </summary>
        public float VisibleHeightOf(SheetDetent detent)
        {
            var full = FullHeight();
            return detent switch
            {
                SheetDetent.Hidden => 0f,
                SheetDetent.Peek => Mathf.Min(peekHeight + bottomInset, full),
                SheetDetent.Half => Mathf.Min(HostHeight() * halfRatio, full),
                _ => full,
            };
        }

        private void SnapTo(SheetDetent detent, bool animate, bool notify)
        {
            var target = detent == SheetDetent.Hidden ? SheetDetent.Hidden : Allowed(detent);
            var changed = target != current;
            current = target;

            CancelDrag();
            SetDetentClasses();
            UpdateScrim();
            ApplyLayout(animate);

            if (changed && notify)
            {
                DetentChanged?.Invoke(current);
            }
        }

        private SheetDetent Allowed(SheetDetent detent)
        {
            if (detents.Contains(detent))
            {
                return detent;
            }

            var best = detents[0];
            foreach (var candidate in detents)
            {
                if (Mathf.Abs((int)candidate - (int)detent) < Mathf.Abs((int)best - (int)detent))
                {
                    best = candidate;
                }
            }

            return best;
        }

        private SheetDetent NextDetent(SheetDetent from, int direction, bool includeHidden)
        {
            var candidates = Candidates(includeHidden);
            var index = candidates.IndexOf(from);
            if (index < 0)
            {
                return from;
            }

            var next = Mathf.Clamp(index + direction, 0, candidates.Count - 1);
            return candidates[next];
        }

        private List<SheetDetent> Candidates(bool includeHidden)
        {
            var candidates = new List<SheetDetent>(detents.Count + 1);
            if (includeHidden)
            {
                candidates.Add(SheetDetent.Hidden);
            }

            candidates.AddRange(detents);
            return candidates;
        }

        private float HostHeight()
        {
            var height = layout.height;
            return float.IsNaN(height) ? 0f : height;
        }

        private float FullHeight() => Mathf.Max(0f, HostHeight() - fullTopOffset);

        private float OffsetFor(SheetDetent detent) => FullHeight() - VisibleHeightOf(detent);

        private void ApplyLayout(bool animate)
        {
            if (docked)
            {
                sheetPanel.style.height = StyleKeyword.Null;
                sheetPanel.style.translate = new Translate(0f, 0f);
                footer.style.translate = new Translate(0f, 0f);
                dragOffset = 0f;
                sheetPanel.style.display = current == SheetDetent.Hidden ? DisplayStyle.None : DisplayStyle.Flex;
                ApplyContentPadding();
                return;
            }

            sheetPanel.style.display = DisplayStyle.Flex;
            sheetPanel.style.height = FullHeight();

            if (isDragging)
            {
                return;
            }

            // Without animation the inline translate change must not start a USS transition.
            EnableInClassList(DraggingClass, !animate);
            SetPanelOffset(OffsetFor(current));
            ApplyContentPadding();
            if (!animate)
            {
                schedule.Execute(restoreDragging);
            }
        }

        private void SetPanelOffset(float offset)
        {
            dragOffset = offset;
            sheetPanel.style.translate = new Translate(0f, offset);

            // The footer moves against the panel, so it stays on the visible bottom edge.
            footer.style.translate = new Translate(0f, hasFooter ? -offset : 0f);
        }

        /// <summary>
        /// Ends the content area at the visible bottom edge, or at the footer when one shows, so scrolling content
        /// reaches its end at every detent. Not updated mid-drag, to avoid a relayout per frame.
        /// </summary>
        private void ApplyContentPadding()
        {
            if (isDragging)
            {
                return;
            }

            footer.style.paddingBottom = bottomInset;
            var footerShown = hasFooter && (docked || current is SheetDetent.Half or SheetDetent.Full);
            var footerHeight = footerShown && !float.IsNaN(footer.layout.height) ? footer.layout.height : 0f;
            var hidden = docked ? 0f : Mathf.Max(0f, dragOffset);
            content.style.paddingBottom = hidden + (footerShown ? footerHeight : bottomInset);
        }

        private void SetDetentClasses()
        {
            EnableInClassList(HiddenClass, current == SheetDetent.Hidden);
            EnableInClassList(PeekClass, current == SheetDetent.Peek);
            EnableInClassList(HalfClass, current == SheetDetent.Half);
            EnableInClassList(FullClass, current == SheetDetent.Full);
            sheetPanel.pickingMode = current == SheetDetent.Hidden ? PickingMode.Ignore : PickingMode.Position;
        }

        private void UpdateScrim()
        {
            var visible = modal && (current == SheetDetent.Half || current == SheetDetent.Full);
            EnableInClassList(ScrimVisibleClass, visible);
            scrim.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        private void OnScrimClicked(ClickEvent evt)
        {
            if (!ClassListContains(ScrimVisibleClass))
            {
                return;
            }

            evt.StopPropagation();
            ScrimClicked?.Invoke();

            if (dismissible)
            {
                Close();
            }
            else
            {
                SnapTo(detents[0]);
            }
        }

        private void OnHandleNavigation(NavigationMoveEvent evt)
        {
            if (docked)
            {
                return;
            }

            var direction = evt.direction switch
            {
                NavigationMoveEvent.Direction.Up => 1,
                NavigationMoveEvent.Direction.Down => -1,
                _ => 0,
            };

            if (direction == 0)
            {
                return;
            }

            var next = NextDetent(current, direction, includeHidden: dismissible);
            if (next != current)
            {
                SnapTo(next);
                evt.StopPropagation();
                handle.focusController?.IgnoreEvent(evt);
            }
        }

        #region Drag

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (docked || current == SheetDetent.Hidden || evt.button != 0)
            {
                return;
            }

            if (evt.currentTarget == header)
            {
                for (var element = evt.target as VisualElement; element != null && element != header; element = element.parent)
                {
                    if (element.focusable || element is ScrollView)
                    {
                        return;
                    }
                }
            }

            DragStart(evt.pointerId, evt.position.y, evt.timestamp / 1000f);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != dragPointerId)
            {
                return;
            }

            DragMove(evt.position.y, evt.timestamp / 1000f);
            if (isDragging)
            {
                sheetPanel.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != dragPointerId)
            {
                return;
            }

            DragEnd(evt.timestamp / 1000f);
        }

        internal void DragStart(int pointerId, float pointerY, float time)
        {
            dragPointerId = pointerId;
            dragStartPointerY = pointerY;
            dragStartOffset = dragOffset;
            lastMoveY = pointerY;
            lastMoveTime = time;
            velocity = 0f;
            isDragging = false;
        }

        internal void DragMove(float pointerY, float time)
        {
            var delta = pointerY - dragStartPointerY;
            if (!isDragging)
            {
                if (Mathf.Abs(delta) < DragSlop)
                {
                    return;
                }

                isDragging = true;
                AddToClassList(DraggingClass);
            }

            var dt = time - lastMoveTime;
            if (dt > 0.0001f)
            {
                var instant = (pointerY - lastMoveY) / dt;
                velocity = Mathf.Lerp(velocity, instant, 0.6f);
            }

            lastMoveY = pointerY;
            lastMoveTime = time;

            var maxOffset = dismissible ? OffsetFor(SheetDetent.Hidden) : OffsetFor(detents[0]);
            SetPanelOffset(Mathf.Clamp(dragStartOffset + delta, 0f, maxOffset));
        }

        internal void DragEnd(float time)
        {
            if (sheetPanel.HasPointerCapture(dragPointerId))
            {
                sheetPanel.ReleasePointer(dragPointerId);
            }

            dragPointerId = PointerId.invalidPointerId;

            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            RemoveFromClassList(DraggingClass);

            // A pause before release is a placement, not a fling.
            if (time - lastMoveTime > ReleaseHoldSeconds)
            {
                velocity = 0f;
            }

            var visible = FullHeight() - dragOffset;
            var candidates = Candidates(includeHidden: dismissible);
            SheetDetent target;

            if (Mathf.Abs(velocity) >= FlingVelocity)
            {
                // Positive velocity is downward: the next lower detent below the current position, else the next higher one.
                target = velocity > 0f ? candidates[0] : candidates[candidates.Count - 1];
                if (velocity > 0f)
                {
                    for (var i = candidates.Count - 1; i >= 0; i--)
                    {
                        if (VisibleHeightOf(candidates[i]) < visible - 1f)
                        {
                            target = candidates[i];
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var candidate in candidates)
                    {
                        if (VisibleHeightOf(candidate) > visible + 1f)
                        {
                            target = candidate;
                            break;
                        }
                    }
                }
            }
            else
            {
                target = candidates[0];
                foreach (var candidate in candidates)
                {
                    if (Mathf.Abs(VisibleHeightOf(candidate) - visible) < Mathf.Abs(VisibleHeightOf(target) - visible))
                    {
                        target = candidate;
                    }
                }
            }

            SnapTo(target, animate: true, notify: true);
        }

        private void CancelDrag()
        {
            if (dragPointerId != PointerId.invalidPointerId && sheetPanel.HasPointerCapture(dragPointerId))
            {
                sheetPanel.ReleasePointer(dragPointerId);
            }

            dragPointerId = PointerId.invalidPointerId;
            if (isDragging)
            {
                isDragging = false;
                RemoveFromClassList(DraggingClass);
                ApplyLayout(animate: true);
            }
        }

        #endregion
    }
}
