/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// An editable list of social links. Each row is a platform-labelled URL field; an "add" button
    /// inserts a new row whose platform is chosen from a <see cref="DropDownControl"/> wheel picker
    /// (only platforms not already present are offered). Choosing a platform commits the row;
    /// dismissing the picker without a choice discards it.
    ///
    /// Platform display names and field placeholders are supplied by optional resolver delegates
    /// (<see cref="PlatformNameResolver"/> / <see cref="PlatformPlaceholderResolver"/>) so the control
    /// carries no localization dependency — they default to the enum name and a generic placeholder.
    /// </summary>
    [UxmlElement]
    public partial class SocialLinkContainer : VisualElement
    {
        public enum SocialPlatform
        {
            Instagram,
            Twitter,
            Facebook,
            LinkedIn,
            TikTok,
            YouTube,
            Patreon
        }

        public const string RootClass = "socialLinkContainer";
        public const string LabelClass = "socialLinkContainer__label";
        public const string ContainerClass = "socialLinkContainer__container";
        public const string ContainerChildClass = "socialLinkContainer__containerChild";
        public const string ContainerChildInputClass = "socialLinkContainer__containerChildInput";
        public const string AddButtonClass = "socialLinkContainer__addButton";
        public const string ContainerChildRemoveButtonClass = "socialLinkContainer__containerChildRemoveButton";
        public const string PlatformPickerClass = "socialLinkContainer__platformPicker";

        private readonly Label labelField;
        private readonly VisualElement socialsContainer;
        private readonly Button addButton;

        private bool isInEditMode;
        private string addLabel = "+ Add Social";
        private string selectPlatformLabel = "Select platform";
        private string removeButtonText = "X";

        private VisualElement pendingSocialRow;
        private DropDownControl pendingPicker;
        private readonly Dictionary<string, SocialPlatform> pendingPlatformLookup = new();

        public event Action<SocialPlatform, string> OnRemoveSocialButtonClicked;
        public event Action<SocialPlatform, string> OnSocialClicked;

        public Func<SocialPlatform, string> PlatformNameResolver { get; set; }

        public Func<SocialPlatform, string> PlatformPlaceholderResolver { get; set; }

        public SocialLinkContainer()
        {
            AddToClassList(RootClass);

            labelField = UIToolkitExtensions.CreateVisualElement<Label>(this, LabelClass);
            labelField.style.display = DisplayStyle.None;

            socialsContainer = UIToolkitExtensions.CreateVisualElement(this, ContainerClass);
            addButton = UIToolkitExtensions.CreateVisualElement<Button>(socialsContainer, AddButtonClass);
            addButton.text = addLabel;
            addButton.clicked += BeginAddSocial;
        }

        [UxmlAttribute("label")]
        public string Label
        {
            get => labelField.text;
            set
            {
                labelField.text = value;
                labelField.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("add-label")]
        public string AddLabel
        {
            get => addLabel;
            set
            {
                addLabel = value;
                addButton.text = value;
            }
        }

        [UxmlAttribute("select-platform-label")]
        public string SelectPlatformLabel
        {
            get => selectPlatformLabel;
            set => selectPlatformLabel = value;
        }

        [UxmlAttribute("remove-button-text")]
        public string RemoveButtonText
        {
            get => removeButtonText;
            set => removeButtonText = value;
        }

        [UxmlAttribute("edit-mode")]
        public bool IsInEditMode
        {
            get => isInEditMode;
            set
            {
                isInEditMode = value;
                addButton.style.display = isInEditMode ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetBackgroundColor(Color color)
        {
            style.backgroundColor = color;
        }

        public VisualElement CreateSocial(SocialPlatform socialPlatform, string value = "")
        {
            var socialContainer = UIToolkitExtensions.CreateVisualElement(ContainerChildClass);
            var socialField = UIToolkitExtensions.CreateVisualElement<TextField>(socialContainer, ContainerChildInputClass);

            socialContainer.name = socialPlatform.ToString(); // identifies the platform for lookups
            socialField.label = GetPlatformName(socialPlatform);
            socialField.value = value ?? string.Empty;
            socialField.textEdition.placeholder = GetPlatformPlaceholder(socialPlatform);
            socialField.RegisterCallback<ClickEvent>(_ => OnSocialClicked?.Invoke(socialPlatform, socialField.value));

            if (isInEditMode)
            {
                var socialRemoveButton = UIToolkitExtensions.CreateVisualElement<Button>(socialContainer, ContainerChildRemoveButtonClass);
                socialRemoveButton.text = removeButtonText;
                socialRemoveButton.clicked += () => RemoveSocial(socialContainer, socialPlatform, socialField.value);
            }

            socialsContainer.Insert(socialsContainer.IndexOf(addButton), socialContainer);
            UpdateAddButtonState();

            return socialContainer;
        }

        /// <summary>
        /// Insert a pending row containing a platform picker pre-populated with the platforms not yet used. Picking a platform turns the pending row into a real one; dismissing the picker without a choice removes it again.
        /// </summary>
        private void BeginAddSocial()
        {
            if (pendingSocialRow != null)
            {
                return;
            }

            var availablePlatforms = GetAvailablePlatforms();
            if (availablePlatforms.Count == 0)
            {
                return;
            }

            pendingSocialRow = UIToolkitExtensions.CreateVisualElement(ContainerChildClass);
            pendingPicker = UIToolkitExtensions.CreateVisualElement<DropDownControl>(pendingSocialRow, PlatformPickerClass);

            pendingPlatformLookup.Clear();
            var items = new List<string>(availablePlatforms.Count + 1) { selectPlatformLabel };
            foreach (var platform in availablePlatforms)
            {
                var displayName = GetPlatformName(platform);
                pendingPlatformLookup[displayName] = platform;
                items.Add(displayName);
            }

            pendingPicker.Items = items;
            pendingPicker.SetDefault(selectPlatformLabel);
            pendingPicker.ValueChanged += OnPlatformPicked;
            pendingPicker.OpenStateChanged += OnPickerOpenStateChanged;

            socialsContainer.Insert(socialsContainer.IndexOf(addButton), pendingSocialRow);

            pendingPicker.RegisterCallbackOnce<GeometryChangedEvent>(_ => pendingPicker?.Open());
        }

        private void OnPlatformPicked(string displayName)
        {
            if (displayName == selectPlatformLabel)
            {
                return;
            }

            if (pendingPlatformLookup.TryGetValue(displayName, out var platform))
            {
                CommitPendingSocial(platform);
            }
        }

        private void OnPickerOpenStateChanged(bool isPickerOpen)
        {
            if (!isPickerOpen && pendingPicker != null && pendingPicker.Value == selectPlatformLabel)
            {
                RemovePendingSocialRow();
            }
        }

        private void CommitPendingSocial(SocialPlatform platform)
        {
            RemovePendingSocialRow();
            CreateSocial(platform);
        }

        private void RemovePendingSocialRow()
        {
            if (pendingSocialRow == null)
            {
                return;
            }

            if (pendingPicker != null)
            {
                pendingPicker.ValueChanged -= OnPlatformPicked;
                pendingPicker.OpenStateChanged -= OnPickerOpenStateChanged;
                pendingPicker = null;
            }

            pendingPlatformLookup.Clear();
            pendingSocialRow.RemoveFromHierarchy();
            pendingSocialRow = null;
            UpdateAddButtonState();
        }

        private List<SocialPlatform> GetAvailablePlatforms()
        {
            var available = new List<SocialPlatform>();
            foreach (SocialPlatform platform in Enum.GetValues(typeof(SocialPlatform)))
            {
                if (socialsContainer.Q(name: platform.ToString()) == null)
                {
                    available.Add(platform);
                }
            }
            return available;
        }

        private void UpdateAddButtonState()
        {
            addButton.SetEnabled(GetAvailablePlatforms().Count > 0);
        }

        private void RemoveSocial(VisualElement socialContainerToRemove, SocialPlatform socialPlatform, string value)
        {
            socialsContainer.Remove(socialContainerToRemove);
            UpdateAddButtonState();
            OnRemoveSocialButtonClicked?.Invoke(socialPlatform, value);
        }

        public void ClearSocials()
        {
            RemovePendingSocialRow();

            for (int childIndex = socialsContainer.childCount - 1; childIndex >= 0; childIndex--)
            {
                var child = socialsContainer[childIndex];
                if (child != addButton)
                {
                    socialsContainer.RemoveAt(childIndex);
                }
            }

            UpdateAddButtonState();
        }

        public List<SocialLink> GetSocials()
        {
            var socials = new List<SocialLink>();
            foreach (var child in socialsContainer.Children())
            {
                if (child == addButton)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(child.name) && Enum.TryParse<SocialPlatform>(child.name, out _))
                {
                    var textField = child.Q<TextField>(className: ContainerChildInputClass);
                    if (textField != null && !string.IsNullOrWhiteSpace(textField.value))
                    {
                        socials.Add(new SocialLink
                        {
                            platform = child.name,
                            url = textField.value
                        });
                    }
                }
            }
            return socials;
        }

        private string GetPlatformName(SocialPlatform platform)
        {
            var resolved = PlatformNameResolver?.Invoke(platform);
            return string.IsNullOrEmpty(resolved) ? platform.ToString() : resolved;
        }

        private string GetPlatformPlaceholder(SocialPlatform platform)
        {
            var resolved = PlatformPlaceholderResolver?.Invoke(platform);
            return string.IsNullOrEmpty(resolved) ? $"Enter your {platform} handle" : resolved;
        }
    }
}