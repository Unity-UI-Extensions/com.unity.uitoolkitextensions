/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A toggle button that displays a single image which changes via USS classes when toggled.
    /// It fires <see cref="OnClicked"/> on every pointer-down. Read the state from <see cref="IsSelected"/>.
    /// </summary>
    /// <remarks>
    /// Use this control in a <see cref="ToggleButtonGroup"/>.
    /// It does not use the Unity button class, to avoid complex style overrides.
    /// </remarks>
    [UxmlElement]
    public partial class ToggleButton : VisualElement
    {
        public const string RootClass = "toggleButton";
        public const string ImageClass = "toggleButton__image";
        public const string SelectedClass = "toggleButton--selected";

        protected readonly VisualElement image;

        private bool isSelected;

        public event Action OnClicked;

        public bool IsSelected => isSelected;

        public ToggleButton()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            pickingMode = PickingMode.Position;

            image = new VisualElement();
            image.AddToClassList(ImageClass);
            image.pickingMode = PickingMode.Ignore;

            Add(image);

            RegisterCallback<PointerDownEvent>(OnPointerDownHandler);
        }

        public void ForceSelect()
        {
            SetSelected(true);
        }

        public void ForceDeselect()
        {
            SetSelected(false);
        }

        [UxmlAttribute("image")]
        public Texture2D Image
        {
            get => image.style.backgroundImage.value.texture;
            set => SetImage(value);
        }

        public void SetImage(Texture2D texture)
        {
            image.style.backgroundImage = texture != null
                ? new StyleBackground(texture)
                : (StyleBackground)StyleKeyword.Null;
            image.style.unityBackgroundImageTintColor = Color.white;
        }

        /// <summary>
        /// Sets the icon from a texture, sprite or vector image and leaves its tint to USS, unlike
        /// <see cref="SetImage"/>, which tints it white. A default value clears the icon.
        /// </summary>
        public void SetIcon(Background icon)
        {
            var hasIcon = icon.HasContent();
            image.style.backgroundImage = hasIcon ? new StyleBackground(icon) : (StyleBackground)StyleKeyword.Null;
            image.style.unityBackgroundImageTintColor = StyleKeyword.Null;
        }

        private void OnPointerDownHandler(PointerDownEvent evt)
        {
            evt.StopPropagation();
            ToggleSelected();
            OnClicked?.Invoke();
        }

        private void ToggleSelected()
        {
            SetSelected(!isSelected);
        }

        private void SetSelected(bool selected)
        {
            if (isSelected == selected)
            {
                return;
            }

            isSelected = selected;
            ApplySelectionState(isSelected);
        }

        protected virtual void ApplySelectionState(bool selected)
        {
            EnableInClassList(SelectedClass, selected);
        }
    }
}