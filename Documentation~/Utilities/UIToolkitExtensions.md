# UIToolkitExtensions

## Summary

`UIToolkitExtensions` is a static helper class in the `UnityUIToolkit.Extensions` namespace that provides factory methods and extension methods for building UI Toolkit hierarchies in code. It reduces boilerplate when constructing and configuring `VisualElement` trees at runtime.

Common use cases:

- Programmatically building form layouts without UXML
- Creating typed sub-elements with CSS classes in a single call
- Generating hyperlink labels, placeholder text fields, and wired buttons
- Validating email address inputs inline with automatic color feedback

---

## API Reference

### CreateVisualElement

| Signature | Description |
| --- | --- |
| `static VisualElement CreateVisualElement(params string[] classNames)` | Creates an unparented `VisualElement` and applies the given USS class names. |
| `static VisualElement CreateVisualElement(VisualElement parent, params string[] classNames)` | Creates a `VisualElement`, adds it to `parent`, and applies the given USS class names. |
| `static T CreateVisualElement<T>(params string[] classNames) where T : VisualElement, new()` | Typed variant — creates an unparented element of type `T` with the given USS class names. |
| `static T CreateVisualElement<T>(VisualElement parent, params string[] classNames) where T : VisualElement, new()` | Typed variant — creates an element of type `T`, adds it to `parent`, and applies the given USS class names. |

All four overloads call `AddToClassList` for every class name supplied. Passing no class names is valid and creates an element with no additional classes.

---

### CreateHyperlinkLabel

```csharp
static Label CreateHyperlinkLabel(VisualElement parent, string text)
```

Creates a `Label` styled as a hyperlink. The label:

- Receives the USS class `link-text` for stylesheet-driven styling.
- Wraps `text` in `<u>…</u>` rich-text tags.
- Has `enableRichText` set to `true`.
- Is added to `parent` before being returned.

---

### CreateTextInputField

```csharp
static TextField CreateTextInputField(VisualElement parent, string placeholder)
```

Creates a `TextField` with the USS class `text-field` that:

- Displays `placeholder` as its initial value.
- Sets `selectAllOnFocus = true`.
- Registers a `FocusInEvent` callback that calls `SelectAll()` so the placeholder is instantly replaced when the user taps or clicks.
- Is added to `parent` before being returned.

> **Note:** The placeholder pattern uses the field's `value` property. Clear the value in your own code before reading user input if you need to distinguish an untouched field from a filled one.

---

### ValidateTextFieldEmailAddress

```csharp
static bool ValidateTextFieldEmailAddress(this TextField inputText)
```

Extension method. Validates `inputText.value` against the pattern `^[^@\s]+@[^@\s]+\.[^@\s]+$` and:

- Sets `inputText.style.color` to `Color.black` when the address is valid.
- Sets `inputText.style.color` to `Color.red` when the address is invalid.
- Returns `true` if valid, `false` otherwise.

---

### CreateButton

| Signature | Description |
| --- | --- |
| `static Button CreateButton(VisualElement parent, string text, UnityEvent onClickAction, bool isInitiallyVisible = true, string className = null)` | Creates a `Button` wired to a `UnityEvent`. |
| `static Button CreateButton(VisualElement parent, string text, Action onClickAction, bool isInitiallyVisible = true, string className = null)` | Creates a `Button` wired to a plain `Action`. |

Both overloads:

- Add the button to `parent`.
- Set `button.text` to `text`.
- Wire `button.clicked` to invoke the supplied delegate (null-safe).
- Apply `className` via `AddToClassList` when `className` is not null or empty.
- Set `display` to `DisplayStyle.Flex` or `DisplayStyle.None` based on `isInitiallyVisible`.

---

## Usage Example

The snippet below builds a simple "Sign Up" form section entirely in code.

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class SignUpForm : MonoBehaviour
{
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Container card
        var card = UIToolkitExtensions.CreateVisualElement(root, "form-card");

        // Email row
        var emailRow = UIToolkitExtensions.CreateVisualElement(card, "form-row");
        UIToolkitExtensions.CreateHyperlinkLabel(emailRow, "Email address");
        var emailField = UIToolkitExtensions.CreateTextInputField(emailRow, "you@example.com");

        // Password row
        var passwordRow = UIToolkitExtensions.CreateVisualElement(card, "form-row");
        UIToolkitExtensions.CreateHyperlinkLabel(passwordRow, "Password");
        UIToolkitExtensions.CreateTextInputField(passwordRow, "••••••••");

        // Submit button — initially hidden until both fields are filled
        var submitButton = UIToolkitExtensions.CreateButton(
            parent: card,
            text: "Create Account",
            onClickAction: OnSubmit,
            isInitiallyVisible: false,
            className: "btn-primary"
        );

        // Show button only when email looks valid
        emailField.RegisterValueChangedCallback(_ =>
        {
            submitButton.style.display = emailField.ValidateTextFieldEmailAddress()
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        });
    }

    private void OnSubmit()
    {
        Debug.Log("Form submitted.");
    }
}
```

---

## Notes

- All methods are `static`; no instance is required.
- The typed `CreateVisualElement<T>` overloads require `T` to have a public parameterless constructor (`new()` constraint). Standard UI Toolkit controls (e.g., `Label`, `Button`, `ScrollView`) all satisfy this.
- `ValidateTextFieldEmailAddress` writes directly to the element's inline style. If your USS stylesheet also sets `color`, the inline style takes precedence. Clear the inline style (`inputText.style.color = StyleKeyword.Null`) to revert to USS-driven color.
