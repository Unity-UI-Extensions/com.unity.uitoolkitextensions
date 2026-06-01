# Example: DropDown Phone Entry

## Overview

This example recreates the phone verification entry layout used in Yperea's onboarding flow, but with package-local controls only. It combines `DropDownControl` for dial-code selection and `PillInputField` for phone-number input inside a single shared shell.

## Controls Featured

- [DropDownControl](../Controls/DropDownControl.md) — wheel-style modal picker used for the dial code
- [PillInputField](../Controls/PillInputField.md) — phone-number text entry field
- [PillButton](../Controls/PillButton.md) — send-code action button

## Scene Setup

This package now ships a ready-made sample scene in `Examples~/DropDownPhoneEntry/DropDownPhoneEntryDemo.unity`.

If you want to recreate it manually instead:

1. Create a new Unity scene.
2. Add a GameObject with a `UIDocument`.
3. Assign the sample panel settings from `Examples~/Shared/UIToolkitExtensionsExamplePanelSettings.asset`.
4. Add the `DropDownPhoneEntryDemo` MonoBehaviour from `Examples~/DropDownPhoneEntry/` to the same GameObject.
5. Press Play.

## What to Expect

The screen presents a single verification card with:

- A dial-code trigger on the left that opens the `DropDownControl` picker.
- A `PillInputField` on the right for entering a phone number.
- A send button that stays disabled until the input contains at least 10 digits.
- A status label that reflects picker state, validation state, and the final sample send action.

## Key Code Pattern

```csharp
dialCodePicker = new DropDownControl
{
    Items = new[] { "+1", "+33", "+44", "+49" },
};

phoneNumberInput.OnValueChanged += value =>
{
    bool isValid = ExtractDigits(value).Length >= 10;
    sendCodeButton.SetEnabled(isValid);
};
```