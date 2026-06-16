/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A rotating loading spinner. Rotation is driven by a scheduled update;
    /// visibility and fade are driven by USS classes.
    /// </summary>
    public class LoadingIcon : VisualElement
    {
        public const string RootClass = "loadingIcon";
        public const string LoadingImageClass = "loadingIcon__image";
        public const string AnimatingClass = "loadingIcon--animating";
        public const string VisibleClass = "loadingIcon--visible";

        private readonly Image loadingImage;
        private float rotationSpeed = 1f;
        private bool isAnimating;
        private IVisualElementScheduledItem animationScheduledItem;

        public LoadingIcon()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList(RootClass);

            loadingImage = UIToolkitExtensions.CreateVisualElement<Image>(this, LoadingImageClass);
            loadingImage.pickingMode = PickingMode.Ignore;
        }

        public void SetIcon(Texture2D texture)
        {
            if (texture != null)
            {
                loadingImage.image = texture;
            }
        }

        /// <param name="customSpeed">Duration of one full rotation in seconds.</param>
        /// <param name="blockInteraction">When true, this element captures pointer events.</param>
        public void PlayLoading(float customSpeed = 1f, bool blockInteraction = false)
        {
            if (isAnimating)
            {
                return;
            }

            rotationSpeed = Mathf.Max(0.1f, customSpeed);
            isAnimating = true;
            AddToClassList(AnimatingClass);
            AddToClassList(VisibleClass);

            pickingMode = blockInteraction ? PickingMode.Position : PickingMode.Ignore;

            AnimateRotation();
        }

        public void StopLoading()
        {
            isAnimating = false;
            RemoveFromClassList(AnimatingClass);
            RemoveFromClassList(VisibleClass);

            animationScheduledItem?.Pause();
            animationScheduledItem = null;

            loadingImage.style.rotate = new Rotate(new Angle(0f, AngleUnit.Degree));

            pickingMode = PickingMode.Ignore;
        }

        private void AnimateRotation()
        {
            animationScheduledItem?.Pause();

            float degreesPerMs = 360f / (rotationSpeed * 1000f);
            float startTime = Time.realtimeSinceStartup * 1000f;

            animationScheduledItem = schedule.Execute(() =>
            {
                if (!isAnimating)
                {
                    return;
                }

                float elapsed = (Time.realtimeSinceStartup * 1000f) - startTime;
                float rotation = (elapsed * degreesPerMs) % 360f;
                loadingImage.style.rotate = new Rotate(new Angle(rotation, AngleUnit.Degree));
            }).Every(16);
        }
    }
}
