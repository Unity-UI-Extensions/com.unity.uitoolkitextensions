# PillSelector

## Summary

`PillSelector` is a read-only pill-shaped field with a chevron icon that fires a `Clicked` event when tapped. It does not manage a picker internally; the consumer is responsible for displaying the selection UI and writing the chosen value back to the `Value` property.

Typical use cases:

- Inline date or time picker trigger row
- Option selector row that opens a modal or bottom sheet
- Any read-only labeled field that indicates a "tap to change" interaction

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `Label` | Gets or sets the descriptive label shown above the selector value. | `string` |
| `Value` | Gets or sets the currently displayed selected value text. | `string` |

## USS Classes

| Class | Description |
| --- | --- |
| `pillSelector` | Root element. |
| `pillSelector__label` | Label element shown above the value. |
| `pillSelector__container` | Pill-shaped row containing the value label and chevron. |
| `pillSelector__clickableLabel` | The text element inside the container that displays `Value`. |
| `pillSelector__icon` | Chevron icon element. Fixed at 16 × 16 px. |

## Events

| Name | Description | Arguments |
| --- | --- | --- |
| `Clicked` | Fired when the user taps the selector row. The consumer should open a picker and update `Value` with the result. | none |

## Public Methods

| Signature | Description |
| --- | --- |
| `SetFontSize(float size)` | Sets the font size for the value label. |

## Using the Control

### Date Picker Trigger

```csharp
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class EventFormController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private PillSelector _dateSelector;
    private DateTime _selectedDate = DateTime.Today;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _dateSelector = new PillSelector();
        _dateSelector.Label = "Event Date";
        _dateSelector.Value = _selectedDate.ToString("MMMM d, yyyy");

        _dateSelector.Clicked += OnDateSelectorTapped;

        root.Q<VisualElement>("eventForm").Add(_dateSelector);
    }

    private void OnDateSelectorTapped()
    {
        // Open your date picker UI here.
        // When the user confirms a date, call ApplyDate().
        Debug.Log("Open date picker");
        ApplyDate(DateTime.Today.AddDays(7)); // example result
    }

    private void ApplyDate(DateTime date)
    {
        _selectedDate = date;
        _dateSelector.Value = date.ToString("MMMM d, yyyy");
    }
}
```

### Country Selector Row

```csharp
_countrySelector = new PillSelector();
_countrySelector.Label = "Country";
_countrySelector.Value = "Select country";
_countrySelector.SetFontSize(15f);

_countrySelector.Clicked += () =>
{
    // Show modal country list
    // On confirmation: _countrySelector.Value = chosenCountry;
};
```
