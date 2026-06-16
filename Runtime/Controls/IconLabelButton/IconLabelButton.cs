/// Credit SimonDarksideJ

using System;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A button with an icon on the left and a label filling the remaining space.
    /// Interaction states (hover, pressed) are driven by USS classes.
    /// </summary>
    public class IconLabelButton : VisualElement
    {
        public const string RootClass = "iconLabelButton";
        public const string ButtonClass = "iconLabelButton__button";
        public const string IconClass = "iconLabelButton__icon";
        public const string LabelClass = "iconLabelButton__label";
        public const string HoverClass = "iconLabelButton--hover";
        public const string PressedClass = "iconLabelButton--pressed";

        public event Action Clicked;

        private VisualElement button;
        private VisualElement icon;
        private Label label;

        public IconLabelButton()
        {
            AddToClassList(RootClass);

            button = UIToolkitExtensions.CreateVisualElement(this, ButtonClass);
            icon = UIToolkitExtensions.CreateVisualElement(button, IconClass);
            label = UIToolkitExtensions.CreateVisualElement<Label>(button, LabelClass);

            button.RegisterCallback<ClickEvent>(OnClicked);
            button.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            button.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            button.RegisterCallback<MouseDownEvent>(OnMouseDown);
            button.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        public string Text
        {
            get => label.text;
            set => label.text = value;
        }

        private void OnClicked(ClickEvent evt)
        {
            Clicked?.Invoke();
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            button.AddToClassList(HoverClass);
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            button.RemoveFromClassList(HoverClass);
            button.RemoveFromClassList(PressedClass);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            button.AddToClassList(PressedClass);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            button.RemoveFromClassList(PressedClass);
        }
    }
}
