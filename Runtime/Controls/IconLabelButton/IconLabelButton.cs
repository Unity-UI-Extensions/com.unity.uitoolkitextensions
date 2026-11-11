/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A button with an icon on the left and a label filling the remaining space.
    /// Interaction states (hover, pressed) are driven by USS classes.
    /// </summary>
    [UxmlElement]
    public partial class IconLabelButton : VisualElement
    {
        public const string RootClass = "iconLabelButton";
        public const string ButtonClass = "iconLabelButton__button";
        public const string IconClass = "iconLabelButton__icon";
        public const string LabelClass = "iconLabelButton__label";
        public const string HoverClass = "iconLabelButton--hover";
        public const string PressedClass = "iconLabelButton--pressed";

        public event Action Clicked;

        private readonly VisualElement button;
        private readonly VisualElement icon;
        private readonly Label label;

        public IconLabelButton()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            button = UIToolkitExtensions.CreateVisualElement(this, ButtonClass);
            icon = UIToolkitExtensions.CreateVisualElement(button, IconClass);
            label = UIToolkitExtensions.CreateVisualElement<Label>(button, LabelClass);

            button.RegisterCallback<ClickEvent>(_ => Clicked?.Invoke());
            button.RegisterCallback<MouseEnterEvent>(_ => button.AddToClassList(HoverClass));
            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                button.RemoveFromClassList(HoverClass);
                button.RemoveFromClassList(PressedClass);
            });
            button.RegisterCallback<MouseDownEvent>(_ => button.AddToClassList(PressedClass));
            button.RegisterCallback<MouseUpEvent>(_ => button.RemoveFromClassList(PressedClass));
        }

        [UxmlAttribute("text")]
        public string Text
        {
            get => label.text;
            set => label.text = value;
        }

        [UxmlAttribute("icon")]
        public Texture2D Icon
        {
            get => icon.style.backgroundImage.value.texture;
            set => icon.style.backgroundImage = value != null ? new StyleBackground(value) : (StyleBackground)StyleKeyword.Null;
        }
    }
}