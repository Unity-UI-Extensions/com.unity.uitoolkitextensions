/// Credit SimonDarksideJ

using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A placeholder panel that displays a "Coming Soon" heading and an optional message body.
    /// </summary>
    public class ComingSoonMessage : VisualElement
    {
        public const string RootClass = "comingSoonMessage";
        public const string BackgroundClass = "comingSoonMessage__background";
        public const string TitleClass = "comingSoonMessage__title";
        public const string LabelClass = "comingSoonMessage__label";

        private readonly Label title;
        private readonly Label label;

        public ComingSoonMessage()
        {
            AddToClassList(RootClass);

            var background = UIToolkitExtensions.CreateVisualElement(this, BackgroundClass);

            title = UIToolkitExtensions.CreateVisualElement<Label>(background, TitleClass);
            title.text = "Coming Soon";

            label = UIToolkitExtensions.CreateVisualElement<Label>(background, LabelClass);
        }

        public string Title
        {
            get => title.text;
            set => title.text = value;
        }

        public void SetMessage(string message)
        {
            label.text = message;
        }
    }
}
