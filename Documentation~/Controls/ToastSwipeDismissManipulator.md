# ToastSwipeDismissManipulator

## Summary

`ToastSwipeDismissManipulator` is a `PointerManipulator` that adds swipe-to-dismiss gesture recognition to any `VisualElement`. It supports both horizontal and vertical dismiss axes. The gesture axis is locked after 12 px of movement along the dominant direction. Dismiss triggers when travel exceeds 80 px or velocity exceeds 1 100 px/s. Incomplete swipes snap back in 140 ms; dismissed elements animate out in 180 ms.

All geometry values (dismiss travel distances, offset target) are provided as delegates evaluated lazily at gesture time, so the manipulator does not need to be re-created when layout changes.

Typical use cases:

- Swipe-to-dismiss notification toasts
- Dismissible alert or info banners
- Swipeable items in a list or feed

## Behavior Constants

| Constant | Value | Description |
| --- | --- | --- |
| Tap slop | 8 px | Maximum movement before a tap becomes a drag. |
| Swipe axis lock threshold | 12 px | Movement required along the dominant axis before the axis is locked. |
| Swipe dismiss threshold | 80 px | Travel distance that triggers a dismiss. |
| Dismiss velocity threshold | 1 100 px/s | Pointer velocity that triggers a dismiss regardless of travel distance. |
| Snap-back duration | 140 ms | Animation duration for returning to the origin position. |
| Dismiss duration | 180 ms | Animation duration for the final dismiss slide-out. |

## USS Classes

This manipulator does not add USS classes. Visual feedback is applied by translating the `getOffsetTarget` element inline.

## Events / Callbacks

All behavior is communicated through constructor-provided delegates.

| Delegate parameter | Description |
| --- | --- |
| `Func<bool> canInteract` | Called on pointer-down. Return `false` to ignore the gesture entirely. |
| `Func<Vector2, bool> canStartAtPosition` | Called with the pointer position. Return `false` to suppress gesture start (e.g. when a button lives in part of the toast). |
| `Func<float> getHorizontalDismissTravelDistance` | Returns the horizontal distance in pixels required to trigger dismiss. |
| `Func<float> getVerticalDismissTravelDistance` | Returns the vertical distance in pixels required to trigger dismiss. |
| `Func<VisualElement> getOffsetTarget` | Returns the element that will be translated during the swipe. Typically the root toast element. |
| `Action onInteractionStarted` | Called when the gesture crosses tap slop and is confirmed as a drag. |
| `Action onInteractionAborted` | Called when a drag that was started does not meet dismiss criteria and the snap-back completes. |
| `Action onTapped` | Called when the pointer is released within tap slop (no significant movement). |
| `Action onDismissed` | Called after the dismiss animation completes. Dispose of the toast element here. |

## Public Methods

| Signature | Description |
| --- | --- |
| `ResetState()` | Cancels any in-progress gesture and immediately restores the offset target to its origin position. Call this if the toast is hidden or replaced externally while a swipe is in progress. |

## Using the Control

### Swipe-to-Dismiss Toast

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class ToastController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private VisualElement _toastRoot;
    private ToastSwipeDismissManipulator _dismissManipulator;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        ShowToast("Session saved successfully.");
    }

    private void ShowToast(string message)
    {
        _toastRoot = new VisualElement();
        _toastRoot.AddToClassList("toast");

        var label = new Label(message);
        _toastRoot.Add(label);

        _dismissManipulator = new ToastSwipeDismissManipulator(
            canInteract: () => true,
            canStartAtPosition: _ => true,
            getHorizontalDismissTravelDistance: () => 80f,
            getVerticalDismissTravelDistance: () => 80f,
            getOffsetTarget: () => _toastRoot,
            onInteractionStarted: () => Debug.Log("Swipe started"),
            onInteractionAborted: () => Debug.Log("Swipe aborted — snapping back"),
            onTapped: () => Debug.Log("Toast tapped"),
            onDismissed: RemoveToast
        );

        _toastRoot.AddManipulator(_dismissManipulator);
        _document.rootVisualElement.Q<VisualElement>("toastLayer").Add(_toastRoot);
    }

    private void RemoveToast()
    {
        _toastRoot?.RemoveFromHierarchy();
        _toastRoot = null;
    }

    // Call this if you need to forcibly clear the toast without waiting for a gesture
    public void ForceClose()
    {
        _dismissManipulator?.ResetState();
        RemoveToast();
    }
}
```
