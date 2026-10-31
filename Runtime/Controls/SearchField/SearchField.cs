/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A search box built on <see cref="RoundedInputField"/>: a search icon, a clear button, an optional
    /// result count, a debounced query event and a submit event.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>placeholder</c>, <c>value</c>, <c>debounce-ms</c>, <c>result-count</c> (negative hides it),
    /// <c>keyboard-type</c>. Events: <see cref="QueryChanged"/> (after the debounce), <see cref="Submitted"/>
    /// (Enter or the keyboard's done key), <see cref="Cleared"/>. Methods: <see cref="ClearQuery"/>,
    /// <see cref="FocusInput"/>.</para>
    /// <para>USS classes: <c>searchField</c> (root), <c>searchField__icon</c>, <c>searchField__input</c>
    /// (the RoundedInputField), <c>searchField__count</c>, <c>searchField__clear</c>.</para>
    /// <para>Modifier classes: <c>searchField--hasValue</c> (shows the clear button), <c>searchField--focused</c>.</para>
    /// <para>Tokens read: <c>--surface-raised</c>, <c>--surface-fg</c>, <c>--surface-muted</c>,
    /// <c>--surface-border</c>, <c>--surface-accent</c>, <c>--surface-pressed</c>, <c>--radius-pill</c>,
    /// <c>--space-1..4</c>, <c>--text-body</c>, <c>--text-caption</c>, <c>--font-body</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class SearchField : VisualElement
    {
        public const string RootClass = "searchField";
        public const string IconClass = "searchField__icon";
        public const string InputClass = "searchField__input";
        public const string CountClass = "searchField__count";
        public const string ClearClass = "searchField__clear";
        public const string HasValueClass = "searchField--hasValue";
        public const string FocusedClass = "searchField--focused";

        private readonly RoundedInputField input;
        private readonly TextField textField;
        private readonly Label countLabel;
        private readonly VisualElement clearButton;
        private IVisualElementScheduledItem debounce;
        private int debounceMs = 300;
        private int resultCount = -1;
        private string lastRaisedQuery = string.Empty;

        /// <summary>
        /// Raised with the query once typing has paused for <see cref="DebounceMs"/>.
        /// </summary>
        public event Action<string> QueryChanged;

        /// <summary>
        /// Raised with the query when the user submits it.
        /// </summary>
        public event Action<string> Submitted;

        /// <summary>
        /// Raised when the clear button empties the field.
        /// </summary>
        public event Action Cleared;

        /// <summary>
        /// The inner field; exposed for tests and focus handling.
        /// </summary>
        public TextField TextField => textField;

        [UxmlAttribute("placeholder")]
        public string Placeholder
        {
            get => input.Placeholder;
            set => input.Placeholder = value;
        }

        /// <summary>
        /// The query. Setting it updates the field without raising <see cref="QueryChanged"/>.
        /// </summary>
        [UxmlAttribute("value")]
        public string Value
        {
            get => input.Value ?? string.Empty;
            set
            {
                input.SetValueWithoutNotify(value ?? string.Empty);
                lastRaisedQuery = Value;
                UpdateHasValue();
            }
        }

        [UxmlAttribute("debounce-ms")]
        public int DebounceMs
        {
            get => debounceMs;
            set => debounceMs = Mathf.Max(0, value);
        }

        /// <summary>
        /// The number of results to show after the query; negative hides the count.
        /// </summary>
        [UxmlAttribute("result-count")]
        public int ResultCount
        {
            get => resultCount;
            set
            {
                resultCount = value;
                countLabel.text = value >= 0 ? value.ToString() : string.Empty;
                countLabel.style.display = value >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        [UxmlAttribute("keyboard-type")]
        public TouchScreenKeyboardType KeyboardType
        {
            get => textField.keyboardType;
            set => textField.keyboardType = value;
        }

        public SearchField()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            this.MakePill();

            var icon = UIToolkitExtensions.CreateVisualElement(this, IconClass);
            icon.pickingMode = PickingMode.Ignore;

            input = new RoundedInputField();
            input.AddToClassList(InputClass);
            Add(input);
            textField = input.Q<TextField>();
            textField.keyboardType = TouchScreenKeyboardType.Search;

            countLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, CountClass);
            countLabel.pickingMode = PickingMode.Ignore;

            clearButton = UIToolkitExtensions.CreateVisualElement(this, ClearClass);
            clearButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                ClearQuery();
            });

            input.ValueChanged += OnValueChanged;
            textField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            textField.RegisterCallback<FocusInEvent>(_ => AddToClassList(FocusedClass));
            textField.RegisterCallback<FocusOutEvent>(_ => RemoveFromClassList(FocusedClass));
            RegisterCallback<ClickEvent>(_ => FocusInput());

            Placeholder = "Search";
            ResultCount = -1;
            UpdateHasValue();
        }

        /// <summary>
        /// Empties the field and raises <see cref="Cleared"/> and an immediate <see cref="QueryChanged"/>.
        /// </summary>
        public void ClearQuery()
        {
            debounce?.Pause();
            var hadValue = Value.Length > 0;
            input.SetValueWithoutNotify(string.Empty);
            UpdateHasValue();
            if (hadValue)
            {
                Cleared?.Invoke();
                RaiseQuery();
            }
        }

        public void FocusInput() => input.Focus();

        private void OnValueChanged(string value)
        {
            UpdateHasValue();
            debounce?.Pause();
            if (debounceMs <= 0)
            {
                RaiseQuery();
            }
            else
            {
                debounce = schedule.Execute(RaiseQuery).StartingIn(debounceMs);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter || evt.character == '\n')
            {
                debounce?.Pause();
                RaiseQuery();
                Submitted?.Invoke(Value);
                evt.StopPropagation();
            }
        }

        private void RaiseQuery()
        {
            var query = Value;
            if (query == lastRaisedQuery)
            {
                return;
            }

            lastRaisedQuery = query;
            QueryChanged?.Invoke(query);
        }

        private void UpdateHasValue()
        {
            var has = Value.Length > 0;
            EnableInClassList(HasValueClass, has);
            clearButton.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
