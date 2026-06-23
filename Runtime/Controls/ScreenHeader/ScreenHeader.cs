/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A configurable screen header bar: an optional notch spacer, a centered title, and up to four
    /// edge buttons (action 1, audio-mute toggle, action 2, action 3).
    ///
    /// The control owns no application state. Button icons are supplied by the host via the icon
    /// properties (or USS), and interactions are surfaced as events: <see cref="Action1Clicked"/>,
    /// <see cref="Action2Clicked"/>, <see cref="Action3Clicked"/>, and <see cref="AudioToggled"/>.
    /// The host is responsible for any muting/persistence logic behind the audio toggle.
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
        public const string AudioButtonClass = "screenHeader__audioButton";

        public event Action Action1Clicked;
        public event Action Action2Clicked;
        public event Action Action3Clicked;

        /// <summary>Raised when the audio toggle is pressed, carrying the new muted state.</summary>
        public event Action<bool> AudioToggled;

        private readonly Label titleLabel;
        private readonly ToggleButton action1Button;
        private readonly ToggleButton audioButton;
        private readonly ToggleButton action2Button;
        private readonly ToggleButton action3Button;

        private Texture2D audioOnIcon;
        private Texture2D audioOffIcon;

        public ScreenHeader()
        {
            AddToClassList(RootClass);

            UIToolkitExtensions.CreateVisualElement(this, NotchSpacerClass);

            var bar = UIToolkitExtensions.CreateVisualElement(this, BarClass);
            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(bar, TitleClass);

            action1Button = UIToolkitExtensions.CreateVisualElement<ToggleButton>(bar, Action1ButtonClass);
            action1Button.OnClicked += () => Action1Clicked?.Invoke();

            audioButton = UIToolkitExtensions.CreateVisualElement<ToggleButton>(bar, AudioButtonClass);
            audioButton.OnClicked += OnAudioToggled;

            action2Button = UIToolkitExtensions.CreateVisualElement<ToggleButton>(bar, Action2ButtonClass);
            action2Button.OnClicked += () => Action2Clicked?.Invoke();

            action3Button = UIToolkitExtensions.CreateVisualElement<ToggleButton>(bar, Action3ButtonClass);
            action3Button.OnClicked += () => Action3Clicked?.Invoke();

            Configure(showAction1: true, showTitle: true, showAction2: false, showAction3: true, showAudio: true);
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

        [UxmlAttribute("audio-on-icon")]
        public Texture2D AudioOnIcon
        {
            get => audioOnIcon;
            set
            {
                audioOnIcon = value;
                RefreshAudioIcon();
            }
        }

        [UxmlAttribute("audio-off-icon")]
        public Texture2D AudioOffIcon
        {
            get => audioOffIcon;
            set
            {
                audioOffIcon = value;
                RefreshAudioIcon();
            }
        }

        public bool IsAudioMuted => audioButton.IsSelected;

        public void SetAudioMuted(bool muted)
        {
            if (muted)
            {
                audioButton.ForceSelect();
            }
            else
            {
                audioButton.ForceDeselect();
            }

            RefreshAudioIcon();
        }

        private void OnAudioToggled()
        {
            RefreshAudioIcon();
            AudioToggled?.Invoke(audioButton.IsSelected);
        }

        private void RefreshAudioIcon()
        {
            var icon = audioButton.IsSelected ? audioOffIcon : audioOnIcon;
            if (icon != null)
            {
                audioButton.SetImage(icon);
            }
        }

        public void Configure(bool showAction1 = true, bool showTitle = true, bool showAction2 = false,
            bool showAction3 = false, bool showAudio = true)
        {
            ShowAction1 = showAction1;
            titleLabel.style.display = showTitle ? DisplayStyle.Flex : DisplayStyle.None;
            ShowAction2 = showAction2;
            ShowAction3 = showAction3;
            ShowAudio = showAudio;
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

        [UxmlAttribute("show-audio")]
        public bool ShowAudio
        {
            get => audioButton.style.display.value == DisplayStyle.Flex;
            set => audioButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetAction1ButtonVisible(bool isVisible) => ShowAction1 = isVisible;

        public void SetAction2ButtonVisible(bool isVisible) => ShowAction2 = isVisible;

        public void SetAction3ButtonVisible(bool isVisible) => ShowAction3 = isVisible;

        public void SetAudioButtonVisible(bool isVisible) => ShowAudio = isVisible;
    }
}
