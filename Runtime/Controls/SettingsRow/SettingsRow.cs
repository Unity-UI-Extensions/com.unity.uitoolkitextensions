/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// What a <see cref="SettingsRow"/> shows at its trailing edge.
    /// </summary>
    public enum SettingsRowTrailing
    {
        None,

        /// <summary>
        /// A chevron: the row opens another page or picker.
        /// </summary>
        Chevron,

        /// <summary>
        /// A value text, such as the current language.
        /// </summary>
        Value,

        /// <summary>
        /// An embedded <see cref="Switch"/>; tapping the row toggles it.
        /// </summary>
        Switch,

        /// <summary>
        /// Whatever is added to the row's children.
        /// </summary>
        Custom,
    }

    /// <summary>
    /// A settings list row: optional icon, title, optional subtitle, and a trailing slot (chevron, value,
    /// switch or custom content). The whole row is one tap target.
    /// </summary>
    /// <remarks>
    /// <para>Children added to the row go into the trailing slot, for <see cref="SettingsRowTrailing.Custom"/>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class SettingsRow : VisualElement
    {
        public const string RootClass = "settingsRow";
        public const string IconTileClass = "settingsRow__iconTile";
        public const string IconClass = "settingsRow__icon";
        public const string TextsClass = "settingsRow__texts";
        public const string TitleClass = "settingsRow__title";
        public const string SubtitleClass = "settingsRow__subtitle";
        public const string TrailingClass = "settingsRow__trailing";
        public const string ValueClass = "settingsRow__value";
        public const string ChevronClass = "settingsRow__chevron";
        public const string SwitchClass = "settingsRow__switch";
        public const string HasIconClass = "settingsRow--hasIcon";
        public const string HasSubtitleClass = "settingsRow--hasSubtitle";
        public const string DividerClass = "settingsRow--divider";
        public const string ChevronModeClass = "settingsRow--chevron";
        public const string ValueModeClass = "settingsRow--value";
        public const string SwitchModeClass = "settingsRow--switch";
        public const string CustomModeClass = "settingsRow--custom";
        public const string CustomClass = "settingsRow__custom";

        private readonly VisualElement iconTile;
        private readonly VisualElement icon;
        private readonly Label title;
        private readonly Label subtitle;
        private readonly VisualElement trailing;
        private readonly Label valueLabel;
        private readonly VisualElement chevron;
        private readonly VisualElement custom;
        private readonly Switch toggle;
        private SettingsRowTrailing trailingMode = SettingsRowTrailing.Chevron;
        private Texture2D iconTexture;

        public event Action Clicked;

        /// <summary>
        /// Raised in <see cref="SettingsRowTrailing.Switch"/> mode when the embedded switch changes.
        /// </summary>
        public event Action<bool> SwitchChanged;

        public Switch Switch => toggle;

        public override VisualElement contentContainer => custom;

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => title.text = value ?? string.Empty;
        }

        [UxmlAttribute("subtitle-text")]
        public string SubtitleText
        {
            get => subtitle.text;
            set
            {
                subtitle.text = value ?? string.Empty;
                var has = !string.IsNullOrEmpty(value);
                subtitle.SetDisplayed(has);
                EnableInClassList(HasSubtitleClass, has);
            }
        }

        [UxmlAttribute("icon")]
        public Texture2D Icon
        {
            get => iconTexture;
            set
            {
                iconTexture = value;
                SetIcon(value != null ? Background.FromTexture2D(value) : default);
            }
        }

        [UxmlAttribute("trailing")]
        public SettingsRowTrailing Trailing
        {
            get => trailingMode;
            set
            {
                trailingMode = value;
                UpdateTrailing();
            }
        }

        [UxmlAttribute("value-text")]
        public string ValueText
        {
            get => valueLabel.text;
            set => valueLabel.text = value ?? string.Empty;
        }

        [UxmlAttribute("switch-value")]
        public bool SwitchValue
        {
            get => toggle.value;
            set => toggle.SetValueWithoutNotify(value);
        }

        [UxmlAttribute("show-divider")]
        public bool ShowDivider
        {
            get => ClassListContains(DividerClass);
            set => EnableInClassList(DividerClass, value);
        }

        public SettingsRow()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            focusable = true;
            tabIndex = 0;

            iconTile = UIToolkitExtensions.CreateVisualElement(IconTileClass).IgnorePicking();
            hierarchy.Add(iconTile);
            icon = UIToolkitExtensions.CreateVisualElement(iconTile, IconClass).IgnorePicking();

            var texts = UIToolkitExtensions.CreateVisualElement(TextsClass).IgnorePicking();
            hierarchy.Add(texts);
            title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass).IgnorePicking();
            subtitle = UIToolkitExtensions.CreateVisualElement<Label>(texts, SubtitleClass).IgnorePicking();

            trailing = UIToolkitExtensions.CreateVisualElement(TrailingClass).IgnorePicking();
            hierarchy.Add(trailing);

            valueLabel = UIToolkitExtensions.CreateVisualElement<Label>(trailing, ValueClass).IgnorePicking();

            toggle = new Switch { focusable = false };
            toggle.AddToClassList(SwitchClass);
            toggle.pickingMode = PickingMode.Ignore;
            toggle.ValueChanged += on => SwitchChanged?.Invoke(on);
            trailing.Add(toggle);

            custom = UIToolkitExtensions.CreateVisualElement(trailing, CustomClass).IgnorePicking();

            chevron = UIToolkitExtensions.CreateVisualElement(trailing, ChevronClass).IgnorePicking();

            this.RegisterActivate(OnClicked);

            SetIcon(default);
            SubtitleText = null;
            ShowDivider = true;
            UpdateTrailing();
        }

        public SettingsRow(string titleText, string subtitleText = null, SettingsRowTrailing trailingMode = SettingsRowTrailing.Chevron)
            : this()
        {
            TitleText = titleText;
            SubtitleText = subtitleText;
            Trailing = trailingMode;
        }

        /// <summary>
        /// Sets the leading icon from a texture, sprite or vector image. A default value hides it.
        /// </summary>
        public void SetIcon(Background background)
        {
            var has = icon.SetImageOrNone(background);
            iconTile.SetDisplayed(has);
            EnableInClassList(HasIconClass, has);
        }

        private void UpdateTrailing()
        {
            EnableInClassList(ChevronModeClass, trailingMode == SettingsRowTrailing.Chevron);
            EnableInClassList(ValueModeClass, trailingMode == SettingsRowTrailing.Value);
            EnableInClassList(SwitchModeClass, trailingMode == SettingsRowTrailing.Switch);
            EnableInClassList(CustomModeClass, trailingMode == SettingsRowTrailing.Custom);

            valueLabel.SetDisplayed(trailingMode == SettingsRowTrailing.Value);
            chevron.SetDisplayed(trailingMode == SettingsRowTrailing.Chevron);
            toggle.SetDisplayed(trailingMode == SettingsRowTrailing.Switch);
            custom.SetDisplayed(trailingMode == SettingsRowTrailing.Custom);
        }

        private void OnClicked()
        {
            if (trailingMode == SettingsRowTrailing.Switch)
            {
                toggle.Toggle();
            }

            Clicked?.Invoke();
        }
    }
}
