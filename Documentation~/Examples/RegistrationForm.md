# Example: Registration Form

## Overview

This example demonstrates a complete mobile-style registration flow built entirely from package controls. It shows how styled input fields, a multi-option selector, a gradient submit button, and shake-based validation feedback can be composed into a cohesive form screen.

## Controls Featured

- [PillInputField](../Controls/PillInputField.md) — pill-shaped single-line input used for Name, Email, and Password fields
- [RoundedInputField](../Controls/RoundedInputField.md) — rounded multiline input used for the Bio field
- [PillButton](../Controls/PillButton.md) — gradient-styled pill button used as the Submit action
- [PillSelector](../Controls/PillSelector.md) — horizontally scrollable option selector used for the Category field; tapping cycles through available options
- [VisualElementShakeUtility](../Controls/VisualElementShakeUtility.md) — plays a horizontal shake animation on any `VisualElement`; triggered on invalid fields at submit time

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject named `RegistrationFormDemo` to the scene hierarchy.
3. Add a `UIDocument` component to the GameObject.
4. Create a `PanelSettings` asset (`Assets > Create > UI Toolkit > Panel Settings`) and configure it:
   - **Scale Mode:** Scale With Screen Size
   - **Reference Resolution:** 1080 × 1920
   - **Screen Match Mode:** Match Width Or Height, blended toward Height
5. Assign the `PanelSettings` asset to the `UIDocument` component's **Panel Settings** field.
6. Add the `RegistrationFormDemo` MonoBehaviour (found in `Examples~/RegistrationForm/`) to the same GameObject.
7. Press **Play**.

## What to Expect

The form presents the following fields from top to bottom:

| Field | Control | Notes |
| --- | --- | --- |
| Name | `PillInputField` | Single-line, required |
| Email | `PillInputField` | Single-line, validated against email regex |
| Password | `PillInputField` | Single-line, obscured |
| Bio | `RoundedInputField` | Multiline, optional |
| Category | `PillSelector` | Tap to cycle options (e.g., Creator, Developer, Designer …) |

Tapping **Submit** (`PillButton` with gradient background) triggers validation:

- Any required field that is empty or contains an invalid value plays the `VisualElementShakeUtility` shake animation and its label turns red.
- When all fields pass validation the form transitions to a success state: the fields are hidden and a confirmation message is displayed.

## Key Code Patterns

Applying a shake animation to an invalid field and marking it red:

```csharp
private void ValidateAndSubmit()
{
    bool isValid = true;

    if (string.IsNullOrWhiteSpace(_nameField.Value))
    {
        VisualElementShakeUtility.Shake(_nameField);
        isValid = false;
    }

    // Validate email format against the @ regex
    string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    if (!System.Text.RegularExpressions.Regex.IsMatch(_emailField.Value, emailPattern))
    {
        VisualElementShakeUtility.Shake(_emailField);
        isValid = false;
    }

    if (isValid)
    {
        ShowSuccessState();
    }
}
```
