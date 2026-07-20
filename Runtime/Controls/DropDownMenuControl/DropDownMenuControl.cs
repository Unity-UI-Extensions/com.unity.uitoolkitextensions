/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A lightweight, anchored drop-down action menu.
    ///
    /// Opens as an overlay anchored to a trigger element and dismisses on a backdrop tap. Each option
    /// is a large, independently tappable row; choosing one closes the menu and invokes that option's
    /// callback. The menu injects a full-screen transparent backdrop and a floating panel into the
    /// panel's root visual tree, so it is unaffected by the clipping or layout of its anchor.
    ///
    /// It is purely programmatic — construct it once and drive it with <see cref="Open"/>:
    /// <code>
    /// var menu = new DropDownMenuControl();
    /// menu.Open(anchorElement, new List&lt;DropDownMenuControl.DropDownOption&gt;
    /// {
    ///     new DropDownMenuControl.DropDownOption("View",   () => { /* ... */ }),
    ///     new DropDownMenuControl.DropDownOption("Remove", () => { /* ... */ }),
    /// });
    /// </code>
    /// </summary>
    public class DropDownMenuControl : VisualElement
    {
        public enum Placement
        {
            /// <summary>
            /// Right edge aligned to the anchor's right edge, dropping downward
            /// (flipping upward on bottom overflow).
            /// </summary>
            AnchorRight,

            /// <summary>
            /// Centered on the anchor on both axes, clamped to the screen edges — so near the
            /// bottom it extends upward, near the top it extends downward, and anywhere else it
            /// straddles the anchor with equal space above and below.
            /// </summary>
            CenteredOnAnchor,
        }

        public readonly struct DropDownOption
        {
            public readonly string Label;
            public readonly Action OnSelected;

            public DropDownOption(string label, Action onSelected)
            {
                Label = label;
                OnSelected = onSelected;
            }
        }

        public const string BackdropClass = "dropDownMenuControl__backdrop";
        public const string PanelClass = "dropDownMenuControl__panel";
        public const string RowClass = "dropDownMenuControl__row";
        public const string RowFirstModifier = "dropDownMenuControl__row--first";
        public const string RowLabelClass = "dropDownMenuControl__rowLabel";

        // Row height in px — must stay in sync with the .dropDownMenuControl__row USS height, because it is used for the vertical-overflow maths when positioning the panel.
        private const float RowHeightPx = 96f;
        private const float MenuWidthPx = 320f;
        private const float EdgeMarginPx = 8f;

        private readonly VisualElement backdropElement;
        private readonly VisualElement panelElement;

        private bool isOpen;
        private Action dismissedCallback;

        public bool IsOpen => isOpen;

        public DropDownMenuControl()
        {
            backdropElement = UIToolkitExtensions.CreateVisualElement(BackdropClass);
            backdropElement.RegisterCallback<ClickEvent>(_ => Close());

            panelElement = UIToolkitExtensions.CreateVisualElement(PanelClass);
            panelElement.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        }

        public void Open(VisualElement anchor, IReadOnlyList<DropDownOption> options,
            Placement placement = Placement.AnchorRight, Action onDismissed = null)
        {
            if (anchor == null || options == null || options.Count == 0)
            {
                return;
            }

            var root = anchor.panel?.visualTree;
            if (root == null)
            {
                return;
            }

            if (isOpen)
            {
                Close();
            }

            isOpen = true;
            dismissedCallback = onDismissed;

            BuildRows(options);

            root.Add(backdropElement);
            backdropElement.BringToFront();
            root.Add(panelElement);
            panelElement.BringToFront();

            PositionPanel(anchor, root, options.Count, placement);
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            panelElement.RemoveFromHierarchy();
            backdropElement.RemoveFromHierarchy();

            var dismissed = dismissedCallback;
            dismissedCallback = null;
            dismissed?.Invoke();
        }

        private void BuildRows(IReadOnlyList<DropDownOption> options)
        {
            panelElement.Clear();

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];

                var row = UIToolkitExtensions.CreateVisualElement(panelElement, RowClass);
                if (i == 0)
                {
                    row.AddToClassList(RowFirstModifier);
                }

                var label = UIToolkitExtensions.CreateVisualElement<Label>(row, RowLabelClass);
                label.text = option.Label;
                label.pickingMode = PickingMode.Ignore; // let the row receive the tap

                row.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    var callback = option.OnSelected;
                    dismissedCallback = null;
                    Close();
                    callback?.Invoke();
                });
            }
        }

        private void PositionPanel(VisualElement anchor, VisualElement root, int optionCount, Placement placement)
        {
            var anchorBounds = anchor.worldBound;
            var rootBounds = root.worldBound;
            float menuHeight = optionCount * RowHeightPx;

            panelElement.style.position = Position.Absolute;
            panelElement.style.width = MenuWidthPx;

            float left;
            float top;

            if (placement == Placement.CenteredOnAnchor)
            {
                left = Mathf.Clamp(anchorBounds.center.x - MenuWidthPx / 2f,
                    rootBounds.xMin + EdgeMarginPx, rootBounds.xMax - EdgeMarginPx - MenuWidthPx);
                top = Mathf.Clamp(anchorBounds.center.y - menuHeight / 2f,
                    rootBounds.yMin + EdgeMarginPx, rootBounds.yMax - EdgeMarginPx - menuHeight);
            }
            else
            {
                left = Mathf.Max(rootBounds.xMin + EdgeMarginPx, anchorBounds.xMax - MenuWidthPx);

                top = anchorBounds.yMin;
                if (top + menuHeight > rootBounds.yMax - EdgeMarginPx)
                {
                    top = Mathf.Max(rootBounds.yMin + EdgeMarginPx, anchorBounds.yMax - menuHeight);
                }
            }

            panelElement.style.left = left;
            panelElement.style.top = top;
        }
    }
}