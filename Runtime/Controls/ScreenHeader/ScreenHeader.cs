/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum ScreenHeaderVariant
    {
        /// <summary>
        /// The large header: a 120 px notch spacer, an 80 px bar and 64 px buttons.
        /// </summary>
        Default,

        /// <summary>
        /// A 56 px bar styled from ThemeTokens, with a back chevron and an optional eyebrow line.
        /// </summary>
        Compact,
    }

    /// <summary>
    /// A configurable screen header bar: an optional notch spacer, a centred title, and up to four
    /// edge buttons (action 1, 2, 3, and action 4, which is a stateful on/off toggle).
    /// The control owns no application state. The host supplies button icons through the icon
    /// properties (or USS) and handles events: <see cref="Action1Clicked"/>,
    /// <see cref="Action2Clicked"/>, <see cref="Action3Clicked"/> and <see cref="Action4Toggled"/>.
    /// The host also keeps any persistence behind the action-4 toggle.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="ScreenHeaderVariant.Compact"/> variant reads ThemeTokens, so it
    /// works on deep and light surfaces. It has a 56 px bar and a back chevron on action 1 unless an icon is set.
    /// <see cref="TopInset"/> sets the notch spacer height. <see cref="SetActionIcon"/> takes any <see cref="Background"/>
    /// and leaves the tint to USS.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ScreenHeader : VisualElement
    {
        public const string RootClass = "screenHeader";
        public const string NotchSpacerClass = "screenHeader__notchSpacer";
        public const string BarClass = "screenHeader__bar";
        public const string Action1ButtonClass = "screenHeader__action1Button";
        public const string TitlesClass = "screenHeader__titles";
        public const string EyebrowClass = "screenHeader__eyebrow";
        public const string TitleClass = "screenHeader__title";
        public const string CompactClass = "screenHeader--compact";
        public const string Action2ButtonClass = "screenHeader__action2Button";
        public const string Action3ButtonClass = "screenHeader__action3Button";
        public const string Action4ToggleClass = "screenHeader__action4Toggle";

        public event Action Action1Clicked;
        public event Action Action2Clicked;
        public event Action Action3Clicked;
        public event Action<bool> Action4Toggled;

        private readonly VisualElement notchSpacer;
        private readonly Label eyebrowLabel;
        private readonly Label titleLabel;
        private ScreenHeaderVariant variant;
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

            notchSpacer = UIToolkitExtensions.CreateVisualElement(this, NotchSpacerClass);

            var bar = UIToolkitExtensions.CreateVisualElement(this, BarClass);
            var titles = UIToolkitExtensions.CreateVisualElement(bar, TitlesClass).IgnorePicking();
            eyebrowLabel = UIToolkitExtensions.CreateVisualElement<Label>(titles, EyebrowClass);
            eyebrowLabel.style.display = DisplayStyle.None;
            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(titles, TitleClass);

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

        [UxmlAttribute("variant")]
        public ScreenHeaderVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                EnableInClassList(CompactClass, variant == ScreenHeaderVariant.Compact);
                if (variant == ScreenHeaderVariant.Compact)
                {
                    this.UseThemeTokens();
                }
            }
        }

        /// <summary>
        /// A small line above the title, such as the parent page; empty hides it.
        /// </summary>
        [UxmlAttribute("eyebrow")]
        public string Eyebrow
        {
            get => eyebrowLabel.text;
            set
            {
                eyebrowLabel.SetTextOrHide(value);
            }
        }

        /// <summary>
        /// The notch spacer height, normally the top safe-area inset. Unset keeps the USS height.
        /// </summary>
        [UxmlAttribute("top-inset")]
        public float TopInset
        {
            get => notchSpacer.resolvedStyle.height;
            set => notchSpacer.style.height = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Sets an action's icon (1 to 3, or 4 for the toggle) from a texture, sprite or vector image, tinted by USS.
        /// A default value clears it, which shows the compact variant's back chevron again on action 1.
        /// </summary>
        public void SetActionIcon(int action, Background icon)
        {
            var button = action switch
            {
                1 => action1Button,
                2 => action2Button,
                3 => action3Button,
                4 => action4Toggle,
                _ => throw new ArgumentOutOfRangeException(nameof(action)),
            };
            button.SetIcon(icon);
        }

        [UxmlAttribute("title")]
        public string Title
        {
            get => titleLabel.text;
            set
            {
                titleLabel.text = value;
                titleLabel.SetDisplayed(!string.IsNullOrEmpty(value));
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
            titleLabel.SetDisplayed(showTitle);
            ShowAction2 = showAction2;
            ShowAction3 = showAction3;
            ShowAction4Toggle = showAction4Toggle;
        }

        [UxmlAttribute("show-action1")]
        public bool ShowAction1
        {
            get => action1Button.style.display.value == DisplayStyle.Flex;
            set => action1Button.SetDisplayed(value);
        }

        [UxmlAttribute("show-action2")]
        public bool ShowAction2
        {
            get => action2Button.style.display.value == DisplayStyle.Flex;
            set => action2Button.SetDisplayed(value);
        }

        [UxmlAttribute("show-action3")]
        public bool ShowAction3
        {
            get => action3Button.style.display.value == DisplayStyle.Flex;
            set => action3Button.SetDisplayed(value);
        }

        [UxmlAttribute("show-action4-toggle")]
        public bool ShowAction4Toggle
        {
            get => action4Toggle.style.display.value == DisplayStyle.Flex;
            set => action4Toggle.SetDisplayed(value);
        }

        public void SetAction1ButtonVisible(bool isVisible) => ShowAction1 = isVisible;

        public void SetAction2ButtonVisible(bool isVisible) => ShowAction2 = isVisible;

        public void SetAction3ButtonVisible(bool isVisible) => ShowAction3 = isVisible;

        public void SetAction4ToggleVisible(bool isVisible) => ShowAction4Toggle = isVisible;
    }
}
