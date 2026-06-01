# VisualElementShakeUtility

## Summary

VisualElementShakeUtility provides a reusable horizontal shake animation for any target VisualElement. It is intended for validation and attention feedback scenarios where movement needs to be short, configurable, and deterministic.

Typical use cases:

- Invalid form step feedback
- Restricted page-transition feedback
- Highlighting a specific field or section after failed validation

## Public API

| Name | Description | Options |
|---|---|---|
| Shake(VisualElement target, int wobbleCount = 3, int wobbleDurationMs = 70, float amplitudePixels = 10f, Func<float, float> easingCurve = null, Action onCompleted = null) | Starts a horizontal wobble sequence on the target element. If another shake is already active on the same target, the current animation is stopped and replaced with a new one from the base position. | target required, wobbleCount >= 1, wobbleDurationMs >= 1, amplitudePixels >= 0 |
| StopShake(VisualElement target, bool resetToBasePosition = true) | Stops the active shake on the target element. Optionally restores the original translate position captured when the shake started. | target required, resetToBasePosition true/false |

## Behavior Notes

- The effect animates translate X and restores the original translate state at completion.
- Repeated calls on the same target are deterministic: replace current shake, then restart.
- The default easing curve is Easing.OutCubic when no curve is provided.
- The utility unregisters its detach callback when stopping or completing.

## Usage

```csharp
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

// Example: 4 wobble cycles, 60ms per segment, 12px amplitude
VisualElementShakeUtility.Shake(
    target: myElement,
    wobbleCount: 4,
    wobbleDurationMs: 60,
    amplitudePixels: 12f);

// Optional: stop manually
VisualElementShakeUtility.StopShake(myElement);
```

## ScrollSnap Restricted Transition Example

Use this pattern when `ScrollSnap` rejects a page transition and you want immediate visual feedback on a specific element.

```csharp
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

// Example setup references
// scrollSnap: your ScrollSnap instance
// validationPanel: the VisualElement to shake on restriction

scrollSnap.OnPageChangeRestricted += _ =>
{
    VisualElementShakeUtility.Shake(
        target: validationPanel,
        wobbleCount: 3,
        wobbleDurationMs: 70,
        amplitudePixels: 10f);
};
```

Notes:

- Keep wobble duration short so feedback feels responsive.
- Shake the smallest relevant container (for example one section panel) to keep intent clear.
- Repeated restrictions are safe; the utility replaces any in-progress shake on the same target.
