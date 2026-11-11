/// Credit SimonDarksideJ

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A row of N segments with a sliding selection indicator, themed with <see cref="ThemeTokens"/>.
    /// It is a <see cref="QuadrantStepper"/> under a general-purpose name, with the same API, events
    /// and child classes.
    /// </summary>
    /// <remarks>
    /// <para>The root carries the class <c>segmentedControl</c> alongside <c>quadrantStepper</c>.
    /// Children and modifiers are the same as on <see cref="QuadrantStepper"/>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class SegmentedControl : QuadrantStepper
    {
        public new const string RootClass = "segmentedControl";

        public SegmentedControl()
            : this(new[] { "One", "Two", "Three" })
        { }

        public SegmentedControl(IReadOnlyList<string> options)
            : base(options)
        {
            AddToClassList(RootClass);
            this.UseThemeTokens();

            // The track and the sliding overlay are pills at any height.
            this.MakePill();
            this.Q(className: OverlayClass)?.MakePill();
        }
    }
}
