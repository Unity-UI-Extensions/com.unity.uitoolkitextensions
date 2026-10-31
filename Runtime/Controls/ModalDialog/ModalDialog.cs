/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum DialogVariant
    {
        Default,

        /// <summary>
        /// The primary action deletes or stops something; it is drawn in the danger colour.
        /// </summary>
        Destructive,
    }

    /// <summary>
    /// How a <see cref="ModalDialog"/> was closed.
    /// </summary>
    public enum DialogAction
    {
        /// <summary>
        /// Dismissed: scrim tap, back, or superseded by another <see cref="ModalDialog.Show"/>.
        /// </summary>
        None,
        Primary,
        Secondary,
    }

    /// <summary>
    /// What to show in a <see cref="ModalDialog"/>. Strings are display text; localise before passing them.
    /// </summary>
    public sealed class DialogOptions
    {
        public string Title;
        public string Body;
        public string PrimaryText = "OK";

        /// <summary>
        /// Leave null for a single-action dialog.
        /// </summary>
        public string SecondaryText;

        public DialogVariant Variant = DialogVariant.Default;

        /// <summary>
        /// Optional content above the title, such as an icon disc. Null leaves the header empty.
        /// </summary>
        public VisualElement Header;

        /// <summary>
        /// Whether the scrim and back dismiss the dialog.
        /// </summary>
        public bool Dismissible = true;

        /// <summary>
        /// Set to show a text field. Null hides it.
        /// </summary>
        public string InputLabel;
        public string InputValue;
        public bool InputIsPassword;

        /// <summary>
        /// Set to show a choice field. Null hides it.
        /// </summary>
        public IReadOnlyList<string> Choices;
        public string ChoicesLabel;
        public int ChoiceIndex;
    }

    /// <summary>
    /// The outcome of a <see cref="ModalDialog"/>.
    /// </summary>
    public readonly struct DialogResult
    {
        public DialogAction Action { get; }
        public string InputValue { get; }
        public int ChoiceIndex { get; }

        public bool Confirmed => Action == DialogAction.Primary;

        public DialogResult(DialogAction action, string inputValue, int choiceIndex)
        {
            Action = action;
            InputValue = inputValue;
            ChoiceIndex = choiceIndex;
        }
    }

    /// <summary>
    /// A modal dialog over a scrim: title, body, an optional text or choice field, custom content,
    /// and one or two actions. Focus is trapped inside the card while it is open.
    /// </summary>
    /// <remarks>
    /// <para>The dialog fills its parent as an overlay layer and is hidden until opened.
    /// <see cref="Show"/> configures and opens it and returns a task that completes when it closes.
    /// Children added to the dialog go into the custom content slot between the body and the actions.</para>
    /// <para>Props: <c>title-text</c>, <c>body-text</c>, <c>primary-text</c>, <c>secondary-text</c>,
    /// <c>variant</c>, <c>dismissible</c>. Events: <see cref="Opened"/>, <see cref="Closed"/>.</para>
    /// <para>Back (<see cref="HandleBack"/>, Escape, gamepad cancel) is the cancel action: it dismisses a
    /// dismissible dialog and is swallowed by a non-dismissible one.</para>
    /// <para>USS classes: <c>modalDialog</c> (root), <c>modalDialog__scrim</c>, <c>modalDialog__card</c>,
    /// <c>modalDialog__header</c> (hidden while empty), <c>modalDialog__title</c>, <c>modalDialog__body</c>, <c>modalDialog__input</c>,
    /// <c>modalDialog__choices</c>, <c>modalDialog__content</c>, <c>modalDialog__actions</c>,
    /// <c>modalDialog__button</c>, <c>modalDialog__button--primary</c>, <c>modalDialog__button--secondary</c>.</para>
    /// <para>Modifier classes: <c>modalDialog--open</c> (shown and transitioning in),
    /// <c>modalDialog--destructive</c>, <c>modalDialog--hasInput</c>, <c>modalDialog--hasChoices</c>,
    /// <c>modalDialog--single</c> (no secondary action), <c>modalDialog--hasHeader</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-fg</c>, <c>--surface-muted</c>,
    /// <c>--surface-border</c>, <c>--surface-accent</c>, <c>--c-scrim</c>, <c>--c-primary</c>,
    /// <c>--c-danger</c>, <c>--c-on-primary</c>, <c>--radius-dialog</c>, <c>--radius-pill</c>,
    /// <c>--radius-sm</c>, <c>--space-2..6</c>, <c>--text-title</c>, <c>--text-body</c>,
    /// <c>--text-label</c>, <c>--font-display</c>, <c>--font-body</c>, <c>--motion-fast</c>, <c>--motion-base</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ModalDialog : VisualElement
    {
        public const string RootClass = "modalDialog";
        public const string ScrimClass = "modalDialog__scrim";
        public const string CardClass = "modalDialog__card";
        public const string HeaderClass = "modalDialog__header";
        public const string TitleClass = "modalDialog__title";
        public const string BodyClass = "modalDialog__body";
        public const string InputClass = "modalDialog__input";
        public const string ChoicesClass = "modalDialog__choices";
        public const string ContentClass = "modalDialog__content";
        public const string ActionsClass = "modalDialog__actions";
        public const string ButtonClass = "modalDialog__button";
        public const string PrimaryButtonClass = "modalDialog__button--primary";
        public const string SecondaryButtonClass = "modalDialog__button--secondary";
        public const string OpenClass = "modalDialog--open";
        public const string DestructiveClass = "modalDialog--destructive";
        public const string HasInputClass = "modalDialog--hasInput";
        public const string HasChoicesClass = "modalDialog--hasChoices";
        public const string SingleActionClass = "modalDialog--single";
        public const string HasHeaderClass = "modalDialog--hasHeader";

        private readonly VisualElement scrim;
        private readonly VisualElement card;
        private readonly VisualElement header;
        private readonly Label titleLabel;
        private readonly Label bodyLabel;
        private readonly TextField inputField;
        private readonly DropdownField choicesField;
        private readonly VisualElement content;
        private readonly Button primaryButton;
        private readonly Button secondaryButton;

        private readonly GradientFill primaryFill = new(GradientPreset.Brand, GradientDirection.Horizontal);
        private DialogVariant variant;
        private bool dismissible = true;
        private bool isOpen;
        private TaskCompletionSource<DialogResult> pending;
        private Focusable focusBeforeOpen;
        private IVisualElementScheduledItem hideItem;

        /// <summary>
        /// Raised when the dialog opens.
        /// </summary>
        public event Action Opened;

        /// <summary>
        /// Raised when the dialog closes, with how it was closed.
        /// </summary>
        public event Action<DialogResult> Closed;

        public bool IsOpen => isOpen;

        /// <summary>
        /// The card; exposed for layout measurement and tests.
        /// </summary>
        public VisualElement Card => card;

        public Button PrimaryButton => primaryButton;
        public Button SecondaryButton => secondaryButton;
        public TextField InputField => inputField;

        /// <summary>
        /// The slot above the title; prefer <see cref="SetHeader"/>, which keeps it hidden while empty.
        /// </summary>
        public VisualElement HeaderContent => header;
        public DropdownField ChoicesField => choicesField;

        public override VisualElement contentContainer => content;

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => titleLabel.text;
            set => SetText(titleLabel, value);
        }

        [UxmlAttribute("body-text")]
        public string BodyText
        {
            get => bodyLabel.text;
            set => SetText(bodyLabel, value);
        }

        [UxmlAttribute("primary-text")]
        public string PrimaryText
        {
            get => primaryButton.text;
            set => primaryButton.text = value ?? string.Empty;
        }

        /// <summary>
        /// Empty or null hides the secondary action.
        /// </summary>
        [UxmlAttribute("secondary-text")]
        public string SecondaryText
        {
            get => secondaryButton.text;
            set
            {
                secondaryButton.text = value ?? string.Empty;
                var single = string.IsNullOrEmpty(value);
                secondaryButton.style.display = single ? DisplayStyle.None : DisplayStyle.Flex;
                EnableInClassList(SingleActionClass, single);
            }
        }

        [UxmlAttribute("variant")]
        public DialogVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                EnableInClassList(DestructiveClass, variant == DialogVariant.Destructive);

                // The primary gradient on the primary action; a destructive action keeps the solid danger colour.
                if (variant == DialogVariant.Destructive)
                {
                    primaryButton.RemoveManipulator(primaryFill);
                }
                else if (primaryFill.target != primaryButton)
                {
                    primaryButton.AddManipulator(primaryFill);
                }
            }
        }

        [UxmlAttribute("dismissible")]
        public bool Dismissible
        {
            get => dismissible;
            set => dismissible = value;
        }

        public ModalDialog()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            style.display = DisplayStyle.None;

            scrim = UIToolkitExtensions.CreateVisualElement(ScrimClass);
            scrim.RegisterCallback<ClickEvent>(OnScrimClicked);
            hierarchy.Add(scrim);

            card = UIToolkitExtensions.CreateVisualElement(CardClass);
            card.focusable = true;
            card.tabIndex = -1;
            hierarchy.Add(card);

            header = UIToolkitExtensions.CreateVisualElement(card, HeaderClass);
            header.style.display = DisplayStyle.None;

            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(card, TitleClass);
            bodyLabel = UIToolkitExtensions.CreateVisualElement<Label>(card, BodyClass);

            inputField = UIToolkitExtensions.CreateVisualElement<TextField>(card, InputClass);
            inputField.style.display = DisplayStyle.None;

            choicesField = UIToolkitExtensions.CreateVisualElement<DropdownField>(card, ChoicesClass);
            choicesField.style.display = DisplayStyle.None;

            content = UIToolkitExtensions.CreateVisualElement(card, ContentClass);

            var actions = UIToolkitExtensions.CreateVisualElement(card, ActionsClass);
            secondaryButton = UIToolkitExtensions.CreateButton(actions, string.Empty, () => Close(DialogAction.Secondary), className: ButtonClass);
            secondaryButton.AddToClassList(SecondaryButtonClass);
            secondaryButton.MakePill();
            primaryButton = UIToolkitExtensions.CreateButton(actions, "OK", () => Close(DialogAction.Primary), className: ButtonClass);
            primaryButton.AddToClassList(PrimaryButtonClass);
            primaryButton.MakePill();

            card.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            RegisterCallback<DetachFromPanelEvent>(_ => CompletePending(DialogAction.None));

            TitleText = string.Empty;
            BodyText = string.Empty;
            SecondaryText = null;
            Variant = DialogVariant.Default;
        }

        /// <summary>
        /// Configures the dialog from <paramref name="options"/> and opens it. A dialog that is already
        /// open completes its previous task with <see cref="DialogAction.None"/> first.
        /// </summary>
        public Task<DialogResult> Show(DialogOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            CompletePending(DialogAction.None);

            TitleText = options.Title;
            BodyText = options.Body;
            PrimaryText = options.PrimaryText;
            SecondaryText = options.SecondaryText;
            Variant = options.Variant;
            Dismissible = options.Dismissible;
            SetHeader(options.Header);

            var hasInput = options.InputLabel != null;
            inputField.label = options.InputLabel ?? string.Empty;
            inputField.SetValueWithoutNotify(options.InputValue ?? string.Empty);
            inputField.isPasswordField = options.InputIsPassword;
            inputField.style.display = hasInput ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(HasInputClass, hasInput);

            var hasChoices = options.Choices != null && options.Choices.Count > 0;
            choicesField.label = options.ChoicesLabel ?? string.Empty;
            choicesField.choices = hasChoices ? options.Choices.ToList() : new List<string>();
            if (hasChoices)
            {
                choicesField.index = Mathf.Clamp(options.ChoiceIndex, 0, options.Choices.Count - 1);
            }
            choicesField.style.display = hasChoices ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(HasChoicesClass, hasChoices);

            pending = new TaskCompletionSource<DialogResult>();
            var task = pending.Task;
            Open();
            return task;
        }

        /// <summary>
        /// Puts an element above the title, replacing what was there; null empties and hides the slot.
        /// </summary>
        public void SetHeader(VisualElement element)
        {
            header.Clear();
            if (element != null)
            {
                header.Add(element);
            }

            header.style.display = element != null ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList(HasHeaderClass, element != null);
        }

        /// <summary>
        /// Opens the dialog with its current content.
        /// </summary>
        public void Open()
        {
            hideItem?.Pause();
            hideItem = null;

            if (!isOpen)
            {
                focusBeforeOpen = focusController?.focusedElement;
            }

            isOpen = true;
            style.display = DisplayStyle.Flex;
            BringToFront();

            // Add the open class a frame later so the transitions start from the closed values.
            schedule.Execute(() =>
            {
                if (!isOpen)
                {
                    return;
                }

                AddToClassList(OpenClass);
                FirstFocusable()?.Focus();
            });

            Opened?.Invoke();
        }

        /// <summary>
        /// Closes the dialog with an action, completing the task from <see cref="Show"/>.
        /// </summary>
        public void Close(DialogAction action = DialogAction.None)
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            RemoveFromClassList(OpenClass);

            var delayMs = Mathf.CeilToInt(LongestTransitionMs(card));
            if (delayMs <= 0)
            {
                style.display = DisplayStyle.None;
            }
            else
            {
                hideItem = schedule.Execute(() =>
                {
                    if (!isOpen)
                    {
                        style.display = DisplayStyle.None;
                    }
                }).StartingIn(delayMs);
            }

            if (focusBeforeOpen is VisualElement previous && previous.panel != null)
            {
                previous.Focus();
            }
            else
            {
                focusController?.focusedElement?.Blur();
            }

            focusBeforeOpen = null;

            var result = CompletePending(action);
            Closed?.Invoke(result);
        }

        /// <summary>
        /// The back-button rule: cancels a dismissible dialog, swallows back for a non-dismissible one.
        /// </summary>
        /// <returns>False when the dialog is not open, so the caller can handle back itself.</returns>
        public bool HandleBack()
        {
            if (!isOpen)
            {
                return false;
            }

            if (dismissible)
            {
                Close(DialogAction.None);
            }

            return true;
        }

        private DialogResult CompletePending(DialogAction action)
        {
            var result = new DialogResult(action, inputField.value, choicesField.index);
            var completing = pending;
            pending = null;
            completing?.TrySetResult(result);
            return result;
        }

        private static void SetText(Label label, string text)
        {
            label.text = text ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static float LongestTransitionMs(VisualElement element)
        {
            var longest = 0f;
            foreach (var duration in element.resolvedStyle.transitionDuration)
            {
                var ms = duration.unit == TimeUnit.Second ? duration.value * 1000f : duration.value;
                longest = Mathf.Max(longest, ms);
            }

            return longest;
        }

        private void OnScrimClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            if (isOpen && dismissible)
            {
                Close(DialogAction.None);
            }
        }

        private void OnNavigationCancel(NavigationCancelEvent evt)
        {
            if (HandleBack())
            {
                evt.StopPropagation();
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape && HandleBack())
            {
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Focusable elements in the card, outermost only (a TextField, not its inner input).
        /// </summary>
        private List<VisualElement> Focusables()
        {
            var result = new List<VisualElement>();
            foreach (var element in card.Query<VisualElement>().ToList())
            {
                if (!element.focusable || !element.canGrabFocus || element.tabIndex < 0 || !IsShown(element))
                {
                    continue;
                }

                if (result.Any(outer => outer.Contains(element)))
                {
                    continue;
                }

                result.Add(element);
            }

            return result;
        }

        private bool IsShown(VisualElement element)
        {
            for (var current = element; current != null && current != card; current = current.hierarchy.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Focus the text input on opening. Disable to leave the keyboard closed until the field is tapped.</summary>
        public bool FocusInputOnOpen { get; set; } = true;

        /// <summary>
        /// Keeps focus inside the dialog, without focusing an action before the user navigates.
        /// </summary>
        private VisualElement FirstFocusable()
        {
            if (FocusInputOnOpen && inputField.resolvedStyle.display != DisplayStyle.None)
            {
                return inputField;
            }

            return card;
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (evt.direction != NavigationMoveEvent.Direction.Next && evt.direction != NavigationMoveEvent.Direction.Previous)
            {
                return;
            }

            var focusables = Focusables();
            if (focusables.Count == 0)
            {
                return;
            }

            var current = focusController?.focusedElement as VisualElement;
            var index = focusables.FindIndex(e => e == current || (current != null && e.Contains(current)));
            var step = evt.direction == NavigationMoveEvent.Direction.Next ? 1 : -1;
            var next = index < 0 ? (step > 0 ? 0 : focusables.Count - 1) : (index + step + focusables.Count) % focusables.Count;

            focusables[next].Focus();
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }
    }
}
