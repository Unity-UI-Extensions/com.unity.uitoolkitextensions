/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Which ThemeTokens pair a <see cref="GradientFill"/> paints.
    /// </summary>
    public enum GradientPreset
    {
        /// <summary>
        /// <c>--grad-brand-start</c> to <c>--grad-brand-end</c>: primary pill buttons, badges.
        /// </summary>
        Brand,

        /// <summary>
        /// <c>--c-navy</c> to <c>--c-navy-2</c>: the launch backdrop.
        /// </summary>
        Navy,
    }

    public enum GradientDirection
    {
        /// <summary>
        /// Start colour on the left, end colour on the right.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Start colour at the top, end colour at the bottom.
        /// </summary>
        Vertical,
    }

    /// <summary>
    /// Paints a two-stop gradient background on its target from ThemeTokens colours. USS has no
    /// gradients, so the manipulator reads the stops from the target's resolved styles and applies a
    /// generated texture as the background image, which the target's border radius clips.
    /// </summary>
    /// <remarks>
    /// <para>Usage: <c>element.AddManipulator(new GradientFill(GradientPreset.Navy, GradientDirection.Vertical));</c></para>
    /// <para>USS classes added to the target: <c>gradientFill</c>, plus the modifier <c>gradientFill--navy</c>
    /// for the navy preset. The stops are the variables <c>--gradient-start</c> and <c>--gradient-end</c>,
    /// declared by those classes from <c>--grad-brand-start</c>/<c>--grad-brand-end</c> or
    /// <c>--c-navy</c>/<c>--c-navy-2</c>. For any other pair, declare the two variables in a rule on the
    /// target that is more specific than <c>.gradientFill</c>.</para>
    /// <para>The texture is rebuilt only when a stop changes, and destroyed when the target leaves its panel.</para>
    /// </remarks>
    public sealed class GradientFill : Manipulator
    {
        public const string RootClass = "gradientFill";
        public const string NavyClass = "gradientFill--navy";

        private const int Resolution = 256;

        private static readonly CustomStyleProperty<Color> StartProperty = new("--gradient-start");
        private static readonly CustomStyleProperty<Color> EndProperty = new("--gradient-end");

        private GradientPreset preset;
        private GradientDirection direction;
        private Texture2D texture;
        private Color startColor;
        private Color endColor;

        public GradientFill(GradientPreset preset = GradientPreset.Brand, GradientDirection direction = GradientDirection.Horizontal)
        {
            this.preset = preset;
            this.direction = direction;
        }

        public GradientPreset Preset
        {
            get => preset;
            set
            {
                preset = value;
                target?.EnableInClassList(NavyClass, preset == GradientPreset.Navy);
            }
        }

        public GradientDirection Direction
        {
            get => direction;
            set
            {
                if (direction == value)
                {
                    return;
                }

                direction = value;
                Rebuild();
            }
        }

        /// <summary>
        /// The start stop last read from USS.
        /// </summary>
        public Color StartColor => startColor;

        /// <summary>
        /// The end stop last read from USS.
        /// </summary>
        public Color EndColor => endColor;

        /// <summary>
        /// The generated texture, or null before styles resolve.
        /// </summary>
        public Texture2D Texture => texture;

        protected override void RegisterCallbacksOnTarget()
        {
            target.AddToClassList(RootClass);
            target.EnableInClassList(NavyClass, preset == GradientPreset.Navy);
            target.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            target.RemoveFromClassList(RootClass);
            target.RemoveFromClassList(NavyClass);
            target.style.backgroundImage = StyleKeyword.Null;
            ReleaseTexture();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (!evt.customStyle.TryGetValue(StartProperty, out var start) || !evt.customStyle.TryGetValue(EndProperty, out var end))
            {
                return;
            }

            if (texture != null && start == startColor && end == endColor)
            {
                return;
            }

            startColor = start;
            endColor = end;
            Rebuild();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            target.style.backgroundImage = StyleKeyword.Null;
            ReleaseTexture();
        }

        private void Rebuild()
        {
            if (target == null || (startColor == default && endColor == default))
            {
                return;
            }

            ReleaseTexture();
            texture = direction == GradientDirection.Horizontal
                ? ProceduralTextureUtility.CreateHorizontalGradient(startColor, endColor, Resolution)
                : ProceduralTextureUtility.CreateVerticalGradient(startColor, endColor, Resolution);
            texture.hideFlags = HideFlags.HideAndDontSave;
            target.style.backgroundImage = new StyleBackground(texture);
        }

        private void ReleaseTexture()
        {
            ProceduralTextureUtility.Release(ref texture);
        }
    }
}
