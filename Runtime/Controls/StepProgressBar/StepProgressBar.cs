/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A horizontal progress bar that fills proportionally to steps completed.
    /// The fill is rendered as a generated gradient texture between two configurable hex colors.
    /// </summary>
    public class StepProgressBar : VisualElement
    {
        public const string RootClass = "stepProgressBar";
        public const string BackgroundBarClass = "stepProgressBar__background";
        public const string FillBarClass = "stepProgressBar__fill";

        private readonly VisualElement backgroundBar;
        private readonly VisualElement fillBar;

        private string innerColorHex = "#0000FF";
        private string outerColorHex = "#00FFFF";
        private int currentSteps;
        private int maxSteps = 1;
        private Texture2D fillTexture;

        public StepProgressBar()
        {
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
        public int CurrentSteps => currentSteps;
        public int MaxSteps => maxSteps;

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
            innerColorHex = colorHex;
            UpdateFillDisplay();
        }

        public void SetOuterColor(string colorHex)
        {
            outerColorHex = colorHex;
            UpdateFillDisplay();
        }

        public void SetGradientColors(string innerColorHex, string outerColorHex)
        {
            this.innerColorHex = innerColorHex;
            this.outerColorHex = outerColorHex;
            UpdateFillDisplay();
        }

        private void UpdateFillDisplay()
        {
            fillBar.style.width = Length.Percent(NormalizedProgress * 100f);
            DestroyFillTexture();
            Color innerColor = ColorUtility.TryParseHtmlString(innerColorHex, out Color parsedInner) ? parsedInner : Color.blue;
            Color outerColor = ColorUtility.TryParseHtmlString(outerColorHex, out Color parsedOuter) ? parsedOuter : Color.cyan;
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
