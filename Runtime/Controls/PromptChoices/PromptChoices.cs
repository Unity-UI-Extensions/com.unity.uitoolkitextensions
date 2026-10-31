/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// One choice of a <see cref="PromptChoices"/> list. Text is display text; localise it.
    /// </summary>
    public sealed class PromptItem
    {
        public string Id;
        public string Text;

        /// <summary>
        /// False for a prompt already used or not available now.
        /// </summary>
        public bool Enabled = true;

        public PromptItem() { }

        public PromptItem(string id, string text, bool enabled = true)
        {
            Id = id;
            Text = text;
            Enabled = enabled;
        }
    }

    /// <summary>
    /// How a prompt row shows an answer, for quizzes.
    /// </summary>
    public enum PromptFeedback
    {
        None,

        /// <summary>
        /// The visitor's pick, while the answer is checked.
        /// </summary>
        Selected,
        Correct,
        Incorrect,
    }

    /// <summary>
    /// The questions a visitor can ask a character: a vertical list of outlined pill rows, each with a numbered
    /// ring and a label. Used or unavailable prompts stay listed, dimmed. Optionally a header with the character's
    /// avatar, name and the question, a hold-to-talk row, and right or wrong feedback for quizzes.
    /// </summary>
    /// <remarks>
    /// <para>Rows are focusable and submit like taps; the number keys 1 to 9 pick the matching row while the list
    /// has focus.</para>
    /// <para>Methods: <see cref="SetItems"/>, <see cref="SetItemEnabled"/>, <see cref="SetSpeaker"/>,
    /// <see cref="SetFeedback"/>, <see cref="ClearFeedback"/>. Props: <c>prompt</c> (the question; empty hides it),
    /// <c>allow-speech</c> (adds a <see cref="HoldToTalk"/> row: <c>speech-title</c>, <c>speech-subtitle</c>),
    /// <c>dismiss-text</c> (a link in the header; empty hides it). Events: <see cref="ChoiceSelected"/> (id),
    /// <see cref="SpeechStarted"/>, <see cref="SpeechEnded"/>, <see cref="SpeechTapped"/>, <see cref="Dismissed"/>.</para>
    /// <para>USS classes: <c>promptChoices</c> (root), <c>promptChoices__header</c>, <c>promptChoices__avatar</c>
    /// (an <see cref="AvatarPortrait"/>), <c>promptChoices__headings</c>, <c>promptChoices__speaker</c>,
    /// <c>promptChoices__prompt</c>, <c>promptChoices__dismiss</c>, <c>promptChoices__list</c>,
    /// <c>promptChoices__item</c>, <c>promptChoices__number</c>, <c>promptChoices__label</c>,
    /// <c>promptChoices__speech</c>. Modifiers: <c>promptChoices__item--disabled</c>, <c>--selected</c>,
    /// <c>--correct</c>, <c>--incorrect</c>.</para>
    /// <para>Tokens read: <c>--surface-input</c>, <c>--surface-border-strong</c>, <c>--surface-fg</c>,
    /// <c>--surface-accent</c>, <c>--surface-pressed</c>, <c>--radius-pill</c>, <c>--space-2</c>, <c>--space-4</c>,
    /// <c>--text-micro</c>, <c>--text-label</c>, <c>--text-subtitle</c>, <c>--font-display</c>, <c>--font-body-medium</c>,
    /// <c>--font-body-semibold</c>, <c>--c-success</c>, <c>--c-warn</c>, <c>--surface-highlight</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class PromptChoices : VisualElement
    {
        public const string RootClass = "promptChoices";
        public const string ItemClass = "promptChoices__item";
        public const string NumberClass = "promptChoices__number";
        public const string LabelClass = "promptChoices__label";
        public const string DisabledClass = "promptChoices__item--disabled";
        public const string SelectedClass = "promptChoices__item--selected";
        public const string CorrectClass = "promptChoices__item--correct";
        public const string IncorrectClass = "promptChoices__item--incorrect";
        public const string HeaderClass = "promptChoices__header";
        public const string AvatarClass = "promptChoices__avatar";
        public const string HeadingsClass = "promptChoices__headings";
        public const string SpeakerClass = "promptChoices__speaker";
        public const string PromptClass = "promptChoices__prompt";
        public const string DismissClass = "promptChoices__dismiss";
        public const string ListClass = "promptChoices__list";
        public const string SpeechClass = "promptChoices__speech";

        private readonly List<PromptItem> items = new();
        private readonly List<VisualElement> rows = new();
        private readonly VisualElement header;
        private readonly AvatarPortrait avatar;
        private readonly Label speaker;
        private readonly Label prompt;
        private readonly Label dismiss;
        private readonly VisualElement list;
        private readonly HoldToTalk speech;

        /// <summary>
        /// Raised with the id of an enabled prompt when it is tapped, submitted or picked by number.
        /// </summary>
        public event Action<string> ChoiceSelected;

        /// <summary>
        /// Raised when the hold-to-talk row is held: start listening.
        /// </summary>
        public event Action SpeechStarted;

        /// <summary>
        /// Raised when the hold ends: true when released over the row.
        /// </summary>
        public event Action<bool> SpeechEnded;

        /// <summary>
        /// Raised for a quick tap on the hold-to-talk row, for example to type instead.
        /// </summary>
        public event Action SpeechTapped;

        public event Action Dismissed;

        public HoldToTalk Speech => speech;

        public AvatarPortrait Avatar => avatar;

        /// <summary>
        /// The question above the choices; empty hides it.
        /// </summary>
        [UxmlAttribute("prompt")]
        public string Prompt
        {
            get => prompt.text;
            set
            {
                prompt.text = value ?? string.Empty;
                prompt.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
                UpdateHeader();
            }
        }

        [UxmlAttribute("allow-speech")]
        public bool AllowSpeech
        {
            get => speech.style.display != DisplayStyle.None;
            set => speech.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        [UxmlAttribute("speech-title")]
        public string SpeechTitle
        {
            get => speech.TitleText;
            set => speech.TitleText = value;
        }

        [UxmlAttribute("speech-subtitle")]
        public string SpeechSubtitle
        {
            get => speech.Subtitle;
            set => speech.Subtitle = value;
        }

        [UxmlAttribute("dismiss-text")]
        public string DismissText
        {
            get => dismiss.text;
            set
            {
                dismiss.text = value ?? string.Empty;
                dismiss.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
                UpdateHeader();
            }
        }

        public IReadOnlyList<PromptItem> Items => items;

        [UxmlAttribute("labels")]
        public List<string> Labels
        {
            get => items.ConvertAll(i => i.Text);
            set
            {
                var list = new List<PromptItem>();
                if (value != null)
                {
                    for (var i = 0; i < value.Count; i++)
                    {
                        list.Add(new PromptItem((i + 1).ToString(), value[i]));
                    }
                }

                SetItems(list);
            }
        }

        public PromptChoices()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            header = UIToolkitExtensions.CreateVisualElement(this, HeaderClass);
            avatar = new AvatarPortrait(string.Empty, AvatarSize.Small);
            avatar.AddToClassList(AvatarClass);
            header.Add(avatar);
            var headings = UIToolkitExtensions.CreateVisualElement(header, HeadingsClass);
            speaker = UIToolkitExtensions.CreateVisualElement<Label>(headings, SpeakerClass);
            prompt = UIToolkitExtensions.CreateVisualElement<Label>(headings, PromptClass);
            dismiss = UIToolkitExtensions.CreateVisualElement<Label>(header, DismissClass);
            dismiss.focusable = true;
            dismiss.tabIndex = 0;
            dismiss.RegisterCallback<ClickEvent>(_ => Dismissed?.Invoke());
            dismiss.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                Dismissed?.Invoke();
                evt.StopPropagation();
            });

            list = UIToolkitExtensions.CreateVisualElement(this, ListClass);

            speech = new HoldToTalk { TitleText = "Hold to ask in your own words" };
            speech.AddToClassList(SpeechClass);
            speech.PressStarted += () => SpeechStarted?.Invoke();
            speech.PressEnded += released => SpeechEnded?.Invoke(released);
            speech.Tapped += () => SpeechTapped?.Invoke();
            Add(speech);

            SetSpeaker(null, default);
            Prompt = null;
            DismissText = null;
            AllowSpeech = false;
        }

        /// <summary>
        /// Shows the character in the header: the name in capitals and the avatar (initials when the image is a default
        /// value). Null or empty name hides both.
        /// </summary>
        public void SetSpeaker(string name, Background image)
        {
            var has = !string.IsNullOrEmpty(name);
            speaker.text = has ? name.ToUpperInvariant() : string.Empty;
            speaker.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            avatar.DisplayName = name ?? string.Empty;
            avatar.SetImage(image);
            avatar.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateHeader();
        }

        /// <summary>
        /// Marks a row as selected, correct or incorrect; None clears it.
        /// </summary>
        public void SetFeedback(string id, PromptFeedback feedback)
        {
            var row = ItemElement(id);
            if (row == null)
            {
                return;
            }

            row.EnableInClassList(SelectedClass, feedback == PromptFeedback.Selected);
            row.EnableInClassList(CorrectClass, feedback == PromptFeedback.Correct);
            row.EnableInClassList(IncorrectClass, feedback == PromptFeedback.Incorrect);
        }

        public void ClearFeedback()
        {
            foreach (var item in items)
            {
                SetFeedback(item.Id, PromptFeedback.None);
            }
        }

        private void UpdateHeader()
        {
            if (header == null)
            {
                return;
            }

            var any = !string.IsNullOrEmpty(speaker.text) || !string.IsNullOrEmpty(prompt.text) || !string.IsNullOrEmpty(dismiss.text);
            header.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetItems(IReadOnlyList<PromptItem> newItems)
        {
            items.Clear();
            rows.Clear();
            list.Clear();
            if (newItems == null)
            {
                return;
            }

            for (var i = 0; i < newItems.Count; i++)
            {
                var item = newItems[i];
                items.Add(item);

                var row = UIToolkitExtensions.CreateVisualElement(list, ItemClass).MakePill();
                row.focusable = true;
                row.tabIndex = 0;
                var id = item.Id;
                row.RegisterCallback<ClickEvent>(_ => Select(id));
                row.RegisterCallback<NavigationSubmitEvent>(evt =>
                {
                    Select(id);
                    evt.StopPropagation();
                });

                var number = UIToolkitExtensions.CreateVisualElement<Label>(row, NumberClass);
                number.text = (i + 1).ToString();
                number.pickingMode = PickingMode.Ignore;
                var label = UIToolkitExtensions.CreateVisualElement<Label>(row, LabelClass);
                label.text = item.Text ?? string.Empty;
                label.pickingMode = PickingMode.Ignore;

                rows.Add(row);
                ApplyEnabled(rows.Count - 1);
            }
        }

        public void SetItemEnabled(string id, bool enabled)
        {
            var index = items.FindIndex(i => i.Id == id);
            if (index < 0)
            {
                throw new ArgumentException($"No prompt has the id '{id}'.", nameof(id));
            }

            items[index].Enabled = enabled;
            ApplyEnabled(index);
        }

        /// <summary>
        /// The row for a prompt, for focus and tests.
        /// </summary>
        public VisualElement ItemElement(string id)
        {
            var index = items.FindIndex(i => i.Id == id);
            return index >= 0 ? rows[index] : null;
        }

        private void ApplyEnabled(int index)
        {
            rows[index].SetEnabled(items[index].Enabled);
            rows[index].EnableInClassList(DisabledClass, !items[index].Enabled);
        }

        private void Select(string id)
        {
            var item = items.Find(i => i.Id == id);
            if (item != null && item.Enabled)
            {
                ChoiceSelected?.Invoke(id);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            var number = evt.keyCode switch
            {
                >= KeyCode.Alpha1 and <= KeyCode.Alpha9 => evt.keyCode - KeyCode.Alpha1 + 1,
                >= KeyCode.Keypad1 and <= KeyCode.Keypad9 => evt.keyCode - KeyCode.Keypad1 + 1,
                _ => 0,
            };

            if (number > 0 && number <= items.Count)
            {
                Select(items[number - 1].Id);
                evt.StopPropagation();
            }
        }
    }
}
