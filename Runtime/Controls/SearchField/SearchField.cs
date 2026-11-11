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
    /// <para><see cref="Submitted"/> is raised by Enter or the keyboard's done key.
    /// The class <c>searchField--hasValue</c> shows the clear button.</para>
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

        public event Action<string> Submitted;

        public event Action Cleared;

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
        /// The number of results to show after the query. A negative value hides the count.
        /// </summary>
        [UxmlAttribute("result-count")]
        public int ResultCount
        {
            get => resultCount;
            set
            {
                resultCount = value;
                countLabel.text = value >= 0 ? value.ToString() : string.Empty;
                countLabel.SetDisplayed(value >= 0);
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

            var icon = UIToolkitExtensions.CreateVisualElement(this, IconClass).IgnorePicking();

            input = new RoundedInputField();
            input.AddToClassList(InputClass);
            Add(input);
            textField = input.Q<TextField>();
            textField.keyboardType = TouchScreenKeyboardType.Search;

            countLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, CountClass).IgnorePicking();

            clearButton = UIToolkitExtensions.CreateVisualElement(this, ClearClass);
            clearButton.focusable = true;
            clearButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                ClearQuery();
            });
            clearButton.RegisterCallback<NavigationSubmitEvent>(evt =>
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
                lastRaisedQuery = Value;
                QueryChanged?.Invoke(lastRaisedQuery);
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
            clearButton.SetDisplayed(has);
        }
    }
}
