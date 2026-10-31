/// Credit SimonDarksideJ

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A row of N segments with a sliding selection indicator, themed with <see cref="ThemeTokens"/>.
    /// The extended <see cref="QuadrantStepper"/> under its general-purpose name: same API, events
    /// and child classes, plus icons, disabled segments, content sizing and keyboard navigation.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>options</c>, <c>selected-index</c>, <c>sizing</c> (Equal or Content).
    /// Event: <see cref="QuadrantStepper.SelectionChanged"/> (index, text).</para>
    /// <para>USS classes: <c>segmentedControl</c> on the root, alongside <c>quadrantStepper</c>;
    /// children and modifiers as documented on <see cref="QuadrantStepper"/>.</para>
    /// <para>Tokens read: <c>--surface-raised</c>, <c>--surface-fg</c>, <c>--surface-border</c>,
    /// <c>--surface-accent</c>, <c>--c-primary</c>, <c>--c-on-primary</c>, <c>--radius-pill</c>,
    /// <c>--space-2</c>, <c>--space-3</c>, <c>--text-label</c>, <c>--font-body</c>.</para>
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
