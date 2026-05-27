/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A single-selection group of <see cref="ColorToggleButton"/> elements.
    /// Supports both tap-to-select and drag-to-select interaction patterns.
    /// </summary>
    public class ColorToggleGroup : VisualElement
    {
        public const string RootClass = "colorToggleGroup";
        public const string ContainerClass = "colorToggleGroup__container";

        private readonly VisualElement container;
        private readonly List<ColorToggleButton> buttons = new();
        private Color[] colors = Array.Empty<Color>();
        private FlexDirection alignment = FlexDirection.Column;
        private bool isPointerDown;
        private bool isDragging;
        private int activePointerId = -1;
        private Vector2 dragStartPosition;
        private ColorToggleButton lastDragSelectedButton;
        private const float DragSelectionThreshold = 8f;

        public event Action<Color> OnColorSelected;

        public Color? SelectedColor
        {
            get
            {
                foreach (var button in buttons)
                {
                    if (button.IsSelected)
                    {
                        return button.TintColor;
                    }
                }
                return null;
            }
        }

        public Color[] Colors
        {
            get => colors;
            set
            {
                colors = value ?? Array.Empty<Color>();
                RebuildButtons();
            }
        }

        public FlexDirection Alignment
        {
            get => alignment;
            set
            {
                alignment = value;
                container.style.flexDirection = alignment;
            }
        }

        public ColorToggleGroup()
        {
            AddToClassList(RootClass);

            container = UIToolkitExtensions.CreateVisualElement(this, ContainerClass);
            container.style.flexDirection = alignment;

            container.RegisterCallback<PointerDownEvent>(OnContainerPointerDown, TrickleDown.TrickleDown);
            container.RegisterCallback<PointerMoveEvent>(OnContainerPointerMove, TrickleDown.TrickleDown);
            container.RegisterCallback<PointerUpEvent>(OnContainerPointerUp, TrickleDown.TrickleDown);
            container.RegisterCallback<PointerCancelEvent>(OnContainerPointerCancel, TrickleDown.TrickleDown);
        }

        public void DeselectAll()
        {
            foreach (var button in buttons)
            {
                button.ForceDeselect();
            }
        }

        public void SelectColor(Color color, bool propagateEvent = true)
        {
            foreach (var button in buttons)
            {
                if (button.TintColor == color)
                {
                    if (!button.IsSelected)
                    {
                        button.ForceSelect();
                        HandleToggleGroup(button, propagateEvent);
                    }
                    return;
                }
            }
        }

        private void RebuildButtons()
        {
            foreach (var button in buttons)
            {
                container.Remove(button);
            }
            buttons.Clear();

            foreach (var color in colors)
            {
                CreateColorToggleButton(color);
            }
        }

        private void CreateColorToggleButton(Color tintColor)
        {
            var button = new ColorToggleButton(tintColor);
            buttons.Add(button);
            button.OnClicked += () => HandleToggleGroup(button);
            container.Add(button);
        }

        private void HandleToggleGroup(ColorToggleButton clicked, bool propagateEvent = true)
        {
            if (!clicked.IsSelected)
            {
                return;
            }

            foreach (var button in buttons)
            {
                if (!ReferenceEquals(button, clicked))
                {
                    button.ForceDeselect();
                }
            }

            if (propagateEvent)
            {
                OnColorSelected?.Invoke(clicked.TintColor);
            }
        }

        private void OnContainerPointerDown(PointerDownEvent evt)
        {
            isPointerDown = true;
            isDragging = false;
            activePointerId = evt.pointerId;
            dragStartPosition = evt.position;
            lastDragSelectedButton = null;
        }

        private void OnContainerPointerMove(PointerMoveEvent evt)
        {
            if (!isPointerDown || evt.pointerId != activePointerId)
            {
                return;
            }

            if (!isDragging)
            {
                if (Vector2.Distance(evt.position, dragStartPosition) < DragSelectionThreshold)
                {
                    return;
                }

                isDragging = true;
                container.CapturePointer(evt.pointerId);
            }

            ColorToggleButton hoveredButton = FindButtonAtPosition(evt.position);
            if (hoveredButton == null || ReferenceEquals(hoveredButton, lastDragSelectedButton))
            {
                return;
            }

            SelectButtonFromDrag(hoveredButton);
            lastDragSelectedButton = hoveredButton;
        }

        private void OnContainerPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            ResetPointerTrackingState();
        }

        private void OnContainerPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            ResetPointerTrackingState();
        }

        private void ResetPointerTrackingState()
        {
            if (container.HasPointerCapture(activePointerId))
            {
                container.ReleasePointer(activePointerId);
            }

            isPointerDown = false;
            isDragging = false;
            activePointerId = -1;
            lastDragSelectedButton = null;
        }

        private ColorToggleButton FindButtonAtPosition(Vector2 panelPosition)
        {
            if (panel == null)
            {
                return null;
            }

            VisualElement current = panel.Pick(panelPosition) as VisualElement;
            while (current != null)
            {
                if (current is ColorToggleButton button && buttons.Contains(button))
                {
                    return button;
                }

                current = current.parent;
            }

            return null;
        }

        private void SelectButtonFromDrag(ColorToggleButton button)
        {
            if (button.IsSelected)
            {
                return;
            }

            button.ForceSelect();
            HandleToggleGroup(button, true);
        }
    }
}
