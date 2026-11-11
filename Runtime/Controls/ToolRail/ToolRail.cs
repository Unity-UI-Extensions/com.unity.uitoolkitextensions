/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum ToolRailOrientation
    {
        /// <summary>
        /// Horizontal when the parent is taller than wide, vertical otherwise.
        /// </summary>
        Auto,
        Horizontal,
        Vertical,
    }

    /// <summary>
    /// One button of a <see cref="ToolRail"/>. Labels are display text; localise before passing them.
    /// </summary>
    public sealed class ToolItem
    {
        public string Id;

        /// <summary>
        /// Shown under the icon when labels are on; always the accessible name.
        /// </summary>
        public string Label;

        public Background Icon;
        public bool Enabled = true;

        /// <summary>
        /// Draws the tool in the warning colour, such as Delete.
        /// </summary>
        public bool Warn;

        public ToolItem() { }

        public ToolItem(string id, string label, Background icon = default, bool warn = false)
        {
            Id = id;
            Label = label;
            Icon = icon;
            Warn = warn;
        }
    }

    /// <summary>
    /// A rail of icon tools in a dark pill: a mode switcher with one active tool, or an action toolbar such as the
    /// selection tools (duplicate, undo, inspect, delete). Horizontal or vertical, or chosen from the parent's shape.
    /// </summary>
    /// <remarks>
    /// <para>The rail lays out in the normal flow. The owner places it. Tools are focusable and submit like taps.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ToolRail : VisualElement
    {
        public const string RootClass = "toolRail";
        public const string ItemClass = "toolRail__item";
        public const string IconClass = "toolRail__icon";
        public const string LabelClass = "toolRail__label";
        public const string HorizontalClass = "toolRail--horizontal";
        public const string VerticalClass = "toolRail--vertical";
        public const string LabelsClass = "toolRail--labels";
        public const string ActiveClass = "toolRail__item--active";
        public const string DisabledClass = "toolRail__item--disabled";
        public const string WarnClass = "toolRail__item--warn";

        private readonly List<ToolItem> items = new();
        private readonly List<VisualElement> elements = new();
        private bool selectable;
        private string activeId;
        private ToolRailOrientation orientation = ToolRailOrientation.Auto;

        public event Action<string> ToolSelected;

        public IReadOnlyList<ToolItem> Items => items;

        public ToolRailOrientation ResolvedOrientation => ClassListContains(VerticalClass) ? ToolRailOrientation.Vertical : ToolRailOrientation.Horizontal;

        [UxmlAttribute("labels")]
        public List<string> Labels
        {
            get => items.ConvertAll(i => i.Label);
            set
            {
                SetItems(value?.ConvertAll(l => new ToolItem(l, l)) ?? new List<ToolItem>());
            }
        }

        /// <summary>
        /// Whether tapping a tool makes it the active one (a mode rail) rather than only raising the event.
        /// </summary>
        [UxmlAttribute("selectable")]
        public bool Selectable
        {
            get => selectable;
            set
            {
                selectable = value;
                UpdateActive();
            }
        }

        /// <summary>
        /// The active tool's id when <see cref="Selectable"/>. Setting it does not raise <see cref="ToolSelected"/>.
        /// </summary>
        [UxmlAttribute("active-id")]
        public string ActiveId
        {
            get => selectable ? activeId : null;
            set
            {
                activeId = value;
                UpdateActive();
            }
        }

        [UxmlAttribute("orientation")]
        public ToolRailOrientation Orientation
        {
            get => orientation;
            set
            {
                orientation = value;
                UpdateOrientation();
            }
        }

        [UxmlAttribute("show-labels")]
        public bool ShowLabels
        {
            get => ClassListContains(LabelsClass);
            set => EnableInClassList(LabelsClass, value);
        }

        public ToolRail()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            this.MakePill();

            RegisterCallback<AttachToPanelEvent>(_ => UpdateOrientation());
            RegisterCallback<GeometryChangedEvent>(_ => UpdateOrientation());
            UpdateOrientation();
        }

        public void SetItems(IReadOnlyList<ToolItem> newItems)
        {
            items.Clear();
            elements.Clear();
            Clear();

            if (newItems == null)
            {
                return;
            }

            foreach (var item in newItems)
            {
                items.Add(item);

                var element = UIToolkitExtensions.CreateVisualElement(this, ItemClass).MakePill();
                element.focusable = true;
                element.tabIndex = 0;
                element.tooltip = item.Label;
                element.EnableInClassList(WarnClass, item.Warn);
                var id = item.Id;
                element.RegisterActivate(() => Select(id));

                var icon = UIToolkitExtensions.CreateVisualElement(element, IconClass).IgnorePicking();
                var hasIcon = icon.SetImageOrNone(item.Icon);

                var label = UIToolkitExtensions.CreateVisualElement<Label>(element, LabelClass);
                label.text = item.Label ?? string.Empty;
                label.pickingMode = PickingMode.Ignore;

                elements.Add(element);
                ApplyEnabled(elements.Count - 1);
            }

            UpdateActive();
        }

        public void SetItemEnabled(string id, bool enabled)
        {
            var index = items.FindIndex(i => i.Id == id);
            if (index < 0)
            {
                throw new ArgumentException($"No tool has the id '{id}'.", nameof(id));
            }

            items[index].Enabled = enabled;
            ApplyEnabled(index);
        }

        public VisualElement ItemElement(string id)
        {
            var index = items.FindIndex(i => i.Id == id);
            return index >= 0 ? elements[index] : null;
        }

        private void ApplyEnabled(int index)
        {
            elements[index].SetEnabled(items[index].Enabled);
            elements[index].EnableInClassList(DisabledClass, !items[index].Enabled);
        }

        private void Select(string id)
        {
            var item = items.Find(i => i.Id == id);
            if (item == null || !item.Enabled)
            {
                return;
            }

            if (selectable)
            {
                activeId = id;
                UpdateActive();
            }

            ToolSelected?.Invoke(id);
        }

        private void UpdateActive()
        {
            for (var i = 0; i < elements.Count; i++)
            {
                elements[i].EnableInClassList(ActiveClass, selectable && items[i].Id == activeId);
            }
        }

        private void UpdateOrientation()
        {
            var resolved = orientation;
            if (resolved == ToolRailOrientation.Auto)
            {
                var host = parent?.layout.size ?? Vector2.zero;
                resolved = !float.IsNaN(host.x) && host.x > host.y && host.y > 0f ? ToolRailOrientation.Vertical : ToolRailOrientation.Horizontal;
            }

            EnableInClassList(HorizontalClass, resolved == ToolRailOrientation.Horizontal);
            EnableInClassList(VerticalClass, resolved == ToolRailOrientation.Vertical);
        }
    }
}
