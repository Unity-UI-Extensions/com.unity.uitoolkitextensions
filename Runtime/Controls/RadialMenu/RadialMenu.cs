/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// How a <see cref="RadialMenu"/> opens from its target.
    /// </summary>
    public enum RadialMenuTrigger
    {
        /// <summary>
        /// Press and hold opens it; sliding to an item and releasing picks it.
        /// </summary>
        LongPress,

        /// <summary>
        /// A tap opens it; a second tap picks an item, a tap elsewhere closes it.
        /// </summary>
        Tap,
    }

    /// <summary>
    /// How a <see cref="RadialMenu"/> draws its items.
    /// </summary>
    public enum RadialMenuStyle
    {
        /// <summary>
        /// Round item buttons on a circle.
        /// </summary>
        Ring,

        /// <summary>
        /// A wheel of segments behind the items, for picking a category.
        /// </summary>
        Wheel,
    }

    /// <summary>
    /// One action of a <see cref="RadialMenu"/>. The label is display text.
    /// </summary>
    public sealed class RadialMenuItem
    {
        public string Id;
        public string Label;
        public Background Icon;

        public RadialMenuItem() { }

        public RadialMenuItem(string id, string label, Background icon = default)
        {
            Id = id;
            Label = label;
            Icon = icon;
        }
    }

    /// <summary>
    /// A ring of four to six actions that opens around the touch point, with labels; a wheel style draws segments for
    /// category picking. Attach it to a target, or open it at a point yourself.
    /// </summary>
    /// <remarks>
    /// <para>The menu fills its parent as an overlay layer and takes input only while open. <see cref="Attach"/> adds a
    /// <see cref="LongPressManipulator"/> (or a tap) to a target; the ring opens at the press, clamped inside the layer.
    /// While holding, the item in the direction of the pointer lights up and releasing picks it; releasing near the
    /// centre cancels.</para>
    /// <para><see cref="ItemSelected"/> passes the item id. The default <c>radius</c> is 88.</para>
    /// </remarks>
    [UxmlElement]
    public partial class RadialMenu : VisualElement
    {
        public const string RootClass = "radialMenu";
        public const string OpenClass = "radialMenu--open";
        public const string WheelModifierClass = "radialMenu--wheel";
        public const string ScrimClass = "radialMenu__scrim";
        public const string WheelClass = "radialMenu__wheel";
        public const string CentreClass = "radialMenu__centre";
        public const string ItemClass = "radialMenu__item";
        public const string DiscClass = "radialMenu__disc";
        public const string IconClass = "radialMenu__icon";
        public const string LabelClass = "radialMenu__label";
        public const string HighlightedClass = "radialMenu__item--highlighted";

        /// <summary>
        /// Item disc diameter in pixels.
        /// </summary>
        public const float ItemSize = 56f;

        private static readonly CustomStyleProperty<Color> SegmentColorProperty = new("--radial-segment");
        private static readonly CustomStyleProperty<Color> HighlightColorProperty = new("--radial-highlight");

        private readonly VisualElement scrim;
        private readonly VisualElement wheel;
        private readonly VisualElement centre;
        private readonly List<RadialMenuItem> items = new();
        private readonly List<VisualElement> itemElements = new();
        private readonly Dictionary<VisualElement, (LongPressManipulator press, EventCallback<ClickEvent> tap)> targets = new();
        private float radius = 88f;
        private RadialMenuTrigger trigger = RadialMenuTrigger.LongPress;
        private RadialMenuStyle menuStyle = RadialMenuStyle.Ring;
        private int holdThresholdMs = 400;
        private Vector2 origin;
        private int highlighted = -1;
        private bool isOpen;
        private Color segmentColor = new(0.09f, 0.1f, 0.12f, 0.9f);
        private Color highlightColor = new(0.24f, 0.51f, 0.6f, 0.6f);

        public event Action<string> ItemSelected;

        public event Action Opened;

        public event Action Closed;

        public bool IsOpen => isOpen;

        public Vector2 Origin => origin;

        /// <summary>
        /// The highlighted item's index, or -1.
        /// </summary>
        public int HighlightedIndex => highlighted;

        public IReadOnlyList<RadialMenuItem> Items => items;

        [UxmlAttribute("radius")]
        public float Radius
        {
            get => radius;
            set
            {
                radius = Mathf.Max(48f, value);
                Layout();
            }
        }

        [UxmlAttribute("trigger")]
        public RadialMenuTrigger Trigger
        {
            get => trigger;
            set => trigger = value;
        }

        [UxmlAttribute("menu-style")]
        public RadialMenuStyle MenuStyle
        {
            get => menuStyle;
            set
            {
                menuStyle = value;
                EnableInClassList(WheelModifierClass, value == RadialMenuStyle.Wheel);
                wheel.MarkDirtyRepaint();
            }
        }

        [UxmlAttribute("hold-threshold-ms")]
        public int HoldThresholdMs
        {
            get => holdThresholdMs;
            set => holdThresholdMs = Mathf.Max(0, value);
        }

        public RadialMenu()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            scrim = UIToolkitExtensions.CreateVisualElement(this, ScrimClass);
            scrim.RegisterCallback<ClickEvent>(_ => Close());
            wheel = UIToolkitExtensions.CreateVisualElement(this, WheelClass).IgnorePicking();
            wheel.generateVisualContent += DrawWheel;
            centre = UIToolkitExtensions.CreateVisualElement(this, CentreClass).IgnorePicking();

            RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.customStyle.TryGetValue(SegmentColorProperty, out var segment))
                {
                    segmentColor = segment;
                }

                if (evt.customStyle.TryGetValue(HighlightColorProperty, out var light))
                {
                    highlightColor = light;
                }

                wheel.MarkDirtyRepaint();
            });
            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (isOpen && evt.keyCode == KeyCode.Escape)
                {
                    Close();
                    evt.StopPropagation();
                }
            });

            ApplyOpen();
        }

        public void SetItems(IReadOnlyList<RadialMenuItem> newItems)
        {
            foreach (var element in itemElements)
            {
                element.RemoveFromHierarchy();
            }

            items.Clear();
            itemElements.Clear();
            if (newItems != null)
            {
                for (var i = 0; i < newItems.Count; i++)
                {
                    var item = newItems[i];
                    items.Add(item);
                    var element = UIToolkitExtensions.CreateVisualElement(this, ItemClass);
                    element.focusable = true;
                    element.tabIndex = 0;
                    var index = i;
                    element.RegisterCallback<ClickEvent>(evt =>
                    {
                        Pick(index);
                        evt.StopPropagation();
                    });
                    element.RegisterCallback<NavigationSubmitEvent>(evt =>
                    {
                        Pick(index);
                        evt.StopPropagation();
                    });

                    var disc = UIToolkitExtensions.CreateVisualElement(element, DiscClass).IgnorePicking();
                    disc.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
                    var icon = UIToolkitExtensions.CreateVisualElement(disc, IconClass).IgnorePicking();
                    icon.style.backgroundImage = new StyleBackground(item.Icon);
                    var label = UIToolkitExtensions.CreateVisualElement<Label>(element, LabelClass).IgnorePicking();
                    label.text = item.Label ?? string.Empty;
                    itemElements.Add(element);
                }
            }

            Layout();
        }

        public VisualElement ItemElement(int index) => itemElements[index];

        /// <summary>
        /// Opens the menu from presses on <paramref name="target"/>, by <see cref="Trigger"/>.
        /// </summary>
        public void Attach(VisualElement target)
        {
            if (target == null || targets.ContainsKey(target))
            {
                return;
            }

            LongPressManipulator press = null;
            EventCallback<ClickEvent> tap = null;
            if (trigger == RadialMenuTrigger.LongPress)
            {
                press = new LongPressManipulator(holdThresholdMs);
                press.HoldStarted += position => Open(this.WorldToLocal(position));
                press.HoldMoved += position => HighlightToward(this.WorldToLocal(position));
                press.HoldEnded += _ => Release();
                target.AddManipulator(press);
            }
            else
            {
                tap = evt => Open(this.WorldToLocal(evt.position));
                target.RegisterCallback(tap);
            }

            targets[target] = (press, tap);
        }

        public void Detach(VisualElement target)
        {
            if (target == null || !targets.TryGetValue(target, out var entry))
            {
                return;
            }

            if (entry.press != null)
            {
                target.RemoveManipulator(entry.press);
            }

            if (entry.tap != null)
            {
                target.UnregisterCallback(entry.tap);
            }

            targets.Remove(target);
        }

        /// <summary>
        /// Opens the ring at a point in the menu's local space, clamped so the items stay inside.
        /// </summary>
        public void Open(Vector2 localPosition)
        {
            if (items.Count == 0)
            {
                return;
            }

            var reach = radius + (ItemSize * 0.5f) + 20f;
            var size = layout.size;
            origin = new Vector2(
                float.IsNaN(size.x) || size.x < reach * 2f ? localPosition.x : Mathf.Clamp(localPosition.x, reach, size.x - reach),
                float.IsNaN(size.y) || size.y < reach * 2f ? localPosition.y : Mathf.Clamp(localPosition.y, reach, size.y - reach));
            isOpen = true;
            SetHighlight(-1);
            Layout();
            ApplyOpen();
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            SetHighlight(-1);
            ApplyOpen();
            Closed?.Invoke();
        }

        public void HighlightToward(Vector2 localPosition)
        {
            if (!isOpen || items.Count == 0)
            {
                return;
            }

            var offset = localPosition - origin;
            if (offset.magnitude < radius * 0.35f)
            {
                SetHighlight(-1);
                return;
            }

            // Item 0 is at twelve o'clock; angles run clockwise in screen space (y down).
            var angle = Mathf.Atan2(offset.x, -offset.y) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            var step = 360f / items.Count;
            SetHighlight(Mathf.RoundToInt(angle / step) % items.Count);
        }

        private void Release()
        {
            var index = highlighted;
            if (index >= 0)
            {
                Pick(index);
            }
            else if (trigger == RadialMenuTrigger.LongPress)
            {
                Close();
            }
        }

        private void Pick(int index)
        {
            if (!isOpen || index < 0 || index >= items.Count)
            {
                return;
            }

            var id = items[index].Id;
            Close();
            ItemSelected?.Invoke(id);
        }

        private void SetHighlight(int index)
        {
            highlighted = index;
            for (var i = 0; i < itemElements.Count; i++)
            {
                itemElements[i].EnableInClassList(HighlightedClass, i == index);
            }

            wheel.MarkDirtyRepaint();
        }

        private void ApplyOpen()
        {
            EnableInClassList(OpenClass, isOpen);
            scrim.pickingMode = isOpen ? PickingMode.Position : PickingMode.Ignore;
            foreach (var element in itemElements)
            {
                element.pickingMode = isOpen ? PickingMode.Position : PickingMode.Ignore;
            }
        }

        private void Layout()
        {
            var count = itemElements.Count;
            for (var i = 0; i < count; i++)
            {
                var angle = (-90f + (360f / count * i)) * Mathf.Deg2Rad;
                var centreOfItem = origin + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                itemElements[i].style.translate = new Translate(centreOfItem.x - (ItemSize * 0.5f), centreOfItem.y - (ItemSize * 0.5f));
            }

            centre.style.translate = new Translate(origin.x - 12f, origin.y - 12f);
            wheel.MarkDirtyRepaint();
        }

        private void DrawWheel(MeshGenerationContext context)
        {
            if (!isOpen || items.Count == 0)
            {
                return;
            }

            var painter = context.painter2D;
            var outer = radius + (ItemSize * 0.5f) + 12f;
            var inner = radius * 0.35f;
            var step = 360f / items.Count;
            for (var i = 0; i < items.Count; i++)
            {
                var isLit = i == highlighted;
                if (menuStyle == RadialMenuStyle.Ring && !isLit)
                {
                    continue;
                }

                // A segment centred on its item: Painter2D angles run clockwise from three o'clock.
                var start = -90f + (step * i) - (step * 0.5f) + 1f;
                var end = start + step - 2f;
                painter.fillColor = isLit ? highlightColor : segmentColor;
                painter.BeginPath();
                painter.Arc(origin, outer, start, end);
                painter.Arc(origin, inner, end, start, ArcDirection.CounterClockwise);
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
