/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A number with a label, a unit and step buttons, for inspectors in a desktop editor. Drag the label
    /// sideways to scrub; type a value; or press minus and plus.
    /// </summary>
    /// <remarks>
    /// <para>The label is also the scrub handle. <c>step</c> defaults to 0.1, <c>decimals</c> to 2 and <c>show-steppers</c>
    /// to true. <c>min</c> and <c>max</c> are NaN for no limit. <c>scrub-pixels</c> is the pixels of drag per step, default 4.
    /// The input is a <see cref="FloatField"/>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class NumericField : VisualElement
    {
        public const string RootClass = "numericField";
        public const string LabelClass = "numericField__label";
        public const string BoxClass = "numericField__box";
        public const string InputClass = "numericField__input";
        public const string UnitClass = "numericField__unit";
        public const string StepClass = "numericField__step";
        public const string MinusClass = "numericField__minus";
        public const string PlusClass = "numericField__plus";
        public const string ScrubbingClass = "numericField--scrubbing";

        private readonly Label label;
        private readonly FloatField input;
        private readonly Label unit;
        private readonly Button minus;
        private readonly Button plus;
        private float value;
        private float step = 0.1f;
        private float min = float.NaN;
        private float max = float.NaN;
        private int decimals = 2;
        private float scrubPixels = 4f;
        private int scrubPointer = PointerId.invalidPointerId;
        private float scrubStartX;
        private float scrubStartValue;

        /// <summary>
        /// Raised when the value changes through typing, scrubbing, the steppers or <see cref="Value"/>.
        /// </summary>
        public event Action<float> ValueChanged;

        public FloatField Input => input;

        [UxmlAttribute("label")]
        public string Label
        {
            get => label.text;
            set
            {
                label.SetTextOrHide(value);
            }
        }

        [UxmlAttribute("value")]
        public float Value
        {
            get => value;
            set => SetValue(value, notify: true);
        }

        [UxmlAttribute("step")]
        public float Step
        {
            get => step;
            set => step = Mathf.Max(0f, value);
        }

        [UxmlAttribute("min")]
        public float Min
        {
            get => min;
            set
            {
                min = value;
                SetValue(this.value, notify: false);
            }
        }

        [UxmlAttribute("max")]
        public float Max
        {
            get => max;
            set
            {
                max = value;
                SetValue(this.value, notify: false);
            }
        }

        [UxmlAttribute("unit")]
        public string Unit
        {
            get => unit.text;
            set
            {
                unit.SetTextOrHide(value);
            }
        }

        [UxmlAttribute("decimals")]
        public int Decimals
        {
            get => decimals;
            set
            {
                decimals = Mathf.Clamp(value, 0, 6);
                input.formatString = "F" + decimals;
                SetValue(this.value, notify: false);
            }
        }

        [UxmlAttribute("show-steppers")]
        public bool ShowSteppers
        {
            get => minus.style.display != DisplayStyle.None;
            set
            {
                minus.SetDisplayed(value);
                plus.SetDisplayed(value);
            }
        }

        [UxmlAttribute("scrub-pixels")]
        public float ScrubPixels
        {
            get => scrubPixels;
            set => scrubPixels = Mathf.Max(1f, value);
        }

        public NumericField()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            label = UIToolkitExtensions.CreateVisualElement<Label>(this, LabelClass);
            label.RegisterCallback<PointerDownEvent>(OnScrubStart);
            label.RegisterCallback<PointerMoveEvent>(OnScrubMove);
            label.RegisterCallback<PointerUpEvent>(OnScrubEnd);
            label.RegisterCallback<PointerCaptureOutEvent>(_ => EndScrub());

            var box = UIToolkitExtensions.CreateVisualElement(this, BoxClass);
            minus = UIToolkitExtensions.CreateButton(box, "−", () => Nudge(-1), className: StepClass);
            minus.AddToClassList(MinusClass);
            input = new FloatField();
            input.AddToClassList(InputClass);
            input.isDelayed = true;
            input.RegisterValueChangedCallback(evt => SetValue(evt.newValue, notify: true));
            box.Add(input);
            unit = UIToolkitExtensions.CreateVisualElement<Label>(box, UnitClass);
            plus = UIToolkitExtensions.CreateButton(box, "+", () => Nudge(1), className: StepClass);
            plus.AddToClassList(PlusClass);

            Label = null;
            Unit = null;
            Decimals = decimals;
        }

        public NumericField(string labelText, float initial, string unitText = null)
            : this()
        {
            Label = labelText;
            Unit = unitText;
            SetValueWithoutNotify(initial);
        }

        public void SetValueWithoutNotify(float newValue) => SetValue(newValue, notify: false);

        /// <summary>
        /// Moves the value by whole steps, as the steppers do.
        /// </summary>
        public void Nudge(int steps) => SetValue(value + (steps * step), notify: true);

        private void SetValue(float newValue, bool notify)
        {
            if (float.IsNaN(newValue) || float.IsInfinity(newValue))
            {
                newValue = value;
            }

            if (!float.IsNaN(min))
            {
                newValue = Mathf.Max(min, newValue);
            }

            if (!float.IsNaN(max))
            {
                newValue = Mathf.Min(max, newValue);
            }

            newValue = (float)Math.Round(newValue, decimals);
            var changed = !Mathf.Approximately(newValue, value);
            value = newValue;
            input.SetValueWithoutNotify(value);
            if (changed && notify)
            {
                ValueChanged?.Invoke(value);
            }
        }

        private void OnScrubStart(PointerDownEvent evt)
        {
            if (evt.button != 0 || scrubPointer != PointerId.invalidPointerId || !enabledInHierarchy)
            {
                return;
            }

            scrubPointer = evt.pointerId;
            scrubStartX = evt.position.x;
            scrubStartValue = value;
            label.CapturePointer(scrubPointer);
            AddToClassList(ScrubbingClass);
            evt.StopPropagation();
        }

        private void OnScrubMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != scrubPointer)
            {
                return;
            }

            var steps = Mathf.RoundToInt((evt.position.x - scrubStartX) / scrubPixels);
            SetValue(scrubStartValue + (steps * step), notify: true);
        }

        private void OnScrubEnd(PointerUpEvent evt)
        {
            if (evt.pointerId == scrubPointer)
            {
                EndScrub();
            }
        }

        private void EndScrub()
        {
            var id = scrubPointer;
            scrubPointer = PointerId.invalidPointerId;
            RemoveFromClassList(ScrubbingClass);
            if (id != PointerId.invalidPointerId && label.HasPointerCapture(id))
            {
                label.ReleasePointer(id);
            }
        }
    }

    /// <summary>
    /// Position, rotation and scale as <see cref="NumericField"/>s, three to a row, with uniform
    /// scale by default. The non-gesture route for moving content.
    /// </summary>
    /// <remarks>
    /// <para><c>uniform-scale</c> defaults to true: one scale field. The steps default to 0.05 m for position, 5° for
    /// rotation and 0.05 for scale. The section labels show in capitals. <see cref="Rotation"/> is in Euler degrees.</para>
    /// </remarks>
    [UxmlElement]
    public partial class TransformGroup : VisualElement
    {
        public const string RootClass = "transformGroup";
        public const string SectionClass = "transformGroup__section";
        public const string HeadingClass = "transformGroup__heading";
        public const string RowClass = "transformGroup__row";
        public const string FieldClass = "transformGroup__field";

        private readonly NumericField[] position = new NumericField[3];
        private readonly NumericField[] rotation = new NumericField[3];
        private readonly NumericField[] scale = new NumericField[3];
        private readonly NumericField uniformScale;
        private readonly Label positionHeading;
        private readonly Label rotationHeading;
        private readonly Label scaleHeading;
        private readonly VisualElement scaleAxes;
        private bool uniform = true;
        private bool applying;

        /// <summary>
        /// Raised when any field changes: position, rotation (Euler degrees) and scale.
        /// </summary>
        public event Action<Vector3, Vector3, Vector3> PoseChanged;

        public Vector3 Position => Read(position);

        public Vector3 Rotation => Read(rotation);

        public Vector3 Scale => uniform ? Vector3.one * uniformScale.Value : Read(scale);

        public NumericField UniformScaleField => uniformScale;

        [UxmlAttribute("uniform-scale")]
        public bool UniformScale
        {
            get => uniform;
            set
            {
                uniform = value;
                uniformScale.SetDisplayed(uniform);
                scaleAxes.SetDisplayed(!uniform);
            }
        }

        [UxmlAttribute("position-step")]
        public float PositionStep
        {
            get => position[0].Step;
            set => SetStep(position, value);
        }

        [UxmlAttribute("rotation-step")]
        public float RotationStep
        {
            get => rotation[0].Step;
            set => SetStep(rotation, value);
        }

        [UxmlAttribute("scale-step")]
        public float ScaleStep
        {
            get => uniformScale.Step;
            set
            {
                SetStep(scale, value);
                uniformScale.Step = value;
            }
        }

        [UxmlAttribute("position-label")]
        public string PositionLabel
        {
            get => positionHeading.text;
            set => positionHeading.text = (value ?? string.Empty).ToUpperInvariant();
        }

        [UxmlAttribute("rotation-label")]
        public string RotationLabel
        {
            get => rotationHeading.text;
            set => rotationHeading.text = (value ?? string.Empty).ToUpperInvariant();
        }

        [UxmlAttribute("scale-label")]
        public string ScaleLabel
        {
            get => scaleHeading.text;
            set => scaleHeading.text = (value ?? string.Empty).ToUpperInvariant();
        }

        /// <summary>
        /// The field for one axis (0 x, 1 y, 2 z) of position, rotation or scale, for tests and focus.
        /// </summary>
        public NumericField PositionField(int axis) => position[axis];

        public NumericField RotationField(int axis) => rotation[axis];

        public NumericField ScaleField(int axis) => scale[axis];

        public TransformGroup()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            positionHeading = CreateSection(position, "m", 0.05f, out _);
            rotationHeading = CreateSection(rotation, "°", 5f, out _);
            scaleHeading = CreateSection(scale, null, 0.05f, out scaleAxes);
            uniformScale = new NumericField("XYZ", 1f) { Step = 0.05f, Min = 0.01f };
            uniformScale.AddToClassList(FieldClass);
            uniformScale.ValueChanged += _ => RaisePose();
            scaleAxes.parent.Add(uniformScale);
            foreach (var field in scale)
            {
                field.Min = 0.01f;
                field.SetValueWithoutNotify(1f);
            }

            PositionLabel = "Position";
            RotationLabel = "Rotation";
            ScaleLabel = "Scale";
            UniformScale = true;
        }

        /// <summary>
        /// Sets every field without raising <see cref="PoseChanged"/>.
        /// </summary>
        public void SetPose(Vector3 newPosition, Vector3 newRotation, Vector3 newScale)
        {
            applying = true;
            Write(position, newPosition);
            Write(rotation, newRotation);
            Write(scale, newScale);
            uniformScale.SetValueWithoutNotify(newScale.x);
            applying = false;
        }

        private static readonly string[] AxisNames = { "X", "Y", "Z" };

        private Label CreateSection(NumericField[] fields, string unitText, float stepSize, out VisualElement row)
        {
            var section = UIToolkitExtensions.CreateVisualElement(this, SectionClass);
            var heading = UIToolkitExtensions.CreateVisualElement<Label>(section, HeadingClass);
            row = UIToolkitExtensions.CreateVisualElement(section, RowClass);
            for (var i = 0; i < 3; i++)
            {
                var field = new NumericField(AxisNames[i], 0f, unitText) { Step = stepSize, ShowSteppers = false };
                field.AddToClassList(FieldClass);
                field.ValueChanged += _ => RaisePose();
                row.Add(field);
                fields[i] = field;
            }

            return heading;
        }

        private void RaisePose()
        {
            if (!applying)
            {
                PoseChanged?.Invoke(Position, Rotation, Scale);
            }
        }

        private static Vector3 Read(NumericField[] fields) => new(fields[0].Value, fields[1].Value, fields[2].Value);

        private static void Write(NumericField[] fields, Vector3 values)
        {
            fields[0].SetValueWithoutNotify(values.x);
            fields[1].SetValueWithoutNotify(values.y);
            fields[2].SetValueWithoutNotify(values.z);
        }

        private static void SetStep(NumericField[] fields, float value)
        {
            foreach (var field in fields)
            {
                field.Step = value;
            }
        }
    }
}
