/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A configurable screen header bar: an optional notch spacer, a centered title, and up to four
    /// edge buttons (action 1, action 2, action 3, and action 4 — a stateful on/off toggle).
    ///
    /// The control owns no application state. Button icons are supplied by the host via the icon
    /// properties (or USS), and interactions are surfaced as events: <see cref="Action1Clicked"/>,
    /// <see cref="Action2Clicked"/>, <see cref="Action3Clicked"/>, and <see cref="Action4Toggled"/>.
    /// The host is responsible for any persistence logic behind the action-4 toggle.
    /// </summary>
    /// <remarks>
    /// The buttons provided are just examples for common interactions.
    /// </remarks>
    [UxmlElement]
    public partial class ScreenHeader : VisualElement
    {
        public const string RootClass = "screenHeader";
        public const string NotchSpacerClass = "screenHeader__notchSpacer";
        public const string BarClass = "screenHeader__bar";
        public const string Action1ButtonClass = "screenHeader__action1Button";
        public const string TitleClass = "screenHeader__title";
        public const string Action2ButtonClass = "screenHeader__action2Button";
        public const string Action3ButtonClass = "screenHeader__action3Button";
        public const string Action4ToggleClass = "screenHeader__action4Toggle";

        public event Action Action1Clicked;
        public event Action Action2Clicked;
        public event Action Action3Clicked;
        public event Action<bool> Action4Toggled;

        private readonly Label titleLabel;
        private readonly ToggleButton action1Button; // AKA Back button (left edge)
        private readonly ToggleButton action2Button;
        private readonly ToggleButton action3Button;
        private readonly ToggleButton action4Toggle;

        private Texture2D action4ToggleOnIcon;
        private Texture2D action4ToggleOffIcon;

        public ScreenHeader()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            UIToolkitExtensions.CreateVisualElement(this, NotchSpacerClass);

            var bar = UIToolkitExtensions.CreateVisualElement(this, BarClass);
            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(bar, TitleClass);

            action1Button = UIToolkitExtensions.CreateVisualElement<ToggleButton>(bar, Action1ButtonClass);
            action1Button.OnClicked += () => Action1Clicked?.Invoke();

            action2Button = UIToolkitExtensions.CreateVisualElement<ToggleButton>(bar, Action2ButtonClass);
            action2Button.OnClicked += () => Action2Clicked?.Invoke();

            action3Button = UIToolkitExtensions.CreateVisualElement<ToggleButton>(bar, Action3ButtonClass);
            action3Button.OnClicked += () => Action3Clicked?.Invoke();

            action4Toggle = UIToolkitExtensions.CreateVisualElement<ToggleButton>(bar, Action4ToggleClass);
            action4Toggle.OnClicked += OnAction4Toggled;

            Configure(showAction1: true, showTitle: true, showAction2: false, showAction3: true, showAction4Toggle: true);
        }

        [UxmlAttribute("title")]
        public string Title
        {
            get => titleLabel.text;
            set
            {
                titleLabel.text = value;
                titleLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("action1-icon")]
        public Texture2D Action1Icon
        {
            get => action1Button.Image;
            set => action1Button.SetImage(value);
        }

        [UxmlAttribute("action2-icon")]
        public Texture2D Action2Icon
        {
            get => action2Button.Image;
            set => action2Button.SetImage(value);
        }

        [UxmlAttribute("action3-icon")]
        public Texture2D Action3Icon
        {
            get => action3Button.Image;
            set => action3Button.SetImage(value);
        }

        [UxmlAttribute("action4-toggle-on-icon")]
        public Texture2D Action4ToggleOnIcon
        {
            get => action4ToggleOnIcon;
            set
            {
                action4ToggleOnIcon = value;
                RefreshAction4ToggleIcon();
            }
        }

        [UxmlAttribute("action4-toggle-off-icon")]
        public Texture2D Action4ToggleOffIcon
        {
            get => action4ToggleOffIcon;
            set
            {
                action4ToggleOffIcon = value;
                RefreshAction4ToggleIcon();
            }
        }

        public bool IsAction4ToggleSelected => action4Toggle.IsSelected;

        public void SetAction4ToggleSelected(bool selected)
        {
            if (selected)
            {
                action4Toggle.ForceSelect();
            }
            else
            {
                action4Toggle.ForceDeselect();
            }

            RefreshAction4ToggleIcon();
        }

        private void OnAction4Toggled()
        {
            RefreshAction4ToggleIcon();
            Action4Toggled?.Invoke(action4Toggle.IsSelected);
        }

        private void RefreshAction4ToggleIcon()
        {
            var icon = action4Toggle.IsSelected ? action4ToggleOffIcon : action4ToggleOnIcon;
            if (icon != null)
            {
                action4Toggle.SetImage(icon);
            }
        }

        public void Configure(bool showAction1 = true, bool showTitle = true, bool showAction2 = false,
            bool showAction3 = false, bool showAction4Toggle = true)
        {
            ShowAction1 = showAction1;
            titleLabel.style.display = showTitle ? DisplayStyle.Flex : DisplayStyle.None;
            ShowAction2 = showAction2;
            ShowAction3 = showAction3;
            ShowAction4Toggle = showAction4Toggle;
        }

        [UxmlAttribute("show-action1")]
        public bool ShowAction1
        {
            get => action1Button.style.display.value == DisplayStyle.Flex;
            set => action1Button.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        [UxmlAttribute("show-action2")]
        public bool ShowAction2
        {
            get => action2Button.style.display.value == DisplayStyle.Flex;
            set => action2Button.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        [UxmlAttribute("show-action3")]
        public bool ShowAction3
        {
            get => action3Button.style.display.value == DisplayStyle.Flex;
            set => action3Button.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        [UxmlAttribute("show-action4-toggle")]
        public bool ShowAction4Toggle
        {
            get => action4Toggle.style.display.value == DisplayStyle.Flex;
            set => action4Toggle.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetAction1ButtonVisible(bool isVisible) => ShowAction1 = isVisible;

        public void SetAction2ButtonVisible(bool isVisible) => ShowAction2 = isVisible;

        public void SetAction3ButtonVisible(bool isVisible) => ShowAction3 = isVisible;

        public void SetAction4ToggleVisible(bool isVisible) => ShowAction4Toggle = isVisible;
    }
}
