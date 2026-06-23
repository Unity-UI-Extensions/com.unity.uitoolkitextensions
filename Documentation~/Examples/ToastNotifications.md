# Example: Toast Notifications

## Overview

This example demonstrates runtime-spawned dismissible notification toasts managed by `ToastSwipeDismissManipulator`. Each toast can be dismissed by swiping horizontally or by tapping it directly. An "Add Toast" button spawns a new toast at the top of the stack; when the stack reaches five toasts the oldest one is automatically removed before the new one is added. Dismissal is accompanied by a combined opacity and translate animation.

## Controls Featured

- [ToastSwipeDismissManipulator](../Controls/ToastSwipeDismissManipulator.md) — `IManipulator` attached to each toast element; handles swipe detection and fires the dismiss callback which drives the exit animation and removes the element from the hierarchy

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject named `ToastNotificationsDemo` to the scene hierarchy.
3. Add a `UIDocument` component to the GameObject.
4. Create a `PanelSettings` asset (`Assets > Create > UI Toolkit > Panel Settings`) and configure it:
   - **Scale Mode:** Scale With Screen Size
   - **Reference Resolution:** 1080 × 1920
   - **Screen Match Mode:** Match Width Or Height, blended toward Height
5. Assign the `PanelSettings` asset to the `UIDocument` component's **Panel Settings** field.
6. Add the `ToastNotificationsDemo` MonoBehaviour (found in `Examples~/ToastNotifications/`) to the same GameObject.
7. Press **Play**.

## What to Expect

On entering Play mode, one toast is shown by default.

**Adding toasts** — tap the **Add Toast** button at the bottom of the screen. Each new toast appears at the top of the stack with a randomly assigned pastel background color and an incrementing message (e.g., "Notification #3"). When a sixth toast would be added, the oldest visible toast is silently removed first so the stack never exceeds five items.

**Dismissing toasts** — swipe a toast left or right past the dismiss threshold. It will animate out: opacity fades to zero and it translates in the swipe direction simultaneously. Tap the toast without swiping to dismiss it in place (translates upward and fades out). Once the animation completes the element is removed from the hierarchy.

**Stack layout** — toasts are stacked vertically with a small gap; newer toasts appear above older ones.

## Key Code Patterns

Creating a toast element and attaching the swipe-dismiss manipulator:

```csharp
private void SpawnToast(string message)
{
    if (_toastStack.childCount >= MaxToasts)
    {
        var oldest = _toastStack.Children().Last();
        _toastStack.Remove(oldest);
    }

    var toast = new VisualElement();
    toast.AddToClassList("toast");
    toast.style.backgroundColor = GetNextPastelColor();

    var label = new Label(message);
    toast.Add(label);

    // All behavior is wired via constructor delegates — there are no post-construction callbacks
    var manipulator = new ToastSwipeDismissManipulator(
        canInteract: () => true,
        canStartAtPosition: _ => true,
        getHorizontalDismissTravelDistance: () => 80f,
        getVerticalDismissTravelDistance: () => float.MaxValue,
        getOffsetTarget: () => toast,
        onInteractionStarted: () => { },
        onInteractionAborted: () => { },
        onTapped: () => AnimateAndRemove(toast),
        onDismissed: () => AnimateAndRemove(toast)
    );
    toast.AddManipulator(manipulator);

    _toastStack.Insert(0, toast);
}

private void AnimateAndRemove(VisualElement toast)
{
    toast.experimental.animation
        .Start(new StyleValues { opacity = 0f, translateX = 300f }, DismissDurationMs)
        .OnCompleted(() => toast.RemoveFromHierarchy());
}
```
