# StepProgressBar

## Summary

`StepProgressBar` is a horizontal progress bar that expresses progress as a fraction of discrete steps. The fill width is `NormalizedProgress × 100%`. The gradient is generated from two colors (inner and outer) into a 256 × 1 `Texture2D`. A warning is logged and `maxSteps` is clamped to `1` when a non-positive value is passed to `SetProgress`.

Typical use cases:

- Onboarding flow completion indicator
- Multi-step form or wizard progress
- Task or challenge completion tracker

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `NormalizedProgress` | Gets the current fill fraction in `[0, 1]`. Computed from `CurrentSteps / MaxSteps`. | `float` (read-only) |
| `CurrentSteps` | Gets the current step count. | `int` (read-only) |
| `MaxSteps` | Gets the total number of steps. | `int` (read-only) |

## USS Classes

| Class | Description |
| --- | --- |
| `stepProgressBar` | Root element. |
| `stepProgressBar__background` | Background track. `border-radius: 4px`, `overflow: hidden`. |
| `stepProgressBar__fill` | Fill element. Width is driven inline as a percentage. |

## Events

This control does not emit events.

## Public Methods

| Signature | Description |
| --- | --- |
| `SetProgress(int steps, int maxSteps)` | Sets the current and maximum steps. Recomputes `NormalizedProgress` and updates the fill width. Logs a warning if `maxSteps <= 0` and clamps it to `1`. |
| `SetInnerColor(string hex)` | Sets the inner (left-edge) gradient color. Rebuilds the gradient texture. |
| `SetOuterColor(string hex)` | Sets the outer (right-edge) gradient color. Rebuilds the gradient texture. |
| `SetGradientColors(string innerColorHex, string outerColorHex)` | Convenience method that sets both gradient colors in one call. |

## Using the Control

### Onboarding Progress

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class OnboardingProgressController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private StepProgressBar _progressBar;
    private int _currentStep = 0;
    private const int TotalSteps = 5;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _progressBar = new StepProgressBar();
        _progressBar.SetGradientColors(innerColorHex: "#e94560", outerColorHex: "#9b1d35");
        _progressBar.SetProgress(_currentStep, TotalSteps);

        root.Q<VisualElement>("progressContainer").Add(_progressBar);
    }

    public void AdvanceStep()
    {
        _currentStep = Mathf.Min(_currentStep + 1, TotalSteps);
        _progressBar.SetProgress(_currentStep, TotalSteps);
        Debug.Log($"Progress: {_progressBar.NormalizedProgress:P0}");
    }
}
```

### Dynamic Color Feedback

```csharp
// Green when complete, default brand color otherwise
private void RefreshBarColor()
{
    if (_progressBar.CurrentSteps >= _progressBar.MaxSteps)
        _progressBar.SetGradientColors("#18cc6e", "#0d7a42");
    else
        _progressBar.SetGradientColors("#e94560", "#9b1d35");
}
```
