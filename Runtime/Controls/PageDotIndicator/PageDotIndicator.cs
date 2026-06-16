/// Credit SimonDarksideJ

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A row of pagination dots where all dots up to and including the current page are marked completed.
    /// Colors can be driven by USS classes or overridden inline via <see cref="SetColors"/>.
    /// </summary>
    public class PageDotIndicator : VisualElement
    {
        public const string RootClass = "pageDotIndicator";
        public const string DotsContainerClass = "pageDotIndicator__dotsContainer";
        public const string DotClass = "pageDotIndicator__dot";
        public const string DotCompletedClass = "pageDotIndicator__dot--completed";

        private readonly VisualElement dotsContainer;
        private readonly List<VisualElement> dots = new();

        private string completedColorHex = "#FFFFFF";
        private string pendingColorHex = "#CCCCCC";
        private int currentPage;
        private int totalPages = 1;
        private bool overrideColors;

        public PageDotIndicator()
        {
            AddToClassList(RootClass);
            dotsContainer = UIToolkitExtensions.CreateVisualElement(this, DotsContainerClass);
        }

        public int CurrentPage => currentPage;
        public int TotalPages => totalPages;
        public float NormalizedProgress => totalPages > 0 ? (float)(currentPage + 1) / totalPages : 0f;

        public void SetProgress(int currentPage, int totalPages)
        {
            if (totalPages <= 0)
            {
                Debug.LogWarning("[PageDotIndicator] totalPages must be greater than 0.");
                totalPages = 1;
            }

            this.currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
            this.totalPages = totalPages;

            UpdateDots();
        }

        public void SetCompletedColor(string colorHex)
        {
            completedColorHex = colorHex;
            overrideColors = true;
            UpdateDots();
        }

        public void SetPendingColor(string colorHex)
        {
            pendingColorHex = colorHex;
            overrideColors = true;
            UpdateDots();
        }

        public void SetColors(string completedColorHex, string pendingColorHex)
        {
            this.completedColorHex = completedColorHex;
            this.pendingColorHex = pendingColorHex;
            overrideColors = true;
            UpdateDots();
        }

        private void UpdateDots()
        {
            if (dots.Count != totalPages)
            {
                dotsContainer.Clear();
                dots.Clear();

                for (int i = 0; i < totalPages; i++)
                {
                    var dot = UIToolkitExtensions.CreateVisualElement(dotsContainer, DotClass);
                    dots.Add(dot);
                }
            }

            for (int i = 0; i < dots.Count; i++)
            {
                bool isCompleted = i <= currentPage;
                dots[i].EnableInClassList(DotCompletedClass, isCompleted);

                if (overrideColors)
                {
                    dots[i].style.backgroundColor = ParseColor(isCompleted ? completedColorHex : pendingColorHex);
                }
            }
        }

        private static Color ParseColor(string colorHex)
        {
            return ColorUtility.TryParseHtmlString(colorHex, out Color color) ? color : Color.white;
        }
    }
}
