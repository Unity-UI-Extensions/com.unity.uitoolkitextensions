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

            backgroundBar = new VisualElement();
            backgroundBar.AddToClassList(BackgroundBarClass);

            fillBar = new VisualElement();
            fillBar.AddToClassList(FillBarClass);

            backgroundBar.Add(fillBar);
            Add(backgroundBar);

            RegisterCallback<DetachFromPanelEvent>(_ => DestroyFillTexture());

            UpdateFillDisplay();
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
                UpdateFillDisplay();
            }
        }

        [UxmlAttribute("outer-color")]
        public Color OuterColor
        {
            get => outerColor;
            set
            {
                outerColor = value;
                UpdateFillDisplay();
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

            UpdateFillDisplay();
        }

        public void SetInnerColor(string colorHex)
        {
            InnerColor = ColorUtility.TryParseHtmlString(colorHex, out var parsed) ? parsed : Color.blue;
        }

        public void SetOuterColor(string colorHex)
        {
            OuterColor = ColorUtility.TryParseHtmlString(colorHex, out var parsed) ? parsed : Color.cyan;
        }

        public void SetGradientColors(string innerColorHex, string outerColorHex)
        {
            innerColor = ColorUtility.TryParseHtmlString(innerColorHex, out var parsedInner) ? parsedInner : Color.blue;
            outerColor = ColorUtility.TryParseHtmlString(outerColorHex, out var parsedOuter) ? parsedOuter : Color.cyan;
            UpdateFillDisplay();
        }

        private void UpdateFillDisplay()
        {
            fillBar.style.width = Length.Percent(NormalizedProgress * 100f);
            DestroyFillTexture();
            fillTexture = ProceduralTextureUtility.CreateHorizontalGradient(innerColor, outerColor);
            fillBar.style.backgroundImage = new StyleBackground(fillTexture);
        }

        private void DestroyFillTexture()
        {
            if (fillTexture != null)
            {
                UnityEngine.Object.Destroy(fillTexture);
                fillTexture = null;
            }
        }
    }
}