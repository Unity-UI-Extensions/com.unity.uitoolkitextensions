/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A determinate progress ring with a centre value and caption, such as scan coverage, "4 of 12" or a
    /// download. The arc is drawn with <see cref="Painter2D"/> in the token colours.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>value</c> (0 to 1), <c>value-text</c> (empty shows the percentage), <c>label</c>,
    /// <c>size</c>, <c>thickness</c>, <c>show-complete</c> (default on; off keeps the fill colour at 1, for a
    /// countdown that starts full).</para>
    /// <para>USS classes: <c>progressRing</c> (root), <c>progressRing__centre</c>, <c>progressRing__value</c>,
    /// <c>progressRing__label</c>. Modifier: <c>progressRing--complete</c> (value 1, while <c>show-complete</c>).</para>
    /// <para>Variables: <c>--progressRing-track</c> (default <c>--surface-border</c>), <c>--progressRing-fill</c>
    /// (default <c>--surface-accent</c>; <c>--c-success</c> when complete).</para>
    /// <para>Tokens read: <c>--surface-border</c>, <c>--surface-accent</c>, <c>--c-success</c>, <c>--surface-fg</c>,
    /// <c>--surface-muted</c>, <c>--text-title</c>, <c>--text-caption</c>, <c>--font-display</c>, <c>--font-body</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ProgressRing : VisualElement
    {
        public const string RootClass = "progressRing";
        public const string CentreClass = "progressRing__centre";
        public const string ValueClass = "progressRing__value";
        public const string LabelClass = "progressRing__label";
        public const string CompleteClass = "progressRing--complete";

        private static readonly CustomStyleProperty<Color> TrackProperty = new("--progressRing-track");
        private static readonly CustomStyleProperty<Color> FillProperty = new("--progressRing-fill");

        private readonly Label valueLabel;
        private readonly Label captionLabel;
        private float value;
        private bool showComplete = true;
        private string valueText;
        private float size = 96f;
        private float thickness = 8f;
        private Color trackColor = new(1f, 1f, 1f, 0.2f);
        private Color fillColor = Color.white;

        [UxmlAttribute("value")]
        public float Value
        {
            get => value;
            set
            {
                this.value = Mathf.Clamp01(float.IsNaN(value) ? 0f : value);
                EnableInClassList(CompleteClass, showComplete && this.value >= 1f);
                UpdateValueText();
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Switches the fill to the success colour at 1. Off keeps the fill colour, as for a countdown.
        /// </summary>
        [UxmlAttribute("show-complete")]
        public bool ShowComplete
        {
            get => showComplete;
            set
            {
                showComplete = value;
                Value = this.value;
            }
        }

        /// <summary>
        /// Text in the centre, such as "4 of 12". Empty shows the percentage.
        /// </summary>
        [UxmlAttribute("value-text")]
        public string ValueText
        {
            get => valueText;
            set
            {
                valueText = value;
                UpdateValueText();
            }
        }

        /// <summary>
        /// Caption under the value, such as "coverage".
        /// </summary>
        [UxmlAttribute("label")]
        public string Label
        {
            get => captionLabel.text;
            set
            {
                captionLabel.text = value ?? string.Empty;
                captionLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("size")]
        public float Size
        {
            get => size;
            set
            {
                size = Mathf.Max(8f, value);
                style.width = size;
                style.height = size;
            }
        }

        [UxmlAttribute("thickness")]
        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Max(1f, value);
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Colours last read from USS; exposed for tests.
        /// </summary>
        public Color TrackColor => trackColor;
        public Color FillColor => fillColor;

        public ProgressRing()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            var centre = UIToolkitExtensions.CreateVisualElement(this, CentreClass);
            centre.pickingMode = PickingMode.Ignore;
            valueLabel = UIToolkitExtensions.CreateVisualElement<Label>(centre, ValueClass);
            captionLabel = UIToolkitExtensions.CreateVisualElement<Label>(centre, LabelClass);

            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            Size = size;
            Label = null;
            Value = 0f;
        }

        private void UpdateValueText()
        {
            valueLabel.text = string.IsNullOrEmpty(valueText) ? $"{Mathf.RoundToInt(value * 100f)}%" : valueText;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(TrackProperty, out var track))
            {
                trackColor = track;
            }

            if (evt.customStyle.TryGetValue(FillProperty, out var fill))
            {
                fillColor = fill;
            }

            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var radius = (Mathf.Min(rect.width, rect.height) - thickness) * 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            var centre = rect.center;
            var painter = context.painter2D;
            painter.lineWidth = thickness;
            painter.lineCap = LineCap.Round;

            painter.strokeColor = trackColor;
            painter.BeginPath();
            painter.Arc(centre, radius, 0f, 360f);
            painter.Stroke();

            if (value <= 0f)
            {
                return;
            }

            // Start at twelve o'clock and run clockwise.
            painter.strokeColor = fillColor;
            painter.BeginPath();
            painter.Arc(centre, radius, -90f, -90f + (360f * value));
            painter.Stroke();
        }
    }
}
