/// Credit SimonDarksideJ

using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A reusable unread-count badge: a small rounded dot showing a count.
    /// Hidden automatically when the count is zero or less; the display clamps to "99+".
    /// </summary>
    [UxmlElement]
    public partial class NotificationBadge : VisualElement
    {
        public const string RootClass = "notificationBadge";
        public const string CountClass = "notificationBadge__count";

        private readonly Label countLabel;
        private int count;

        public NotificationBadge()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            pickingMode = PickingMode.Position;

            countLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, CountClass);
            countLabel.pickingMode = PickingMode.Ignore;

            SetCount(0);
        }

        [UxmlAttribute("count")]
        public int Count
        {
            get => count;
            set => SetCount(value);
        }

        public void SetCount(int value)
        {
            count = value;

            if (count <= 0)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
            countLabel.text = count < 100 ? count.ToString() : "99+";
        }
    }
}