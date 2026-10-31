/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Directions to the next stop: a gradient disc with an arrow that points the way, a title such as
    /// "Stop 3 · 18 m", and a line of detail. On arrival the arrow becomes a check.
    /// </summary>
    /// <remarks>
    /// <para>Call <see cref="SetBearing"/> each frame with the direction to the stop relative to where the camera
    /// faces: 0 is straight ahead, positive turns clockwise. The arrow follows without easing, so it never spins the
    /// long way round.</para>
    /// <para>Props: <c>title</c>, <c>detail</c> (empty hides it), <c>bearing</c>, <c>arrived</c>.</para>
    /// <para>USS classes: <c>wayfinderCard</c> (root), <c>wayfinderCard__disc</c>, <c>wayfinderCard__arrow</c>,
    /// <c>wayfinderCard__check</c>, <c>wayfinderCard__texts</c>, <c>wayfinderCard__title</c>,
    /// <c>wayfinderCard__detail</c>. Modifier: <c>wayfinderCard--arrived</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-border</c>, <c>--surface-fg</c>, <c>--surface-muted</c>,
    /// <c>--c-on-primary</c>, <c>--radius-card</c>, <c>--radius-pill</c>, <c>--text-subtitle</c>,
    /// <c>--text-caption</c>, <c>--font-display</c>, <c>--font-body</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class WayfinderCard : VisualElement
    {
        public const string RootClass = "wayfinderCard";
        public const string DiscClass = "wayfinderCard__disc";
        public const string ArrowClass = "wayfinderCard__arrow";
        public const string CheckClass = "wayfinderCard__check";
        public const string TextsClass = "wayfinderCard__texts";
        public const string TitleClass = "wayfinderCard__title";
        public const string DetailClass = "wayfinderCard__detail";
        public const string ArrivedClass = "wayfinderCard--arrived";

        private readonly VisualElement arrow;
        private readonly Label titleLabel;
        private readonly Label detailLabel;
        private float bearing;
        private bool arrived;

        public VisualElement Arrow => arrow;

        [UxmlAttribute("title")]
        public string Title
        {
            get => titleLabel.text;
            set => titleLabel.text = value ?? string.Empty;
        }

        /// <summary>
        /// A line under the title, such as "Ahead on the left, past the test cell."; empty hides it.
        /// </summary>
        [UxmlAttribute("detail")]
        public string Detail
        {
            get => detailLabel.text;
            set
            {
                detailLabel.text = value ?? string.Empty;
                detailLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Degrees clockwise from straight ahead, normalised to -180 to 180.
        /// </summary>
        [UxmlAttribute("bearing")]
        public float Bearing
        {
            get => bearing;
            set => SetBearing(value);
        }

        /// <summary>
        /// Swaps the arrow for a check.
        /// </summary>
        [UxmlAttribute("arrived")]
        public bool Arrived
        {
            get => arrived;
            set
            {
                arrived = value;
                EnableInClassList(ArrivedClass, arrived);
            }
        }

        public WayfinderCard()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var disc = UIToolkitExtensions.CreateVisualElement(this, DiscClass);
            disc.pickingMode = PickingMode.Ignore;
            disc.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            arrow = UIToolkitExtensions.CreateVisualElement(disc, ArrowClass);
            arrow.pickingMode = PickingMode.Ignore;
            var check = UIToolkitExtensions.CreateVisualElement(disc, CheckClass);
            check.pickingMode = PickingMode.Ignore;

            var texts = UIToolkitExtensions.CreateVisualElement(this, TextsClass);
            texts.pickingMode = PickingMode.Ignore;
            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            titleLabel.pickingMode = PickingMode.Ignore;
            detailLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, DetailClass);
            detailLabel.pickingMode = PickingMode.Ignore;

            Detail = null;
            SetBearing(0f);
        }

        public WayfinderCard(string title, string detail = null)
            : this()
        {
            Title = title;
            Detail = detail;
        }

        /// <summary>
        /// Points the arrow: degrees clockwise from straight ahead, relative to the camera.
        /// </summary>
        public void SetBearing(float degrees)
        {
            bearing = float.IsNaN(degrees) || float.IsInfinity(degrees) ? 0f : Mathf.DeltaAngle(0f, degrees);
            arrow.style.rotate = new Rotate(Angle.Degrees(bearing));
        }
    }
}
