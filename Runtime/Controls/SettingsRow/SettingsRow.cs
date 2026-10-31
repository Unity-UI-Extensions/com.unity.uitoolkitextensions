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
    /// <para>Props: <c>title-text</c>, <c>subtitle-text</c>, <c>icon</c>, <c>trailing</c>, <c>value-text</c>,
    /// <c>switch-value</c>, <c>show-divider</c>. Events: <see cref="Clicked"/>, <see cref="SwitchChanged"/>.</para>
    /// <para>USS classes: <c>settingsRow</c> (root), <c>settingsRow__iconTile</c>, <c>settingsRow__icon</c>, <c>settingsRow__texts</c>,
    /// <c>settingsRow__title</c>, <c>settingsRow__subtitle</c>, <c>settingsRow__trailing</c>, <c>settingsRow__custom</c>
    /// (the custom slot, a row),
    /// <c>settingsRow__value</c>, <c>settingsRow__chevron</c>, <c>settingsRow__switch</c>.</para>
    /// <para>Modifier classes: <c>settingsRow--hasIcon</c>, <c>settingsRow--hasSubtitle</c>,
    /// <c>settingsRow--divider</c>, <c>settingsRow--chevron</c>, <c>settingsRow--value</c>,
    /// <c>settingsRow--switch</c>, <c>settingsRow--custom</c>.</para>
    /// <para>Tokens read: <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-border</c>,
    /// <c>--surface-pressed</c>, <c>--surface-accent</c>, <c>--surface-tile</c>, <c>--radius-sm</c>, <c>--space-2..4</c>, <c>--text-body</c>,
    /// <c>--text-caption</c>, <c>--text-label</c>, <c>--font-body</c>, <c>--font-body-medium</c>.</para>
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

        /// <summary>
        /// Raised when the row is tapped or submitted.
        /// </summary>
        public event Action Clicked;

        /// <summary>
        /// Raised when the embedded switch changes, in <see cref="SettingsRowTrailing.Switch"/> mode.
        /// </summary>
        public event Action<bool> SwitchChanged;

        /// <summary>
        /// The embedded switch, used in <see cref="SettingsRowTrailing.Switch"/> mode.
        /// </summary>
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
                subtitle.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
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

            iconTile = UIToolkitExtensions.CreateVisualElement(IconTileClass);
            iconTile.pickingMode = PickingMode.Ignore;
            hierarchy.Add(iconTile);
            icon = UIToolkitExtensions.CreateVisualElement(iconTile, IconClass);
            icon.pickingMode = PickingMode.Ignore;

            var texts = UIToolkitExtensions.CreateVisualElement(TextsClass);
            texts.pickingMode = PickingMode.Ignore;
            hierarchy.Add(texts);
            title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            title.pickingMode = PickingMode.Ignore;
            subtitle = UIToolkitExtensions.CreateVisualElement<Label>(texts, SubtitleClass);
            subtitle.pickingMode = PickingMode.Ignore;

            trailing = UIToolkitExtensions.CreateVisualElement(TrailingClass);
            trailing.pickingMode = PickingMode.Ignore;
            hierarchy.Add(trailing);

            valueLabel = UIToolkitExtensions.CreateVisualElement<Label>(trailing, ValueClass);
            valueLabel.pickingMode = PickingMode.Ignore;

            toggle = new Switch { focusable = false };
            toggle.AddToClassList(SwitchClass);
            toggle.pickingMode = PickingMode.Ignore;
            toggle.ValueChanged += on => SwitchChanged?.Invoke(on);
            trailing.Add(toggle);

            custom = UIToolkitExtensions.CreateVisualElement(trailing, CustomClass);
            custom.pickingMode = PickingMode.Ignore;

            chevron = UIToolkitExtensions.CreateVisualElement(trailing, ChevronClass);
            chevron.pickingMode = PickingMode.Ignore;

            RegisterCallback<ClickEvent>(_ => OnClicked());
            RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                OnClicked();
                evt.StopPropagation();
            });

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
        /// Sets the leading icon from a texture, sprite or vector image; a default value hides it.
        /// </summary>
        public void SetIcon(Background background)
        {
            var has = background.texture != null || background.sprite != null || background.vectorImage != null || background.renderTexture != null;
            icon.style.backgroundImage = has ? new StyleBackground(background) : new StyleBackground(StyleKeyword.None);
            iconTile.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(HasIconClass, has);
        }

        private void UpdateTrailing()
        {
            EnableInClassList(ChevronModeClass, trailingMode == SettingsRowTrailing.Chevron);
            EnableInClassList(ValueModeClass, trailingMode == SettingsRowTrailing.Value);
            EnableInClassList(SwitchModeClass, trailingMode == SettingsRowTrailing.Switch);
            EnableInClassList(CustomModeClass, trailingMode == SettingsRowTrailing.Custom);

            valueLabel.style.display = trailingMode == SettingsRowTrailing.Value ? DisplayStyle.Flex : DisplayStyle.None;
            chevron.style.display = trailingMode == SettingsRowTrailing.Chevron ? DisplayStyle.Flex : DisplayStyle.None;
            toggle.style.display = trailingMode == SettingsRowTrailing.Switch ? DisplayStyle.Flex : DisplayStyle.None;
            custom.style.display = trailingMode == SettingsRowTrailing.Custom ? DisplayStyle.Flex : DisplayStyle.None;
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
