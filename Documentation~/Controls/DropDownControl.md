# DropDownControl

## Summary

`DropDownControl` is a wheel-style dropdown picker that opens from an inline trigger and lets the user confirm a single value from a vertically scrollable modal list.

Typical use cases:

- Dial-code or country-code selection before a phone field
- Compact selectors that need a touch-friendly modal confirmation step
- Any workflow where a finite string list should feel like a native mobile picker rather than a standard menu

## Properties

| Property | Type | Description |
| --- | --- | --- |
| `Items` | `IReadOnlyList<string>` | Ordered values shown in the picker. Updating the list refreshes the trigger label and open list content. |
| `Value` | `string` | The currently selected value, or an empty string when `Items` is empty. |

## USS Classes

| Class | Description |
| --- | --- |
| `dropDownControl` | Root element for the control. |
| `dropDownControl__trigger` | Closed-state trigger element. |
| `dropDownControl__triggerLabel` | Label showing the current selection. |
| `dropDownControl__triggerIcon` | Chevron icon inside the trigger. |
| `dropDownControl__backdrop` | Full-screen backdrop injected while the picker is open. |
| `dropDownControl__panel` | Floating modal panel that contains the picker list. |
| `dropDownControl__viewport` | Clipped viewport used to display the visible rows. |
| `dropDownControl__list` | Scrolling row container translated by the control logic. |
| `dropDownControl__row` | A single rendered picker row. |
| `dropDownControl__selectionLane` | Highlight lane showing the centred selection. |
| `dropDownControl__fadeTop` | Top fade overlay used to de-emphasize off-centre rows. |
| `dropDownControl__fadeBottom` | Bottom fade overlay used to de-emphasize off-centre rows. |
| `dropDownControl--open` | Root modifier applied while the modal picker is visible. |

## Events

| Name | Description | Arguments |
| --- | --- | --- |
| `ValueChanged` | Fired after the user confirms a new selection. | `string selectedValue` |
| `OpenStateChanged` | Fired when the modal picker is opened or closed. | `bool isOpen` |

## Public Methods

| Signature | Description |
| --- | --- |
| `SetDefault(string value)` | Sets the selected item when the supplied value exists in `Items`. |

## Using the Control

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class DialCodePickerExample : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private void OnEnable()
    {
        var root = document.rootVisualElement;

        var picker = new DropDownControl
        {
            Items = new[] { "+1", "+33", "+44", "+49" },
        };

        picker.SetDefault("+44");
        picker.ValueChanged += selected => Debug.Log($"Selected {selected}");

        root.Add(picker);
    }
}
```