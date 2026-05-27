/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A circular clickable image element with a no-image overlay (icon + optional upload label)
    /// that hides when an image is set. State is driven by the <see cref="HasImageClass"/> modifier.
    /// </summary>
    public class CircularImageButton : VisualElement
    {
        public const string RootClass = "circularImageButton";
        public const string ImageClass = "circularImageButton__image";
        public const string NoImageOverlayClass = "circularImageButton__noImageOverlay";
        public const string IconClass = "circularImageButton__icon";
        public const string UploadLabelClass = "circularImageButton__uploadLabel";
        public const string HasImageClass = "circularImageButton--hasImage";

        private readonly VisualElement imageContainer;
        private readonly VisualElement noImageOverlay;
        private readonly VisualElement iconElement;
        private readonly Label uploadNewImageLabel;

        public event Action Clicked;

        public CircularImageButton()
        {
            AddToClassList(RootClass);

            focusable = true;
            this.AddManipulator(new Clickable(OnClickedHandler));

            imageContainer = UIToolkitExtensions.CreateVisualElement(this, ImageClass);
            imageContainer.pickingMode = PickingMode.Ignore;

            noImageOverlay = UIToolkitExtensions.CreateVisualElement(this, NoImageOverlayClass);
            noImageOverlay.pickingMode = PickingMode.Ignore;

            iconElement = UIToolkitExtensions.CreateVisualElement(noImageOverlay, IconClass);
            iconElement.pickingMode = PickingMode.Ignore;

            uploadNewImageLabel = UIToolkitExtensions.CreateVisualElement<Label>(noImageOverlay, UploadLabelClass);
            uploadNewImageLabel.text = string.Empty;
            uploadNewImageLabel.style.display = DisplayStyle.None;
            uploadNewImageLabel.pickingMode = PickingMode.Ignore;
        }

        private void OnClickedHandler()
        {
            Clicked?.Invoke();
        }

        public void SetImage(Texture2D texture, bool isDefault = false)
        {
            if (texture != null)
            {
                imageContainer.style.backgroundImage = new StyleBackground(texture);
                UpdateImageState(!isDefault);
            }
        }

        public void SetImage(Sprite sprite, bool isDefault = false)
        {
            if (sprite != null)
            {
                imageContainer.style.backgroundImage = new StyleBackground(sprite);
                UpdateImageState(!isDefault);
            }
        }

        public void SetUploadLabel(string labelText)
        {
            uploadNewImageLabel.text = labelText;
            RefreshUploadLabelVisibility();
        }

        public void ClearImage()
        {
            imageContainer.style.backgroundImage = StyleKeyword.Null;
            UpdateImageState(false);
        }

        public void SetImageTint(Color color)
        {
            imageContainer.style.unityBackgroundImageTintColor = color;
        }

        private void UpdateImageState(bool hasImage)
        {
            EnableInClassList(HasImageClass, hasImage);
            RefreshUploadLabelVisibility();
        }

        private void RefreshUploadLabelVisibility()
        {
            bool hasImage = ClassListContains(HasImageClass);
            uploadNewImageLabel.style.display = hasImage || string.IsNullOrEmpty(uploadNewImageLabel.text)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }
}
