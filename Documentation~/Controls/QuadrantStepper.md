# QuadrantStepper

## Summary

`QuadrantStepper` is a segmented control that divides its width into equally-sized tap targets. A sliding overlay animates between the selected segment. By default it is created with four equal segments; the option list can be replaced at any time via `SetOptions`. The overlay sits at 90% of the control's height (5% inset top and bottom) to create a floating appearance.

Typical use cases:

- Tab bar or mode switcher
- Category or filter selector
- Any fixed-option segmented navigation control

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `SelectedIndex` | Gets or sets the index of the currently selected segment. | `int` |
| `SelectedText` | Gets the label text of the currently selected segment. | `string` (read-only) |

### USS Custom Properties

| Name | Description | Default |
| --- | --- | --- |
| `--quadrantStepper-animation-duration-ms` | Duration of the sliding overlay animation in milliseconds. | Defined in package USS |

## USS Classes

| Class | Description |
| --- | --- |
| `quadrantStepper` | Root element. |
| `quadrantStepper__overlay` | The sliding highlight overlay. Absolutely positioned. Transitions between segments. |
| `quadrantStepper__segments` | Flex row container holding all segment elements. |
| `quadrantStepper__segment` | Individual segment tap target. Flex-grows equally to divide available width. |
| `quadrantStepper__label` | Text label inside each segment. |
| `is-selected` | Modifier applied to the currently selected segment. |

## Events

| Name | Description | Arguments |
| --- | --- | --- |
| `SelectionChanged` | Fired when the selected segment changes via tap or programmatic call (unless suppressed). | `int index, string text` |

## Constructors

| Signature | Description |
| --- | --- |
| `QuadrantStepper()` | Creates a stepper with four default options (`"1"`, `"2"`, `"3"`, `"4"`). |
| `QuadrantStepper(IReadOnlyList<string> options)` | Creates a stepper with the provided option labels. |

## Public Methods

| Signature | Description |
| --- | --- |
| `SetOptions(IReadOnlyList<string> options)` | Replaces all segment labels. Resets selection to index 0. |
| `SetOptions(IReadOnlyList<string> options, int defaultIndex)` | Replaces all segment labels and sets the initial selection to `defaultIndex`. |
| `bool SetOptions(IReadOnlyList<string> options, string defaultText)` | Replaces labels and attempts to select the segment matching `defaultText`. Returns `true` if the text was found and selected. |
| `SetSelectedIndex(int index)` | Selects the segment at `index` with animation and fires `SelectionChanged`. |
| `SetSelectedIndex(int index, bool notify, bool animate)` | Selects the segment at `index` with optional event notification and animation. |
| `bool TrySetSelectedText(string text, bool notify, bool animate)` | Selects the segment whose label matches `text`. Returns `false` if not found. |
| `ForceUnselect()` | Removes the selection without firing `SelectionChanged`. The overlay is hidden. |

## Using the Control

### Mode Switcher

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;
using System.Collections.Generic;

public class ModeController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private QuadrantStepper _modeStepper;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        var modes = new List<string> { "Daily", "Weekly", "Monthly", "All Time" };
        _modeStepper = new QuadrantStepper(modes);

        // Default to "Weekly" without firing the event
        _modeStepper.SetOptions(modes, defaultIndex: 1);

        _modeStepper.SelectionChanged += (index, text) =>
        {
            Debug.Log($"Mode changed to [{index}] {text}");
            LoadData(text);
        };

        root.Q<VisualElement>("filterContainer").Add(_modeStepper);
    }

    private void LoadData(string period)
    {
        Debug.Log($"Loading data for period: {period}");
    }

    public void ResetToDefault()
    {
        // Restore to "Daily" silently, no animation
        _modeStepper.SetSelectedIndex(0, notify: false, animate: false);
    }
}
```

### Restoring Saved State

```csharp
// Restore selection from saved preference
string savedMode = PlayerPrefs.GetString("selectedMode", "Weekly");
bool found = _modeStepper.TrySetSelectedText(savedMode, notify: false, animate: false);
if (!found)
    _modeStepper.SetSelectedIndex(0, notify: false, animate: false);
```
