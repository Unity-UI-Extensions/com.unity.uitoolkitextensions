# PillButton

## Summary

`PillButton` is a rounded, gradient-filled call-to-action button with a flash feedback animation on press. The gradient is generated from two colors (inner and outer) into a 256 × 1 `Texture2D` that is set as the background image. A white overlay fades in briefly on tap to provide tactile feedback. The gradient texture is destroyed when the element detaches from the panel.

Typical use cases:

- Primary call-to-action buttons (Continue, Submit, Get Started)
- Form submission buttons
- Prominent single-action navigation controls

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `Text` | Gets or sets the button label text. | `string` |

## USS Classes

| Class | Description |
| --- | --- |
| `pillButton` | Root element. |
| `pillButton__background` | Gradient background layer. `border-radius: 999px`, `overflow: hidden`. Holds the gradient texture. |
| `pillButton__flash` | White overlay element used for the press flash effect. |
| `pillButton__flash--active` | Modifier that triggers a 100 ms opacity transition on the flash overlay. |
| `pillButton__label` | Text label element centered over the button. |

## Events

| Name | Description | Arguments |
| --- | --- | --- |
| `Clicked` | Fired when the button is tapped or clicked. The flash animation plays before this event is dispatched. | none |

## Public Methods

| Signature | Description |
| --- | --- |
| `SetInnerColor(string hex)` | Sets the inner (left-edge) gradient color using a hex string (e.g. `"#e94560"`). Rebuilds the gradient texture. |
| `SetOuterColor(string hex)` | Sets the outer (right-edge) gradient color using a hex string. Rebuilds the gradient texture. |
| `SetTextColor(Color color)` | Sets the label text color. |
| `SetFontSize(float size)` | Sets the label font size in pixels. |

## Using the Control

### Primary CTA

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class OnboardingFooterController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private PillButton _continueButton;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _continueButton = new PillButton();
        _continueButton.Text = "Continue";
        _continueButton.SetInnerColor("#e94560");
        _continueButton.SetOuterColor("#9b1d35");
        _continueButton.SetTextColor(Color.white);
        _continueButton.SetFontSize(16f);

        _continueButton.Clicked += OnContinueTapped;

        root.Q<VisualElement>("footer").Add(_continueButton);
    }

    private void OnContinueTapped()
    {
        Debug.Log("Continue tapped");
    }
}
```

### Dynamic Color Update

```csharp
// Reflect form validity through button color
private void UpdateButtonState(bool isValid)
{
    if (isValid)
    {
        _continueButton.SetInnerColor("#18cc6e");
        _continueButton.SetOuterColor("#0d7a42");
        _continueButton.Text = "Submit";
    }
    else
    {
        _continueButton.SetInnerColor("#555555");
        _continueButton.SetOuterColor("#333333");
        _continueButton.Text = "Fill all fields";
    }
}
```
