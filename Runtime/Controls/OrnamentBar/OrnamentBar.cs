/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Where an <see cref="OrnamentBar"/> sits.
    /// </summary>
    public enum OrnamentAnchor
    {
        /// <summary>
        /// Bottom in portrait, right edge in landscape, from the parent's shape.
        /// </summary>
        Auto,
        Bottom,
        Right,
    }

    /// <summary>
    /// One action in an <see cref="OrnamentBar"/>. Labels are display text; localise before passing them.
    /// </summary>
    public sealed class OrnamentItem
    {
        public string Id;
        public string Label;
        public Background Icon;
        public bool Enabled = true;

        public OrnamentItem() { }

        public OrnamentItem(string id, string label, Background icon = default)
        {
            Id = id;
            Label = label;
            Icon = icon;
        }
    }

    /// <summary>
    /// A floating action bar that sizes to its items and never stretches: centred at the bottom in
    /// portrait, centred on the right edge in landscape. One item can be the primary action.
    /// </summary>
    /// <remarks>
    /// <para>The ornament fills its parent as an overlay layer; only the bar takes input. The primary item is a
    /// pill showing its icon and label side by side, whether or not <see cref="ShowLabels"/> is on. Active items
    /// (<see cref="SetItemActive(string, bool)"/>) take the same filled pill, for example "Map" while wayfinding.</para>
    /// <para>A centre slot (<see cref="SetCentreElement"/>) holds one custom element among the items, such as a
    /// shutter button. The slot never leaves the bar, so its element stays attached across <see cref="SetItems"/>
    /// and a press held on it survives a rebuild. Message mode (<see cref="Message"/>) hides the items and shows one
    /// line of text before the slot, such as "Release to stop · 30 s max".</para>
    /// <para>Every item's label is also its tooltip. The primary and active pills use <see cref="GradientFill"/>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class OrnamentBar : VisualElement
    {
        public const string RootClass = "ornamentBar";
        public const string BarClass = "ornamentBar__bar";
        public const string ItemClass = "ornamentBar__item";
        public const string IconClass = "ornamentBar__icon";
        public const string LabelClass = "ornamentBar__label";
        public const string BottomClass = "ornamentBar--bottom";
        public const string RightClass = "ornamentBar--right";
        public const string LabelsClass = "ornamentBar--labels";
        public const string PrimaryItemClass = "ornamentBar__item--primary";
        public const string DisabledItemClass = "ornamentBar__item--disabled";
        public const string ActiveItemClass = "ornamentBar__item--active";
        public const string SlotClass = "ornamentBar__slot";
        public const string MessageClass = "ornamentBar__message";
        public const string SpacerClass = "ornamentBar__spacer";
        public const string StretchClass = "ornamentBar--stretch";
        public const string MessageModeClass = "ornamentBar--message";
        public const string PrimaryIconOnlyClass = "ornamentBar--primaryIconOnly";
        public const string PrimaryActionClass = "ornamentBar__item--primaryAction";

        private readonly VisualElement bar;
        private readonly Label messageLabel;
        private readonly VisualElement slot;
        private readonly VisualElement spacer;
        private readonly List<GradientFill> fills = new();
        private readonly List<OrnamentItem> items = new();
        private readonly List<VisualElement> itemElements = new();
        private readonly HashSet<int> activeIndices = new();
        private VisualElement centreElement;
        private int centreIndex = -1;
        private string message;
        private bool stretch;
        private bool primaryIconOnly;
        private int primaryIndex = -1;
        private OrnamentAnchor anchor = OrnamentAnchor.Auto;
        private float maxLength = 480f;
        private float edgeInset;
        private bool showLabels = true;

        /// <summary>
        /// Raised when an item is tapped or submitted: its index and id.
        /// </summary>
        public event Action<int, string> ItemSelected;

        /// <summary>
        /// The bar itself; exposed for layout measurement and tests.
        /// </summary>
        public VisualElement Bar => bar;

        public IReadOnlyList<OrnamentItem> Items => items;

        /// <summary>
        /// The anchor in use, with <see cref="OrnamentAnchor.Auto"/> resolved.
        /// </summary>
        public OrnamentAnchor ResolvedAnchor => ClassListContains(RightClass) ? OrnamentAnchor.Right : OrnamentAnchor.Bottom;

        /// <summary>
        /// UXML shorthand: one item per label, its id the label.
        /// </summary>
        [UxmlAttribute("labels")]
        public List<string> Labels
        {
            get => items.ConvertAll(i => i.Label);
            set
            {
                SetItems(value?.ConvertAll(l => new OrnamentItem(l, l)) ?? new List<OrnamentItem>());
            }
        }

        /// <summary>
        /// The item drawn as the primary action, or -1 for none.
        /// </summary>
        [UxmlAttribute("primary-index")]
        public int PrimaryIndex
        {
            get => primaryIndex;
            set
            {
                primaryIndex = value;
                UpdatePrimary();
            }
        }

        [UxmlAttribute("anchor")]
        public OrnamentAnchor Anchor
        {
            get => anchor;
            set
            {
                anchor = value;
                UpdateAnchor();
            }
        }

        /// <summary>
        /// Largest width (bottom) or height (right) of the bar.
        /// </summary>
        [UxmlAttribute("max-length")]
        public float MaxLength
        {
            get => maxLength;
            set
            {
                maxLength = Mathf.Max(0f, value);
                UpdateAnchor();
            }
        }

        [UxmlAttribute("show-labels")]
        public bool ShowLabels
        {
            get => showLabels;
            set
            {
                showLabels = value;
                EnableInClassList(LabelsClass, showLabels);
            }
        }

        /// <summary>
        /// The bar fills the anchored edge up to <see cref="MaxLength"/> and spreads its items evenly.
        /// </summary>
        [UxmlAttribute("stretch")]
        public bool Stretch
        {
            get => stretch;
            set
            {
                stretch = value;
                EnableInClassList(StretchClass, stretch);
            }
        }

        /// <summary>
        /// Draws the primary item as a round gradient disc with a large icon and no visible label, sized to the other
        /// icons on the bar, such as a plain "+" for Add content. The label stays as the item's tooltip and text, for
        /// assistive technology. Active items keep their labelled pills.
        /// </summary>
        [UxmlAttribute("primary-icon-only")]
        public bool PrimaryIconOnly
        {
            get => primaryIconOnly;
            set
            {
                primaryIconOnly = value;
                EnableInClassList(PrimaryIconOnlyClass, primaryIconOnly);
            }
        }

        /// <summary>
        /// Where the centre slot sits among the items: the number of items before it. Negative (the default) puts it
        /// in the middle.
        /// </summary>
        [UxmlAttribute("centre-index")]
        public int CentreIndex
        {
            get => centreIndex;
            set
            {
                centreIndex = value;
                ArrangeItems();
            }
        }

        /// <summary>
        /// The element in the centre slot, or null.
        /// </summary>
        public VisualElement CentreElement => centreElement;

        /// <summary>
        /// One line shown in place of the items, before the centre slot; null or empty leaves message mode. Display
        /// text, shown in capitals.
        /// </summary>
        [UxmlAttribute("message")]
        public string Message
        {
            get => message;
            set
            {
                message = string.IsNullOrEmpty(value) ? null : value;
                messageLabel.text = message?.ToUpperInvariant() ?? string.Empty;
                EnableInClassList(MessageModeClass, message != null);
            }
        }

        public bool IsMessageMode => message != null;

        /// <summary>
        /// Safe-area inset at the anchored edge, added to the bar's gap from that edge.
        /// </summary>
        [UxmlAttribute("edge-inset")]
        public float EdgeInset
        {
            get => edgeInset;
            set
            {
                edgeInset = Mathf.Max(0f, value);
                UpdateAnchor();
            }
        }

        public OrnamentBar()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            bar = UIToolkitExtensions.CreateVisualElement(BarClass).MakePill();
            hierarchy.Add(bar);

            // The message, the slot and the spacer never leave the bar; items are inserted around the slot.
            messageLabel = UIToolkitExtensions.CreateVisualElement<Label>(bar, MessageClass).IgnorePicking();
            slot = UIToolkitExtensions.CreateVisualElement(bar, SlotClass).IgnorePicking();
            slot.style.display = DisplayStyle.None;
            spacer = UIToolkitExtensions.CreateVisualElement(bar, SpacerClass).IgnorePicking();

            RegisterCallback<GeometryChangedEvent>(_ => UpdateAnchor());

            ShowLabels = true;
            UpdateAnchor();
        }

        public void SetItems(IReadOnlyList<OrnamentItem> newItems, int primary = -1)
        {
            ClearFills();
            foreach (var element in itemElements)
            {
                element.RemoveFromHierarchy();
            }

            items.Clear();
            itemElements.Clear();
            activeIndices.Clear();

            if (newItems != null)
            {
                for (var i = 0; i < newItems.Count; i++)
                {
                    var index = i;
                    var item = newItems[i];
                    items.Add(item);

                    var element = UIToolkitExtensions.CreateVisualElement(ItemClass).MakePill();
                    element.focusable = true;
                    element.tabIndex = 0;
                    element.RegisterCallback<ClickEvent>(_ =>
                    {
                        element.Blur();
                        Select(index);
                    });
                    element.RegisterCallback<NavigationSubmitEvent>(evt =>
                    {
                        Select(index);
                        evt.StopPropagation();
                    });

                    var icon = UIToolkitExtensions.CreateVisualElement(element, IconClass).IgnorePicking();
                    var hasIcon = icon.SetImageOrNone(item.Icon);
                    icon.SetDisplayed(hasIcon);

                    var label = UIToolkitExtensions.CreateVisualElement<Label>(element, LabelClass);
                    label.text = item.Label ?? string.Empty;
                    label.pickingMode = PickingMode.Ignore;

                    // The label names the item even when it is hidden: icon-only bars and the icon-only primary.
                    element.tooltip = item.Label ?? string.Empty;

                    itemElements.Add(element);
                    SetItemEnabled(index, item.Enabled);
                }
            }

            if (primary >= 0)
            {
                primaryIndex = primary;
            }

            ArrangeItems();
            UpdatePrimary();
        }

        /// <summary>
        /// Puts an element in the centre slot, replacing any earlier one; null empties the slot. The element keeps its
        /// own input; the bar does not raise <see cref="ItemSelected"/> for it.
        /// </summary>
        public void SetCentreElement(VisualElement element)
        {
            if (centreElement == element)
            {
                return;
            }

            centreElement?.RemoveFromHierarchy();
            centreElement = element;
            if (element != null)
            {
                slot.Add(element);
            }

            slot.SetDisplayed(element != null);
            ArrangeItems();
        }

        /// <summary>
        /// The index of the item with this id, or -1.
        /// </summary>
        public int IndexOf(string id) => items.FindIndex(i => i.Id == id);

        public void SetItemEnabled(string id, bool enabled)
        {
            var index = IndexOf(id);
            if (index >= 0)
            {
                SetItemEnabled(index, enabled);
            }
        }

        /// <summary>
        /// Draws an item as a filled pill with its label, for example the mode in use. Cleared by <see cref="SetItems"/>.
        /// </summary>
        public void SetItemActive(int index, bool active)
        {
            if (index < 0 || index >= itemElements.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (active ? activeIndices.Add(index) : activeIndices.Remove(index))
            {
                UpdatePrimary();
            }
        }

        public void SetItemActive(string id, bool active)
        {
            var index = IndexOf(id);
            if (index >= 0)
            {
                SetItemActive(index, active);
            }
        }

        public bool IsItemActive(int index) => activeIndices.Contains(index);

        public void SetItemEnabled(int index, bool enabled)
        {
            if (index < 0 || index >= itemElements.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            items[index].Enabled = enabled;
            itemElements[index].SetEnabled(enabled);
            itemElements[index].EnableInClassList(DisabledItemClass, !enabled);
        }

        /// <summary>
        /// The element for an item, for focus or tests.
        /// </summary>
        public VisualElement ItemElement(int index) => itemElements[index];

        private void Select(int index)
        {
            if (index < 0 || index >= items.Count || !items[index].Enabled)
            {
                return;
            }

            ItemSelected?.Invoke(index, items[index].Id);
        }

        private void UpdatePrimary()
        {
            // Removing a fill clears its classes and texture; the primary and active items get new ones.
            ClearFills();

            for (var i = 0; i < itemElements.Count; i++)
            {
                var primary = i == primaryIndex;
                var active = activeIndices.Contains(i);
                itemElements[i].EnableInClassList(PrimaryItemClass, primary || active);
                itemElements[i].EnableInClassList(PrimaryActionClass, primary);
                itemElements[i].EnableInClassList(ActiveItemClass, active);
                if (primary || active)
                {
                    var fill = new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal);
                    itemElements[i].AddManipulator(fill);
                    fills.Add(fill);
                }
            }
        }

        private void ClearFills()
        {
            foreach (var fill in fills)
            {
                fill.target?.RemoveManipulator(fill);
            }

            fills.Clear();
        }

        /// <summary>
        /// Places the items before and after the slot. Only items move; the slot stays attached.
        /// </summary>
        private void ArrangeItems()
        {
            var before = centreElement == null ? itemElements.Count
                : centreIndex < 0 ? (itemElements.Count + 1) / 2
                : Mathf.Min(centreIndex, itemElements.Count);

            for (var i = 0; i < itemElements.Count; i++)
            {
                var element = itemElements[i];
                element.RemoveFromHierarchy();
                bar.Insert(bar.IndexOf(i < before ? slot : spacer), element);
            }
        }

        private void UpdateAnchor()
        {
            var resolved = anchor;
            if (resolved == OrnamentAnchor.Auto)
            {
                var size = layout.size;
                resolved = !float.IsNaN(size.x) && !float.IsNaN(size.y) && size.x > size.y ? OrnamentAnchor.Right : OrnamentAnchor.Bottom;
            }

            EnableInClassList(BottomClass, resolved == OrnamentAnchor.Bottom);
            EnableInClassList(RightClass, resolved == OrnamentAnchor.Right);

            if (resolved == OrnamentAnchor.Bottom)
            {
                bar.style.maxWidth = maxLength;
                bar.style.maxHeight = StyleKeyword.Null;
                style.paddingBottom = edgeInset;
                style.paddingRight = StyleKeyword.Null;
            }
            else
            {
                bar.style.maxHeight = maxLength;
                bar.style.maxWidth = StyleKeyword.Null;
                style.paddingRight = edgeInset;
                style.paddingBottom = StyleKeyword.Null;
            }
        }
    }
}
