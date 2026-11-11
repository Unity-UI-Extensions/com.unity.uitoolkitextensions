/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A horizontal progress bar that fills proportionally to steps completed.
    /// The fill is rendered as a generated gradient texture between two configurable hex colors.
    /// </summary>
    [UxmlElement]
    public partial class StepProgressBar : VisualElement
    {
        public const string RootClass = "stepProgressBar";
        public const string BackgroundBarClass = "stepProgressBar__background";
        public const string FillBarClass = "stepProgressBar__fill";

        private readonly VisualElement backgroundBar;
        private readonly VisualElement fillBar;

        private Color innerColor = Color.blue;
        private Color outerColor = Color.cyan;
        private int currentSteps;
        private int maxSteps = 1;
        private Texture2D fillTexture;

        public StepProgressBar()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            backgroundBar = UIToolkitExtensions.CreateVisualElement(this, BackgroundBarClass);
            fillBar = UIToolkitExtensions.CreateVisualElement(backgroundBar, FillBarClass);

            RegisterCallback<DetachFromPanelEvent>(_ => DestroyFillTexture());
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (fillTexture == null)
                {
                    RebuildFillTexture();
                }
            });

            UpdateFillWidth();
            RebuildFillTexture();
        }

        public float NormalizedProgress => maxSteps > 0 ? (float)currentSteps / maxSteps : 0f;

        [UxmlAttribute("current-steps")]
        public int CurrentSteps
        {
            get => currentSteps;
            set => SetProgress(value, maxSteps);
        }

        [UxmlAttribute("max-steps")]
        public int MaxSteps
        {
            get => maxSteps;
            set => SetProgress(currentSteps, value);
        }

        [UxmlAttribute("inner-color")]
        public Color InnerColor
        {
            get => innerColor;
            set
            {
                innerColor = value;
                RebuildFillTexture();
            }
        }

        [UxmlAttribute("outer-color")]
        public Color OuterColor
        {
            get => outerColor;
            set
            {
                outerColor = value;
                RebuildFillTexture();
            }
        }

        public void SetProgress(int steps, int maxSteps)
        {
            if (maxSteps <= 0)
            {
                Debug.LogWarning("[StepProgressBar] maxSteps must be greater than 0.");
                maxSteps = 1;
            }

            currentSteps = Mathf.Clamp(steps, 0, maxSteps);
            this.maxSteps = maxSteps;

            UpdateFillWidth();
        }

        public void SetInnerColor(string colorHex) => InnerColor = UIToolkitExtensions.ParseColorOr(colorHex, Color.blue);

        public void SetOuterColor(string colorHex) => OuterColor = UIToolkitExtensions.ParseColorOr(colorHex, Color.cyan);

        public void SetGradientColors(string innerColorHex, string outerColorHex)
        {
            innerColor = UIToolkitExtensions.ParseColorOr(innerColorHex, Color.blue);
            outerColor = UIToolkitExtensions.ParseColorOr(outerColorHex, Color.cyan);
            RebuildFillTexture();
        }

        private void UpdateFillWidth() => fillBar.style.width = Length.Percent(NormalizedProgress * 100f);

        private void RebuildFillTexture()
        {
            DestroyFillTexture();
            fillTexture = ProceduralTextureUtility.CreateHorizontalGradient(innerColor, outerColor);
            fillBar.style.backgroundImage = new StyleBackground(fillTexture);
        }

        private void DestroyFillTexture()
        {
            if (fillTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(fillTexture);
                fillTexture = null;
            }
        }
    }
}