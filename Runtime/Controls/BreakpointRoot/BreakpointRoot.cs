/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Width classes of a <see cref="BreakpointRoot"/>.
    /// </summary>
    public enum Breakpoint
    {
        /// <summary>
        /// Phones in portrait: under <see cref="BreakpointRoot.MediumMin"/>.
        /// </summary>
        Compact,

        /// <summary>
        /// Phones in landscape, small tablets.
        /// </summary>
        Medium,

        /// <summary>
        /// Tablets and desktop: at least <see cref="BreakpointRoot.ExpandedMin"/>.
        /// </summary>
        Expanded,
    }

    /// <summary>
    /// A container that classifies its own width (compact, medium, expanded) and shape (portrait, landscape) and puts
    /// matching classes on itself, so descendants' USS can change layout, such as docking a sheet at medium and above.
    /// </summary>
    /// <remarks>
    /// <para><c>medium-min</c> defaults to 600 and <c>expanded-min</c> to 840, in panel pixels. The root fills its parent.
    /// Descendant rules use the classes as ancestors, for example
    /// <c>.breakpointRoot--expanded .myList { flex-direction: row; }</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class BreakpointRoot : VisualElement
    {
        public const string RootClass = "breakpointRoot";
        public const string CompactClass = "breakpointRoot--compact";
        public const string MediumClass = "breakpointRoot--medium";
        public const string ExpandedClass = "breakpointRoot--expanded";
        public const string PortraitClass = "breakpointRoot--portrait";
        public const string LandscapeClass = "breakpointRoot--landscape";

        private float mediumMin = 600f;
        private float expandedMin = 840f;
        private Breakpoint current = Breakpoint.Compact;
        private bool landscape;
        private bool classified;

        public event Action<Breakpoint> BreakpointChanged;

        /// <summary>
        /// Raised when the shape changes: true for landscape.
        /// </summary>
        public event Action<bool> OrientationChanged;

        public Breakpoint Current => current;

        public bool IsLandscape => landscape;

        [UxmlAttribute("medium-min")]
        public float MediumMin
        {
            get => mediumMin;
            set
            {
                mediumMin = Mathf.Max(0f, value);
                Classify();
            }
        }

        [UxmlAttribute("expanded-min")]
        public float ExpandedMin
        {
            get => expandedMin;
            set
            {
                expandedMin = Mathf.Max(0f, value);
                Classify();
            }
        }

        public BreakpointRoot()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;
            RegisterCallback<GeometryChangedEvent>(_ => Classify());
            ApplyClasses();
        }

        /// <summary>
        /// The breakpoint for a width, with this root's thresholds.
        /// </summary>
        public Breakpoint Classify(float width) =>
            width >= expandedMin ? Breakpoint.Expanded : width >= mediumMin ? Breakpoint.Medium : Breakpoint.Compact;

        private void Classify()
        {
            var size = layout.size;
            if (float.IsNaN(size.x) || float.IsNaN(size.y) || size.x <= 0f)
            {
                return;
            }

            var next = Classify(size.x);
            var nextLandscape = size.x > size.y;
            var breakpointChanged = !classified || next != current;
            var orientationChanged = !classified || nextLandscape != landscape;
            current = next;
            landscape = nextLandscape;
            classified = true;
            ApplyClasses();

            if (breakpointChanged)
            {
                BreakpointChanged?.Invoke(current);
            }

            if (orientationChanged)
            {
                OrientationChanged?.Invoke(landscape);
            }
        }

        private void ApplyClasses()
        {
            EnableInClassList(CompactClass, current == Breakpoint.Compact);
            EnableInClassList(MediumClass, current == Breakpoint.Medium);
            EnableInClassList(ExpandedClass, current == Breakpoint.Expanded);
            EnableInClassList(PortraitClass, !landscape);
            EnableInClassList(LandscapeClass, landscape);
        }
    }
}
