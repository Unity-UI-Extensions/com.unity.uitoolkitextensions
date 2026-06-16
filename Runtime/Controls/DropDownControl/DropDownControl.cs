/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A wheel-style dropdown picker that opens from a trigger pill and presents a
    /// vertically scrollable list of values in a modal overlay.
    /// </summary>
    public class DropDownControl : VisualElement
    {
        public const string RootClass = "dropDownControl";
        public const string TriggerClass = "dropDownControl__trigger";
        public const string TriggerLabelClass = "dropDownControl__triggerLabel";
        public const string TriggerIconClass = "dropDownControl__triggerIcon";
        public const string BackdropClass = "dropDownControl__backdrop";
        public const string PanelClass = "dropDownControl__panel";
        public const string ViewportClass = "dropDownControl__viewport";
        public const string ListClass = "dropDownControl__list";
        public const string RowClass = "dropDownControl__row";
        public const string SelectionLaneClass = "dropDownControl__selectionLane";
        public const string FadeTopClass = "dropDownControl__fadeTop";
        public const string FadeBottomClass = "dropDownControl__fadeBottom";
        public const string OpenModifier = "dropDownControl--open";

        private const int VisibleRows = 7;
        private const float RowHeightPx = 96f;
        private const int BufferRows = 3;
        private const float SnapVelocityThreshold = 50f;
        private const float MomentumDamping = 0.88f;
        private const float CenteredPoolTranslateY = -BufferRows * RowHeightPx;

        private string[] items = Array.Empty<string>();
        private int selectedIndex;
        private bool isOpen;
        private bool isDragging;
        private int activePointerId = -1;
        private float pointerDownY;
        private float dragStartOffset;
        private float currentOffset;
        private float lastPointerY;
        private float dragVelocity;
        private long lastPointerTime;
        private bool isCoasting;
        private IVisualElementScheduledItem coastSchedule;

        private readonly VisualElement triggerElement;
        private readonly Label triggerLabel;
        private readonly VisualElement backdropElement;
        private readonly VisualElement panelElement;
        private readonly VisualElement viewportElement;
        private readonly VisualElement listContainer;
        private readonly List<Label> rowLabels = new();

        public event Action<string> ValueChanged;
        public event Action<bool> OpenStateChanged;

        /// <summary>Gets or sets the ordered values shown in the picker.</summary>
        public IReadOnlyList<string> Items
        {
            get => items;
            set
            {
                items = value != null ? new List<string>(value).ToArray() : Array.Empty<string>();
                selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, items.Length - 1));
                currentOffset = OffsetForIndex(selectedIndex);

                if (items.Length == 0 && isOpen)
                {
                    Close();
                }

                RefreshTriggerLabel();
                if (isOpen)
                {
                    RebuildRows();
                    ApplyOffset(currentOffset);
                }
            }
        }

        /// <summary>Gets the currently selected string value.</summary>
        public string Value => items.Length > 0 ? items[selectedIndex] : string.Empty;

        public DropDownControl()
        {
            AddToClassList(RootClass);

            triggerElement = UIToolkitExtensions.CreateVisualElement(this, TriggerClass);

            triggerLabel = UIToolkitExtensions.CreateVisualElement<Label>(triggerElement, TriggerLabelClass);

            var triggerIcon = UIToolkitExtensions.CreateVisualElement(triggerElement, TriggerIconClass);
            triggerIcon.pickingMode = PickingMode.Ignore;

            triggerElement.RegisterCallback<ClickEvent>(_ => Open());
            triggerElement.RegisterCallback<PointerDownEvent>(OnTriggerPointerDown, TrickleDown.TrickleDown);

            backdropElement = UIToolkitExtensions.CreateVisualElement(BackdropClass);
            backdropElement.RegisterCallback<ClickEvent>(OnBackdropClicked);

            panelElement = UIToolkitExtensions.CreateVisualElement(PanelClass);
            panelElement.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            viewportElement = UIToolkitExtensions.CreateVisualElement(panelElement, ViewportClass);

            listContainer = UIToolkitExtensions.CreateVisualElement(viewportElement, ListClass);

            var fadeTop = UIToolkitExtensions.CreateVisualElement(panelElement, FadeTopClass);
            fadeTop.pickingMode = PickingMode.Ignore;

            var fadeBottom = UIToolkitExtensions.CreateVisualElement(panelElement, FadeBottomClass);
            fadeBottom.pickingMode = PickingMode.Ignore;

            var selectionLane = UIToolkitExtensions.CreateVisualElement(panelElement, SelectionLaneClass);
            selectionLane.pickingMode = PickingMode.Ignore;

            viewportElement.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            viewportElement.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            viewportElement.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            viewportElement.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            viewportElement.RegisterCallback<WheelEvent>(OnViewportWheel, TrickleDown.TrickleDown);

            RefreshTriggerLabel();
        }

        /// <summary>Pre-selects a value. No-op if the value is not present.</summary>
        public void SetDefault(string value)
        {
            if (items.Length == 0 || string.IsNullOrEmpty(value))
            {
                return;
            }

            int index = Array.IndexOf(items, value);
            if (index < 0)
            {
                return;
            }

            selectedIndex = index;
            currentOffset = OffsetForIndex(selectedIndex);
            RefreshTriggerLabel();
            if (isOpen)
            {
                ApplyOffset(currentOffset);
                RefreshRowLabels(currentOffset);
            }
        }

        private void OnTriggerPointerDown(PointerDownEvent evt)
        {
            Open();
            evt.StopPropagation();
        }

        private void Open()
        {
            if (isOpen || items.Length == 0)
            {
                return;
            }

            VisualElement root = panel?.visualTree;
            if (root == null)
            {
                return;
            }

            isOpen = true;
            AddToClassList(OpenModifier);

            root.Add(backdropElement);
            backdropElement.BringToFront();

            Rect triggerBounds = triggerElement.worldBound;
            panelElement.style.position = Position.Absolute;
            panelElement.style.left = triggerBounds.x;
            panelElement.style.top = triggerBounds.yMax + 8f;
            panelElement.style.width = triggerBounds.width;

            root.Add(panelElement);
            panelElement.BringToFront();

            currentOffset = OffsetForIndex(selectedIndex);
            RebuildRows();
            ApplyOffset(currentOffset);
            OpenStateChanged?.Invoke(true);
        }

        private void Close()
        {
            if (!isOpen)
            {
                return;
            }

            StopCoasting();
            isOpen = false;
            RemoveFromClassList(OpenModifier);
            panelElement.RemoveFromHierarchy();
            backdropElement.RemoveFromHierarchy();
            OpenStateChanged?.Invoke(false);
        }

        private void OnBackdropClicked(ClickEvent evt)
        {
            Close();
        }

        private void OnViewportWheel(WheelEvent evt)
        {
            if (!isOpen || items.Length == 0)
            {
                return;
            }

            int direction = evt.delta.y > 0f ? 1 : -1;
            int centreIndex = Mathf.RoundToInt(-currentOffset / RowHeightPx);
            AnimateToIndex(centreIndex + direction);
            evt.StopPropagation();
        }

        private void Confirm(int index)
        {
            if (items.Length == 0)
            {
                Close();
                return;
            }

            selectedIndex = WrapIndex(index);
            RefreshTriggerLabel();
            Close();
            ValueChanged?.Invoke(Value);
        }

        private void RebuildRows()
        {
            listContainer.Clear();
            rowLabels.Clear();

            int totalRows = VisibleRows + (BufferRows * 2);
            for (int i = 0; i < totalRows; i++)
            {
                var row = UIToolkitExtensions.CreateVisualElement<Label>(listContainer, RowClass);
                rowLabels.Add(row);
            }

            RefreshRowLabels(currentOffset);
        }

        private void RefreshRowLabels(float offset)
        {
            if (items.Length == 0 || rowLabels.Count == 0)
            {
                return;
            }

            int centreIndex = Mathf.RoundToInt(-offset / RowHeightPx);
            int topLogicalIndex = centreIndex - (VisibleRows / 2) - BufferRows;

            for (int i = 0; i < rowLabels.Count; i++)
            {
                int logicalIndex = topLogicalIndex + i;
                rowLabels[i].text = items[WrapIndex(logicalIndex)];
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (activePointerId != -1)
            {
                return;
            }

            StopCoasting();
            activePointerId = evt.pointerId;
            pointerDownY = evt.position.y;
            lastPointerY = evt.position.y;
            dragStartOffset = currentOffset;
            isDragging = false;
            dragVelocity = 0f;
            lastPointerTime = DateTime.Now.Ticks;
            (evt.currentTarget as VisualElement)?.CapturePointer(activePointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            float delta = evt.position.y - pointerDownY;
            if (!isDragging && Mathf.Abs(delta) > 4f)
            {
                isDragging = true;
            }

            if (!isDragging)
            {
                return;
            }

            long now = DateTime.Now.Ticks;
            float deltaTime = (now - lastPointerTime) / (float)TimeSpan.TicksPerSecond;
            if (deltaTime > 0f)
            {
                dragVelocity = (evt.position.y - lastPointerY) / deltaTime;
            }

            lastPointerY = evt.position.y;
            lastPointerTime = now;

            currentOffset = dragStartOffset + delta;
            ApplyOffset(currentOffset);
            RefreshRowLabels(currentOffset);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            (evt.currentTarget as VisualElement)?.ReleasePointer(activePointerId);
            activePointerId = -1;

            if (!isDragging)
            {
                TrySelectTappedRow(evt.position.y);
            }
            else
            {
                isDragging = false;
                if (Mathf.Abs(dragVelocity) > SnapVelocityThreshold)
                {
                    StartCoasting();
                }
                else
                {
                    SnapToNearest();
                }
            }

            evt.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            (evt.currentTarget as VisualElement)?.ReleasePointer(activePointerId);
            activePointerId = -1;
            isDragging = false;
            SnapToNearest();
        }

        private void TrySelectTappedRow(float worldY)
        {
            if (rowLabels.Count == 0)
            {
                return;
            }

            for (int i = 0; i < rowLabels.Count; i++)
            {
                Rect bounds = rowLabels[i].worldBound;
                if (bounds.yMin <= worldY && worldY <= bounds.yMax)
                {
                    int centreIndex = Mathf.RoundToInt(-currentOffset / RowHeightPx);
                    int topLogicalIndex = centreIndex - (VisibleRows / 2) - BufferRows;
                    Confirm(topLogicalIndex + i);
                    return;
                }
            }

            Confirm(Mathf.RoundToInt(-currentOffset / RowHeightPx));
        }

        private void SnapToNearest()
        {
            int nearest = Mathf.RoundToInt(-currentOffset / RowHeightPx);
            AnimateToIndex(nearest);
        }

        private void StartCoasting()
        {
            isCoasting = true;
            coastSchedule = schedule.Execute(CoastStep).Every(16);
        }

        private void StopCoasting()
        {
            if (!isCoasting)
            {
                return;
            }

            isCoasting = false;
            coastSchedule?.Pause();
            coastSchedule = null;
        }

        private void CoastStep()
        {
            dragVelocity *= MomentumDamping;
            currentOffset += dragVelocity * 0.016f;
            ApplyOffset(currentOffset);
            RefreshRowLabels(currentOffset);

            if (Mathf.Abs(dragVelocity) < SnapVelocityThreshold)
            {
                StopCoasting();
                SnapToNearest();
            }
        }

        private void AnimateToIndex(int logicalIndex)
        {
            float targetOffset = OffsetForIndex(logicalIndex);
            const int steps = 9;
            int currentStep = 0;
            float startOffset = currentOffset;

            IVisualElementScheduledItem animation = null;
            animation = schedule.Execute(() =>
            {
                currentStep++;
                float t = currentStep / (float)steps;
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                currentOffset = Mathf.Lerp(startOffset, targetOffset, easedT);
                ApplyOffset(currentOffset);
                RefreshRowLabels(currentOffset);

                if (currentStep >= steps)
                {
                    animation?.Pause();
                    currentOffset = targetOffset;
                    ApplyOffset(currentOffset);
                    RefreshRowLabels(currentOffset);
                }
            }).Every(16);
        }

        private float OffsetForIndex(int index)
        {
            return -index * RowHeightPx;
        }

        private int WrapIndex(int index)
        {
            if (items.Length == 0)
            {
                return 0;
            }

            return ((index % items.Length) + items.Length) % items.Length;
        }

        private void ApplyOffset(float offset)
        {
            int centreIndex = Mathf.RoundToInt(-offset / RowHeightPx);
            float centredOffset = OffsetForIndex(centreIndex);
            float fractionalOffset = offset - centredOffset;
            listContainer.style.translate = new Translate(0f, CenteredPoolTranslateY + fractionalOffset, 0f);
        }

        private void RefreshTriggerLabel()
        {
            triggerLabel.text = Value;
        }
    }
}