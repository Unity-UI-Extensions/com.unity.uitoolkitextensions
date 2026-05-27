/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A toggle button that displays a single image which changes via USS classes when toggled.
    /// Fires <see cref="OnClicked"/> on every pointer-down; use <see cref="IsSelected"/> to read state.
    /// </summary>
    public class ToggleButton : VisualElement
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

        public void SetImage(Texture2D texture)
        {
            image.style.backgroundImage = texture != null
                ? new StyleBackground(texture)
                : StyleKeyword.Null;
            image.style.unityBackgroundImageTintColor = Color.white;
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
