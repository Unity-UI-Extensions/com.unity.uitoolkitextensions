/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Gives an element true pill ends: each corner radius is half its shorter side, kept up to date as it resizes.
    /// </summary>
    /// <remarks>
    /// <para>UI Toolkit clamps a corner radius on each axis separately, to half the width and half the height. CSS
    /// scales the radii together instead. So a large radius such as <c>--radius-pill</c> (50 px) on a 44 px high,
    /// wide element draws 50 by 22 px elliptical ends, not semicircles, and a border looks thicker at the tips.
    /// <see cref="MakePill{T}"/> sets the radius to exactly half the shorter side whenever the element's size changes,
    /// so the ends are semicircles at any height and text scale.</para>
    /// <para>The radius is set inline, so it wins over USS. <see cref="ClearPill"/> stops it and returns the radius to
    /// the stylesheet, for an element that stops being a pill (a PillButton switched to its Custom variant).</para>
    /// <para>USS class: <c>uitk-pill</c>, added to every element made a pill.</para>
    /// </remarks>
    public static class PillShape
    {
        public const string PillClass = "uitk-pill";

        /// <summary>
        /// Makes the element a pill (or a circle when square). Safe to call more than once.
        /// </summary>
        public static T MakePill<T>(this T element) where T : VisualElement
        {
            if (element == null || element.ClassListContains(PillClass))
            {
                return element;
            }

            element.AddToClassList(PillClass);
            element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            Apply(element);
            return element;
        }

        /// <summary>
        /// Stops <see cref="MakePill{T}"/> and hands the corner radii back to the stylesheet.
        /// </summary>
        public static void ClearPill(this VisualElement element)
        {
            if (element == null || !element.ClassListContains(PillClass))
            {
                return;
            }

            element.RemoveFromClassList(PillClass);
            element.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            element.style.borderTopLeftRadius = StyleKeyword.Null;
            element.style.borderTopRightRadius = StyleKeyword.Null;
            element.style.borderBottomLeftRadius = StyleKeyword.Null;
            element.style.borderBottomRightRadius = StyleKeyword.Null;
        }

        /// <summary>
        /// The pill radius for a size: half the shorter side, or 0 before layout.
        /// </summary>
        public static float RadiusFor(Vector2 size)
        {
            if (float.IsNaN(size.x) || float.IsNaN(size.y) || size.x <= 0f || size.y <= 0f)
            {
                return 0f;
            }

            return Mathf.Min(size.x, size.y) * 0.5f;
        }

        private static void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.currentTarget is VisualElement element && evt.oldRect.size != evt.newRect.size)
            {
                Apply(element);
            }
        }

        private static void Apply(VisualElement element)
        {
            var radius = RadiusFor(element.layout.size);
            if (radius <= 0f)
            {
                return;
            }

            var current = element.style.borderTopLeftRadius;
            if (current.keyword == StyleKeyword.Undefined && Mathf.Approximately(current.value.value, radius))
            {
                return;
            }

            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
