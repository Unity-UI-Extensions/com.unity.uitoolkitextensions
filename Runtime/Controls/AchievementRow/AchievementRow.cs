/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A compact achievement line: a gradient badge disc with an icon, a title such as
    /// "Conversationalist · 2 of 3 characters met", and an optional <see cref="LinearProgress"/>.
    /// </summary>
    /// <remarks>
    /// <para>A negative <c>progress</c> hides the bar.</para>
    /// </remarks>
    [UxmlElement]
    public partial class AchievementRow : VisualElement
    {
        public const string RootClass = "achievementRow";
        public const string BadgeClass = "achievementRow__badge";
        public const string IconClass = "achievementRow__icon";
        public const string TextsClass = "achievementRow__texts";
        public const string TitleClass = "achievementRow__title";
        public const string ProgressClass = "achievementRow__progress";

        private readonly VisualElement icon;
        private readonly Label title;
        private readonly LinearProgress progress;

        public event Action Clicked;

        public LinearProgress ProgressBar => progress;

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => title.text = value ?? string.Empty;
        }

        /// <summary>
        /// Progress towards the achievement, 0 to 1; negative hides the bar.
        /// </summary>
        [UxmlAttribute("progress")]
        public float Progress
        {
            get => progress.style.display == DisplayStyle.None ? -1f : progress.Value;
            set
            {
                progress.style.display = value < 0f ? DisplayStyle.None : DisplayStyle.Flex;
                progress.Value = Mathf.Max(0f, value);
            }
        }

        public AchievementRow()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var badge = UIToolkitExtensions.CreateVisualElement(this, BadgeClass).IgnorePicking();
            badge.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            icon = UIToolkitExtensions.CreateVisualElement(badge, IconClass).IgnorePicking();

            var texts = UIToolkitExtensions.CreateVisualElement(this, TextsClass).IgnorePicking();
            title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            progress = new LinearProgress();
            progress.AddToClassList(ProgressClass);
            texts.Add(progress);

            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke());
            Progress = -1f;
        }

        public AchievementRow(string titleText, float progressValue = -1f)
            : this()
        {
            TitleText = titleText;
            Progress = progressValue;
        }

        public void SetIcon(Background image)
        {
            var has = icon.SetImageOrNone(image);
        }
    }
}
