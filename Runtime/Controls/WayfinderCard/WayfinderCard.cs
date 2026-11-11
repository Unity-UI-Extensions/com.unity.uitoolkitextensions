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
        /// A line under the title, such as "Ahead on the left, past the test cell." Empty hides it.
        /// </summary>
        [UxmlAttribute("detail")]
        public string Detail
        {
            get => detailLabel.text;
            set
            {
                detailLabel.SetTextOrHide(value);
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

            var disc = UIToolkitExtensions.CreateVisualElement(this, DiscClass).IgnorePicking();
            disc.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            arrow = UIToolkitExtensions.CreateVisualElement(disc, ArrowClass).IgnorePicking();
            var check = UIToolkitExtensions.CreateVisualElement(disc, CheckClass).IgnorePicking();

            var texts = UIToolkitExtensions.CreateVisualElement(this, TextsClass).IgnorePicking();
            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass).IgnorePicking();
            detailLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, DetailClass).IgnorePicking();

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
